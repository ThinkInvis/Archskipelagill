using Archskipelagill.ArchipelagoCompat;
using Archskipelagill.ItemsAndLocations.Components;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;

namespace Archskipelagill.ItemsAndLocations;

public class TrapHandler : Module<TrapHandler> {

    ////// Initializer/Fields/Properties //////

    public float TrapInterval => _cfgTrapInterval.Value;
    public float DamageTrapStrength => _cfgDamageTrapStrength.Value;
    public float SpeedTrapDuration => _cfgSpeedTrapDuration.Value;
    public float SpeedTrapStrength => _cfgSpeedTrapStrength.Value;
    public float JamTrapDuration => _cfgJamTrapDuration.Value;
    public float DrainSkiTrapStrength => _cfgDrainSkiTrapStrength.Value;
    public float MobTrapStrength => _cfgMobTrapStrength.Value;
    public float SpawnTimeTrapStrength => _cfgSpawnTimeTrapStrength.Value;

    private readonly ConfigEntry<float> _cfgTrapInterval;
    private readonly ConfigEntry<float> _cfgDamageTrapStrength;
    private readonly ConfigEntry<float> _cfgSpeedTrapDuration;
    private readonly ConfigEntry<float> _cfgSpeedTrapStrength;
    private readonly ConfigEntry<float> _cfgJamTrapDuration;
    private readonly ConfigEntry<float> _cfgDrainSkiTrapStrength;
    private readonly ConfigEntry<float> _cfgMobTrapStrength;
    private readonly ConfigEntry<float> _cfgSpawnTimeTrapStrength;

    private float _lastTrapTime = 0f;
    private readonly Queue<string> _queuedTraps = [];

    public TrapHandler() {
        On.timerScript.Update += On_TimerScript_Update;
        On.timerScript.Start += On_TimerScript_Start;
        On.CharaStats.Update += On_CharaStats_Update;

        _cfgTrapInterval = Plugin.Instance.MainConfig.Bind<float>(new ConfigDefinition("Difficulty", "Trap Interval"), 15f, new ConfigDescription("How much mid-run time to wait between activating queued traps. Traps will not activate while the game is paused or on the menu.", new AcceptableValueRange<float>(0f, 300f)));
        _cfgDamageTrapStrength = Plugin.Instance.MainConfig.Bind<float>(new ConfigDefinition("Difficulty", "Damage Trap Strength"), 0.5f, new ConfigDescription("Fraction of health in damage dealt by Trap: Damage.", new AcceptableValueRange<float>(0f, 1f)));
        _cfgSpeedTrapDuration = Plugin.Instance.MainConfig.Bind<float>(new ConfigDefinition("Difficulty", "Pull Enemies Trap Duration"), 5f, new ConfigDescription("Duration of Trap: Pull Enemies in seconds.", new AcceptableValueRange<float>(0f, 300f)));
        _cfgSpeedTrapStrength = Plugin.Instance.MainConfig.Bind<float>(new ConfigDefinition("Difficulty", "Pull Enemies Trap Strength"), 2f, new ConfigDescription("Strength of Trap: Pull Enemies as an added multiplier to base speed.", new AcceptableValueRange<float>(0f, 100f)));
        _cfgJamTrapDuration = Plugin.Instance.MainConfig.Bind<float>(new ConfigDefinition("Difficulty", "Weapon Jam Trap Duration"), 10f, new ConfigDescription("Duration of Trap: Weapon Jam in seconds.", new AcceptableValueRange<float>(0f, 180f)));
        _cfgDrainSkiTrapStrength = Plugin.Instance.MainConfig.Bind<float>(new ConfigDefinition("Difficulty", "Drain Ski Trap Strength"), 0.5f, new ConfigDescription("Fraction of current Ski removed by Trap: Drain Ski.", new AcceptableValueRange<float>(0f, 1f)));
        _cfgMobTrapStrength = Plugin.Instance.MainConfig.Bind<float>(new ConfigDefinition("Difficulty", "Flash Mob Trap Strength"), 30f, new ConfigDescription("Additional enemies spawned by Trap: Flash Mob.", new AcceptableValueRange<float>(0f, 1000f)));
        _cfgSpawnTimeTrapStrength = Plugin.Instance.MainConfig.Bind<float>(new ConfigDefinition("Difficulty", "Stronger Enemies Trap Strength"), 60f, new ConfigDescription("Time added to the monster wave strength timer by Trap: Stronger Enemies.", new AcceptableValueRange<float>(0f, 300f)));
    }


    ////// MonoMod Hooks //////
    #region MonoMod Hooks
    private void On_TimerScript_Start(On.timerScript.orig_Start orig, timerScript self) {
        orig(self);
        _lastTrapTime = 0f;
    }

    private void On_TimerScript_Update(On.timerScript.orig_Update orig, timerScript self) {
        orig(self);
        if(_queuedTraps.Count == 0) return;
        if((self.t - _lastTrapTime) > _cfgTrapInterval.Value) {
            _lastTrapTime = self.t;
            TriggerTrap(_queuedTraps.Dequeue());
        }
    }

