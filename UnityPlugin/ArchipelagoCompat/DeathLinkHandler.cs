using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archskipelagill.Itemizers;
using BepInEx;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archskipelagill.ArchipelagoCompat;

public class DeathLinkHandler : IDisposable {
    private static bool deathLinkEnabled;
    private readonly string slotName;
    private readonly DeathLinkService service;
    bool _responding = false;
    private readonly Queue<DeathLink> deathLinks = new();
    public enum DeathLinkTx { Off, Receive, Send, Both }
    public enum DeathLinkType { RandomTrap, Kill, EndRun }

    /// <summary>
    /// instantiates our death link handler, sets up the hook for receiving death links, and enables death link if needed
    /// </summary>
    /// <param name="deathLinkService">The new DeathLinkService that our handler will use to send and
    /// receive death links</param>
    /// <param name="enableDeathLink">Whether we should enable death link or not on startup</param>
    public DeathLinkHandler(DeathLinkService deathLinkService, string name, bool enableDeathLink = false) {
        service = deathLinkService;
        service.OnDeathLinkReceived += DeathLinkReceived;
        On.CharaStats.Update += CharaStats_Update;
        On.mainCameraScript.playerDeath += MainCameraScript_playerDeath;
        On.endMenuManager.returnToMenu += EndMenuManager_returnToMenu;
        slotName = name;
        deathLinkEnabled = enableDeathLink;

        if(deathLinkEnabled) {
            service.EnableDeathLink();
        }
    }

    private void EndMenuManager_returnToMenu(On.endMenuManager.orig_returnToMenu orig, endMenuManager self) {
        orig(self);

        if(Plugin.instance.cfgDeathLinkQuitIsDeath.Value && (Plugin.instance.cfgDeathLinkTx.Value == DeathLinkTx.Send || Plugin.instance.cfgDeathLinkTx.Value == DeathLinkTx.Both)) {
            var cs = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
            if(!cs.won && !cs.dead)
                SendDeathLink();
        }
    }

    private void MainCameraScript_playerDeath(On.mainCameraScript.orig_playerDeath orig, mainCameraScript self) {
        orig(self);
        if(Plugin.instance.cfgDeathLinkTx.Value == DeathLinkTx.Send || Plugin.instance.cfgDeathLinkTx.Value == DeathLinkTx.Both)
            SendDeathLink();
    }

    /// <summary>
    /// enables/disables death link
    /// </summary>
    public void ToggleDeathLink() {
        deathLinkEnabled = !deathLinkEnabled;

        if(deathLinkEnabled) {
            service.EnableDeathLink();
        } else {
            service.DisableDeathLink();
        }
    }

    /// <summary>
    /// what happens when we receive a deathLink
    /// </summary>
    /// <param name="deathLink">Received Death Link object to handle</param>
    private void DeathLinkReceived(DeathLink deathLink) {
        if(Plugin.instance.cfgDeathLinkTx.Value == DeathLinkTx.Receive || Plugin.instance.cfgDeathLinkTx.Value == DeathLinkTx.Both)
            deathLinks.Enqueue(deathLink);

        Plugin.BepinLogger.LogDebug(deathLink.Cause.IsNullOrWhiteSpace()
            ? $"Received Death Link from: {deathLink.Source}"
            : deathLink.Cause);
    }

    private void CharaStats_Update(On.CharaStats.orig_Update orig, CharaStats self) {
        orig(self);

        if(self.metaMenu) return;

        if(self.dead)
            _responding = false;
        else
            KillPlayer(self);
    }

    /// <summary>
    /// can be called when in a valid state to kill the player, dequeueing and immediately killing the player with a
    /// message if we have a death link in the queue
    /// </summary>
    public void KillPlayer(CharaStats targetPlayer) {
        try {
            if(_responding || deathLinks.Count < 1) return;

            _responding = true;

            var deathLink = deathLinks.Dequeue();
            var cause = deathLink.Cause.IsNullOrWhiteSpace() ? GetDeathLinkCause(deathLink) : deathLink.Cause;

            Plugin.instance.trapHandler.CreateTrapNotif("trap-deathlink", 10f);

            switch(Plugin.instance.cfgDeathLinkType.Value) {
                case DeathLinkType.EndRun:
                    GameObject.Find("PlayerCharacter/Main Camera/Canvas/endMenu").GetComponent<endMenuManager>().returnToMenu();
                    _responding = false;
                    break;
                case DeathLinkType.RandomTrap:
                    Plugin.instance.trapHandler.TriggerTrap(new string[] { "Damage", "Pull Enemies", "Weapon Jam", "Drain Ski", "Scramble Stats", "Flash Mob", "Stronger Enemies" }[UnityEngine.Random.Range(0, 7)] );
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
    /// returns message for the player to see when a death link is received without a cause
    /// </summary>
    /// <param name="deathLink">death link object to get relevant info from</param>
    /// <returns></returns>
    private string GetDeathLinkCause(DeathLink deathLink) {
        return $"Received death from {deathLink.Source}";
    }

    /// <summary>
    /// called to send a death link to the multiworld
    /// </summary>
    public void SendDeathLink() {
        try {
            if(_responding || !deathLinkEnabled) return;

            Plugin.BepinLogger.LogMessage("sharing your death...");

            // add the cause here
            var linkToSend = new DeathLink(slotName);

            service.SendDeathLink(linkToSend);
        } catch(Exception e) {
            Plugin.BepinLogger.LogError(e);
        }
    }

    public void Dispose() {
        service.OnDeathLinkReceived -= DeathLinkReceived;
        On.CharaStats.Update -= CharaStats_Update;
        On.mainCameraScript.playerDeath -= MainCameraScript_playerDeath;
    }
}