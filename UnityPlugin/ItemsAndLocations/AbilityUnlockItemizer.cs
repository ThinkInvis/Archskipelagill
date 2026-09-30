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
    private bool _doneStartingCharSwap = false;

    public AbilityUnlockItemizer() {
        On.charaSelectScript.selected += On_CharaSelectScript_selected;
        On.charaSelectScript.updateUnlockStatus += On_CharaSelectScript_updateUnlockStatus;
        On.diffSelectScript.updateUnlockStatus += On_DiffSelectScript_updateUnlockStatus;
        On.modeSelectScript.updateUnlockStatus += On_ModeSelectScript_updateUnlockStatus;
        On.chestLootScript.loote += On_ChestLootScript_loote;
        On.weaponDictionary.Start += On_WeaponDictionary_Start;
        On.skigillNode.Update += On_SkigillNode_Update;
        On.skigillNode.Start += On_SkigillNode_Start;
        On.weaponDisplayer.Start += On_WeaponDisplayer_Start;
        On.charaSelectScript.Start += On_CharaSelectScript_Start;
        On.endMenuManager.Start += On_EndMenuManager_Start;
        On.gridResetter.Start += On_GridResetter_Start;

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
        self.startWeapon = GameData.allWeapons.First(w => w.prefabName == ArchipelagoSaver.Instance.StartingWeaponNames[self.ID - 1]).id;
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
        if(self.metaProg && self.type == 20 && self.transform.parent.name == "ARMES") {
            var unlockables = GameData.allWeapons.Where(w => !ArchipelagoSaver.Instance.StartingWeaponNames.Contains(w.prefabName)).ToList();
            var wpnIndex = self.transform.GetSiblingIndex();
            if(wpnIndex >= unlockables.Count) {
                self.icon.sprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/item-unknown.png");
                self.name = "UnknownWeapon";
            } else {
                self.icon.sprite = GameDataAccess.GameData.weaponSprites[unlockables[wpnIndex]];
                self.name = unlockables[wpnIndex].prefabName;
            }
        }
        orig(self);
        if(self.metaProg && self.type == 20) {
            if(ArchipelagoDataUtils.HasLocation($"Escaped with {GameData.allCharacters.FirstOrDefault(n => n.internalName == self.name).name}") == ArchipelagoDataUtils.LocationState.Unchecked
                || ArchipelagoDataUtils.HasLocation($"Escaped with Weapon {GameData.allWeapons.FirstOrDefault(n => n.prefabName == self.name).prefabName}") == ArchipelagoDataUtils.LocationState.Unchecked) {
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
        var adci = self.gameObject.GetComponent<AbilityLocationTrackerDisplay>();
        adci.IsLocked = !ArchipelagoSaver.Instance.metaProg.TryGetValue(self.unlockKey, out var ulStr) || ulStr != "unlocked" || !ArchipelagoDataUtils.HasCharacterById(self.ID);
        self.startWeapon = GameData.allWeapons.First(w => w.prefabName == ArchipelagoSaver.Instance.StartingWeaponNames[self.ID - 1]).id;
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

    private void On_GridResetter_Start(On.gridResetter.orig_Start orig, gridResetter self) {
        orig(self);
        ResetMetaTreeCharacter(self);
    }

    private void On_WeaponDictionary_Start(On.weaponDictionary.orig_Start orig, weaponDictionary self) {
        if(!ResourceGrabber.Instance.GridSceneDuringLoading) {
            self.unlocksableSaveNames = [.. GameData.allWeapons.Where(w => !ArchipelagoSaver.Instance.StartingWeaponNames.Contains(w.prefabName)).Select(w => w.prefabName)];
            int ui = 0;
            for(var i = 0; i < self.WeaponList.Length; i++) {
                var wpnStatsId = GameData.weaponPrefabs[GameData.allWeapons[i]].GetComponent<weaponStats>().ID;
                if(ArchipelagoSaver.Instance.StartingWeaponNames.Contains(GameData.allWeapons[i].prefabName)) {
                    self.WeaponList[wpnStatsId - 1] = GameData.weaponPrefabs[GameData.allWeapons[i]].transform;
                } else {
                    self.WeaponList[wpnStatsId - 1] = null;
                    self.unlockablePrefabs[ui] = GameData.weaponPrefabs[GameData.allWeapons[i]].transform;
                    ui++;
                }
            }
        }
        orig(self);
    }
    #endregion

    ////// Public API //////

    public void ResetMetaTreeCharacter(gridResetter resetter) {
        int startCharInd = 0;
        if(ArchipelagoSaver.Instance.metaProg.TryGetValue("archi_startChar", out var startChar)) {
            var startCharObj = GameData.allCharacters.First(n => n.name == startChar);
            resetter.root = GameObject.Find($"metaGrid/{startCharObj.internalName}").GetComponent<skigillNode>();
            startCharInd = startCharObj.id - 1;
        } else
            resetter.root = GameObject.Find($"metaGrid/Mage").GetComponent<skigillNode>();
        var chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        chara.transform.position = resetter.root.transform.position;
        foreach(var sk in chara.skins)
            GameObject.Destroy(sk);
        var skinsObj = chara.transform.Find("Skins");
        if(skinsObj != null)
            GameObject.Destroy(skinsObj.gameObject);
        chara.skins = [GameObject.Instantiate(ResourceGrabber.Instance.playerSkinsPrefabs[startCharInd].gameObject, chara.transform)];
        chara.skins[0].SetActive(true);
        chara.GetComponent<CharaMove>().anim = chara.skins[0].GetComponent<Animator>();
    }
}