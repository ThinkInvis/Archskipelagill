using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Archskipelagill.UX;
using BepInEx.Configuration;
using System;
using System.Linq;
using System.Threading;

namespace Archskipelagill.ArchipelagoCompat;

public class ArchipelagoClient : Module<ArchipelagoClient> {

    ////// Initializer/Fields/Properties //////
    
    public const string AP_VERSION = "0.6.7";
    private const string _GAME_NAME = "Skigill";

    public bool Authenticated {get; private set;} = false;
    public float LastConnectTime {get; private set;} = 0f;
    public string AutoConnectHostname => _cfgAutoConnectHostname.Value;
    public string AutoConnectSlot => _cfgAutoConnectSlot.Value;
    public string AutoConnectPassword => _cfgAutoConnectPassword.Value;

    private ArchipelagoSession _session = null;
    private DeathLinkHandler _deathLinkHandler = null;
    private ArchipelagoConnectionInfo _serverData = new();
    private bool _attemptingConnection = false;
    private bool _disconnecting = false;
    private ArchipelagoConnectionInfo? _lastSlotDataReceived = null;
    private string _seed = null;
    private ConfigEntry<string> _cfgAutoConnectHostname;
    private ConfigEntry<string> _cfgAutoConnectSlot;
    private ConfigEntry<string> _cfgAutoConnectPassword;


    ////// Public API //////
    
    /// <summary>
    /// Call to connect to an Archipelago session with the given connection info.
    /// </summary>
    /// <returns></returns>
    public void Connect(string hostname, string slot, string password) {
        if(Authenticated || _attemptingConnection) return;

        try {
            _serverData = new(hostname, slot, password);
            _session = ArchipelagoSessionFactory.CreateSession(_serverData.uri);
            SetupSession();
        } catch(Exception e) {
            Plugin.BepinLogger.LogError(e);
        }

        TryConnect();
    }

    public void AutoConnect() {
        _cfgAutoConnectHostname = Plugin.Instance.MainConfig.Bind<string>(new ConfigDefinition("Autoconnect", "Hostname"), "", new ConfigDescription("Which hostname to use when attempting autoconnect on game launch. Autoconnect will only be attempted if this setting is not a blank string."));
        _cfgAutoConnectSlot = Plugin.Instance.MainConfig.Bind<string>(new ConfigDefinition("Autoconnect", "Slot name"), "", new ConfigDescription("Which slot name to use when attempting autoconnect on game launch. Autoconnect will only be attempted if this setting is not a blank string."));
        _cfgAutoConnectPassword = Plugin.Instance.MainConfig.Bind<string>(new ConfigDefinition("Autoconnect", "Password"), "", new ConfigDescription("Which password to use when attempting autoconnect on game launch."));

        if(AutoConnectHostname != "" && AutoConnectSlot != "") {
            Connect(AutoConnectHostname, AutoConnectSlot, AutoConnectPassword);
        }
    }

    /// <summary>
    /// Something went wrong, or we need to properly disconnect from the server; cleanup and re-null session.
    /// </summary>
    public void Disconnect() {
        if(_disconnecting) return;
        _disconnecting = true;
        Plugin.BepinLogger.LogDebug("disconnecting from server...");
        var task = _session?.Socket.DisconnectAsync();
        _session.Socket.SocketClosed -= OnSessionSocketClosed;
        _session.MessageLog.OnMessageReceived -= OnMessageReceived;
        _session.Items.ItemReceived -= OnItemReceived;
        _session.Socket.ErrorReceived -= OnSessionErrorReceived;
        _deathLinkHandler?.Dispose();
        _session = null;
        Authenticated = false;
        _disconnecting = false;
        MainMenuInjector.Instance.OnDisconnect();
    }

    public void SendMessage(string message) {
        _session.Socket.SendPacketAsync(new SayPacket { Text = message });
    }

    public void CheckLocationsByName(params string[] names) {
        var unsentNames = names.Except(ArchipelagoSaver.Instance.SentChecks).Distinct().ToList();
        if(unsentNames.Count() == 0) return;
        Plugin.BepinLogger.LogMessage($"Attempting checks: {string.Join(", ", unsentNames.Select(n => '"' + n + '"'))}");
        if(_session == null) {
            Plugin.BepinLogger.LogWarning($"Offline, can't send; queueing");
            ArchipelagoSaver.Instance.ReceiveUnsentChecks([.. unsentNames]);
            return;
        }
        RunLocationCheck([.. unsentNames.Where(n => _session.Locations.AllMissingLocations.Contains(_session.Locations.GetLocationIdFromName("Skigill", n)))]);
    }


    ////// Private API //////
    
    /// <summary>
    /// Add handlers for Archipelago events.
    /// </summary>
    private void SetupSession() {
        _session.MessageLog.OnMessageReceived += OnMessageReceived;
        _session.Items.ItemReceived += OnItemReceived;
        _session.Socket.ErrorReceived += OnSessionErrorReceived;
        _session.Socket.SocketClosed += OnSessionSocketClosed;
    }

