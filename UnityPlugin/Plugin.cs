using Archskipelagill.Archipelago;
using Archskipelagill.Itemizers;
using Archskipelagill.Utils;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archskipelagill;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class Plugin:BaseUnityPlugin {
    public const string PluginGUID = "com.ThinkInvisible.Archskipelagill";
    public const string PluginName = "Archskipelagill";
    public const string PluginVersion = "1.0.0";
    public static Plugin instance { get; private set; }

    public ConfigFile config { get; private set; }
    public ConfigEntry<string> cfgRunSuffix;

    public const string ModDisplayInfo = $"{PluginName} v{PluginVersion}";
    private const string APDisplayInfo = $"Archipelago v{ArchipelagoClient.APVersion}";
    public static ManualLogSource BepinLogger;
    public static ArchipelagoClient ArchipelagoClient;
    public SkillTreeItemizer skillTreeItemizer;
    public RoundEndItemizer roundEndItemizer;
    public AbilityUnlockItemizer abilityUnlockItemizer;
    public CustomSaveLoad customSaveLoad;

#pragma warning disable IDE0051 //Used by Unity Engine
    private void Awake() {
        if(instance != null) {
            Logger.LogFatal("Duplicate plugin instance registered, this shouldn't be possible!");
        }
        instance = this;

        // Plugin startup logic
        BepinLogger = Logger;
        config = new(Path.Combine(Paths.ConfigPath, PluginGUID + ".cfg"), true);

        cfgRunSuffix = config.Bind<string>(new ConfigDefinition("Save/Load", "Run Suffix"), "default", new ConfigDescription("A suffix added to the custom save file redirect used by the client plugin. Must be changed if you want to participate in multiple Archipelago runs including this game simultaneously."));

        ArchipelagoClient = new ArchipelagoClient();
        ArchipelagoConsole.Awake();

        skillTreeItemizer = new();
        roundEndItemizer = new();
        abilityUnlockItemizer = new();
        customSaveLoad = new();

        ArchipelagoConsole.LogMessage($"{ModDisplayInfo} loaded!");
    }

    private void OnApplicationQuit() {
        ArchipelagoClient.Disconnect();
    }

    private void Update() {
        if(confirmWipeSaveMode) {
            confirmWipeSaveTimer += Time.deltaTime;
            if(confirmWipeSaveTimer >= 20f) {
                confirmWipeSaveMode = false;
                confirmWipeSaveTimer = 0f;
            }
        }
    }
    float confirmWipeSaveTimer = 0f;
    bool confirmWipeSaveMode = false;
    private void OnGUI() {
        
        GUI.BeginGroup(new Rect(Screen.width - 332, 200, 332, Screen.height - 200));
        // show the mod is currently loaded in the corner
        GUI.Label(new Rect(16, 16, 300, 20), ModDisplayInfo);
        ArchipelagoConsole.OnGUI();

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

        var wipeStr = "! Reset Save File !";
        bool allowWipe = false;
        if(confirmWipeSaveMode) {
            if(confirmWipeSaveTimer < 5f) {
                wipeStr = $"! Reset - ARE YOU SURE? Wait {(5f - confirmWipeSaveTimer):n0}s... !";
            } else {
                wipeStr = $"! Reset - Click again to confirm !";
                allowWipe = true;
            }
        }
        if(SceneManager.GetActiveScene().name == "menu") {
            if(GUI.Button(new Rect(16, 180, 300, 20), wipeStr)) {
                if(confirmWipeSaveMode) {
                    if(allowWipe) {
                        customSaveLoad.Wipe();
                        confirmWipeSaveTimer = 0f;
                        confirmWipeSaveMode = false;
                    }
                } else {
                    confirmWipeSaveMode = true;
                    confirmWipeSaveTimer = 0f;
                }
            }
        }
        // a bunch of debug buttons
#if DEBUG
        if(GUI.Button(new Rect(16, 210, 200, 20), "DEBUG: Bake Skill Tree Map")) {
            SkillTree.BuildAndExportMaps();
        }
        if(GUI.Button(new Rect(16, 240, 200, 20), "DEBUG: Interrupt Connection")) {
            ArchipelagoClient.Disconnect();
        }
        if(GUI.Button(new Rect(16, 270, 200, 20), "DEBUG: Cheat: Lodsemone")) {
            var cs = GameObject.Find("PlayerCharacter").GetComponent<CharaStats>();
            cs.XP += 1000;
            cs.XPtoDisplay += 1000;
            cs.totalXP += 1000;
        }
        if(GUI.Button(new Rect(16, 300, 200, 20), "DEBUG: Cheat: Regen")) {
            var cs = GameObject.Find("PlayerCharacter").GetComponent<CharaStats>();
            cs.HPREGEN += 10000;
        }
        if(GUI.Button(new Rect(16, 330, 200, 20), "DEBUG: Cheat: Skip Time")) {
            var ts = GameObject.Find("Main Camera/Canvas/timer").GetComponent<timerScript>();
            ts.t = ts.endTime - 10f;
        }
#endif
        GUI.EndGroup();
    }
#pragma warning restore IDE0051
}