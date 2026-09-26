using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archskipelagill.ItemsAndLocations;
using BepInEx;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archskipelagill.ArchipelagoCompat;

public class DeathLinkHandler : IDisposable {

    ////// Nested Data Types //////

    public enum DeathLinkTx { Off, Receive, Send, Both }
    public enum DeathLinkType { RandomTrap, Kill, EndRun }


    ////// Initializer/Fields/Properties //////

    private static bool _deathLinkEnabled;
    private readonly string _slotName;
    private readonly DeathLinkService _service;
    private bool _responding = false;
    private readonly Queue<DeathLink> _deathLinks = new();

    /// <summary>
    /// instantiates our death link handler, sets up the hook for receiving death links, and enables death link if needed
    /// </summary>
    /// <param name="deathLinkService">The new DeathLinkService that our handler will use to send and
    /// receive death links</param>
    /// <param name="enableDeathLink">Whether we should enable death link or not on startup</param>
    public DeathLinkHandler(DeathLinkService deathLinkService, string name, bool enableDeathLink = false) {
        _service = deathLinkService;
        _service.OnDeathLinkReceived += DeathLinkReceived;
        On.CharaStats.Update += On_CharaStats_Update;
        On.mainCameraScript.playerDeath += On_MainCameraScript_playerDeath;
        On.endMenuManager.returnToMenu += On_EndMenuManager_returnToMenu;
        _slotName = name;
        _deathLinkEnabled = enableDeathLink;

        if(_deathLinkEnabled) {
            _service.EnableDeathLink();
        }
    }

    ////// MonoMod Hooks //////
    #region MonoMod Hooks
    private void On_EndMenuManager_returnToMenu(On.endMenuManager.orig_returnToMenu orig, endMenuManager self) {
        orig(self);

        if(Plugin.Instance.DeathLinkQuitIsDeath && (Plugin.Instance.DeathLinkTx == DeathLinkTx.Send || Plugin.Instance.DeathLinkTx == DeathLinkTx.Both)) {
            var cs = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
            if(!cs.won && !cs.dead)
                SendDeathLink();
        }
    }

    private void On_MainCameraScript_playerDeath(On.mainCameraScript.orig_playerDeath orig, mainCameraScript self) {
        orig(self);
        if(Plugin.Instance.DeathLinkTx == DeathLinkTx.Send || Plugin.Instance.DeathLinkTx == DeathLinkTx.Both) {
            var cs = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
            if(!cs.won && cs.dead)
                SendDeathLink();
        }
    }

    private void On_CharaStats_Update(On.CharaStats.orig_Update orig, CharaStats self) {
        orig(self);

        if(self.metaMenu) return;

        if(self.dead)
            _responding = false;
        else
            KillPlayer(self);
    }
    #endregion


    ////// Public API //////

    /// <summary>
    /// enables/disables death link
    /// </summary>
    public void ToggleDeathLink() {
        _deathLinkEnabled = !_deathLinkEnabled;

        if(_deathLinkEnabled) {
            _service.EnableDeathLink();
        } else {
            _service.DisableDeathLink();
        }
    }

    /// <summary>
    /// can be called when in a valid state to kill the player, dequeueing and immediately killing the player with a
    /// message if we have a death link in the queue
    /// </summary>
    public void KillPlayer(CharaStats targetPlayer) {
        try {
            if(_responding || _deathLinks.Count < 1) return;

            _responding = true;

            var deathLink = _deathLinks.Dequeue();
            var cause = deathLink.Cause.IsNullOrWhiteSpace() ? GetDeathLinkCause(deathLink) : deathLink.Cause;

            TrapHandler.Instance.CreateTrapNotif("trap-deathlink", 10f);

            switch(Plugin.Instance.DeathLinkType) {
                case DeathLinkType.EndRun:
                    GameObject.Find("PlayerCharacter/Main Camera/Canvas/endMenu").GetComponent<endMenuManager>().returnToMenu();
                    _responding = false;
                    break;
                case DeathLinkType.RandomTrap:
                    TrapHandler.Instance.TriggerTrap(new string[] { "Damage", "Pull Enemies", "Weapon Jam", "Drain Ski", "Scramble Stats", "Flash Mob", "Stronger Enemies" }[UnityEngine.Random.Range(0, 7)]);
                    _responding = false;
                    break;
                default:
                    targetPlayer.HP = -9001f; //make the player EXCEPTIONALLY dead
                    //don't clear responding-to-deathlink flag until after death actually happens because it isn't instant
                    break;
            }


            Plugin.BepinLogger.LogMessage(cause);
        } catch(Exception e) {
            Plugin.BepinLogger.LogError(e);
        }
    }

    /// <summary>
    /// called to send a death link to the multiworld
    /// </summary>
    public void SendDeathLink() {
        try {
            if(_responding || !_deathLinkEnabled) return;

            Plugin.BepinLogger.LogMessage("Sharing your death...");

            // add the cause here
            var linkToSend = new DeathLink(_slotName);

            _service.SendDeathLink(linkToSend);
        } catch(Exception e) {
            Plugin.BepinLogger.LogError(e);
        }
    }

    public void Dispose() {
        _service.OnDeathLinkReceived -= DeathLinkReceived;
        On.CharaStats.Update -= On_CharaStats_Update;
        On.mainCameraScript.playerDeath -= On_MainCameraScript_playerDeath;
    }


    ////// Private API //////

    /// <summary>
    /// what happens when we receive a deathLink
    /// </summary>
    /// <param name="deathLink">Received Death Link object to handle</param>
    private void DeathLinkReceived(DeathLink deathLink) {
        if(Plugin.Instance.DeathLinkTx == DeathLinkTx.Receive || Plugin.Instance.DeathLinkTx == DeathLinkTx.Both)
            _deathLinks.Enqueue(deathLink);

        Plugin.BepinLogger.LogDebug(deathLink.Cause.IsNullOrWhiteSpace()
            ? $"Received Death Link from: {deathLink.Source}"
            : deathLink.Cause);
    }

    /// <summary>
    /// returns message for the player to see when a death link is received without a cause
    /// </summary>
    /// <param name="deathLink">death link object to get relevant info from</param>
    /// <returns></returns>
    private string GetDeathLinkCause(DeathLink deathLink) {
        return $"Received death from {deathLink.Source}";
    }
}