    /// <summary>
    /// attempt to connect to the server with our connection info
    /// </summary>
    private void TryConnect() {
        try {
            // it's safe to thread this function call but unity notoriously hates threading so do not use excessively
            ThreadPool.QueueUserWorkItem(
                _ => HandleConnectResult(
                    _session.TryConnectAndLogin(
                        _GAME_NAME,
                        _serverData.slotName,
                        ItemsHandlingFlags.AllItems,
                        new Version(AP_VERSION),
                        password: _serverData.password,
                        requestSlotData: !_lastSlotDataReceived.HasValue || !_lastSlotDataReceived.Value.Equals(_serverData)
                    )));
        } catch(Exception e) {
            Plugin.BepinLogger.LogError(e);
            HandleConnectResult(new LoginFailure(e.ToString()));
            _attemptingConnection = false;
        }
    }

    /// <summary>
    /// Handle the connection result and do things.
    /// </summary>
    /// <param name="result"></param>
    private void HandleConnectResult(LoginResult result) {
        string outText;
        if(result.Successful) {
            var success = (LoginSuccessful)result;

            _seed = _session.RoomState.Seed;
            Authenticated = true;

            outText = $"Successfully connected to {_serverData.uri} as {_serverData.slotName}!";

            LastConnectTime = UnityEngine.Time.unscaledTime;

            MainMenuInjector.Instance.OnConnect();
            MainMenuInjector.Instance.ReceiveMessage(outText);

            ArchipelagoSaver.Instance.StoreSlotData(success.SlotData, _session);
            _lastSlotDataReceived = _serverData;
            _deathLinkHandler = new(_session.CreateDeathLinkService(), _serverData.slotName, Plugin.Instance.DeathLinkTx != DeathLinkHandler.DeathLinkTx.Off);
            ArchipelagoSaver.Instance.ResendChecks();
        } else {
            var failure = (LoginFailure)result;
            outText = $"Failed to connect to {_serverData.uri} as {_serverData.slotName}.";
            outText = failure.Errors.Aggregate(outText, (current, error) => current + $"\n    {error}");

            Plugin.BepinLogger.LogError(outText);

            Authenticated = false;
            Disconnect();
            MainMenuInjector.Instance.OnDisconnect();
        }

        MainMenuInjector.Instance.ReceiveMessage(outText);
        _attemptingConnection = false;
    }

    private void OnMessageReceived(LogMessage message) {
        MainMenuInjector.Instance.ReceiveMessage(message.ToString());
    }

    /// <summary>
    /// we received an item so reward it here
    /// </summary>
    /// <param name="helper">item helper which we can grab our item from</param>
    private void OnItemReceived(ReceivedItemsHelper helper) {
        var receivedItem = helper.DequeueItem();

        ArchipelagoSaver.Instance.ReceiveArchiItem(receivedItem, helper.Index);
    }

    /// <summary>
    /// something went wrong with our socket connection
    /// </summary>
    /// <param name="e">thrown exception from our socket</param>
    /// <param name="message">message received from the server</param>
    private void OnSessionErrorReceived(Exception e, string message) {
        Plugin.BepinLogger.LogError(e);
        MainMenuInjector.Instance.ReceiveMessage(message);
    }

    /// <summary>
    /// something went wrong closing our connection. disconnect and clean up
    /// </summary>
    /// <param name="reason"></param>
    private void OnSessionSocketClosed(string reason) {
        Plugin.BepinLogger.LogError($"Connection to Archipelago lost: {reason}");
        Disconnect();
    }
    
    private async void RunLocationCheck(params string[] unsentNames) {
        try {
            await _session.Locations.CompleteLocationChecksAsync([.. unsentNames.Select(n => _session.Locations.GetLocationIdFromName("Skigill", n))]);
        } catch(Exception ex) {
            Plugin.BepinLogger.LogError("Failed to send checks:");
            Plugin.BepinLogger.LogError(ex);
            ArchipelagoSaver.Instance.ReceiveUnsentChecks([.. unsentNames]);
            return;
        }
        ArchipelagoSaver.Instance.ReceiveSentChecks([.. unsentNames]);

        var goalType = Int64.Parse(ArchipelagoSaver.Instance.metaProg["archi_goal"]);
        string[] validGoals = goalType switch {
            0 => ["Defeated Gari", "Defeated Bouboul", "Defeated Jello", "Defeated Roger", "Defeated Pilpou", "Defeated Rosa"],
            2 => ["I'm The Boss Now"],
            3 => ["Defeated Gari on Difficulty 7", "Defeated Bouboul on Difficulty 7", "Defeated Jello on Difficulty 7", "Defeated Roger on Difficulty 7", "Defeated Pilpou on Difficulty 7", "Defeated Rosa on Difficulty 7"],
            4 => ["Defeated Final Boss on Difficulty 7"],
            5 => ["I'm The Boss Now on Difficulty 7"],
            _ => ["Defeated Final Boss"]
        };
        if(unsentNames.Intersect(validGoals).Any()) {
            Plugin.BepinLogger.LogMessage("Goal!!!");
            _session.SetGoalAchieved();
            ArchiSendController.CreateSend(true);
        }
    }
}