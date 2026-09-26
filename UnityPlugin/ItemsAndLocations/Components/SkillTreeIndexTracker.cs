using Archskipelagill.ArchipelagoCompat;
using Archskipelagill.GameDataAccess;
using System;
using UnityEngine;

namespace Archskipelagill.ItemsAndLocations.Components;

public class SkillTreeIndexTracker:MonoBehaviour {

    ////// Initializer/Fields/Properties //////

    public GameData.SkillNode DataNode {get; set;}
    public bool isUnlocked { get; private set; } = false;

    private bool _hasCheck = false;
    private Transform[] _spinners;
    private UnityEngine.UI.Image _activateVfx;
    private SpriteRenderer _iconColor, _nodeOcto;
    private Color _origIconColor;
    private instantiateBoss _bossCpt;


    ////// Unity Engine API //////
#pragma warning disable IDE0051 //Used by Unity Engine
    void Awake() {
        if(SkillTreeItemizer.Instance.SkillTreeLocationTracker) {
            _spinners = new Transform[6];
            for(var i = 0; i < 6; i++) {
                var spinner = new GameObject("Spinner");
                spinner.transform.parent = this.transform;
                var spr = spinner.AddComponent<SpriteRenderer>();
                spr.sprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/archi-big-single.png");
                spr.drawMode = SpriteDrawMode.Sliced;
                if(GetComponent<skigillNode>().metaProg)
                    spr.size *= 0.0625f;
                else
                    spr.size *= 0.5f;
                _spinners[i] = spinner.transform;
                _spinners[i].gameObject.SetActive(_hasCheck);
            }
        }
        _activateVfx = transform.Find("canvas/Activate").GetComponent<UnityEngine.UI.Image>();
        _iconColor = transform.Find("IconColor").GetComponent<SpriteRenderer>();
        _nodeOcto = transform.Find("nodeOcto").GetComponent<SpriteRenderer>();
        _origIconColor = _iconColor.color;
        _bossCpt = GetComponent<instantiateBoss>();
    }

    void Update() {
        if(_hasCheck) {
            if(SkillTreeItemizer.Instance.SkillTreeLocationTracker) {
                var phase = Time.time * 0.5f * Mathf.PI;
                for(var i = 0; i < _spinners.Length; i++) {
                    var iphase = i / 3f * Mathf.PI;
                    _spinners[i].transform.localPosition = new(Mathf.Cos(phase + iphase) * 1.25f, Mathf.Sin(phase + iphase) * 1.25f, -2f);
                }
                if(isUnlocked)
                    _iconColor.color = ((Time.unscaledTime % 1f) > 0.5f) ? _origIconColor : new(0.25f, 1f, 0.25f);
                else
                    _iconColor.color = ((Time.unscaledTime % 1f) > 0.5f) ? new(0.6f, 0.6f, 0.6f, 0.25f) : new(0.8f, 0.15f, 0.15f, 0.25f);
            } else {
                if(isUnlocked)
                    _iconColor.color = _origIconColor;
                else
                    _iconColor.color = new(0.6f, 0.6f, 0.6f, 0.25f);
            }
        }
    }
#pragma warning restore IDE0051


    ////// Public API //////

