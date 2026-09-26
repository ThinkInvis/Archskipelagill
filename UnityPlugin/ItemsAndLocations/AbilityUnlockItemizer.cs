using Archskipelagill.ArchipelagoCompat;
using Archskipelagill.GameDataAccess;
using Archskipelagill.ItemsAndLocations.Components;
using BepInEx.Configuration;
using System.Linq;
using UnityEngine;

namespace Archskipelagill.ItemsAndLocations;

public class AbilityUnlockItemizer : Module<AbilityUnlockItemizer> {

    ////// Initializer/Fields/Properties //////

    public Sprite CustomLockSprite {get; private set;}
    public bool AbilityLocationTracker => _cfgAbilityLocationTracker.Value;

    private readonly ConfigEntry<bool> _cfgAbilityLocationTracker;

    public AbilityUnlockItemizer() {
        On.charaSelectScript.selected += On_CharaSelectScript_selected;
        On.charaSelectScript.updateUnlockStatus += On_CharaSelectScript_updateUnlockStatus;
        On.diffSelectScript.updateUnlockStatus += On_DiffSelectScript_updateUnlockStatus;
        On.modeSelectScript.updateUnlockStatus += On_ModeSelectScript_updateUnlockStatus;
        On.chestLootScript.loote += On_ChestLootScript_loote;
        On.skigillNode.Update += On_SkigillNode_Update;
        On.skigillNode.Start += On_SkigillNode_Start;
        On.weaponDisplayer.Start += On_WeaponDisplayer_Start;
        On.charaSelectScript.Start += On_CharaSelectScript_Start;
        On.endMenuManager.Start += On_EndMenuManager_Start;

        CustomLockSprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/locked-archi.png");

        _cfgAbilityLocationTracker = Plugin.Instance.MainConfig.Bind<bool>(new ConfigDefinition("Special Effects", "Ability Location Tracker"), true, new ConfigDescription("If true, unchecked locations corresponding to weapons and characters will be marked."));
    }

    ////// MonoMod Hooks //////
    #region MonoMod Hooks
    private void On_EndMenuManager_Start(On.endMenuManager.orig_Start orig, endMenuManager self) {
        orig(self);
        var checkStr = $"Escaped with {GameData.allCharacters.First(n => n.id == self.st.chara).name}";
        if(self.st.won && ArchipelagoDataUtils.HasLocation(checkStr) == ArchipelagoDataUtils.LocationState.Unchecked) {
            if(AbilityLocationTracker)
                self.winIcons[self.st.chara].AddComponent<AbilityLocationTrackerDisplay>();

            ArchipelagoClient.Instance.CheckLocationsByName(checkStr);
        }
    }

    private void On_CharaSelectScript_Start(On.charaSelectScript.orig_Start orig, charaSelectScript self) {
        orig(self);
        var checkStr = $"Escaped with {GameData.allCharacters.First(n => n.id == self.ID).name}";
        if(AbilityLocationTracker && ArchipelagoDataUtils.HasLocation(checkStr) == ArchipelagoDataUtils.LocationState.Unchecked) {
            var adci = self.gameObject.AddComponent<AbilityLocationTrackerDisplay>();
            adci.IsLocked = !ArchipelagoSaver.Instance.metaProg.TryGetValue(self.unlockKey, out var ulStr) || ulStr != "unlocked" || !ArchipelagoDataUtils.HasCharacterById(self.ID);
        }
    }

    private void On_WeaponDisplayer_Start(On.weaponDisplayer.orig_Start orig, weaponDisplayer self) {
        orig(self);
        var checkStr = $"Escaped with Weapon {self.GetComponent<weaponDisplayer>().source.name.Replace("(Clone)", "")}";
        if(AbilityLocationTracker && self.levelToDisplay == 0 && ArchipelagoDataUtils.HasLocation(checkStr) == ArchipelagoDataUtils.LocationState.Unchecked)
            self.gameObject.AddComponent<AbilityLocationTrackerDisplay>();
    }

    private void On_SkigillNode_Update(On.skigillNode.orig_Update orig, skigillNode self) {
        orig(self);
        if(!self.metaProg) return;
        if(self.type == 20) {
            //TODO: cache this
            if(!ArchipelagoDataUtils.HasWeaponBySaveName(self.name) && !ArchipelagoDataUtils.HasCharacterByInternalName(self.name)) {
                self.toggleMetaWeapon.SetActive(true);
                self.toggleMetaWeapon.GetComponent<SpriteRenderer>().sprite = CustomLockSprite;
                self.toggleMetaWeapon.transform.localPosition = new(0f, -1.25f, -1f);
            } else {
                self.toggleMetaWeapon.transform.localPosition = new(0f, -1.25f, 0f); //default position
            }
        }
    }

