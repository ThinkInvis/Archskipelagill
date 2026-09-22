using UnityEngine;

namespace Archskipelagill.Itemizers;

public class RoundEndItemizer {
    public RoundEndItemizer() {
        On.mainCameraScript.playerWin += MainCameraScript_playerWin;
        On.VieScript.dies += VieScript_dies;
        On.mainCameraScript.cancelWinVortex += MainCameraScript_cancelWinVortex;
    }

    private void MainCameraScript_cancelWinVortex(On.mainCameraScript.orig_cancelWinVortex orig, mainCameraScript self) {
        if(ArchiSaver.GetItemCount("Endless Mode") < 1) return;
        orig(self);
    }

    private void VieScript_dies(On.VieScript.orig_dies orig, VieScript self) {
        orig(self);
        var stats = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        if(self.isBoss) {
            var targetBossName = self.bossName switch {
                "OVNI" => "Rosa",
                "GRENOUILLE" => "Roger",
                "SLIME" => "Jello",
                "POULPE" => "Pilpou",
                "BOULE" => "Bouboul",
                "COCHON" => "Gari",
                "FINAL" => "Final Boss",
                _ => "N/A"
            };
            if(targetBossName != "N/A") {
                Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated {targetBossName}");
                if(stats.difficulty > 6)
                    Plugin.ArchipelagoClient.CheckLocationsByName($"Defeated {targetBossName} on Difficulty 7");
            }
            if(stats.bossesKilled >= 6) {
                Plugin.ArchipelagoClient.CheckLocationsByName("I'm The Boss Now");
                if(stats.difficulty > 6) {
                    Plugin.ArchipelagoClient.CheckLocationsByName("I'm The Boss Now on Difficulty 7");
                }
            }
        }
    }

    private void MainCameraScript_playerWin(On.mainCameraScript.orig_playerWin orig, mainCameraScript self) {
        orig(self);

        var charaStats = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();

        var itemsObj = charaStats.playerItemsParent.transform;
        for(var i = 0; i < itemsObj.childCount; i++) {
            var ch = itemsObj.GetChild(i);
            var chn = ch.name.Replace("(Clone)", "");
            if(int.TryParse(chn[..3], out var n) && n >= 1 && n <= 60)
                Plugin.ArchipelagoClient.CheckLocationsByName($"Escaped with Weapon {chn}");
        }
    }
}
