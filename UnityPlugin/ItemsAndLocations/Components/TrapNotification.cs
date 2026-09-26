using System.Collections.Generic;
using UnityEngine;

namespace Archskipelagill.ItemsAndLocations.Components;

public class TrapNotification:MonoBehaviour {

    ////// Initializer/Fields/Properties //////
    
    public float Lifetime { get; set; } = 5f;

    float _t = 0f;
    int _state = 0;

    static readonly List<TrapNotification> _instances = [];


    ////// Unity Engine API //////
#pragma warning disable IDE0051 //Used by Unity Engine
    void Awake() {
        _instances.Add(this);
    }

    void OnDestroy() {
        _instances.Remove(this);
    }

    void Update() {
        _t += Time.deltaTime;
        var pY = _instances.Count * 1f - _instances.IndexOf(this) * 2f;
        var tY = transform.localPosition.y + Time.deltaTime * 2f * (pY - transform.localPosition.y);

        switch(_state) {
            case 2:
                transform.localPosition = new(16f + 3f * (_t - Lifetime) / 0.5f, tY, 0f);
                if(_t >= Lifetime)
                    GameObject.Destroy(gameObject);
                break;
            case 1:
                transform.localPosition = new(13f, tY, 0f);
                if(_t >= Lifetime - 0.5f) _state++;
                break;
            case 0:
                transform.localPosition = new(13f + 3f * (1f - _t / 0.5f), tY, 0f);
                if(_t >= 0.5f) _state++;
                break;
        }
    }
#pragma warning restore IDE0051
}