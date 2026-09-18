using System;
using UnityEngine;

namespace Archskipelagill;

public class AbilityUnlockItemizer {
    public AbilityUnlockItemizer() {
        On.charaSelectScript.selected += CharaSelectScript_selected;
        On.charaSelectScript.updateUnlockStatus += CharaSelectScript_updateUnlockStatus;
        On.diffSelectScript.updateUnlockStatus += DiffSelectScript_updateUnlockStatus;
        On.modeSelectScript.updateUnlockStatus += ModeSelectScript_updateUnlockStatus;
        On.chestLootScript.loote += ChestLootScript_loote;
    }

    private int ChestLootScript_loote(On.chestLootScript.orig_loote orig, chestLootScript self) {
        var dict = GameObject.FindGameObjectWithTag("Dict").GetComponent<weaponDictionary>();
        var origList = (Transform[])dict.WeaponList.Clone();
        for(int i = 0; i < origList.Length; i++) {
            if(origList[i] == null) continue;
            var wname = origList[i].gameObject.name.Replace("(Clone)", "");
            if(!Plugin.ArchipelagoClient.receivedItemCounts.TryGetValue("Weapon: " + wname, out var wcount) || wcount == 0) {
                Plugin.BepinLogger.LogMessage($"Blocked weapon {wname} from loot due to archilock");
                dict.WeaponList[i] = null;
            }
        }
        var retv = orig(self);
        dict.WeaponList = origList;
        return retv;
    }

    private void ModeSelectScript_updateUnlockStatus(On.modeSelectScript.orig_updateUnlockStatus orig, modeSelectScript self) {
        orig(self);
        if(self.mode != "normal" && self.unlocked) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
        }
    }

    private void DiffSelectScript_updateUnlockStatus(On.diffSelectScript.orig_updateUnlockStatus orig, diffSelectScript self) {
        orig(self);
        Plugin.ArchipelagoClient.receivedItemCounts.TryGetValue("Progressive Difficulty", out var tcc);
        if(tcc < self.difficulty && self.unlocked) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
        }
    }

    private void CharaSelectScript_updateUnlockStatus(On.charaSelectScript.orig_updateUnlockStatus orig, charaSelectScript self) {
        orig(self);
        var targetChar = "Character: " + Enum.GetName(typeof(SkillTree.SkillNodeSpawnId), self.ID).ToTitleCase();
        if(self.unlocked && (!Plugin.ArchipelagoClient.receivedItemCounts.TryGetValue(targetChar, out var tcc) || tcc == 0)) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
        }
    }

    private void CharaSelectScript_selected(On.charaSelectScript.orig_selected orig, charaSelectScript self) {
        orig(self);
        var targetChar = "Character: " + Enum.GetName(typeof(SkillTree.SkillNodeSpawnId), self.ID).ToTitleCase();
        if(self.unlocked && (!Plugin.ArchipelagoClient.receivedItemCounts.TryGetValue(targetChar, out var tcc) || tcc == 0)) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
            self.ls.charaUnlocked = false;
        }
    }
}
