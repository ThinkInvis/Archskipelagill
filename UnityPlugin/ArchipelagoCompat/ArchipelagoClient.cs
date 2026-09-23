using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Archskipelagill.EffectComponents;
using BepInEx.Configuration;
using System;
using System.Linq;
using System.Threading;

namespace Archskipelagill.ArchipelagoCompat;

public class ArchipelagoClient {
    public const string APVersion = "0.6.7";
    private const string Game = "Skigill";

    public static bool Authenticated;
    private bool attemptingConnection;
    public static float lastConnectTime = 0f;

    public static ArchipelagoData ServerData = new();
    private DeathLinkHandler DeathLinkHandler;
    internal ArchipelagoSession session;

    public ConfigEntry<string> cfgAutoConnectHostname;
    public ConfigEntry<string> cfgAutoConnectSlot;
    public ConfigEntry<string> cfgAutoConnectPassword;

    /// <summary>
    /// call to connect to an Archipelago session. Connection info should already be set up on ServerData
    /// </summary>
    /// <returns></returns>
    public void Connect() {
        if(Authenticated || attemptingConnection) return;

        try {
            session = ArchipelagoSessionFactory.CreateSession(ServerData.Uri);
            SetupSession();
        } catch(Exception e) {
            Plugin.BepinLogger.LogError(e);
        }

        TryConnect();
    }

    public void AutoConnect() {
        cfgAutoConnectHostname = Plugin.instance.config.Bind<string>(new ConfigDefinition("Autoconnect", "Hostname"), "", new ConfigDescription("Which hostname to use when attempting autoconnect on game launch. Autoconnect will only be attempted if this setting is not a blank string."));
        cfgAutoConnectSlot = Plugin.instance.config.Bind<string>(new ConfigDefinition("Autoconnect", "Slot name"), "", new ConfigDescription("Which slot name to use when attempting autoconnect on game launch. Autoconnect will only be attempted if this setting is not a blank string."));
        cfgAutoConnectPassword = Plugin.instance.config.Bind<string>(new ConfigDefinition("Autoconnect", "Password"), "", new ConfigDescription("Which password to use when attempting autoconnect on game launch."));

        if(cfgAutoConnectHostname.Value != "" && cfgAutoConnectSlot.Value != "") {
            ServerData.Uri = cfgAutoConnectHostname.Value;
            ServerData.SlotName = cfgAutoConnectSlot.Value;
            ServerData.Password = cfgAutoConnectPassword.Value;
            Connect();
        }
    }

    /// <summary>
    /// add handlers for Archipelago events
    /// </summary>
    private void SetupSession() {
        session.MessageLog.OnMessageReceived += OnMessageReceived;
        session.Items.ItemReceived += OnItemReceived;
        session.Socket.ErrorReceived += OnSessionErrorReceived;
        session.Socket.SocketClosed += OnSessionSocketClosed;
    }

    /// <summary>
    /// attempt to connect to the server with our connection info
    /// </summary>
    private void TryConnect() {
        try {
            // it's safe to thread this function call but unity notoriously hates threading so do not use excessively
            ThreadPool.QueueUserWorkItem(
                _ => HandleConnectResult(
                    session.TryConnectAndLogin(
                        Game,
                        ServerData.SlotName,
                        ItemsHandlingFlags.AllItems,
                        new Version(APVersion),
                        password: ServerData.Password,
                        requestSlotData: ServerData.NeedSlotData
                    )));
        } catch(Exception e) {
            Plugin.BepinLogger.LogError(e);
            HandleConnectResult(new LoginFailure(e.ToString()));
            attemptingConnection = false;
        }
    }

    /// <summary>
    /// handle the connection result and do things
    /// </summary>
    /// <param name="result"></param>
    private void HandleConnectResult(LoginResult result) {
        string outText;
        if(result.Successful) {
            var success = (LoginSuccessful)result;

            ServerData.SetupSession(success.SlotData, session.RoomState.Seed);
            Authenticated = true;

            outText = $"Successfully connected to {ServerData.Uri} as {ServerData.SlotName}!";

            lastConnectTime = UnityEngine.Time.unscaledTime;

            Plugin.instance.mainMenuInjector.OnConnect();
            Plugin.instance.mainMenuInjector.ReceiveMessage(outText);

            Plugin.BepinLogger.LogMessage($"Pre begin SSD on {ArchiSaver.instance}");
            ArchiSaver.instance.StoreSlotData(session.DataStorage.GetSlotData(), session);
            DeathLinkHandler = new(session.CreateDeathLinkService(), ServerData.SlotName, Plugin.instance.cfgDeathLinkTx.Value != DeathLinkHandler.DeathLinkTx.Off);
            ArchiSaver.instance.ResendChecks();
        } else {
            var failure = (LoginFailure)result;
            outText = $"Failed to connect to {ServerData.Uri} as {ServerData.SlotName}.";
            outText = failure.Errors.Aggregate(outText, (current, error) => current + $"\n    {error}");

            Plugin.BepinLogger.LogError(outText);

            Authenticated = false;
            Disconnect();
            Plugin.instance.mainMenuInjector.OnDisconnect();
        }

        Plugin.instance.mainMenuInjector.ReceiveMessage(outText);
        attemptingConnection = false;
    }

