using UnityEngine;

namespace Archskipelagill.EffectComponents;

public class ArchiDropController : MonoBehaviour {
    public static GameObject CreateDrop(string itemName) {
        var obj = new GameObject("ArchiDrop");

        var ctrl = obj.AddComponent<ArchiDropController>();
        ctrl.itemName = itemName;

        var spriteMtl = GameObject.FindGameObjectWithTag("Player").transform.Find("Skins/Mage").GetComponent<SpriteRenderer>().material;

        for(var i = 0; i < 6; i++) {
            var spinner = new GameObject("Spinner");
            spinner.transform.parent = obj.transform;
            var spr = spinner.AddComponent<SpriteRenderer>();
            spr.material = spriteMtl;
            spr.sprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/archi-big-single.png");
            spr.drawMode = SpriteDrawMode.Sliced;
            spr.size *= 0.16f;
        }

        var sfx = obj.AddComponent<AudioSource>();
        sfx.clip = Plugin.resources.LoadAsset<AudioClip>("Assets/Sounds/archi_item_fall.wav");
        sfx.volume = 0f;
        ctrl.basePitch = UnityEngine.Random.Range(0.9f, 1.1f);
        sfx.pitch = ctrl.basePitch * Time.timeScale;
        sfx.Play();

        var sfx2 = obj.AddComponent<AudioSource>();
        sfx2.clip = Plugin.resources.LoadAsset<AudioClip>($"Assets/Sounds/archi_{(itemName.StartsWith("Trap: ") ? "trap" : "item")}_arrive.wav");
        sfx2.volume = PlayerPrefs.GetFloat("SFXvol") * 1.3f;
        sfx2.pitch = Time.timeScale;

        var follower = new GameObject("Follower");
        follower.transform.parent = obj.transform;
        follower.SetActive(false);
        var spr2 = follower.AddComponent<SpriteRenderer>();
        spr2.material = spriteMtl;
        spr2.sprite = Plugin.resources.LoadAsset<Sprite>($"Assets/Textures/item-{
            itemName switch {
                string str when str.StartsWith("Character: ") => "character",
                string str when str.StartsWith("Weapon: ") => "weapon",
                string str when str.StartsWith("Trap: ") => "trap",
                string str when str.StartsWith("Skigill Region: ") => "key",
                "Final Boss Key" => "key",
                "Progressive Difficulty" => "key",
                "Bonus Gill" => "gill",
                _ => "unknown"
            }}.png");
        spr2.drawMode = SpriteDrawMode.Sliced;
        spr2.size *= 0.16f;

        var sparkler = obj.AddComponent<instantiateRepeat>();
        sparkler.GO = Plugin.instance.resourceGrabber.worldLayerSparklePrefab.transform;
        sparkler.radius = 0f;
        sparkler.randomRot = false;
        sparkler.rate = 0.03f;
        sparkler.makeChild = false;

        return obj;
    }

    public string itemName;
    CharaStats chara;
    Vector3 posStart;
    Vector3 posTarget;
    AudioSource sfx;
    AudioSource sfx2;
    Transform[] spinners;
    Vector3[] spinnerV;
    bool landed = false;
    float basePitch;
    float itemTimer = 0f;
    Transform follower;
    Vector3 followerOffset;
    instantiateRepeat sparkler;

#pragma warning disable IDE0051 //Used by Unity Engine
    void Start() {
        sfx = GetComponents<AudioSource>()[0];
        sfx2 = GetComponents<AudioSource>()[1];
        sparkler = GetComponent<instantiateRepeat>();
        spinners = new Transform[6];
        spinnerV = new Vector3[6];
        follower = transform.GetChild(6);
        chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        if(!chara.metaMenu) {
            posTarget = chara.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 1.75f;
            posStart = posTarget + new Vector3(0f, 10f, 10f) + (Vector3)UnityEngine.Random.insideUnitCircle * 3.5f;
            transform.localScale *= 5f;
            for(var i = 0; i < 6; i++) {
                spinners[i] = transform.GetChild(i);
                spinnerV[i] = (UnityEngine.Random.onUnitSphere + new Vector3(0f, 1.5f, 1.5f)) * UnityEngine.Random.Range(2f, 4f);
            }
            followerOffset = UnityEngine.Random.onUnitSphere * 1f + new Vector3(0f, 2f, 5f);
        } else {
            posTarget = chara.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 0.35f;
            posStart = posTarget + new Vector3(0f, 2f, 2f) + (Vector3)UnityEngine.Random.insideUnitCircle * 0.7f;
            transform.localScale *= 0.5f;
            for(var i = 0; i < 6; i++) {
                spinners[i] = transform.GetChild(i);
                spinnerV[i] = (UnityEngine.Random.onUnitSphere + new Vector3(0f, 1.5f, 1.5f)) * UnityEngine.Random.Range(0.4f, 0.8f);
            }
            followerOffset = UnityEngine.Random.onUnitSphere * 0.1f + new Vector3(0f, 0.2f, 0.5f);
        }
        transform.position = posStart;
    }

    void Update() {
        sfx.pitch = basePitch * Time.timeScale;
        sfx2.pitch = Time.timeScale;
        if(!landed) {
            if(sfx.time <= 0.85f) {
                sfx.volume = (sfx.time / 0.85f) * 1.3f * PlayerPrefs.GetFloat("SFXvol");
                transform.position = posStart + (sfx.time / 0.85f) * (posTarget - posStart);
                var phase = sfx.time * 6f * Mathf.PI;
                for(var i = 0; i < spinners.Length; i++) {
                    var iphase = i / 3f * Mathf.PI;
                    spinners[i].transform.localPosition = new(Mathf.Cos(phase + iphase) * 0.08f, Mathf.Sin(phase + iphase) * 0.08f, Mathf.Sin(phase + iphase) * 0.08f);
                }
            } else {
                landed = true;
                sfx2.Play();
                for(var i = 0; i < 8; i++)
                    sparkler.insto();
                GameObject.Destroy(sparkler);
            }
        } else {
            if(itemTimer == 0f) {
                follower.localScale = Vector3.zero;
                follower.gameObject.SetActive(true);
            }

            transform.position = posTarget;
            for(var i = 0; i < spinners.Length; i++) {
                spinners[i].transform.position += spinnerV[i] * Time.deltaTime;
                spinners[i].transform.localScale *= 1f - Time.deltaTime * 2f;
                spinnerV[i] += new Vector3(0, (!chara.metaMenu ? -25f : -1f) * Time.deltaTime, (!chara.metaMenu ? -25f : -1f) * Time.deltaTime);
            }

            var fadeInFac = Mathf.Min(itemTimer / 0.5f, 1f);
            follower.localScale = new Vector3(fadeInFac, fadeInFac, fadeInFac);

            follower.position += Time.deltaTime * 2f * (chara.transform.position + followerOffset - follower.position);

            if(itemTimer > 4f) {
                follower.gameObject.SetActive((itemTimer % 0.25f) > 0.125f);
            }

            itemTimer += Time.deltaTime;
            if(itemTimer > 5f)
                Destroy(gameObject);
        }
    }
#pragma warning restore IDE0051
}
