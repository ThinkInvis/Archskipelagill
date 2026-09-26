using Archipelago.MultiClient.Net.Models;
using Archskipelagill.GameDataAccess;
using Archskipelagill.ItemsAndLocations;
using Archskipelagill.UX;
using MonoMod.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Archskipelagill.ArchipelagoCompat;

public class ArchipelagoSaver:JSONsaver {

    ////// Initializer/Fields/Properties //////
    
    public static ArchipelagoSaver Instance { get; private set; } //should only be created once by plugin
    public ReadOnlyCollection<string> AllValidChecks { get; private set; }
    public ReadOnlyCollection<string> SentChecks { get; private set; }
    public ReadOnlyCollection<string> UnsentChecks { get; private set; }

    private readonly Dictionary<string, int> _receivedItemCounts = [];
    private readonly ConcurrentQueue<string> _queuedSentChecks = [];
    private readonly ConcurrentQueue<string> _queuedUnsentChecks = [];
    private readonly List<string> _sentChecks = [];
    private readonly List<string> _unsentChecks = [];
    private readonly List<string> _allValidChecks = [];
    private readonly Queue<ItemInfo> _itemsToProcess = [];
    private readonly Queue<string> _itemNotifsToProcess = [];
    private int _sendNotifsToProcess = 0;
    private int _lastReceivedIndex = 0;
    private int _lastSavedIndex = 0;
    private float _tSinceLastSend = 0f;
    private float _tSinceLastReceive = 0f;


    ////// Unity Engine API //////

#pragma warning disable IDE0051 //Used by Unity Engine
    private void Awake() {
        if(Instance != null) {
            Plugin.BepinLogger.LogFatal("Multiple instances of intended-singleton ArchiSaver component created");
            return;
        }
        Instance = this;

        AllValidChecks = new(_allValidChecks);
        SentChecks = new(_sentChecks);
        UnsentChecks = new(_unsentChecks);

        load(); //load a little earlier to increase margins around item received events
        if(metaProg.TryGetValue("archi_lastIndex", out var indexStr))
            _lastSavedIndex = int.Parse(indexStr);
        if(metaProg.TryGetValue("archi_savedItems", out var itemsStr)) {
            var pairs = itemsStr.Split("|");
            foreach(var pair in pairs) {
                var kv = pair.Split(";");
                _receivedItemCounts[kv[0]] = int.Parse(kv[1]);
            }
        }
        if(metaProg.TryGetValue("archi_unsentChecks", out var checksStr) && checksStr.Length > 0) {
            _unsentChecks.AddRange(checksStr.Split("|"));
        }
        if(metaProg.TryGetValue("archi_sentChecks", out var checksStr2) && checksStr2.Length > 0) {
            _sentChecks.AddRange(checksStr2.Split("|"));
        }
        if(metaProg.TryGetValue("archi_validChecks", out var checksStr3) && checksStr3.Length > 0) {
            _allValidChecks.AddRange(checksStr3.Split("|"));
        }
    }

    public new void Start() { }

