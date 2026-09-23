using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Archskipelagill.Itemizers;

public class AbilityUnlockItemizer {
    public enum CharacterInIngameOrder { None, Mage, Strongman, Fox, Dragon, Prototype, Dwarves };

    readonly Sprite customLockSprite;
    public ConfigEntry<bool> cfgAbilityLocationTracker;

    static readonly Dictionary<string, string> CHARACTER_NAME_TRANSLATE = new() {
        {"Mage", "Mage"},
        {"Jugger", "Prototype"},
        {"Dragon", "Dragon"},
        {"Baldo", "Strongman"},
        {"Fox", "Fox"},
        {"Nain", "Dwarves"}
    };

    public AbilityUnlockItemizer() {
        On.charaSelectScript.selected += CharaSelectScript_selected;
        On.charaSelectScript.updateUnlockStatus += CharaSelectScript_updateUnlockStatus;
        On.diffSelectScript.updateUnlockStatus += DiffSelectScript_updateUnlockStatus;
        On.modeSelectScript.updateUnlockStatus += ModeSelectScript_updateUnlockStatus;
        On.chestLootScript.loote += ChestLootScript_loote;
        On.skigillNode.Update += SkigillNode_Update;
        On.skigillNode.Start += SkigillNode_Start;
        On.weaponDisplayer.Start += WeaponDisplayer_Start;
        On.charaSelectScript.Start += CharaSelectScript_Start;
        On.endMenuManager.Start += EndMenuManager_Start;

        customLockSprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/locked-archi.png");

        cfgAbilityLocationTracker = Plugin.instance.config.Bind<bool>(new ConfigDefinition("Special Effects", "Ability Location Tracker"), true, new ConfigDescription("If true, unchecked locations corresponding to weapons and characters will be marked."));
    }

    private void EndMenuManager_Start(On.endMenuManager.orig_Start orig, endMenuManager self) {
        orig(self);
        var checkStr = $"Escaped with {Enum.GetName(typeof(CharacterInIngameOrder), self.st.chara)}";
        if(self.st.won && !ArchiSaver.instance.sentChecks.Contains(checkStr) && !ArchiSaver.instance.unsentChecks.Contains(checkStr) && ArchiSaver.instance.allValidChecks.Contains(checkStr)) {
            if(cfgAbilityLocationTracker.Value)
                self.winIcons[self.st.chara].AddComponent<AbilityDisplayerCheckInd>();

            Plugin.ArchipelagoClient.CheckLocationsByName(checkStr);
        }
    }

    private void CharaSelectScript_Start(On.charaSelectScript.orig_Start orig, charaSelectScript self) {
        orig(self);
        var charName = Enum.GetName(typeof(CharacterInIngameOrder), self.ID);
        var checkStr = $"Escaped with {charName}";
        if(cfgAbilityLocationTracker.Value && 
            !ArchiSaver.instance.sentChecks.Contains(checkStr) && !ArchiSaver.instance.unsentChecks.Contains(checkStr) && ArchiSaver.instance.allValidChecks.Contains(checkStr)) {
            var adci = self.gameObject.AddComponent<AbilityDisplayerCheckInd>();
            adci.isLocked = !ArchiSaver.instance.metaProg.TryGetValue(self.unlockKey, out var ulStr) || ulStr != "unlocked" || ArchiSaver.GetItemCount("Character: " + charName) == 0;
        }
    }

    private void WeaponDisplayer_Start(On.weaponDisplayer.orig_Start orig, weaponDisplayer self) {
        orig(self);
        var checkStr = $"Escaped with Weapon {self.GetComponent<weaponDisplayer>().source.name.Replace("(Clone)", "")}";
        if(cfgAbilityLocationTracker.Value && self.levelToDisplay == 0 &&
            !ArchiSaver.instance.sentChecks.Contains(checkStr) && !ArchiSaver.instance.unsentChecks.Contains(checkStr) && ArchiSaver.instance.allValidChecks.Contains(checkStr))
            self.gameObject.AddComponent<AbilityDisplayerCheckInd>();
    }

    private void SkigillNode_Update(On.skigillNode.orig_Update orig, skigillNode self) {
        orig(self);
        if(!self.metaProg) return;
        if(self.type == 20) {
            //TODO: cache this
            var matches = ArchiSaver.instance.receivedItemCounts.Keys.Where(k => (k.StartsWith("Weapon: ") && k.EndsWith(self.name)) || (CHARACTER_NAME_TRANSLATE.TryGetValue(self.name, out var cn) && k == $"Character: {cn}"));
            if(!matches.Any() || ArchiSaver.GetItemCount(matches.First()) == 0) {
                self.toggleMetaWeapon.SetActive(true);
                self.toggleMetaWeapon.GetComponent<SpriteRenderer>().sprite = customLockSprite;
                self.toggleMetaWeapon.transform.localPosition = new(0f, -1.25f, -1f);
            } else {
                self.toggleMetaWeapon.transform.localPosition = new(0f, -1.25f, 0f); //default position
            }
        }
    }

    private void SkigillNode_Start(On.skigillNode.orig_Start orig, skigillNode self) {
        orig(self);
        if(self.metaProg) {
            if(self.type == 20) {
                var matches = ArchiSaver.instance.allValidChecks.Except(ArchiSaver.instance.unsentChecks).Except(ArchiSaver.instance.sentChecks).Where(k => (k.StartsWith("Escaped with Weapon") && k.EndsWith(self.name)) || (CHARACTER_NAME_TRANSLATE.TryGetValue(self.name, out var cn) && k == $"Escaped with {cn}"));
                if(matches.Any()) {
                    var tkr = self.gameObject.AddComponent<SkillTreeIndexTracker>();
                    tkr.Unlock(true);
                }
            }
        }
    }