    private void On_SkigillNode_Start(On.skigillNode.orig_Start orig, skigillNode self) {
        orig(self);
        if(self.metaProg && self.type == 20) {
            if(ArchipelagoDataUtils.HasLocation($"Escaped with {GameData.allCharacters.FirstOrDefault(n => n.internalName == self.name).name}") == ArchipelagoDataUtils.LocationState.Unchecked
                || ArchipelagoDataUtils.HasLocation($"Escaped with Weapon {GameData.allWeapons.FirstOrDefault(n => n.saveName == self.name).prefabName}") == ArchipelagoDataUtils.LocationState.Unchecked) {
                var tkr = self.gameObject.AddComponent<SkillTreeIndexTracker>();
                tkr.Unlock(true);
            }
        }
    }

    private int On_ChestLootScript_loote(On.chestLootScript.orig_loote orig, chestLootScript self) {
        var dict = GameObject.FindGameObjectWithTag("Dict").GetComponent<weaponDictionary>();
        var origList = (Transform[])dict.WeaponList.Clone();
        for(int i = 0; i < origList.Length; i++) {
            if(origList[i] == null) continue;
            if(!ArchipelagoDataUtils.HasWeaponByPrefabName(origList[i].gameObject.name.Replace("(Clone)", "")))
                dict.WeaponList[i] = null;
        }
        var retv = orig(self);
        dict.WeaponList = origList;
        return retv;
    }

    private void On_ModeSelectScript_updateUnlockStatus(On.modeSelectScript.orig_updateUnlockStatus orig, modeSelectScript self) {
        orig(self);
        var isArchiLocked = false;
        if(self.mode != "normal") {
            if(self.unlocked) {
                self.unlocked = false;
                self.cadenas.SetActive(true);
            }
            isArchiLocked = true;
        }
        var lockObj = self.transform.GetChild(2);
        if(!lockObj.TryGetComponent<LockIconReplacer>(out var lir))
            lir = lockObj.gameObject.AddComponent<LockIconReplacer>();
        if(lir.Renderer != null)
            lir.Renderer.color = isArchiLocked ? new(1f, 0f, 0f) : new(1f, 1f, 1f);
        lir.IsArchiLocked = isArchiLocked;
        lir.UpdateIcon();
    }

    private void On_DiffSelectScript_updateUnlockStatus(On.diffSelectScript.orig_updateUnlockStatus orig, diffSelectScript self) {
        orig(self);
        var isArchiLocked = false;
        if(ArchipelagoSaver.GetItemCount("Progressive Difficulty") < self.difficulty && self.unlocked) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
            isArchiLocked = true;
        }
        var lockObj = self.transform.GetChild(2);
        if(!lockObj.TryGetComponent<LockIconReplacer>(out var lir))
            lir = lockObj.gameObject.AddComponent<LockIconReplacer>();
        lir.IsArchiLocked = isArchiLocked;
        lir.UpdateIcon();
    }

    private void On_CharaSelectScript_updateUnlockStatus(On.charaSelectScript.orig_updateUnlockStatus orig, charaSelectScript self) {
        orig(self);
        var isArchiLocked = false;
        if(self.unlocked && !ArchipelagoDataUtils.HasCharacterById(self.ID)) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
            isArchiLocked = true;
        }
        var lockObj = self.transform.GetChild(0);
        if(!lockObj.TryGetComponent<LockIconReplacer>(out var lir))
            lir = lockObj.gameObject.AddComponent<LockIconReplacer>();
        lir.IsArchiLocked = isArchiLocked;
        lir.UpdateIcon();
    }

    private void On_CharaSelectScript_selected(On.charaSelectScript.orig_selected orig, charaSelectScript self) {
        orig(self);
        var isArchiLocked = false;
        if(self.unlocked && !ArchipelagoDataUtils.HasCharacterById(self.ID)) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
            self.ls.charaUnlocked = false;
            isArchiLocked = true;
        }
        var lockObj = self.transform.GetChild(0);
        if(!lockObj.TryGetComponent<LockIconReplacer>(out var lir))
            lir = lockObj.gameObject.AddComponent<LockIconReplacer>();
        lir.IsArchiLocked = isArchiLocked;
        lir.UpdateIcon();
    }
    #endregion
}