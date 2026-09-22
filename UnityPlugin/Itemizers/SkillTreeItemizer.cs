using BepInEx.Configuration;
using System;
using System.Linq;
using UnityEngine;

namespace Archskipelagill.Itemizers;

public class SkillTreeItemizer {
    public ConfigEntry<bool> cfgSkillTreeLocationTracker;

    public SkillTreeItemizer() {
        On.skigillNode.activate += SkigillNode_activate;
        On.skigillNode.OnTriggerStay2D += SkigillNode_OnTriggerStay2D;
        On.CharaStats.Start += CharaStats_Start;
        On.skigillNode.showConnex += SkigillNode_showConnex;

        cfgSkillTreeLocationTracker = Plugin.instance.config.Bind<bool>(new ConfigDefinition("Special Effects", "Skigill Location Tracker"), true, new ConfigDescription("If true, unchecked locations on the Skigill will be marked."));
    }

    private void CharaStats_Start(On.CharaStats.orig_Start orig, CharaStats self) {
        orig(self);

        if(self.metaMenu) return;

        ApplyTrackers();
        RescanAll();
        EnsureSafeSpawn(self);
    }

    void ApplyTrackers() {
        var gridObj = GameObject.Find("gridHolder/grid")?.transform;
        if(gridObj == null) return;
        var allValidNodes = GameObject.FindObjectsByType<skigillNode>(FindObjectsSortMode.InstanceID).Where(n => n.isActiveAndEnabled && !n.metaProg && n.transform.IsChildOf(gridObj)).OrderBy(n => n.transform.position.y).ThenBy(n => n.transform.position.x).ToList();
        for(var i = 0; i < allValidNodes.Count; i++) {
            if(!allValidNodes[i].gameObject.TryGetComponent<SkillTreeIndexTracker>(out var tkr))
                tkr = allValidNodes[i].gameObject.AddComponent<SkillTreeIndexTracker>();
            tkr.node = SkillTree.skillTree[i];
        }
    }

    void EnsureSafeSpawn(CharaStats self) {
        var targetChar = "Skigill Region: " + Enum.GetName(typeof(AbilityUnlockItemizer.CharacterInIngameOrder), self.chara).ToTitleCase();
        if(ArchiSaver.GetItemCount(targetChar) == 0) { //spawn region is locked, teleport to and activate mage region which for now is guaranteed unlocked
            var mgo = GameObject.Find("gridHolder/grid/Perks/Mage");
            mgo.GetComponent<skigillNode>().autoActivate();
            self.transform.position = new(mgo.transform.position.x, mgo.transform.position.y, 0);
        }
    }

    private void SkigillNode_OnTriggerStay2D(On.skigillNode.orig_OnTriggerStay2D orig, skigillNode self, UnityEngine.Collider2D collision) {
        if(!self.TryGetComponent<SkillTreeIndexTracker>(out var tkr) || tkr.isUnlocked)
            orig(self, collision);
    }

    private void SkigillNode_activate(On.skigillNode.orig_activate orig, skigillNode self) {
        orig(self);
        var tkr = self.GetComponent<SkillTreeIndexTracker>();
        if(!tkr) return;
        var isChest = tkr.node.type == SkillTree.SkillNodeType.CHEST;
        var isPerk = tkr.node.type == SkillTree.SkillNodeType.PERK;
        if(!isChest && !isPerk) return;
        Plugin.ArchipelagoClient.CheckLocationsByName($"Skigill {(isChest ? "Chest" : "Perk")} #{(isChest ? tkr.node.chestIndex : tkr.node.perkIndex) + 1} ({Enum.GetName(typeof(SkillTree.SkillNodeRegion), tkr.node.region)})");
        tkr.Rescan();
    }

    private void SkigillNode_showConnex(On.skigillNode.orig_showConnex orig, skigillNode self) {
        orig(self);
        if(self.TryGetComponent<SkillTreeIndexTracker>(out var tkr)) {
            for(var j = 0; j < self.transform.childCount; j++) {
                var ch = self.transform.GetChild(j);
                if(ch.name != "connexion(Clone)") continue;
                ch.Find("GameObject").localScale = tkr.isUnlocked ? new(0.5f, 0.5f, 1f) : new(0.2f, 0.1f, 1f);
            }
        }
    }

    public void RescanAll() {
        foreach(var tkr in GameObject.FindObjectsByType<SkillTreeIndexTracker>(FindObjectsSortMode.InstanceID)) {
            tkr.Rescan();
        }
    }
}

public class SkillTreeIndexTracker:MonoBehaviour {
    public SkillTree.SkillNode node;
    public bool isUnlocked { get; private set; } = false;
    bool hasCheck = false;
    Transform[] spinners;
    UnityEngine.UI.Image activateVfx;
    SpriteRenderer iconColor, nodeOcto;
    Color origIconColor;


#pragma warning disable IDE0051 //Used by Unity Engine
    void Awake() {
        if(Plugin.instance.skillTreeItemizer.cfgSkillTreeLocationTracker.Value) {
            spinners = new Transform[6];
            for(var i = 0; i < 6; i++) {
                var spinner = new GameObject("Spinner");
                spinner.transform.parent = this.transform;
                var spr = spinner.AddComponent<SpriteRenderer>();
                spr.sprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/archi-big-single.png");
                spr.drawMode = SpriteDrawMode.Sliced;
                spr.size *= 0.5f;
                spinners[i] = spinner.transform;
                spinners[i].gameObject.SetActive(hasCheck);
            }
        }
        activateVfx = transform.Find("canvas/Activate").GetComponent<UnityEngine.UI.Image>();
        iconColor = transform.Find("IconColor").GetComponent<SpriteRenderer>();
        nodeOcto = transform.Find("nodeOcto").GetComponent<SpriteRenderer>();
        origIconColor = iconColor.color;
    }

