using UnityEngine;

namespace Archskipelagill;

public class VanillaBugfixes {
    public VanillaBugfixes() {
        On.eclairProjectileScript.Start += On_EclairProjectileScript_Start;
        On.CharaStats.Update += On_CharaStats_Update;
    }

    private void On_CharaStats_Update(On.CharaStats.orig_Update orig, CharaStats self) {
        orig(self);
        lightningArcsThisTick = 0;
    }

    //possible fix for infinite zero-delay chain lightning stalling the entire game out (rare issue?)
    int lightningArcsThisTick = 0;
    private void On_EclairProjectileScript_Start(On.eclairProjectileScript.orig_Start orig, eclairProjectileScript self) {
        if(lightningArcsThisTick > 1000) {
            GameObject.Destroy(self);
            return;
        }

        orig(self);
        lightningArcsThisTick++;
    }
}
