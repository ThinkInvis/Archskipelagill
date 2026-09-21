using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Archskipelagill.EffectComponents;

public class ArchiSendController : MonoBehaviour {
    public static GameObject CreateSend(bool goal = false) {
        var obj = new GameObject("ArchiSend");

        var ctrl = obj.AddComponent<ArchiSendController>();
        var chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        ctrl.transform.position = chara.transform.position;
        ctrl.goal = goal;

        for(var i = 0; i < 6; i++) {
            var spinner = new GameObject("Spinner");
            spinner.transform.parent = obj.transform;
            var spr = spinner.AddComponent<SpriteRenderer>();
            spr.material = GameObject.FindGameObjectWithTag("Player").transform.Find("Skins/Mage").GetComponent<SpriteRenderer>().material;
            spr.sprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/archi-big-single.png");
            spr.drawMode = SpriteDrawMode.Sliced;
            spr.size *= 0.16f;
        }

        var sfx = obj.AddComponent<AudioSource>();
        sfx.clip = Plugin.resources.LoadAsset<AudioClip>("Assets/Sounds/archi_send.wav");
        sfx.volume = PlayerPrefs.GetFloat("SFXvol") * 1.3f;
        ctrl.basePitch = UnityEngine.Random.Range(0.9f, 1.1f);
        sfx.pitch = ctrl.basePitch * Time.timeScale;
        sfx.Play();

        return obj;
    }

    public string itemName;
    CharaStats chara;
    Vector3 v;
    AudioSource sfx;
    float basePitch;
    Transform[] spinners;
    bool goal;

#pragma warning disable IDE0051 //Used by Unity Engine
    void Start() {
        sfx = GetComponent<AudioSource>();
        spinners = new Transform[6];
        chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        for(var i = 0; i < 6; i++) {
            spinners[i] = transform.GetChild(i);
        }
        if(!chara.metaMenu) {
            v = new(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-0.25f, 0.6f));
            transform.localScale *= 5f;
        } else {
            v = new(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-0.05f, 0.125f));
            transform.localScale *= 0.5f;
        }
        if(goal) {
            transform.localScale *= 3f;
            basePitch *= 0.5f;
            v *= 0.5f;
        }
    }

    void Update() {
        if(sfx.isPlaying) {
            sfx.pitch = basePitch * Time.timeScale;
            sfx.volume = (1f - sfx.time / sfx.clip.length) * 1.3f * PlayerPrefs.GetFloat("SFXvol");
            transform.position += v * Time.deltaTime;
            v += new Vector3(0, (!chara.metaMenu ? 5f : 1f) * Time.deltaTime, (!chara.metaMenu ? 5f : 1f) * Time.deltaTime) * (goal ? 0.2f : 1f);
            var phase = Mathf.Pow(sfx.time + 1f, 1.2f) * 1f * Mathf.PI;
            var phaseb = Mathf.Pow(sfx.time + 1f, 1.2f) * 0.384f * Mathf.PI;
            for(var i = 0; i < spinners.Length; i++) {
                var iphase = i / 3f * Mathf.PI;
                spinners[i].transform.localPosition = new(Mathf.Cos(phase + iphase) * 0.08f, Mathf.Sin(phase + iphase) * 0.08f, Mathf.Sin(phase + iphase) * 0.08f);
                if(goal)
                    spinners[i].GetComponent<SpriteRenderer>().color = Color.HSVToRGB((phaseb + iphase * 0.25f) % 1f, 1f, 1f);
            }
            transform.localScale *= 1f - Time.deltaTime * 0.5f;
        } else {
            Destroy(gameObject);
        }
    }
#pragma warning restore IDE0051
}
