using Archskipelagill.ArchipelagoCompat;
using Archskipelagill.GameDataAccess;
using Archskipelagill.ItemsAndLocations.Components;
using BepInEx.Configuration;
using System;
using System.Linq;
using UnityEngine;

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

        _cfgSkillTreeLocationTracker = Plugin.Instance.MainConfig.Bind<bool>(new ConfigDefinition("Special Effects", "Skigill Location Tracker"), true, new ConfigDescription("If true, unchecked locations on the Skigill will be marked."));
    }


    ////// MonoMod Hooks //////
    #region MonoMod Hooks
    private void On_CharaStats_Start(On.CharaStats.orig_Start orig, CharaStats self) {
        orig(self);

        if(self.metaMenu) return;

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
        if(!isChest && !isPerk) return;
        ArchipelagoClient.Instance.CheckLocationsByName($"Skigill {(isChest ? "Chest" : "Perk")} #{(isChest ? tkr.DataNode.chestIndex : tkr.DataNode.perkIndex) + 1} ({Enum.GetName(typeof(GameData.SkillNodeRegion), tkr.DataNode.region)})");
        tkr.Rescan();
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
    #endregion


    ////// Public API //////
    
    public void RescanAll() {
        foreach(var tkr in GameObject.FindObjectsByType<SkillTreeIndexTracker>(FindObjectsSortMode.InstanceID)) {
            tkr.Rescan();
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
            //spawn region is locked, teleport to and activate mage region which for now is guaranteed unlocked
            var mgo = GameObject.Find("gridHolder/grid/Perks/Mage");
            mgo.GetComponent<skigillNode>().autoActivate();
            self.transform.position = new(mgo.transform.position.x, mgo.transform.position.y, 0);
        }
    }
}