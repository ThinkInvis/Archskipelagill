using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archskipelagill.GameDataAccess;

public class ResourceGrabber : Module<ResourceGrabber> {

    ////// Initializer/Fields/Properties //////
    
    private readonly GameObject _prefabSetup;
    public Transform worldLayerSparklePrefab { get; private set; } = null;
    public Transform runWorldLayerSparklePrefab { get; private set; } = null;
    public bool GridSceneDuringLoading { get; private set; } = true;
    public List<Transform> playerSkinsPrefabs = [];

    public ResourceGrabber() {
        _prefabSetup = new("Archskipelagill Prefab Setup Dummy");
        _prefabSetup.SetActive(false);
        _prefabSetup.hideFlags = HideFlags.HideAndDontSave;
        On.mainMenuCamScript.Start += On_MainMenuCamScript_Start;
        On.introCutsceneScript.loadMenu += On_IntroCutsceneScript_loadMenu;
        On.weaponDictionary.Start += WeaponDictionary_Start;
        On.CharaStats.Start += CharaStats_Start;
    }

    private void CharaStats_Start(On.CharaStats.orig_Start orig, CharaStats self) {
        orig(self);

        if(GridSceneDuringLoading) {
            foreach(var sk in self.skins) {
                var nsk = GameObject.Instantiate(sk, _prefabSetup.transform).transform;
                GameObject.Destroy(nsk.GetChild(0).gameObject);
                playerSkinsPrefabs.Add(nsk);
            }
        }
    }

    private void On_IntroCutsceneScript_loadMenu(On.introCutsceneScript.orig_loadMenu orig, introCutsceneScript self) {
        SceneManager.LoadScene("grid", LoadSceneMode.Single);
        //orig(self);
    }

    private void WeaponDictionary_Start(On.weaponDictionary.orig_Start orig, weaponDictionary self) {
        if(!GridSceneDuringLoading) {
            orig(self);
            return;
        }

        GameData.PopulateWeapons(self);

        SceneManager.LoadScene("menu", LoadSceneMode.Single);
    }


    ////// MonoMod Hooks //////
    #region MonoMod Hooks
    private void On_MainMenuCamScript_Start(On.mainMenuCamScript.orig_Start orig, mainMenuCamScript self) {
        orig(self);
        GridSceneDuringLoading = false;
        if(worldLayerSparklePrefab == null) {
            worldLayerSparklePrefab = GameObject.Instantiate(GameObject.Find("Canvas/mainMenu/shop button").GetComponent<readPlayerMoneyAndActivate>().toActivate.GetComponent<instantiateRepeat>().GO, _prefabSetup.transform);
            worldLayerSparklePrefab.gameObject.layer = 0;
            worldLayerSparklePrefab.transform.localScale = new(0.1f, 0.1f, 0.1f);
            worldLayerSparklePrefab.GetComponent<moveOverTimeDirection>().Speed /= 2f;
            worldLayerSparklePrefab.GetComponent<Animator>().speed *= 2f;
            worldLayerSparklePrefab.GetComponent<destructionRetard>().time /= 2f;
        }

        if(runWorldLayerSparklePrefab == null) {
            runWorldLayerSparklePrefab = GameObject.Instantiate(GameObject.Find("Canvas/mainMenu/shop button").GetComponent<readPlayerMoneyAndActivate>().toActivate.GetComponent<instantiateRepeat>().GO, _prefabSetup.transform);
            runWorldLayerSparklePrefab.gameObject.layer = 0;
            runWorldLayerSparklePrefab.transform.localScale = new(1f, 1f, 1f);
            runWorldLayerSparklePrefab.GetComponent<moveOverTimeDirection>().Speed *= 3f;
            runWorldLayerSparklePrefab.GetComponent<Animator>().speed *= 2f;
            runWorldLayerSparklePrefab.GetComponent<destructionRetard>().time /= 2f;
        }
    }
    #endregion
}
