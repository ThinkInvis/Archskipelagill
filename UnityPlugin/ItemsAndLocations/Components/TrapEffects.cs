using UnityEngine;

namespace Archskipelagill.ItemsAndLocations.Components;


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
    protected override float duration => TrapHandler.Instance.SpeedTrapDuration;
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
            var adjFactor = 1f + (1f - (timer.t - startTime) / duration) * TrapHandler.Instance.SpeedTrapStrength;
            walker.speed = startingSpeed * adjFactor;
            walker.runSpeed = startingRunSpeed * adjFactor;
            walker.acceleration = startingAccel * adjFactor;
        }
    }
}
public class WeaponJamTrap:TimedTrapBase {
    protected override float duration => TrapHandler.Instance.JamTrapDuration;
}
#pragma warning restore IDE0051
