using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Unity.Audio.Handle;

namespace Archskipelagill.Itemizers;

public class AbilityUnlockItemizer {
    readonly Sprite customLockSprite;
    public AbilityUnlockItemizer() {
        On.charaSelectScript.selected += CharaSelectScript_selected;
        On.charaSelectScript.updateUnlockStatus += CharaSelectScript_updateUnlockStatus;
        On.diffSelectScript.updateUnlockStatus += DiffSelectScript_updateUnlockStatus;
        On.modeSelectScript.updateUnlockStatus += ModeSelectScript_updateUnlockStatus;
        On.chestLootScript.loote += ChestLootScript_loote;
        On.skigillNode.Update += SkigillNode_Update;
        On.weaponDisplayer.Start += WeaponDisplayer_Start;

        customLockSprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/locked-archi.png");
    }

    private void WeaponDisplayer_Start(On.weaponDisplayer.orig_Start orig, weaponDisplayer self) {
        orig(self);
        var checkStr = $"Escaped with Weapon {self.GetComponent<weaponDisplayer>().source.name.Replace("(Clone)", "")}";
        if(!ArchiSaver.instance.sentChecks.Contains(checkStr) && !ArchiSaver.instance.unsentChecks.Contains(checkStr) && ArchiSaver.instance.allValidChecks.Contains(checkStr))
            self.gameObject.AddComponent<WeaponDisplayerCheckInd>();
    }

    private void SkigillNode_Update(On.skigillNode.orig_Update orig, skigillNode self) {
        orig(self);
        if(self.metaProg && self.activated && self.type == 20 && self.amount == 1f) {
            var matches = ArchiSaver.instance.receivedItemCounts.Keys.Where(k => k.EndsWith(self.name));
            if(!matches.Any() || ArchiSaver.GetItemCount(matches.First()) == 0)
                self.toggleMetaWeapon.GetComponent<SpriteRenderer>().sprite = customLockSprite;
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
        if(self.cadenas != null) {
            if(!self.cadenas.TryGetComponent<LockIconReplacer>(out var lir))
                lir = self.cadenas.AddComponent<LockIconReplacer>();
            if(lir.renderer != null) {
                lir.renderer.sprite = isArchiLocked ? customLockSprite : lir.originalSprite;
                lir.renderer.color = isArchiLocked ? new(1f, 0f, 0f) : new(1f, 1f, 1f);
            }
        }
    }

    private void DiffSelectScript_updateUnlockStatus(On.diffSelectScript.orig_updateUnlockStatus orig, diffSelectScript self) {
        orig(self);
        var isArchiLocked = false;
        if(ArchiSaver.GetItemCount("Progressive Difficulty") < self.difficulty && self.unlocked) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
            isArchiLocked = true;
        }
        if(self.cadenas != null) {
            if(!self.cadenas.TryGetComponent<LockIconReplacer>(out var lir))
                lir = self.cadenas.AddComponent<LockIconReplacer>();
            if(lir.renderer != null)
                lir.renderer.sprite = isArchiLocked ? customLockSprite : lir.originalSprite;
        }
    }

    private void CharaSelectScript_updateUnlockStatus(On.charaSelectScript.orig_updateUnlockStatus orig, charaSelectScript self) {
        orig(self);
        var isArchiLocked = false;
        var targetChar = "Character: " + Enum.GetName(typeof(SkillTree.SkillNodeRegion), self.ID - 1).ToTitleCase();
        if(self.unlocked && ArchiSaver.GetItemCount(targetChar) == 0) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
            isArchiLocked = true;
        }
        if(self.cadenas != null) {
            if(!self.cadenas.TryGetComponent<LockIconReplacer>(out var lir))
                lir = self.cadenas.AddComponent<LockIconReplacer>();
            if(lir.renderer != null)
                lir.renderer.sprite = isArchiLocked ? customLockSprite : lir.originalSprite;
        }
    }

    private void CharaSelectScript_selected(On.charaSelectScript.orig_selected orig, charaSelectScript self) {
        orig(self);
        var isArchiLocked = false;
        var targetChar = "Character: " + Enum.GetName(typeof(SkillTree.SkillNodeRegion), self.ID - 1).ToTitleCase();
        if(self.unlocked && ArchiSaver.GetItemCount(targetChar) == 0) {
            self.unlocked = false;
            self.cadenas.SetActive(true);
            self.ls.charaUnlocked = false;
            isArchiLocked = true;
        }
        if(self.cadenas != null) {
            if(!self.cadenas.TryGetComponent<LockIconReplacer>(out var lir))
                lir = self.cadenas.AddComponent<LockIconReplacer>();
            if(lir.renderer != null)
                lir.renderer.sprite = isArchiLocked ? customLockSprite : lir.originalSprite;
        }
    }

    class LockIconReplacer : MonoBehaviour {
        public Sprite originalSprite;
        public SpriteRenderer renderer;
        void Awake() {
            renderer = GetComponent<SpriteRenderer>();
            originalSprite = renderer.sprite;
        }
    }

    class WeaponDisplayerCheckInd : MonoBehaviour {
        Transform[] spinners;
        CharaStats chara;
#pragma warning disable IDE0051 //Used by Unity Engine
        void Awake() {
            var icon = this.transform.Find("GameObject/icon");
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
                if(chara.won)
                    spinners[i].GetComponent<SpriteRenderer>().color = new(0f, 1f, 0f);
            }
        }
#pragma warning restore IDE0051
    }
}