using Archipelago.MultiClient.Net.Models;
using Archskipelagill.ArchipelagoCompat;
using Archskipelagill.EffectComponents;
using BepInEx.Configuration;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archskipelagill;

public class ArchiSaver:JSONsaver {
    public static ArchiSaver instance; //should only be created once by plugin setup

    public int lastReceivedIndex = 0;
    public int lastSavedIndex = 0;

    public readonly Dictionary<string, int> receivedItemCounts = [];
    public readonly List<string> unsentChecks = [];
    public readonly ConcurrentQueue<string> queuedSentChecks = [];
    public readonly ConcurrentQueue<string> queuedUnsentChecks = [];
    public readonly List<string> sentChecks = [];
    public readonly List<string> allValidChecks = [];
    public readonly Queue<string> queuedTraps = [];
    public readonly Queue<ItemInfo> itemsToProcess = [];
    public readonly Queue<string> itemNotifsToProcess = [];
    public int sendNotifsToProcess = 0;

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
        if(metaProg.TryGetValue("archi_unsentChecks", out var checksStr) && checksStr.Length > 0) {
            unsentChecks.AddRange(checksStr.Split("|"));
        }
        if(metaProg.TryGetValue("archi_sentChecks", out var checksStr2) && checksStr2.Length > 0) {
            sentChecks.AddRange(checksStr2.Split("|"));
        }
        if(metaProg.TryGetValue("archi_validChecks", out var checksStr3) && checksStr3.Length > 0) {
            allValidChecks.AddRange(checksStr3.Split("|"));
        }
    }

    public new void Start() {}

    float tSinceLastSend = 0f;
    float tSinceLastReceive = 0f;
    public new void Update() {
        base.Update();

        if(itemsToProcess.Count > 0 || queuedSentChecks.Count > 0 || queuedUnsentChecks.Count > 0) {
            PreSave();

            if(itemsToProcess.Count > 0) {
                while(itemsToProcess.Count > 0)
                    ProcessItem(itemsToProcess.Dequeue());
                metaProg["archi_savedItems"] = string.Join("|", receivedItemCounts.ToList().Select(kvp => kvp.Key + ";" + kvp.Value.ToString()));
                metaProg["archi_lastIndex"] = lastSavedIndex.ToString();
            }

            if(queuedSentChecks.Count > 0) {
                List<string> qscList = [];
                while(queuedSentChecks.Count > 0) {
                    if(!queuedSentChecks.TryDequeue(out var qsc)) break;
                    qscList.Add(qsc);
                }
                sentChecks.AddRange(qscList);
                metaProg["archi_sentChecks"] = String.Join("|", sentChecks);

                if(Plugin.instance.customSaveLoad.cfgSendNotifs.Value)
                    sendNotifsToProcess += qscList.Count;
            }

            if(queuedUnsentChecks.Count > 0) {
                List<string> qucList = [];
                while(queuedUnsentChecks.Count > 0) {
                    if(!queuedUnsentChecks.TryDequeue(out var qsc)) break;
                    qucList.Add(qsc);
                }
                unsentChecks.AddRange(qucList.Except(unsentChecks.Distinct()));
                metaProg["archi_unsentChecks"] = String.Join("|", unsentChecks);
            }

            PostSave();
        }

        bool doNotifs = false;
        var camObj = GameObject.FindGameObjectWithTag("MainCamera");
        if(camObj != null) {
            if(camObj.TryGetComponent<mainCameraScript>(out var mcs) && mcs.transitionVal <= 1f) doNotifs = true;
            else if(camObj.TryGetComponent<mainMenuCamScript>(out var mmcs) && mmcs.shopMenuTransitionValue == 1) doNotifs = true;
        }
        if(doNotifs) {
            if(sendNotifsToProcess > 0) {
                tSinceLastSend += Time.deltaTime;
                if(tSinceLastSend > 0.4f) {
                    tSinceLastSend = 0f;
                    ArchiSendController.CreateSend();
                    sendNotifsToProcess--;
                }
            } else tSinceLastSend = 0f;

            if(itemNotifsToProcess.Count > 0) {
                tSinceLastReceive += Time.deltaTime;
                if(tSinceLastReceive > 0.4f) {
                    tSinceLastReceive = 0f;
                    ArchiDropController.CreateDrop(itemNotifsToProcess.Dequeue());
                }
            } else tSinceLastReceive = 0f;
        }
    }

    public void ReceiveArchiItem(Archipelago.MultiClient.Net.Models.ItemInfo receivedItem) {
        Plugin.BepinLogger.LogDebug($"Received item {receivedItem.ItemName} at index {lastReceivedIndex}/{lastSavedIndex}");

        lastReceivedIndex++;

        if(lastReceivedIndex > lastSavedIndex) {
            //ReceiveArchiItem is called from a thread, which can cause issues with BepInEx error handling; queue and check everything on the next main thread update instead
            itemsToProcess.Enqueue(receivedItem);
        }
    }

    private void ProcessItem(ItemInfo item) {
        if(!receivedItemCounts.ContainsKey(item.ItemName))
            receivedItemCounts[item.ItemName] = 0;
        receivedItemCounts[item.ItemName]++;

        if(Plugin.instance.customSaveLoad.cfgReceiveNotifs.Value
            && (
                (item.LocationId != -2
                    && (Plugin.instance.customSaveLoad.cfgOnlyActiveDrops.Value <= 0f || Time.unscaledTime - ArchipelagoClient.lastConnectTime > Plugin.instance.customSaveLoad.cfgOnlyActiveDrops.Value))
                || (item.LocationId == -2
                    && !Plugin.instance.customSaveLoad.cfgNoStartingInventory.Value)
               ))
            itemNotifsToProcess.Enqueue(item.ItemName);

        var cs = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();

        switch(item.ItemName) {
            case "Bonus Gill":
                metaProg["totalMetaMoney"] = (int.Parse(metaProg.GetValueOrDefault("totalMetaMoney", "0")) + 50).ToString();
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
            case "Final Boss Key":
                Plugin.instance.skillTreeItemizer.RescanAll();
                break;
            case string trapTest when trapTest.StartsWith("Trap: "):
                queuedTraps.Enqueue(item.ItemName[6..]);
                break;
        }

        lastSavedIndex++;

        Plugin.BepinLogger.LogMessage($"Received item {item.ItemName}, total count now {receivedItemCounts[item.ItemName]}");
    }

    public void ReceiveUnsentChecks(params string[] checkNames) {
        foreach(var n in checkNames)
            queuedUnsentChecks.Enqueue(n);
    }
    public void ReceiveSentChecks(params string[] checkNames) {
        foreach(var n in checkNames)
            queuedSentChecks.Enqueue(n);
    }
    public void ResendChecks() {
        if(unsentChecks.Count > 0) {
            Plugin.BepinLogger.LogMessage($"Retrying {unsentChecks.Count} unsent checks");
            PreSave();
            metaProg["archi_unsentChecks"] = "";
            PostSave();
            Plugin.ArchipelagoClient.CheckLocationsByName([.. unsentChecks]);
        }
    }

    public void StoreSlotData(Dictionary<string, object> slotData, Archipelago.MultiClient.Net.ArchipelagoSession session) {
        try {
            PreSave();
            metaProg["archi_goal"] = ((Int64)slotData["goal_type"]).ToString();
            metaProg["archi_boss_last"] = ((Int64)slotData["boss_region_last"]).ToString();
            if(metaProg.ContainsKey("archi_uuid") && metaProg["archi_uuid"] != (string)slotData["world_uuid"] && metaProg["archi_uuid"] != "") {
                Plugin.instance.mainMenuInjector.ReceiveMessage(" !!! WARNING !!!  Your saved world UUID doesn't match with the server's. Please make sure you've RESET YOUR SAVE FILE before proceeding if this is a new run.");
            }
            metaProg["archi_uuid"] = (string)slotData["world_uuid"];
            var locNames = session.Locations.AllLocations.Select(l => session.Locations.GetLocationNameFromId(l));
            metaProg["archi_validChecks"] = string.Join('|', locNames);
            allValidChecks.Clear();
            allValidChecks.AddRange(locNames);
            PostSave();
        } catch(Exception e) {
            Plugin.BepinLogger.LogError(e);
        }
    }

    private void PreSave() {
        var mpo = GameObject.FindGameObjectWithTag("MetaProg");
        JSONsaver stockSaver = null;
        if(mpo != null)
            mpo.TryGetComponent<JSONsaver>(out stockSaver);
        if(stockSaver != null) {
            metaProg.Clear();
            metaProg.AddRange(stockSaver.metaProg);
        }
    }
    private void PostSave() {
        var mpo = GameObject.FindGameObjectWithTag("MetaProg");
        JSONsaver stockSaver = null;
        if(mpo != null)
            mpo.TryGetComponent<JSONsaver>(out stockSaver);
        if(stockSaver != null) {
            stockSaver.metaProg.Clear();
            stockSaver.metaProg.AddRange(metaProg);
        }
        save();
    }

    public static int GetItemCount(string name) {
        return instance.receivedItemCounts.GetValueOrDefault(name, 0);
    }
}

