using Archskipelagill.ArchipelagoCompat;
using UnityEngine;

namespace Archskipelagill.ItemsAndLocations;

public class RoundEndItemizer {

	////// Initializer/Fields/Properties //////
    
	public RoundEndItemizer() {
        On.mainCameraScript.playerWin += On_MainCameraScript_playerWin;
        On.VieScript.dies += On_VieScript_dies;
        On.mainCameraScript.cancelWinVortex += On_MainCameraScript_cancelWinVortex;
    }


	////// MonoMod Hooks //////
	#region MonoMod Hooks
	private void On_MainCameraScript_cancelWinVortex(On.mainCameraScript.orig_cancelWinVortex orig, mainCameraScript self) {
        if(ArchipelagoSaver.GetItemCount("Endless Mode") < 1) return;
        orig(self);
    }

    private void On_VieScript_dies(On.VieScript.orig_dies orig, VieScript self) {
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
                ArchipelagoClient.Instance.CheckLocationsByName($"Defeated {targetBossName}");
                if(stats.difficulty > 6)
                    ArchipelagoClient.Instance.CheckLocationsByName($"Defeated {targetBossName} on Difficulty 7");
            }
            if(stats.bossesKilled >= 6) {
                ArchipelagoClient.Instance.CheckLocationsByName("I'm The Boss Now");
                if(stats.difficulty > 6) {
                    ArchipelagoClient.Instance.CheckLocationsByName("I'm The Boss Now on Difficulty 7");
                }
            }
        }
    }

    private void On_MainCameraScript_playerWin(On.mainCameraScript.orig_playerWin orig, mainCameraScript self) {
        orig(self);

        var charaStats = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();

        var itemsObj = charaStats.playerItemsParent.transform;
        for(var i = 0; i < itemsObj.childCount; i++) {
            var ch = itemsObj.GetChild(i);
            var chn = ch.name.Replace("(Clone)", "");
            if(int.TryParse(chn[..3], out var n) && n >= 1 && n <= 60)
                ArchipelagoClient.Instance.CheckLocationsByName($"Escaped with Weapon {chn}");
        }
    }
    #endregion
}
