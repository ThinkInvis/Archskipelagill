using System;
using UnityEngine;

namespace Archskipelagill;

public class RoundEndItemizer {
    public RoundEndItemizer() {
        On.mainCameraScript.playerWin += MainCameraScript_playerWin;
        On.VieScript.dies += VieScript_dies;
    }

    private void VieScript_dies(On.VieScript.orig_dies orig, VieScript self) {
        orig(self);
        var stats = GameObject.Find("PlayerCharacter").GetComponent<CharaStats>();
        if(self.isBoss) {
            Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Any Boss (Event)");
            if(stats.difficulty > 6)
                Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Any Boss on Difficulty 7 (Event)");
            switch(self.bossName) {
                case "OVNI":
                    Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Rosa");
                    if(stats.difficulty > 6)
                        Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Rosa on Difficulty 7");
                    break;
                case "GRENOUILLE":
                    Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Roger");
                    if(stats.difficulty > 6)
                        Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Roger on Difficulty 7");
                    break;
                case "SLIME":
                    Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Jello");
                    if(stats.difficulty > 6)
                        Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Jello on Difficulty 7");
                    break;
                case "POULPE":
                    Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Pilpou");
                    if(stats.difficulty > 6)
                        Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Pilpou on Difficulty 7");
                    break;
                case "BOULE":
                    Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Bouboul");
                    if(stats.difficulty > 6)
                        Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Bouboul on Difficulty 7");
                    break;
                case "COCHON":
                    Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Gari");
                    if(stats.difficulty > 6)
                        Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Gari on Difficulty 7");
                    break;
                case "FINAL":
                    Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Final Boss");
                    Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Final Boss (Event)");
                    if(stats.difficulty > 6)
                        Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated Final Boss on Difficulty 7 (Event)");
                    break;
            }
            if(stats.bossesKilled >= 6) {
                Plugin.ArchipelagoClient.CheckLocationsByName($"I'm The Boss Now");
                Plugin.ArchipelagoClient.CheckLocationsByName($"I'm The Boss Now (Event)");
                if(stats.difficulty > 6) {
                    Plugin.ArchipelagoClient.CheckLocationsByName($"I'm The Boss Now on Difficulty 7");
                    Plugin.ArchipelagoClient.CheckLocationsByName($"I'm The Boss Now on Difficulty 7 (Event)");
                }
            }
        }
    }

    private void MainCameraScript_playerWin(On.mainCameraScript.orig_playerWin orig, mainCameraScript self) {
        orig(self);

        var charaStats = GameObject.Find("PlayerCharacter").GetComponent<CharaStats>();

        Plugin.ArchipelagoClient.CheckLocationsByName($"Escaped with {Enum.GetName(typeof(SkillTree.SkillNodeSpawnId), charaStats.chara).ToTitleCase()}");

        var itemsObj = charaStats.playerItemsParent.transform;
        for(var i = 0; i < itemsObj.childCount; i++) {
            var ch = itemsObj.GetChild(i);
            var chn = ch.name.Replace("(Clone)", "");
            if(int.TryParse(chn[..3], out var n) && n >= 1 && n <= 60)
                Plugin.ArchipelagoClient.CheckLocationsByName($"Escaped with Weapon {chn}");
        }
    }
}