public class CustomSaveLoad {
    public ConfigEntry<string> cfgRunSuffix;
    public ConfigEntry<float> cfgOnlyActiveDrops;
    public ConfigEntry<bool> cfgNoStartingInventory;
    public ConfigEntry<bool> cfgSendNotifs;
    public ConfigEntry<bool> cfgReceiveNotifs;
    public ConfigEntry<bool> cfgMuteNotifs;

    public CustomSaveLoad() {
        cfgRunSuffix = Plugin.instance.config.Bind<string>(new ConfigDefinition("Save/Load", "Run Suffix"), "default", new ConfigDescription("A suffix added to the custom save file redirect used by the client plugin. Must be changed if you want to participate in multiple Archipelago runs including this game simultaneously."));
        cfgSendNotifs = Plugin.instance.config.Bind<bool>(new ConfigDefinition("Special Effects", "Location Notifications"), true, new ConfigDescription("If true, notifications will be displayed for sent location checks. These will be queued and only display once a run is active."));
        cfgReceiveNotifs = Plugin.instance.config.Bind<bool>(new ConfigDefinition("Special Effects", "Item Notifications"), true, new ConfigDescription("If true, notifications will be displayed for received items. These will be queued and only display once a run is active."));
        cfgOnlyActiveDrops = Plugin.instance.config.Bind<float>(new ConfigDefinition("Special Effects", "Item Notifications: Only Active Drops"), 0f, new ConfigDescription("If above 0, only items sent/received this amount of time after the client is connected will generate notifications. Adjusted for your network conditions, this can filter out large batches of notifications generated by async play. Does not affect starting inventory; use the Ignore Starting Inventory setting instead.", new AcceptableValueRange<float>(0f, 60f)));
        cfgNoStartingInventory = Plugin.instance.config.Bind<bool>(new ConfigDefinition("Special Effects", "Item Notifications: Ignore Starting Inventory"), true, new ConfigDescription("If true, starting inventory items will not generate notifications."));
        cfgMuteNotifs = Plugin.instance.config.Bind<bool>(new ConfigDefinition("Special Effects", "Item Notifications: Mute"), false, new ConfigDescription("If true, item notifications will not play sound."));

        IL.JSONsaver.save += JSONsaver_save;
        IL.JSONsaver.load += JSONsaver_load;
        On.MoneyBagScript.addMoneyToBag += MoneyBagScript_addMoneyToBag;
        On.JSONsaver.save += JSONsaver_save1;
        var cslGO = new GameObject(); //main menu JSONSaver uses a tag for ident/finding, so this should be safe from intercepting MoneyBagScript et al.
        UnityEngine.Object.DontDestroyOnLoad(cslGO);
        cslGO.AddComponent<ArchiSaver>();
    }

