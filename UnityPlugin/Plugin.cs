using Archskipelagill.ArchipelagoCompat;
using Archskipelagill.ItemsAndLocations;
using Archskipelagill.UX;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using static Archskipelagill.ArchipelagoCompat.DeathLinkHandler;

namespace Archskipelagill;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin:BaseUnityPlugin {

    ////// Initializer/Fields/Properties //////
    
    public const string PLUGIN_GUID = "com.ThinkInvisible.Archskipelagill";
    public const string PLUGIN_NAME = "Archskipelagill";
    public const string PLUGIN_VERSION = "1.0.0";

    public const string MOD_DISPLAY_INFO = $"{PLUGIN_NAME} v{PLUGIN_VERSION}";
    public const string AP_DISPLAY_INFO = $"Archipelago v{ArchipelagoClient.AP_VERSION}";

    public static ManualLogSource BepinLogger { get; private set; }
    public static Plugin Instance { get; private set; }
    public ConfigFile MainConfig { get; private set; }
    internal static AssetBundle Resources { get; private set; }

    public DeathLinkTx DeathLinkTx => _cfgDeathLinkTx.Value;
    public DeathLinkType DeathLinkType => _cfgDeathLinkType.Value;
    public bool DeathLinkQuitIsDeath => _cfgDeathLinkQuitIsDeath.Value;

    private ConfigEntry<DeathLinkTx> _cfgDeathLinkTx;
    private ConfigEntry<DeathLinkType> _cfgDeathLinkType;
    private ConfigEntry<bool> _cfgDeathLinkQuitIsDeath;


    ////// Unity Engine API //////
#pragma warning disable IDE0051 //Used by Unity Engine
    private void Awake() {
        if(Instance != null) {
            Logger.LogFatal("Duplicate plugin instance registered, this shouldn't be possible!");
        }
        Instance = this;

        // Plugin startup logic
        BepinLogger = Logger;

        using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Archskipelagill.archskipelagill-assets")) {
            Resources = AssetBundle.LoadFromStream(stream);
        }

        MainConfig = new(Path.Combine(Paths.ConfigPath, PLUGIN_GUID + ".cfg"), true);

        Module.InitAllModules();

        _cfgDeathLinkTx = MainConfig.Bind<DeathLinkTx>(new ConfigDefinition("Death Link", "Channel"), DeathLinkTx.Off, new ConfigDescription("Whether to receive and/or transmit Death Link to other players in the Archipelago run."));
        _cfgDeathLinkType = MainConfig.Bind<DeathLinkType>(new ConfigDefinition("Death Link", "Type"), DeathLinkType.Kill, new ConfigDescription("What to do when a Death Link is received. Note that End Run is more punishing than Kill: no Gill reward will be received."));
        _cfgDeathLinkQuitIsDeath = MainConfig.Bind<bool>(new ConfigDefinition("Death Link", "Death on Quit"), true, new ConfigDescription("If true, ending a run from the pause menu will count as a death for Death Link purposes."));

        MainMenuInjector.Instance.ReceiveMessage($"{MOD_DISPLAY_INFO} loaded!");

        ArchipelagoClient.Instance.AutoConnect();
    }

    private void OnApplicationQuit() {
        ArchipelagoClient.Instance.Disconnect();
    }

#if DEBUG
    string _dbgUri, _dbgSlotName, _dbgPassword = "";
    private void OnGUI() {
        GUI.BeginGroup(new Rect(Screen.width - 332, 200, 332, Screen.height - 200));
        // show the mod is currently loaded in the corner
        GUI.Label(new Rect(16, 16, 300, 20), MOD_DISPLAY_INFO);

        string statusMessage;
        // show the Archipelago Version and whether we're connected or not
        if(ArchipelagoClient.Instance.Authenticated) {
            statusMessage = " Status: Connected";
            GUI.Label(new Rect(16, 50, 300, 20), AP_DISPLAY_INFO + statusMessage);
        } else {
            statusMessage = " Status: Disconnected";
            GUI.Label(new Rect(16, 50, 300, 20), AP_DISPLAY_INFO + statusMessage);
            GUI.Label(new Rect(16, 70, 150, 20), "Host: ");
            GUI.Label(new Rect(16, 90, 150, 20), "Player Name: ");
            GUI.Label(new Rect(16, 110, 150, 20), "Password: ");

            _dbgUri = GUI.TextField(new Rect(150, 70, 150, 20), _dbgUri);
            _dbgSlotName = GUI.TextField(new Rect(150, 90, 150, 20), _dbgSlotName);
            _dbgPassword = GUI.TextField(new Rect(150, 110, 150, 20), _dbgPassword);

            // requires that the player at least puts *something* in the slot name
            if(GUI.Button(new Rect(16, 130, 100, 20), "Connect")) {
                ArchipelagoClient.Instance.Connect(_dbgUri, _dbgSlotName, _dbgPassword);
            }
        }

        // a bunch of debug buttons
        if(GUI.Button(new Rect(16, 210, 200, 20), "DEBUG: Scrape Game Data")) {
            GameDataAccess.GameData.ScrapeSkillTree();
        }
        if(GUI.Button(new Rect(16, 240, 200, 20), "DEBUG: Interrupt Connection")) {
            ArchipelagoClient.Instance.Disconnect();
        }
        if(GUI.Button(new Rect(16, 270, 200, 20), "DEBUG: Cheat: Lodsemone")) {
            var cs = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
            cs.XP += 1000;
            cs.XPtoDisplay += 1000;
            cs.totalXP += 1000;
        }
        if(GUI.Button(new Rect(16, 300, 200, 20), "DEBUG: Cheat: Regen")) {
            var cs = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
            cs.HPREGEN += 10000;
        }
        if(GUI.Button(new Rect(16, 330, 200, 20), "DEBUG: Cheat: Skip Time")) {
            var ts = GameObject.Find("Main Camera/Canvas/timer").GetComponent<timerScript>();
            ts.t = ts.endTime - 10f;
        }
        if(GUI.Button(new Rect(16, 360, 200, 20), "DEBUG: Cheat: Test Goal")) {
            var goalType = Int64.Parse(ArchipelagoSaver.Instance.metaProg["archi_goal"]);
            string targetGoal = goalType switch {
                0 => "Defeated Gari",
                2 => "I'm The Boss Now",
                3 => "Defeated Gari on Difficulty 7",
                4 => "Defeated Final Boss on Difficulty 7",
                5 => "I'm The Boss Now on Difficulty 7",
                _ => "Defeated Final Boss"
            };
            ArchipelagoClient.Instance.CheckLocationsByName(targetGoal);
        }
        if(GUI.Button(new Rect(16, 390, 200, 20), "DEBUG: Test send notif")) {
            var fld = typeof(ArchipelagoSaver).GetField("_sendNotifsToProcess", BindingFlags.Instance | BindingFlags.NonPublic);
            fld.SetValue(ArchipelagoSaver.Instance, (int)fld.GetValue(ArchipelagoSaver.Instance) + 1);
        }
        if(GUI.Button(new Rect(16, 420, 200, 20), "DEBUG: Test receive notif (key item)")) {
            var q = typeof(ArchipelagoSaver).GetField("_itemNotifsToProcess", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ArchipelagoSaver.Instance) as Queue<string>;
            q.Enqueue("Skigill Region: Mage");
        }
        GUI.EndGroup();
    }
#endif
#pragma warning restore IDE0051
}