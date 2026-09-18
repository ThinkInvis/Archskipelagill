using Archskipelagill.Itemizers;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archskipelagill;

public class ArchiSaver:JSONsaver {
    public static ArchiSaver instance; //should only be created once by plugin setup

    public int lastReceivedIndex = 0;
    public int lastSavedIndex = 0;

    public void Awake() {
        if(instance != null) {
            Plugin.BepinLogger.LogFatal("Multiple instances of intended-singleton ArchiSaver component created");
            return;
        }
        instance = this;
        load(); //load a little earlier to increase margins around item received events
        if(metaProg.TryGetValue("archi_lastIndex", out var indexStr))
            lastSavedIndex = int.Parse(indexStr);
        if(metaProg.TryGetValue("archi_savedItems", out var itemsStr)) {
            var pairs = itemsStr.Split("|");
            foreach(var pair in pairs) {
                var kv = pair.Split(";");
                receivedItemCounts[kv[0]] = int.Parse(kv[1]);
            }
        }
    }

    public new void Start() {}

    public void ReceiveArchiItem(Archipelago.MultiClient.Net.Models.ItemInfo receivedItem) {
        Plugin.BepinLogger.LogDebug($"Received item {receivedItem.ItemName} at index {lastReceivedIndex}/{lastSavedIndex}");

        lastReceivedIndex++;

        if(lastReceivedIndex > lastSavedIndex) {
            ProcessItem(receivedItem.ItemName);
        }
    }

    private void ProcessItem(string itemName) {
        if(!receivedItemCounts.ContainsKey(itemName))
            receivedItemCounts[itemName] = 0;
        receivedItemCounts[itemName]++;
        var mpo = GameObject.FindGameObjectWithTag("MetaProg");
        JSONsaver stockSaver = null;
        if(mpo != null)
            mpo.TryGetComponent<JSONsaver>(out stockSaver);
        if(stockSaver != null) {
            metaProg = stockSaver.metaProg;
        }

        var cs = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();

        switch(itemName) {
            case "Bonus Gill":
                //todo: progressive amount based on region/difficulty unlocks
                metaProg["totalMetaMoney"] = (int.Parse(metaProg.GetValueOrDefault("totalMetaMoney", "0")) + 50).ToString();
                if(stockSaver != null) stockSaver.metaProg["totalMetaMoney"] = metaProg["totalMetaMoney"];
                if(cs.metaMenu) {
                    cs.XP += 50;
                    cs.XPtoDisplay += 50;
                    cs.updateXPtoDisplay();
                }
                break;
            case "Skigill Region: Mage":
            case "Skigill Region: Strongman":
            case "Skigill Region: Fox":
            case "Skigill Region: Prototype":
            case "Skigill Region: Dragon":
            case "Skigill Region: Dwarves":
            case "Skigill Region: Bosses":
                Plugin.instance.skillTreeItemizer.RescanRegions();
                break;
        }

        lastSavedIndex++;
        metaProg["archi_savedItems"] = string.Join("|", receivedItemCounts.ToList().Select(kvp => kvp.Key + ";" + kvp.Value.ToString()));
        metaProg["archi_lastIndex"] = (lastSavedIndex).ToString();
        if(stockSaver != null)
            stockSaver.metaProg = metaProg;
        save();

        Plugin.BepinLogger.LogMessage($"Received item {itemName}, total count now {receivedItemCounts[itemName]}");
    }

    public readonly Dictionary<string, int> receivedItemCounts = [];
}

public class CustomSaveLoad {
    public CustomSaveLoad() {
        IL.JSONsaver.save += JSONsaver_save;
        IL.JSONsaver.load += JSONsaver_load;
        On.MoneyBagScript.addMoneyToBag += MoneyBagScript_addMoneyToBag;
        var cslGO = new GameObject(); //main menu JSONSaver uses a tag for ident/finding, so this should be safe from intercepting MoneyBagScript et al.
        UnityEngine.Object.DontDestroyOnLoad(cslGO);
        cslGO.AddComponent<ArchiSaver>();
    }

    private void MoneyBagScript_addMoneyToBag(On.MoneyBagScript.orig_addMoneyToBag orig, MoneyBagScript self) {
        var mpo = GameObject.FindGameObjectWithTag("MetaProg");
        if(mpo != null && mpo.TryGetComponent<JSONsaver>(out var saver)) {
            if(!saver.metaProg.TryGetValue("archi_manualMetaMoney", out var manualMoneyStr))
                manualMoneyStr = "0";
            saver.metaProg["archi_manualMetaMoney"] = (int.Parse(manualMoneyStr) + self.amount).ToString(); //save this just in case we need to implement rebuilding total from manual collection + filler item count at some point
        }
        orig(self);
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
        if(mb != null)
            GameObject.Destroy(mb);

        var chara = GameObject.Find("MetaGridAndStuff/PlayerCharacter").GetComponent<CharaStats>();
        chara.XP = 0;
        chara.XPtoDisplay = 0;

        GameObject.FindFirstObjectByType<gridResetter>().resetMetaProg(); //also saves save file
    }
}
