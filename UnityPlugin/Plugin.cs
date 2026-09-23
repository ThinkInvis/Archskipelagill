using Archskipelagill.ArchipelagoCompat;
using Archskipelagill.Itemizers;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using static Archskipelagill.ArchipelagoCompat.DeathLinkHandler;

namespace Archskipelagill;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class Plugin:BaseUnityPlugin {
    public const string PluginGUID = "com.ThinkInvisible.Archskipelagill";
    public const string PluginName = "Archskipelagill";
    public const string PluginVersion = "1.0.0";
    public static Plugin instance { get; private set; }

    internal static AssetBundle resources;

    public ConfigFile config { get; private set; }
    public ConfigEntry<DeathLinkTx> cfgDeathLinkTx;
    public ConfigEntry<DeathLinkType> cfgDeathLinkType;
    public ConfigEntry<bool> cfgDeathLinkQuitIsDeath;

    public const string ModDisplayInfo = $"{PluginName} v{PluginVersion}";
    public const string APDisplayInfo = $"Archipelago v{ArchipelagoClient.APVersion}";
    public static ManualLogSource BepinLogger;
    public static ArchipelagoClient ArchipelagoClient;
    public VanillaBugfixes vanillaBugfixes;
    public ResourceGrabber resourceGrabber;
    public MainMenuInjector mainMenuInjector;
    public SkillTreeItemizer skillTreeItemizer;
    public RoundEndItemizer roundEndItemizer;
    public AbilityUnlockItemizer abilityUnlockItemizer;
    public CustomSaveLoad customSaveLoad;
    public TrapHandler trapHandler;

#pragma warning disable IDE0051 //Used by Unity Engine
    private void Awake() {
        if(instance != null) {
            Logger.LogFatal("Duplicate plugin instance registered, this shouldn't be possible!");
        }
        instance = this;

        // Plugin startup logic
        BepinLogger = Logger;

        using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Archskipelagill.archskipelagill-assets")) {
            resources = AssetBundle.LoadFromStream(stream);
        }

        config = new(Path.Combine(Paths.ConfigPath, PluginGUID + ".cfg"), true);

        ArchipelagoClient = new ArchipelagoClient();

        vanillaBugfixes = new();
        resourceGrabber = new();
        mainMenuInjector = new();
        skillTreeItemizer = new();
        roundEndItemizer = new();
        abilityUnlockItemizer = new();
        customSaveLoad = new();
        trapHandler = new();

        cfgDeathLinkTx = config.Bind<DeathLinkTx>(new ConfigDefinition("Death Link", "Channel"), DeathLinkTx.Off, new ConfigDescription("Whether to receive and/or transmit Death Link to other players in the Archipelago run."));
        cfgDeathLinkType = config.Bind<DeathLinkType>(new ConfigDefinition("Death Link", "Type"), DeathLinkType.Kill, new ConfigDescription("What to do when a Death Link is received. Note that End Run is more punishing than Kill: no Gill reward will be received."));
        cfgDeathLinkQuitIsDeath = config.Bind<bool>(new ConfigDefinition("Death Link", "Death on Quit"), true, new ConfigDescription("If true, ending a run from the pause menu will count as a death for Death Link purposes."));

        mainMenuInjector.ReceiveMessage($"{ModDisplayInfo} loaded!");

        ArchipelagoClient.AutoConnect();
    }

    private void OnApplicationQuit() {
        ArchipelagoClient.Disconnect();
    }

#if DEBUG
    private void OnGUI() {
        GUI.BeginGroup(new Rect(Screen.width - 332, 200, 332, Screen.height - 200));
        // show the mod is currently loaded in the corner
        GUI.Label(new Rect(16, 16, 300, 20), ModDisplayInfo);

        string statusMessage;
        // show the Archipelago Version and whether we're connected or not
        if(ArchipelagoClient.Authenticated) {
            statusMessage = " Status: Connected";
            GUI.Label(new Rect(16, 50, 300, 20), APDisplayInfo + statusMessage);
        } else {
            statusMessage = " Status: Disconnected";
            GUI.Label(new Rect(16, 50, 300, 20), APDisplayInfo + statusMessage);
            GUI.Label(new Rect(16, 70, 150, 20), "Host: ");
            GUI.Label(new Rect(16, 90, 150, 20), "Player Name: ");
            GUI.Label(new Rect(16, 110, 150, 20), "Password: ");

            ArchipelagoClient.ServerData.Uri = GUI.TextField(new Rect(150, 70, 150, 20),
                ArchipelagoClient.ServerData.Uri);
            ArchipelagoClient.ServerData.SlotName = GUI.TextField(new Rect(150, 90, 150, 20),
                ArchipelagoClient.ServerData.SlotName);
            ArchipelagoClient.ServerData.Password = GUI.TextField(new Rect(150, 110, 150, 20),
                ArchipelagoClient.ServerData.Password);

            // requires that the player at least puts *something* in the slot name
            if(GUI.Button(new Rect(16, 130, 100, 20), "Connect") &&
                !ArchipelagoClient.ServerData.SlotName.IsNullOrWhiteSpace()) {
                ArchipelagoClient.Connect();
            }
        }

        // a bunch of debug buttons
        if(GUI.Button(new Rect(16, 210, 200, 20), "DEBUG: Scrape Game Data")) {
            SkillTree.ScrapeSkillTree();
        }
        if(GUI.Button(new Rect(16, 240, 200, 20), "DEBUG: Interrupt Connection")) {
            ArchipelagoClient.Disconnect();
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
            var slotData = ArchipelagoClient.session.DataStorage.GetSlotData();
            var goalType = (Int64)slotData["goal_type"];
            string targetGoal = goalType switch {
                0 => "Defeated Gari",
                2 => "I'm The Boss Now",
                3 => "Defeated Gari on Difficulty 7",
                4 => "Defeated Final Boss on Difficulty 7",
                5 => "I'm The Boss Now on Difficulty 7",
                _ => "Defeated Final Boss"
            };
            ArchipelagoClient.CheckLocationsByName(targetGoal);
        }
        if(GUI.Button(new Rect(16, 390, 200, 20), "DEBUG: Test send notif")) {
            ArchiSaver.instance.sendNotifsToProcess++;
        }
        if(GUI.Button(new Rect(16, 420, 200, 20), "DEBUG: Test receive notif (key item)")) {
            ArchiSaver.instance.itemNotifsToProcess.Enqueue("Skigill Region: Mage");
        }
        GUI.EndGroup();
    }
#endif
#pragma warning restore IDE0051
}