    private void On_CharaStats_Update(On.CharaStats.orig_Update orig, CharaStats self) {
        orig(self);
        if(self.GetComponent<WeaponJamTrap>())
            self.effectiveATKSPEED = -99f;
    }
    #endregion

    ////// Public API //////

    public void QueueTrap(string trapName) {
        _queuedTraps.Enqueue(trapName);
    }

    public void CreateTrapNotif(string trapSpriteName, float lifetime) {
        var trapNotif = new GameObject("Trap Notification");
        trapNotif.SetActive(false);
        trapNotif.transform.parent = GameObject.FindGameObjectWithTag("MainCamera").transform.Find("Canvas");
        trapNotif.layer = 5;
        trapNotif.transform.localPosition = new(16f, 0f, 0f);
        var trapSprite = trapNotif.AddComponent<SpriteRenderer>();
        trapSprite.sprite = Plugin.Resources.LoadAsset<Sprite>($"Assets/Textures/{trapSpriteName}.png");
        var tnc = trapNotif.AddComponent<TrapNotification>();
        tnc.Lifetime = lifetime;
        var tac = trapNotif.AddComponent<AudioSource>();
        tac.clip = Plugin.Resources.LoadAsset<AudioClip>("Assets/Sounds/archi_trap_activate.wav");
        tac.volume = 1.3f * PlayerPrefs.GetFloat("SFXvol");
        tac.pitch = UnityEngine.Random.Range(0.95f, 1.15f);
        trapNotif.SetActive(true);
        tac.Play();
        trapNotif.transform.localScale = new(1f, 1f, 1f);
    }

    public void TriggerTrap(string trapName) {
        var cs = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        var hb = cs.transform.Find("Hitbox").GetComponent<playerHitbox>();
        var spw = GameObject.FindGameObjectWithTag("Spawner").GetComponent<MonsterSpawner>();

        string trapSpriteName = "trap-base";
        float lifetime = 5f;

        switch(trapName) {
            case "Damage":
                cs.HP *= DamageTrapStrength;
                hb.hurtSFX.PlayHurtSFX();
                var hurtNotif = UnityEngine.Object.Instantiate<GameObject>(hb.damageTakenNotif, hb.transform.position, Quaternion.identity);
                hurtNotif.GetComponent<Rigidbody2D>().AddForce(Vector2.up * 300f);
                var dsp = hurtNotif.GetComponent<displayDamage>();
                dsp.damage = (int)(cs.HP / 10f);
                dsp.fond.enabled = false;
                dsp.virgule.enabled = true;
                dsp.color = new Color(0.57254905f, 0.07450981f, 0.10980392f);
                hb.shake.dur = 0.35f;
                hb.shake.amp = 0.75f;
                trapSpriteName = "trap-damage";
                break;
            case "Pull Enemies":
                foreach(var enemy in GameObject.FindGameObjectsWithTag("Monster")) {
                    if(!enemy.TryGetComponent<WalkerScript>(out _))
                        continue;
                    if(enemy.TryGetComponent<MonsterSpeedupTrap>(out var msuTrap))
                        msuTrap.Reset();
                    else
                        enemy.AddComponent<MonsterSpeedupTrap>();
                }
                lifetime = SpeedTrapDuration;
                trapSpriteName = "trap-pull";
                break;
            case "Weapon Jam":
                if(cs.TryGetComponent<WeaponJamTrap>(out var wjTrap))
                    wjTrap.Reset();
                else
                    cs.gameObject.AddComponent<WeaponJamTrap>();
                lifetime = JamTrapDuration;
                trapSpriteName = "trap-jam";
                break;
            case "Drain Ski":
                var penalty = cs.XP * DrainSkiTrapStrength;
                cs.XP -= penalty;
                cs.totalXP -= penalty;
                trapSpriteName = "trap-drainski";
                break;
            case "Scramble Stats":
                (cs.INT, cs.STR, cs.DEX) = (cs.STR, cs.DEX, cs.INT);
                cs.statsUI.transform.GetChild(0).GetChild(0).GetComponent<numberDisplayer>()
.display((int)cs.STR);
                cs.statsUI.transform.GetChild(1).GetChild(0).GetComponent<numberDisplayer>()
                    .display((int)cs.DEX);
                cs.statsUI.transform.GetChild(2).GetChild(0).GetComponent<numberDisplayer>()
                    .display((int)cs.INT);
                cs.bringStatUIDown(0);
                cs.bringStatUIDown(1);
                cs.bringStatUIDown(2);
                trapSpriteName = "trap-scramble";
                break;
            case "Flash Mob":
                spw.spawnAmountOverflow += MobTrapStrength;
                trapSpriteName = "trap-flashmob";
                break;
            case "Stronger Enemies":
                spw.t += SpawnTimeTrapStrength;
                trapSpriteName = "trap-enemytime";
                break;
            default:
                Plugin.BepinLogger.LogWarning($"Triggered unrecognized trap with name \"{trapName}\"");
                break;
        }

        CreateTrapNotif(trapSpriteName, lifetime);
    }
}
