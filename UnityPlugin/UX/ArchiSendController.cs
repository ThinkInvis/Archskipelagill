using Archskipelagill.GameDataAccess;
using UnityEngine;

namespace Archskipelagill.UX;

public class ArchiSendController : MonoBehaviour {

	////// Initializer/Fields/Properties //////

    private CharaStats _chara;
    private Vector3 _velocity;
    private AudioSource _sfx;
    private float _basePitch;
    private Transform[] _spinners;
    private bool _goal;


    ////// Unity Engine API //////

#pragma warning disable IDE0051 //Used by Unity Engine
    void Start() {
        var sparkler = GetComponent<instantiateRepeat>();
        _sfx = GetComponent<AudioSource>();
        _spinners = new Transform[6];
        _chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        for(var i = 0; i < 6; i++) {
            _spinners[i] = transform.GetChild(i);
        }
        if(!_chara.metaMenu) {
            _velocity = new(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-0.25f, 0.6f));
            transform.localScale *= 3f;
            sparkler.GO = ResourceGrabber.Instance.runWorldLayerSparklePrefab.transform;
        } else {
            _velocity = new(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-0.05f, 0.125f));
            transform.localScale *= 0.5f;
            sparkler.GO = ResourceGrabber.Instance.worldLayerSparklePrefab.transform;
        }
        if(_goal) {
            transform.localScale *= 3f;
            _basePitch *= 0.5f;
            _velocity *= 0.5f;
        }
    }

    void Update() {
        if(_sfx.isPlaying) {
            _sfx.pitch = _basePitch * Time.timeScale;
            _sfx.volume = (1f - _sfx.time / _sfx.clip.length) * 1.3f * PlayerPrefs.GetFloat("SFXvol");
            transform.position += _velocity * Time.deltaTime;
            _velocity += new Vector3(0, (!_chara.metaMenu ? 5f : 1f) * Time.deltaTime, (!_chara.metaMenu ? 5f : 1f) * Time.deltaTime) * (_goal ? 0.2f : 1f);
            var phase = Mathf.Pow(_sfx.time + 1f, 1.2f) * 1f * Mathf.PI;
            var phaseb = Mathf.Pow(_sfx.time + 1f, 1.2f) * 0.384f * Mathf.PI;
            for(var i = 0; i < _spinners.Length; i++) {
                var iphase = i / 3f * Mathf.PI;
                _spinners[i].transform.localPosition = new(Mathf.Cos(phase + iphase) * 0.08f, Mathf.Sin(phase + iphase) * 0.08f, Mathf.Sin(phase + iphase) * 0.08f);
                if(_goal)
                    _spinners[i].GetComponent<SpriteRenderer>().color = Color.HSVToRGB((phaseb + iphase * 0.25f) % 1f, 1f, 1f);
            }
            transform.localScale *= 1f - Time.deltaTime * 0.5f;
        } else {
            Destroy(gameObject);
        }
    }
#pragma warning restore IDE0051

    ////// Public Static API //////

	public static GameObject CreateSend(bool goal = false) {
		var obj = new GameObject("ArchiSend");

		var ctrl = obj.AddComponent<ArchiSendController>();
		var chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
		ctrl.transform.position = chara.transform.position;
		ctrl._goal = goal;

		for(var i = 0; i < 6; i++) {
			var spinner = new GameObject("Spinner");
			spinner.transform.parent = obj.transform;
			var spr = spinner.AddComponent<SpriteRenderer>();
			spr.sprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/archi-big-single.png");
			spr.drawMode = SpriteDrawMode.Sliced;
			spr.size *= 0.16f;
		}

		var sfx = obj.AddComponent<AudioSource>();
		sfx.clip = Plugin.Resources.LoadAsset<AudioClip>("Assets/Sounds/archi_send.wav");
		sfx.volume = PlayerPrefs.GetFloat("SFXvol") * 1.3f;
		ctrl._basePitch = UnityEngine.Random.Range(0.9f, 1.1f);
		sfx.pitch = ctrl._basePitch * Time.timeScale;
		if(!SaveFileRedirect.Instance.MuteNotifs)
			sfx.Play();

		var sparkler = obj.AddComponent<instantiateRepeat>();
		sparkler.radius = 0f;
		sparkler.randomRot = false;
		sparkler.rate = 0.05f;
		sparkler.makeChild = false;

		return obj;
	}
}