    private int ChestLootScript_loote(On.chestLootScript.orig_loote orig, chestLootScript self) {
        var dict = GameObject.FindGameObjectWithTag("Dict").GetComponent<weaponDictionary>();
        var origList = (Transform[])dict.WeaponList.Clone();
        for(int i = 0; i < origList.Length; i++) {
            if(origList[i] == null) continue;
            var wname = origList[i].gameObject.name.Replace("(Clone)", "");
            if(ArchiSaver.GetItemCount("Weapon: " + wname) == 0) {
                Plugin.BepinLogger.LogDebug($"Blocked weapon {wname} from loot due to archilock");
                dict.WeaponList[i] = null;
            }
        }
        var retv = orig(self);
        dict.WeaponList = origList;
        return retv;
    }

    private void ModeSelectScript_updateUnlockStatus(On.modeSelectScript.orig_updateUnlockStatus orig, modeSelectScript self) {
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
        if(lir.renderer != null)
            lir.renderer.color = isArchiLocked ? new(1f, 0f, 0f) : new(1f, 1f, 1f);
        lir.isArchiLocked = isArchiLocked;
        lir.UpdateIcon();
    }

    private void DiffSelectScript_updateUnlockStatus(On.diffSelectScript.orig_updateUnlockStatus orig, diffSelectScript self) {
        orig(self);
        var isArchiLocked = false;
        if(ArchiSaver.GetItemCount("Progressive Difficulty") < self.difficulty && self.unlocked) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
            isArchiLocked = true;
        }
        var lockObj = self.transform.GetChild(2);
        if(!lockObj.TryGetComponent<LockIconReplacer>(out var lir))
            lir = lockObj.gameObject.AddComponent<LockIconReplacer>();
        lir.isArchiLocked = isArchiLocked;
        lir.UpdateIcon();
    }

    private void CharaSelectScript_updateUnlockStatus(On.charaSelectScript.orig_updateUnlockStatus orig, charaSelectScript self) {
        orig(self);
        var isArchiLocked = false;
        var targetChar = "Character: " + Enum.GetName(typeof(CharacterInIngameOrder), self.ID);
        if(self.unlocked && ArchiSaver.GetItemCount(targetChar) == 0) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
            isArchiLocked = true;
        }
        var lockObj = self.transform.GetChild(0);
        if(!lockObj.TryGetComponent<LockIconReplacer>(out var lir))
            lir = lockObj.gameObject.AddComponent<LockIconReplacer>();
        lir.isArchiLocked = isArchiLocked;
        lir.UpdateIcon();
    }

    private void CharaSelectScript_selected(On.charaSelectScript.orig_selected orig, charaSelectScript self) {
        orig(self);
        var isArchiLocked = false;
        var targetChar = "Character: " + Enum.GetName(typeof(CharacterInIngameOrder), self.ID);
        if(self.unlocked && ArchiSaver.GetItemCount(targetChar) == 0) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
            self.ls.charaUnlocked = false;
            isArchiLocked = true;
        }
        var lockObj = self.transform.GetChild(0);
        if(!lockObj.TryGetComponent<LockIconReplacer>(out var lir))
            lir = lockObj.gameObject.AddComponent<LockIconReplacer>();
        lir.isArchiLocked = isArchiLocked;
        lir.UpdateIcon();
    }

    class LockIconReplacer : MonoBehaviour {
        public Sprite originalSprite;
        public SpriteRenderer renderer;
        public bool isArchiLocked = false;
#pragma warning disable IDE0051 //Used by Unity Engine
        public void UpdateIcon() {
            if(renderer == null) {
                if(TryGetComponent<SpriteRenderer>(out renderer))
                    originalSprite = renderer.sprite;
            }
            if(renderer != null)
                renderer.sprite = isArchiLocked ? Plugin.instance.abilityUnlockItemizer.customLockSprite : originalSprite;
        }
#pragma warning restore IDE0051
    }

    class AbilityDisplayerCheckInd : MonoBehaviour {
        Transform[] spinners;
        CharaStats chara;
        public bool isLocked = false;
#pragma warning disable IDE0051 //Used by Unity Engine
        void Awake() {
            var icon = this.transform.Find("GameObject/icon");
            if(icon == null) icon = this.transform;
            chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
            spinners = new Transform[6];
            for(var i = 0; i < 6; i++) {
                var spinner = new GameObject("Spinner");
                spinner.transform.parent = icon;
                var spr = spinner.AddComponent<SpriteRenderer>();
                spr.sprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/archi-big-single.png");
                spr.drawMode = SpriteDrawMode.Sliced;
                spr.size *= 0.5f;
                spr.gameObject.layer = 5;
                spr.sortingOrder = 2;
                spinner.transform.localScale = new(1f, 1f, 1f);
                spinners[i] = spinner.transform;
            }
        }

        void Update() {
            var phase = Time.unscaledTime * 0.5f * Mathf.PI;
            for(var i = 0; i < spinners.Length; i++) {
                var iphase = i / 3f * Mathf.PI;
                spinners[i].transform.localPosition = new(Mathf.Cos(phase + iphase) * 1f, Mathf.Sin(phase + iphase) * 1f, -2f);
                if(chara.won) {
                    spinners[i].GetComponent<SpriteRenderer>().color = new(0f, 1f, 0f);
                    spinners[i].transform.localPosition *= 2f;
                    }
                else if(isLocked)
                    spinners[i].GetComponent<SpriteRenderer>().color = new(0.2f, 0.2f, 0.2f);
                else
                    spinners[i].GetComponent<SpriteRenderer>().color = new(1f, 1f, 1f);
            }
        }
#pragma warning restore IDE0051
    }
}