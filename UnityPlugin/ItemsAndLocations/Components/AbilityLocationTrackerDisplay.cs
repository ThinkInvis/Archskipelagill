using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Archskipelagill.ItemsAndLocations.Components;

public class AbilityLocationTrackerDisplay:MonoBehaviour {

    ////// Initializer/Fields/Properties //////
    
    Transform[] _spinners;
    CharaStats _chara;
    public bool IsLocked {get; set;} = false;


    ////// Unity Engine API //////
#pragma warning disable IDE0051 //Used by Unity Engine
    void Awake() {
        var icon = this.transform.Find("GameObject/icon");
        if(icon == null) icon = this.transform;
        _chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        _spinners = new Transform[6];
        for(var i = 0; i < 6; i++) {
            var spinner = new GameObject("Spinner");
            spinner.transform.parent = icon;
            var spr = spinner.AddComponent<SpriteRenderer>();
            spr.sprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/archi-big-single.png");
            spr.drawMode = SpriteDrawMode.Sliced;
            spr.size *= 0.5f;
            spr.gameObject.layer = 5;
            spr.sortingOrder = 2;
            spinner.transform.localScale = new(1f, 1f, 1f);
            _spinners[i] = spinner.transform;
        }
    }

    void Update() {
        var phase = Time.unscaledTime * 0.5f * Mathf.PI;
        for(var i = 0; i < _spinners.Length; i++) {
            var iphase = i / 3f * Mathf.PI;
            _spinners[i].transform.localPosition = new(Mathf.Cos(phase + iphase) * 1f, Mathf.Sin(phase + iphase) * 1f, -2f);
            if(_chara.won) {
                _spinners[i].GetComponent<SpriteRenderer>().color = new(0f, 1f, 0f);
                _spinners[i].transform.localPosition *= 2f;
            } else if(IsLocked)
                _spinners[i].GetComponent<SpriteRenderer>().color = new(0.2f, 0.2f, 0.2f);
            else
                _spinners[i].GetComponent<SpriteRenderer>().color = new(1f, 1f, 1f);
        }
    }
#pragma warning restore IDE0051
}
