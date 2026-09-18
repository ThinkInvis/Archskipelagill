using MonoMod.Cil;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archskipelagill;

public class CustomSaveLoad {
    public CustomSaveLoad() {
        IL.JSONsaver.save += JSONsaver_save;
        IL.JSONsaver.load += JSONsaver_load;
    }

    private void JSONsaver_load(ILContext il) {
        ILCursor c = new(il);
        c.GotoNext(MoveType.After, x => x.MatchLdstr("/save.json"));
        c.EmitDelegate<Func<string, string>>((origStr) => $"/archi-save-{Plugin.instance.cfgRunSuffix.Value}.json");
        c.GotoNext(MoveType.After, x => x.MatchLdstr("/save.json"));
        c.EmitDelegate<Func<string, string>>((origStr) => $"/archi-save-{Plugin.instance.cfgRunSuffix.Value}.json");
    }

    private void JSONsaver_save(ILContext il) {
        ILCursor c = new(il);
        c.GotoNext(MoveType.After, x => x.MatchLdstr("/save.json"));
        c.EmitDelegate<Func<string, string>>((origStr) => $"/archi-save-{Plugin.instance.cfgRunSuffix.Value}.json");
    }

    public void Wipe() {
        if(SceneManager.GetActiveScene().name != "menu") {
            Plugin.BepinLogger.LogError("CustomSaveLoad.Wipe must be called from main menu");
            return;
        }
        var saver = GameObject.FindGameObjectWithTag("MetaProg").GetComponent<JSONsaver>();
        saver.metaProg = [];
        var mb = GameObject.FindGameObjectWithTag("MoneyBag");
#pragma warning disable IDE0031 //DO NOT use null propagation on Unity objects
        if(mb != null) mb.GetComponent<MoneyBagScript>().addMoneyToBag();
#pragma warning restore IDE0031

        var chara = GameObject.Find("MetaGridAndStuff/PlayerCharacter").GetComponent<CharaStats>();
        chara.XP = 0;
        chara.XPtoDisplay = 0;

        GameObject.FindFirstObjectByType<gridResetter>().resetMetaProg(); //also saves save file
    }
}