    private void JSONsaver_save1(On.JSONsaver.orig_save orig, JSONsaver self) {
        orig(self);
        if(self != ArchiSaver.instance) {
            ArchiSaver.instance.metaProg.Clear();
            ArchiSaver.instance.metaProg.AddRange(self.metaProg);
        }
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
        c.EmitDelegate<Func<string, string>>((origStr) => $"/archi-save-{cfgRunSuffix.Value}.json");
        c.GotoNext(MoveType.After, x => x.MatchLdstr("/save.json"));
        c.EmitDelegate<Func<string, string>>((origStr) => $"/archi-save-{cfgRunSuffix.Value}.json");
    }

    private void JSONsaver_save(ILContext il) {
        ILCursor c = new(il);
        c.GotoNext(MoveType.After, x => x.MatchLdstr("/save.json"));
        c.EmitDelegate<Func<string, string>>((origStr) => $"/archi-save-{cfgRunSuffix.Value}.json");
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

        var chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        chara.XP = 0;
        chara.totalXP = 0;
        chara.XPtoDisplay = 0;
        chara.updateXPtoDisplay();

        ArchiSaver.instance.receivedItemCounts.Clear();
        ArchiSaver.instance.lastReceivedIndex = 0;
        ArchiSaver.instance.lastSavedIndex = 0;
        ArchiSaver.instance.sentChecks.Clear();
        ArchiSaver.instance.unsentChecks.Clear();

        GameObject.FindFirstObjectByType<gridResetter>().resetMetaProg(); //also saves save file
    }
}
