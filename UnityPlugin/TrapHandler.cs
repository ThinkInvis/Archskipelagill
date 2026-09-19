using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Archskipelagill;

public class TrapHandler {
    float lastTrapTime = 0f;

    public TrapHandler() {
        On.timerScript.Update += TimerScript_Update;
        On.timerScript.Start += TimerScript_Start;
        On.CharaStats.Update += CharaStats_Update;
    }

    private void TimerScript_Start(On.timerScript.orig_Start orig, timerScript self) {
        orig(self);
        lastTrapTime = 0f;
    }

    private void TimerScript_Update(On.timerScript.orig_Update orig, timerScript self) {
        orig(self);
        if(ArchiSaver.instance.queuedTraps.Count == 0) return;
        if((self.t - lastTrapTime) > Plugin.instance.cfgTrapInterval.Value) {
            lastTrapTime = self.t;
            var trapName = ArchiSaver.instance.queuedTraps.Dequeue();

            var cs = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
            var hb = cs.transform.Find("Hitbox").GetComponent<playerHitbox>();
            var spw = GameObject.FindGameObjectWithTag("Spawner").GetComponent<MonsterSpawner>();

            switch(trapName) {
                case "Damage":
                    Plugin.BepinLogger.LogMessage($"Damage Trap triggered, current HP halved!");
                    cs.HP *= 0.5f;
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
                    break;
                case "Weapon Jam":
                    if(cs.TryGetComponent<WeaponJamTrap>(out var wjTrap))
                        wjTrap.Reset();
                    else
                        cs.gameObject.AddComponent<WeaponJamTrap>();
                    break;
                case "Drain Ski":
                    var penalty = cs.XP * 0.5f;
                    cs.XP -= penalty;
                    cs.totalXP -= penalty;
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
                    break;
                case "Flash Mob":
                    spw.spawnAmountOverflow += Plugin.instance.cfgMobTrapStrength.Value;
                    break;
                case "Stronger Enemies":
                    spw.t += Plugin.instance.cfgSpawnTimeTrapStrength.Value;
                    break;
                default:
                    Plugin.BepinLogger.LogWarning($"Triggered unrecognized trap with name \"{trapName}\"");
                    break;
            }
        }
    }

    private void CharaStats_Update(On.CharaStats.orig_Update orig, CharaStats self) {
        orig(self);
        if(self.GetComponent<WeaponJamTrap>())
            self.effectiveATKSPEED = -99f;
    }

#pragma warning disable IDE0051 //Used by Unity Engine
    public abstract class TimedTrapBase:MonoBehaviour {
        protected abstract float duration { get; }
        protected float startTime;
        protected timerScript timer;
        protected bool timeUp = false;

        protected virtual void Start() {
            timer = GameObject.FindGameObjectWithTag("Timer").GetComponent<timerScript>();
            startTime = timer.t;
        }

        protected virtual void Update() {
            if((timer.t - startTime) > duration) {
                timeUp = true;
                GameObject.Destroy(this);
            }
        }

        public void Reset() {
            startTime = timer.t;
        }
    }
    public class MonsterSpeedupTrap:TimedTrapBase {
        protected override float duration => Plugin.instance.cfgSpeedTrapDuration.Value;
        float startingSpeed;
        float startingRunSpeed;
        float startingAccel;
        WalkerScript walker;
        protected override void Start() {
            base.Start();
            walker = GetComponent<WalkerScript>();
            timer = GameObject.FindGameObjectWithTag("Timer").GetComponent<timerScript>();
            startingSpeed = walker.speed;
            startingAccel = walker.acceleration;
            startingRunSpeed = walker.runSpeed;
        }

        protected override void Update() {
            base.Update();
            if(timeUp) {
                walker.speed = startingSpeed;
                walker.runSpeed = startingRunSpeed;
                walker.acceleration = startingAccel;
                GameObject.Destroy(this);
            } else {
                var adjFactor = 1f + (1f - (timer.t - startTime) / duration) * Plugin.instance.cfgSpeedTrapStrength.Value;
                walker.speed = startingSpeed * adjFactor;
                walker.runSpeed = startingRunSpeed * adjFactor;
                walker.acceleration = startingAccel * adjFactor;
            }
        }
    }
    public class WeaponJamTrap:TimedTrapBase {
        protected override float duration => Plugin.instance.cfgJamTrapDuration.Value;
    }
#pragma warning restore IDE0051
}