    public void Rescan() {
        if(!isActiveAndEnabled) return;

        var hasRegion = ArchipelagoSaver.GetItemCount($"Skigill Region: {Enum.GetName(typeof(GameData.SkillNodeRegion), DataNode.region).ToTitleCase()}") > 0;
        var hasFBK = ArchipelagoSaver.GetItemCount($"Final Boss Key") > 0;

        //lock boss region behind all others if option enabled
        var bossLast = Int64.Parse(ArchipelagoSaver.Instance.metaProg["archi_boss_last"]);
        if(bossLast > 0 && DataNode.region == GameData.SkillNodeRegion.BOSSES) {
            foreach(var n in Enum.GetNames(typeof(GameData.SkillNodeRegion))) {
                if(ArchipelagoSaver.GetItemCount($"Skigill Region: {n.ToTitleCase()}") == 0) {
                    hasRegion = false;
                    break;
                }
            }
        }

        //lock unreachable regions
        if(DataNode.region == GameData.SkillNodeRegion.STRONGMAN
            && ArchipelagoSaver.GetItemCount("Skigill Region: Prototype") == 0
            && ArchipelagoSaver.GetItemCount("Character: Strongman") == 0)
            hasRegion = false;
        if(DataNode.region == GameData.SkillNodeRegion.FOX
            && ArchipelagoSaver.GetItemCount("Skigill Region: Dragon") == 0
            && ArchipelagoSaver.GetItemCount("Character: Fox") == 0)
            hasRegion = false;
        if(DataNode.region == GameData.SkillNodeRegion.DWARVES
            && ((ArchipelagoSaver.GetItemCount("Skigill Region: Prototype") == 0 && ArchipelagoSaver.GetItemCount("Character: Strongman") == 0)
                || ArchipelagoSaver.GetItemCount("Skigill Region: Strongman") == 0)
            && ((ArchipelagoSaver.GetItemCount("Skigill Region: Dragon") == 0 && ArchipelagoSaver.GetItemCount("Character: Fox") == 0)
                || ArchipelagoSaver.GetItemCount("Skigill Region: Fox") == 0)
            )
            hasRegion = false;

        _hasCheck = false;
        var isChest = DataNode.type == GameData.SkillNodeType.CHEST;
        var isPerk = DataNode.type == GameData.SkillNodeType.PERK;
        var isBoss = DataNode.type == GameData.SkillNodeType.BOSS;
        if(isChest || isPerk) {
            _hasCheck = ArchipelagoDataUtils.HasLocation($"Skigill {(isChest ? "Chest" : "Perk")} #{(isChest ? DataNode.chestIndex : DataNode.perkIndex) + 1} ({Enum.GetName(typeof(GameData.SkillNodeRegion), DataNode.region)})") == ArchipelagoDataUtils.LocationState.Unchecked;
        } else if(isBoss && _bossCpt != null) {
            var targetBossName = _bossCpt.bossPrefab.GetComponentInChildren<VieScript>().bossName switch {
                "OVNI" => "Rosa",
                "GRENOUILLE" => "Roger",
                "SLIME" => "Jello",
                "POULPE" => "Pilpou",
                "BOULE" => "Bouboul",
                "COCHON" => "Gari",
                "FINAL" => "Final Boss",
                _ => "N/A"
            };
            if(targetBossName != "N/A") {
                _hasCheck = ArchipelagoDataUtils.HasLocation($"Defeated {targetBossName}") == ArchipelagoDataUtils.LocationState.Unchecked;
            }
        }

        if(hasRegion && (DataNode.type != GameData.SkillNodeType.BOSS_FINAL || hasFBK))
            Unlock();
        else
            Lock();

        foreach(var s in _spinners) {
            s.gameObject.SetActive(_hasCheck);
            s.GetComponent<SpriteRenderer>().color = isUnlocked ? new(1f, 1f, 1f) : new(0.2f, 0.2f, 0.2f);
        }
    }

    public void Unlock(bool forceHasCheck = false) {
        isUnlocked = true;
        GetComponent<SpriteRenderer>().color = new(1f, 1f, 1f, 1f);
        _activateVfx.color = new(1f, 1f, 1f, 1f);
        _iconColor.color = _origIconColor;
        _nodeOcto.color = new(1f, 1f, 1f, 1f);
        foreach(var sr in transform.Find("chiffres").GetComponentsInChildren<SpriteRenderer>()) {
            sr.color = new(1f, 1f, 1f, 1f);
        }

        if(forceHasCheck) {
            _hasCheck = true;
            foreach(var s in _spinners) {
                s.gameObject.SetActive(_hasCheck);
                s.GetComponent<SpriteRenderer>().color = new(1f, 1f, 1f);
            }
        }
    }

    public void Lock() {
        isUnlocked = false;
        GetComponent<SpriteRenderer>().color = new(0.35f, 0.35f, 0.35f, 1f);
        _activateVfx.color = new(0.25f, 0.25f, 0.25f, 1f);
        _iconColor.color = new(0.6f, 0.6f, 0.6f, 0.25f);
        _nodeOcto.color = new(0.35f, 0.35f, 0.35f, 1f);
        foreach(var sr in transform.Find("chiffres").GetComponentsInChildren<SpriteRenderer>()) {
            sr.color = new(0.35f, 0.35f, 0.35f, 1f);
        }
    }
}