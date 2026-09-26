using Archskipelagill.GameDataAccess;
using UnityEngine;

namespace Archskipelagill.UX;

public class ArchiDropController : MonoBehaviour {

	////// Initializer/Fields/Properties //////

	private CharaStats _chara;
	private Vector3 _posStart;
	private Vector3 _posTarget;
	private AudioSource _sfx;
	private AudioSource _sfx2;
	private Transform[] _spinners;
	private Vector3[] _spinnerV;
	private bool _landed = false;
	private float _basePitch;
	private float _itemTimer = 0f;
	private Transform _follower;
	private Vector3 _followerOffset;
	private instantiateRepeat _sparkler;


    ////// Unity Engine API //////
#pragma warning disable IDE0051 //Used by Unity Engine
    void Start() {
        _sfx = GetComponents<AudioSource>()[0];
        _sfx2 = GetComponents<AudioSource>()[1];
        _sparkler = GetComponent<instantiateRepeat>();
        _spinners = new Transform[6];
        _spinnerV = new Vector3[6];
        _follower = transform.GetChild(6);
        _chara = GameObject.FindGameObjectWithTag("Player").GetComponent<CharaStats>();
        if(!_chara.metaMenu) {
            _posTarget = _chara.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 1.75f;
            _posStart = _posTarget + new Vector3(0f, 10f, 0f) + (Vector3)UnityEngine.Random.insideUnitCircle * 3.5f;
            transform.localScale *= 3f;
            for(var i = 0; i < 6; i++) { 
                _spinners[i] = transform.GetChild(i);
                _spinnerV[i] = (UnityEngine.Random.onUnitSphere + new Vector3(0f, 1.5f, 1.5f)) * UnityEngine.Random.Range(2f, 4f);
            }
            _followerOffset = UnityEngine.Random.onUnitSphere * 1f + new Vector3(0f, 2f, -3f);
            _sparkler.GO = ResourceGrabber.Instance.runWorldLayerSparklePrefab.transform;
        } else {
            _posTarget = _chara.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 0.35f;
            _posStart = _posTarget + new Vector3(0f, 2f, 0f) + (Vector3)UnityEngine.Random.insideUnitCircle * 0.7f;
            transform.localScale *= 0.5f;
            for(var i = 0; i < 6; i++) {
                _spinners[i] = transform.GetChild(i);
                _spinnerV[i] = (UnityEngine.Random.onUnitSphere + new Vector3(0f, 1.5f, 1.5f)) * UnityEngine.Random.Range(0.4f, 0.8f);
            }
            _followerOffset = UnityEngine.Random.onUnitSphere * 0.1f + new Vector3(0f, 0.2f, -3f);
            _sparkler.GO = ResourceGrabber.Instance.worldLayerSparklePrefab.transform;
        }
        transform.position = _posStart;
    }

    void Update() {
        _sfx.pitch = _basePitch * Time.timeScale;
        _sfx2.pitch = Time.timeScale;
        if(!_landed) {
            if(_itemTimer <= 0.85f) {
                _sfx.volume = (_sfx.time / 0.85f) * 1.3f * PlayerPrefs.GetFloat("SFXvol");
                transform.position = _posStart + (_sfx.time / 0.85f) * (_posTarget - _posStart);
                var phase = _sfx.time * 6f * Mathf.PI;
                for(var i = 0; i < _spinners.Length; i++) {
                    var iphase = i / 3f * Mathf.PI;
                    _spinners[i].transform.localPosition = new(Mathf.Cos(phase + iphase) * 0.08f, Mathf.Sin(phase + iphase) * 0.08f, Mathf.Sin(phase + iphase) * 0.08f);
                }
            } else {
                _landed = true;
                _itemTimer = 0f;
                if(!SaveFileRedirect.Instance.MuteNotifs)
                    _sfx2.Play();
                for(var i = 0; i < 8; i++)
                    _sparkler.insto();
                GameObject.Destroy(_sparkler);
            }
            _itemTimer += Time.deltaTime;
        } else {
            if(_itemTimer == 0f) {
                _follower.localScale = Vector3.zero;
                _follower.gameObject.SetActive(true);
            }

            transform.position = _posTarget;
            for(var i = 0; i < _spinners.Length; i++) {
                _spinners[i].transform.position += _spinnerV[i] * Time.deltaTime;
                _spinners[i].transform.localScale *= 1f - Time.deltaTime * 2f;
                _spinnerV[i] += new Vector3(0, (!_chara.metaMenu ? -25f : -1f) * Time.deltaTime, (!_chara.metaMenu ? -25f : -1f) * Time.deltaTime);
            }

            var fadeInFac = Mathf.Min(_itemTimer / 0.5f, 1f) * (_chara.metaMenu ? 1f : 1.25f);
            _follower.localScale = new Vector3(fadeInFac, fadeInFac, fadeInFac);

            _follower.position += Time.deltaTime * 2f * (_chara.transform.position + _followerOffset - _follower.position);

            if(_itemTimer > 4f) {
                _follower.gameObject.SetActive((_itemTimer % 0.25f) > 0.125f);
            } else _follower.gameObject.SetActive(true);

            _itemTimer += Time.deltaTime;
            if(_itemTimer > 5f)
                Destroy(gameObject);
        }
    }
#pragma warning restore IDE0051

    ////// Public Static API //////

	public static GameObject CreateDrop(string itemName) {
		var obj = new GameObject("ArchiDrop");

		var ctrl = obj.AddComponent<ArchiDropController>();

		for(var i = 0; i < 6; i++) {
			var spinner = new GameObject("Spinner");
			spinner.transform.parent = obj.transform;
			var spr = spinner.AddComponent<SpriteRenderer>();
			spr.sprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/archi-big-single.png");
			spr.drawMode = SpriteDrawMode.Sliced;
			spr.size *= 0.16f;
		}

		var sfx = obj.AddComponent<AudioSource>();
		sfx.clip = Plugin.Resources.LoadAsset<AudioClip>("Assets/Sounds/archi_item_fall.wav");
		sfx.volume = 0f;
		ctrl._basePitch = UnityEngine.Random.Range(0.9f, 1.1f);
		sfx.pitch = ctrl._basePitch * Time.timeScale;
		if(!SaveFileRedirect.Instance.MuteNotifs)
			sfx.Play();

		var sfx2 = obj.AddComponent<AudioSource>();
		sfx2.clip = Plugin.Resources.LoadAsset<AudioClip>($"Assets/Sounds/archi_{(itemName.StartsWith("Trap: ") ? "trap" : "item")}_arrive.wav");
		sfx2.volume = PlayerPrefs.GetFloat("SFXvol") * 1.3f;
		sfx2.pitch = Time.timeScale;

		var follower = new GameObject("Follower");
		follower.transform.parent = obj.transform;
		follower.SetActive(false);
		var spr2 = follower.AddComponent<SpriteRenderer>();
		spr2.sprite = Plugin.Resources.LoadAsset<Sprite>($"Assets/Textures/item-{itemName switch {
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
		sparkler.radius = 0f;
		sparkler.randomRot = false;
		sparkler.rate = 0.03f;
		sparkler.makeChild = false;

		return obj;
	}
}