    void Update() {
        if(hasCheck) {
            if(Plugin.instance.skillTreeItemizer.cfgSkillTreeLocationTracker.Value) {
                var phase = Time.time * 0.5f * Mathf.PI;
                for(var i = 0; i < spinners.Length; i++) {
                    var iphase = i / 3f * Mathf.PI;
                    spinners[i].transform.localPosition = new(Mathf.Cos(phase + iphase) * 1.25f, Mathf.Sin(phase + iphase) * 1.25f, -2f);
                }
                if(isUnlocked)
                    iconColor.color = ((Time.unscaledTime % 1f) > 0.5f) ? origIconColor : new(0.25f, 1f, 0.25f);
                else
                    iconColor.color = ((Time.unscaledTime % 1f) > 0.5f) ? new(0.6f, 0.6f, 0.6f, 0.25f) : new(0.8f, 0.15f, 0.15f, 0.25f);
            } else {
                if(isUnlocked)
                    iconColor.color = origIconColor;
                else
                    iconColor.color = new(0.6f, 0.6f, 0.6f, 0.25f);
            }
        }
    }
#pragma warning restore IDE0051

    public void Rescan() {
        if(!isActiveAndEnabled) return;

        var hasRegion = ArchiSaver.GetItemCount($"Skigill Region: {Enum.GetName(typeof(SkillTree.SkillNodeRegion), node.region).ToTitleCase()}") > 0;
        var hasFBK = ArchiSaver.GetItemCount($"Final Boss Key") > 0;

        //lock boss region behind all others if option enabled
        var bossLast = Int64.Parse(ArchiSaver.instance.metaProg["archi_boss_last"]);
        if(bossLast > 0 && node.region == SkillTree.SkillNodeRegion.BOSSES) {
            foreach(var n in Enum.GetNames(typeof(SkillTree.SkillNodeRegion))) {
                if(ArchiSaver.GetItemCount($"Skigill Region: {n.ToTitleCase()}") == 0) {
                    hasRegion = false;
                    break;
                }
            }
        }

        //lock unreachable regions
        if(node.region == SkillTree.SkillNodeRegion.STRONGMAN
            && ArchiSaver.GetItemCount("Skigill Region: Prototype") == 0
            && ArchiSaver.GetItemCount("Character: Strongman") == 0)
            hasRegion = false;
        if(node.region == SkillTree.SkillNodeRegion.FOX
            && ArchiSaver.GetItemCount("Skigill Region: Dragon") == 0
            && ArchiSaver.GetItemCount("Character: Fox") == 0)
            hasRegion = false;
        if(node.region == SkillTree.SkillNodeRegion.DWARVES
            && ((ArchiSaver.GetItemCount("Skigill Region: Prototype") == 0 && ArchiSaver.GetItemCount("Character: Strongman") == 0)
                || ArchiSaver.GetItemCount("Skigill Region: Strongman") == 0)
            && ((ArchiSaver.GetItemCount("Skigill Region: Dragon") == 0 && ArchiSaver.GetItemCount("Character: Fox") == 0)
                || ArchiSaver.GetItemCount("Skigill Region: Fox") == 0)
            )
            hasRegion = false;

        hasCheck = false;
        var isChest = node.type == SkillTree.SkillNodeType.CHEST;
        var isPerk = node.type == SkillTree.SkillNodeType.PERK;
        if(isChest || isPerk) {
            var checkStr = $"Skigill {(isChest ? "Chest" : "Perk")} #{(isChest ? node.chestIndex : node.perkIndex) + 1} ({Enum.GetName(typeof(SkillTree.SkillNodeRegion), node.region)})";
            hasCheck = !ArchiSaver.instance.sentChecks.Contains(checkStr) && !ArchiSaver.instance.unsentChecks.Contains(checkStr) && ArchiSaver.instance.allValidChecks.Contains(checkStr);
        }

        if(hasRegion && (node.type != SkillTree.SkillNodeType.BOSS_FINAL || hasFBK))
            Unlock();
        else
            Lock();

        foreach(var s in spinners) {
            s.gameObject.SetActive(hasCheck);
            s.GetComponent<SpriteRenderer>().color = isUnlocked ? new(1f, 1f, 1f) : new(0.2f, 0.2f, 0.2f);
        }
    }

    public void Unlock() {
        isUnlocked = true;
        GetComponent<SpriteRenderer>().color = new(1f, 1f, 1f, 1f);
        activateVfx.color = new(1f, 1f, 1f, 1f);
        iconColor.color = origIconColor;
        nodeOcto.color = new(1f, 1f, 1f, 1f);
        foreach(var sr in transform.Find("chiffres").GetComponentsInChildren<SpriteRenderer>()) {
            sr.color = new(1f, 1f, 1f, 1f);
        }
    }

    public void Lock() {
        isUnlocked = false;
        GetComponent<SpriteRenderer>().color = new(0.35f, 0.35f, 0.35f, 1f);
        activateVfx.color = new(0.25f, 0.25f, 0.25f, 1f);
        iconColor.color = new(0.6f, 0.6f, 0.6f, 0.25f);
        nodeOcto.color = new(0.35f, 0.35f, 0.35f, 1f);
        foreach(var sr in transform.Find("chiffres").GetComponentsInChildren<SpriteRenderer>()) {
            sr.color = new(0.35f, 0.35f, 0.35f, 1f);
        }
    }
}