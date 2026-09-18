using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Archskipelagill.Itemizers;

public class SkillTreeItemizer {
    public SkillTreeItemizer() {
        On.skigillNode.activate += SkigillNode_activate;
        On.skigillNode.OnTriggerStay2D += SkigillNode_OnTriggerStay2D;
        On.CharaStats.Start += CharaStats_Start;
    }

    private void CharaStats_Start(On.CharaStats.orig_Start orig, CharaStats self) {
        orig(self);

        var gridObj = GameObject.Find("gridHolder/grid")?.transform;
        if(gridObj == null) return;

        var avnUnsorted = GameObject.FindObjectsByType<skigillNode>(FindObjectsSortMode.InstanceID).Where(n => n.isActiveAndEnabled && !n.metaProg && n.transform.IsChildOf(gridObj)).ToList();
        for(var i = 0; i < avnUnsorted.Count; i++) {
            if(!avnUnsorted[i].gameObject.TryGetComponent<SkillTreeIndexTracker>(out var tkr))
                tkr = avnUnsorted[i].gameObject.AddComponent<SkillTreeIndexTracker>();
            tkr.unsortedIndex = i;
        }
        var allValidNodes = avnUnsorted.OrderBy(n => n.transform.position.y).ThenBy(n => n.transform.position.x).ToList();
        for(var i = 0; i < allValidNodes.Count; i++) {
            var tkr = allValidNodes[i].gameObject.GetComponent<SkillTreeIndexTracker>(); //guaranteed to exist from previous loop
            tkr.sortedIndex = i;
            tkr.node = SkillTree.skillTree[i];

            if(ArchiSaver.instance.receivedItemCounts.TryGetValue($"Skigill Region: {Enum.GetName(typeof(SkillTree.SkillNodeRegion), tkr.node.region).ToTitleCase()}", out var n) && n > 0)
                tkr.Unlock();
            else
                tkr.Lock();

            if(!tkr.isUnlocked) {
                tkr.GetComponent<UnityEngine.SpriteRenderer>().color = new(0.35f, 0f, 0f, 1f);
                tkr.transform.Find("canvas/Activate").GetComponent<UnityEngine.UI.Image>().color = new(0.35f, 0f, 0f, 1f);
                tkr.transform.Find("IconColor").GetComponent<UnityEngine.SpriteRenderer>().color = new(0.6f, 0.6f, 0.6f, 0.25f);
                tkr.transform.Find("nodeOcto").GetComponent<UnityEngine.SpriteRenderer>().color = new(0.35f, 0f, 0f, 1f);
                for(var j = 0; j < tkr.transform.childCount; j++) {
                    var ch = tkr.transform.GetChild(j);
                    if(ch.name != "connexion(Clone)") continue; 
                    ch.Find("GameObject").localScale = new(0.15f, 0.15f, 1f);
                }
            }
        }
        EnsureSafeSpawn(self);
    }

    void EnsureSafeSpawn(CharaStats self) {
        var targetChar = "Skigill Region: " + Enum.GetName(typeof(SkillTree.SkillNodeSpawnId), self.chara).ToTitleCase();
        if(!ArchiSaver.instance.receivedItemCounts.TryGetValue(targetChar, out var tcc) || tcc == 0) { //spawn region is locked, teleport to and activate mage region which for now is guaranteed unlocked
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
    }

    public void RescanRegions() {
        foreach(var tkr in GameObject.FindObjectsByType<SkillTreeIndexTracker>(FindObjectsSortMode.InstanceID)) {
            if(!tkr.isActiveAndEnabled) continue;

            if(ArchiSaver.instance.receivedItemCounts.TryGetValue($"Skigill Region: {Enum.GetName(typeof(SkillTree.SkillNodeRegion), tkr.node.region).ToTitleCase()}", out var n) && n > 0)
                tkr.Unlock();
            else
                tkr.Lock();

        }
    }
}

public class SkillTreeIndexTracker:MonoBehaviour {
    public int unsortedIndex;
    public int sortedIndex;
    public SkillTree.SkillNode node;
    public bool isUnlocked { get; private set; } = false;

    public void Unlock() {
        isUnlocked = true;
        GetComponent<SpriteRenderer>().color = new(1f, 1f, 1f, 1f);
        transform.Find("canvas/Activate").GetComponent<UnityEngine.UI.Image>().color = new(1f, 1f, 1f, 1f);
        transform.Find("IconColor").GetComponent<SpriteRenderer>().color = new(1f, 1f, 1f, 1f);
        transform.Find("nodeOcto").GetComponent<SpriteRenderer>().color = new(1f, 1f, 1f, 1f);
        for(var j = 0; j < transform.childCount; j++) {
            var ch = transform.GetChild(j);
            if(ch.name != "connexion(Clone)") continue;
            ch.Find("GameObject").localScale = new(0.5f, 0.5f, 1f);
        }
    }

    public void Lock() {
        isUnlocked = false;
        GetComponent<SpriteRenderer>().color = new(0.35f, 0f, 0f, 1f);
        transform.Find("canvas/Activate").GetComponent<UnityEngine.UI.Image>().color = new(0.35f, 0f, 0f, 1f);
        transform.Find("IconColor").GetComponent<SpriteRenderer>().color = new(0.6f, 0.6f, 0.6f, 0.25f);
        transform.Find("nodeOcto").GetComponent<SpriteRenderer>().color = new(0.35f, 0f, 0f, 1f);
        for(var j = 0; j < transform.childCount; j++) {
            var ch = transform.GetChild(j);
            if(ch.name != "connexion(Clone)") continue;
            ch.Find("GameObject").localScale = new(0.15f, 0.15f, 1f);
        }
    }
}