    public new void Update() {
        base.Update();

        if(_itemsToProcess.Count > 0 || _queuedSentChecks.Count > 0 || _queuedUnsentChecks.Count > 0) {
            PreSave();

            if(_itemsToProcess.Count > 0) {
                while(_itemsToProcess.Count > 0)
                    ProcessItem(_itemsToProcess.Dequeue());
                metaProg["archi_savedItems"] = string.Join("|", _receivedItemCounts.ToList().Select(kvp => kvp.Key + ";" + kvp.Value.ToString()));
                metaProg["archi_lastIndex"] = _lastSavedIndex.ToString();
            }

            if(_queuedSentChecks.Count > 0) {
                List<string> qscList = [];
                while(_queuedSentChecks.Count > 0) {
                    if(!_queuedSentChecks.TryDequeue(out var qsc)) break;
                    qscList.Add(qsc);
                }
                _sentChecks.AddRange(qscList);
                metaProg["archi_sentChecks"] = String.Join("|", _sentChecks);

                if(SaveFileRedirect.Instance.SendNotifs)
                    _sendNotifsToProcess += qscList.Count;

                SkillTreeItemizer.Instance.RescanAll();
            }

            if(_queuedUnsentChecks.Count > 0) {
                List<string> qucList = [];
                while(_queuedUnsentChecks.Count > 0) {
                    if(!_queuedUnsentChecks.TryDequeue(out var qsc)) break;
                    qucList.Add(qsc);
                }
                _unsentChecks.AddRange(qucList.Except(_unsentChecks.Distinct()));
                metaProg["archi_unsentChecks"] = String.Join("|", _unsentChecks);
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
            if(_sendNotifsToProcess > 0) {
                _tSinceLastSend += Time.deltaTime;
                if(_tSinceLastSend > 0.4f) {
                    _tSinceLastSend = 0f;
                    ArchiSendController.CreateSend();
                    _sendNotifsToProcess--;
                }
            } else _tSinceLastSend = 0f;

            if(_itemNotifsToProcess.Count > 0) {
                _tSinceLastReceive += Time.deltaTime;
                if(_tSinceLastReceive > 0.4f) {
                    _tSinceLastReceive = 0f;
                    ArchiDropController.CreateDrop(_itemNotifsToProcess.Dequeue());
                }
            } else _tSinceLastReceive = 0f;
        }
    }
#pragma warning restore IDE0051


    ////// Public API //////

    public static int GetItemCount(string name) {
        return Instance._receivedItemCounts.GetValueOrDefault(name, 0);
    }

    public void ReceiveArchiItem(Archipelago.MultiClient.Net.Models.ItemInfo receivedItem, int index) {
        Plugin.BepinLogger.LogDebug($"Received item {receivedItem.ItemName} at index {_lastReceivedIndex}/{_lastSavedIndex} (helper index {index})");
        if(index <= _lastReceivedIndex) return;

        Interlocked.Increment(ref _lastReceivedIndex);

        if(_lastReceivedIndex > _lastSavedIndex) {
            //ReceiveArchiItem is called from a thread, which can cause issues with BepInEx error handling; queue and check everything on the next main thread update instead
            _itemsToProcess.Enqueue(receivedItem);
        }
    }

    public void ReceiveUnsentChecks(params string[] checkNames) {
        foreach(var n in checkNames)
            _queuedUnsentChecks.Enqueue(n);
    }

    public void ReceiveSentChecks(params string[] checkNames) {
        foreach(var n in checkNames)
            _queuedSentChecks.Enqueue(n);
    }

    public void ResendChecks() {
        if(_unsentChecks.Count > 0) {
            Plugin.BepinLogger.LogMessage($"Retrying {_unsentChecks.Count} unsent checks");
            PreSave();
            metaProg["archi_unsentChecks"] = "";
            PostSave();
            ArchipelagoClient.Instance.CheckLocationsByName([.. _unsentChecks]);
        }
    }

    public void Wipe() {
        _receivedItemCounts.Clear();
        _sentChecks.Clear();
        _unsentChecks.Clear();
        _lastReceivedIndex = 0;
        _lastSavedIndex = 0;
    }

    public void StoreSlotData(Dictionary<string, object> slotData, Archipelago.MultiClient.Net.ArchipelagoSession session) {
        try {
            PreSave();
            metaProg["archi_goal"] = ((Int64)slotData["goal_type"]).ToString();
            metaProg["archi_boss_last"] = ((Int64)slotData["boss_region_last"]).ToString();
            if(metaProg.ContainsKey("archi_uuid") && metaProg["archi_uuid"] != (string)slotData["world_uuid"] && metaProg["archi_uuid"] != "") {
                MainMenuInjector.Instance.ReceiveMessage(" !!! WARNING !!!  Your saved world UUID doesn't match with the server's. Please make sure you've RESET YOUR SAVE FILE before proceeding if this is a new run.");
            }
            metaProg["archi_uuid"] = (string)slotData["world_uuid"];
            var locNames = session.Locations.AllLocations.Select(l => session.Locations.GetLocationNameFromId(l));
            metaProg["archi_validChecks"] = string.Join('|', locNames);
            _allValidChecks.Clear();
            _allValidChecks.AddRange(locNames);
            PostSave();
        } catch(Exception e) {
            Plugin.BepinLogger.LogError(e);
        }
    }


    ////// Private API //////

    private void ProcessItem(ItemInfo item) {
        if(!_receivedItemCounts.ContainsKey(item.ItemName))
            _receivedItemCounts[item.ItemName] = 0;
        _receivedItemCounts[item.ItemName]++;

        if(SaveFileRedirect.Instance.ReceiveNotifs
            && (
                (item.LocationId != -2
                    && (SaveFileRedirect.Instance.OnlyActiveDrops <= 0f || Time.unscaledTime - ArchipelagoClient.Instance.LastConnectTime > SaveFileRedirect.Instance.OnlyActiveDrops))
                || (item.LocationId == -2
                    && !SaveFileRedirect.Instance.NoStartingInventory)
               ))
            _itemNotifsToProcess.Enqueue(item.ItemName);

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
                SkillTreeItemizer.Instance.RescanAll();
                break;
            case string trapTest when trapTest.StartsWith("Trap: "):
                TrapHandler.Instance.QueueTrap(item.ItemName[6..]);
                break;
        }

        _lastSavedIndex++;

        Plugin.BepinLogger.LogMessage($"Received item {item.ItemName}, total count now {_receivedItemCounts[item.ItemName]}");
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
}