    bool disconnecting = false;
    /// <summary>
    /// something went wrong, or we need to properly disconnect from the server. cleanup and re null our session
    /// </summary>
    public void Disconnect() {
        if(disconnecting) return;
        disconnecting = true;
        Plugin.BepinLogger.LogDebug("disconnecting from server...");
        var task = session?.Socket.DisconnectAsync();
        session.Socket.SocketClosed -= OnSessionSocketClosed;
        session.MessageLog.OnMessageReceived -= OnMessageReceived;
        session.Items.ItemReceived -= OnItemReceived;
        session.Socket.ErrorReceived -= OnSessionErrorReceived;
        DeathLinkHandler?.Dispose();
        session = null;
        Authenticated = false;
        disconnecting = false;
        Plugin.instance.mainMenuInjector.OnDisconnect();
    }

    public void SendMessage(string message) {
        session.Socket.SendPacketAsync(new SayPacket { Text = message });
    }

    private void OnMessageReceived(LogMessage message) {
        Plugin.instance.mainMenuInjector.ReceiveMessage(message.ToString());
        Plugin.BepinLogger.LogMessage($"[APMsg] {message}");
    }

    /// <summary>
    /// we received an item so reward it here
    /// </summary>
    /// <param name="helper">item helper which we can grab our item from</param>
    private void OnItemReceived(ReceivedItemsHelper helper) {
        var receivedItem = helper.DequeueItem();

        if(helper.Index <= ArchiSaver.instance.lastReceivedIndex) return;

        ArchiSaver.instance.ReceiveArchiItem(receivedItem);
    }

    /// <summary>
    /// something went wrong with our socket connection
    /// </summary>
    /// <param name="e">thrown exception from our socket</param>
    /// <param name="message">message received from the server</param>
    private void OnSessionErrorReceived(Exception e, string message) {
        Plugin.BepinLogger.LogError(e);
        Plugin.instance.mainMenuInjector.ReceiveMessage(message);
    }

    /// <summary>
    /// something went wrong closing our connection. disconnect and clean up
    /// </summary>
    /// <param name="reason"></param>
    private void OnSessionSocketClosed(string reason) {
        Plugin.BepinLogger.LogError($"Connection to Archipelago lost: {reason}");
        Disconnect();
    }

    public void CheckLocationsByName(params string[] names) {
        var unsentNames = names.Except(ArchiSaver.instance.sentChecks).Distinct().ToList();
        if(unsentNames.Count() == 0) return;
        Plugin.BepinLogger.LogMessage($"Attempting checks: {string.Join(", ", unsentNames.Select(n => '"' + n + '"'))}");
        if(session == null) {
            Plugin.BepinLogger.LogWarning($"Offline, can't send; queueing");
            ArchiSaver.instance.ReceiveUnsentChecks([.. unsentNames]);
            return;
        }
        unsentNames = [.. unsentNames.Where(n => session.Locations.AllMissingLocations.Contains(session.Locations.GetLocationIdFromName("Skigill", n)))];
        session.Locations.CompleteLocationChecks([.. unsentNames.Select(n => session.Locations.GetLocationIdFromName("Skigill", n))]);
        ArchiSaver.instance.ReceiveSentChecks([.. unsentNames]);
        var slotData = session.DataStorage.GetSlotData();

        var goalType = Int64.Parse(ArchiSaver.instance.metaProg["archi_goal"]);
        string[] validGoals = goalType switch {
            0 => ["Defeated Gari", "Defeated Bouboul", "Defeated Jello", "Defeated Roger", "Defeated Pilpou", "Defeated Rosa"],
            2 => ["I'm The Boss Now"],
            3 => ["Defeated Gari on Difficulty 7", "Defeated Bouboul on Difficulty 7", "Defeated Jello on Difficulty 7", "Defeated Roger on Difficulty 7", "Defeated Pilpou on Difficulty 7", "Defeated Rosa on Difficulty 7"],
            4 => ["Defeated Final Boss on Difficulty 7"],
            5 => ["I'm The Boss Now on Difficulty 7"],
            _ => ["Defeated Final Boss"]
        };
        if(names.Intersect(validGoals).Any()) {
            Plugin.BepinLogger.LogMessage("Goal!!!");
            session.SetGoalAchieved();
            ArchiSendController.CreateSend(true);
        }
    }
}