using Archskipelagill.ArchipelagoCompat;
using Archskipelagill.GameDataAccess;
using Archskipelagill.ItemsAndLocations.Components;
using BepInEx.Configuration;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Archskipelagill.ItemsAndLocations;

public class SkillTreeItemizer : Module<SkillTreeItemizer> {

    ////// Initializer/Fields/Properties //////

    public bool SkillTreeLocationTracker => _cfgSkillTreeLocationTracker.Value;
    private readonly ConfigEntry<bool> _cfgSkillTreeLocationTracker;

    public SkillTreeItemizer() {
        On.skigillNode.activate += On_SkigillNode_activate;
        On.skigillNode.OnTriggerStay2D += On_SkigillNode_OnTriggerStay2D;
        On.CharaStats.Start += On_CharaStats_Start;
        On.skigillNode.showConnex += On_SkigillNode_showConnex;
        On.nodeContentDisplayerUI.OnPointerEnter += On_NodeContentDisplayerUI_OnPointerEnter;

        _cfgSkillTreeLocationTracker = Plugin.Instance.MainConfig.Bind<bool>(new ConfigDefinition("Special Effects", "Skigill Location Tracker"), true, new ConfigDescription("If true, unchecked locations on the Skigill will be marked."));
    }


    ////// MonoMod Hooks //////
    #region MonoMod Hooks
    private void On_CharaStats_Start(On.CharaStats.orig_Start orig, CharaStats self) {
        orig(self);

        if(self.metaMenu || ResourceGrabber.Instance.GridSceneDuringLoading) return;

        ApplyTrackers();
        RescanAll();
        EnsureSafeSpawn(self);
    }

    private void On_SkigillNode_OnTriggerStay2D(On.skigillNode.orig_OnTriggerStay2D orig, skigillNode self, UnityEngine.Collider2D collision) {
        if(!self.TryGetComponent<SkillTreeIndexTracker>(out var tkr) || tkr.isUnlocked)
            orig(self, collision);
    }

    private void On_SkigillNode_activate(On.skigillNode.orig_activate orig, skigillNode self) {
        orig(self);
        var tkr = self.GetComponent<SkillTreeIndexTracker>();
        if(!tkr) return;
        var isChest = tkr.DataNode.type == GameData.SkillNodeType.CHEST;
        var isPerk = tkr.DataNode.type == GameData.SkillNodeType.PERK;
        var isStat = tkr.DataNode.type == GameData.SkillNodeType.STAT;
        if(!isChest && !isPerk && !isStat) return;
        ArchipelagoClient.Instance.CheckLocationsByName($"Skigill {(isChest ? "Chest" : (isPerk ? "Perk" : "Stat"))} #{tkr.DataNode.indexOfType + 1} ({tkr.DataNode.region.name.ToUpper()})");
        ArchipelagoClient.Instance.CheckLocationsByName($"Skigill {(isChest ? "Chest" : (isPerk ? "Perk" : "Stat"))} #{tkr.DataNode.indexOfType + 1} ({tkr.DataNode.region.name.ToUpper()}) as {GameData.allCharacters.First(c => c.id == GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>().chara).name}");
        tkr.Unlock(false);
    }

    private void On_SkigillNode_showConnex(On.skigillNode.orig_showConnex orig, skigillNode self) {
        orig(self);
        if(self.TryGetComponent<SkillTreeIndexTracker>(out var tkr)) {
            for(var j = 0; j < self.transform.childCount; j++) {
                var ch = self.transform.GetChild(j);
                if(ch.name != "connexion(Clone)") continue;
                ch.Find("GameObject").localScale = tkr.isUnlocked ? new(0.5f, 0.5f, 1f) : new(0.2f, 0.1f, 1f);
            }
        }
    }


    private void On_NodeContentDisplayerUI_OnPointerEnter(On.nodeContentDisplayerUI.orig_OnPointerEnter orig, nodeContentDisplayerUI self, UnityEngine.EventSystems.PointerEventData pointerEventData) {
        orig(self, pointerEventData);
        if(self.instanceDisplayer == null) return;
        var tkr = self.source.GetComponent<SkillTreeIndexTracker>();
        if(tkr == null) return;
        var isChest = tkr.DataNode.type == GameData.SkillNodeType.CHEST;
        var isPerk = tkr.DataNode.type == GameData.SkillNodeType.PERK;
        if(!isChest && !isPerk) return;
        var textObj = new GameObject("Index Label") {
            layer = 9
        };
        var cvs = textObj.AddComponent<Canvas>();
        cvs.worldCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        cvs.pixelPerfect = true;
        cvs.scaleFactor = 16;
        var textCpt = textObj.AddComponent<Text>();
        textCpt.font = Plugin.Resources.LoadAsset<Font>("Assets/Fonts/PerfectDOSVGA437.ttf");
        textCpt.fontSize = 16;
        textCpt.text = $"#{tkr.DataNode.indexOfType+1}";
        textCpt.alignment = TextAnchor.MiddleCenter;
        textObj.transform.SetParent(self.instanceDisplayer.transform);
        textObj.transform.localScale = new(0.0625f, 0.0625f, 0.0625f);
        textObj.transform.localPosition = new Vector3(0.75f, -0.75f, 0f);
        var o1 = textObj.AddComponent<Outline>();
        o1.effectDistance = new(0.5f, 0.5f);
        o1.effectColor = new(0f, 0f, 0f, 1f);
    }
    #endregion


    ////// Public API //////

    public void RescanAll() {
        var chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        var spawnChar = GameData.allCharacters.First(n => n.id == chara.chara);
        if(!ArchipelagoDataUtils.HasRegionItem(spawnChar)) {
            spawnChar = GameData.allCharacters.First(n => n.name == ArchipelagoSaver.Instance.metaProg["archi_startChar"]);
        }
        var spawnRegion = GameData.allRegions.First(n => n.originCharacter.HasValue && n.originCharacter.Value == spawnChar);
        foreach(var tkr in GameObject.FindObjectsByType<SkillTreeIndexTracker>(FindObjectsSortMode.InstanceID)) {
            tkr.Rescan(spawnRegion);
        }
    }


    ////// Private API //////

    void ApplyTrackers() {
        var gridObj = GameObject.Find("gridHolder/grid")?.transform;
        if(gridObj == null) return;
        var allValidNodes = GameObject.FindObjectsByType<skigillNode>(FindObjectsSortMode.InstanceID).Where(n => n.isActiveAndEnabled && !n.metaProg && n.transform.IsChildOf(gridObj)).OrderBy(n => -n.transform.position.y).ThenBy(n => n.transform.position.x).ToList();
        for(var i = 0; i < allValidNodes.Count; i++) {
            if(!allValidNodes[i].gameObject.TryGetComponent<SkillTreeIndexTracker>(out var tkr))
                tkr = allValidNodes[i].gameObject.AddComponent<SkillTreeIndexTracker>();
            tkr.DataNode = GameData.skillTree[i];
        }
    }

    void EnsureSafeSpawn(CharaStats self) {
        if(!ArchipelagoDataUtils.HasRegionByCharacterId(self.chara)) {
            //spawn region is locked, teleport to and activate starter region
            var mgo = GameObject.Find($"gridHolder/grid/Perks/{GameData.allCharacters.First(n => n.name == ArchipelagoSaver.Instance.metaProg["archi_startChar"]).internalName}");
            mgo.GetComponent<skigillNode>().autoActivate();
            self.transform.position = new(mgo.transform.position.x, mgo.transform.position.y, 0);
        }
    }
}