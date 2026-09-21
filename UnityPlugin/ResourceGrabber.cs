using UnityEngine;

namespace Archskipelagill;

public class ResourceGrabber {
    public GameObject prefabSetup = null;
    public Transform worldLayerSparklePrefab { get; private set; } = null;
    public ResourceGrabber() {
        prefabSetup = new("Archskipelagill Prefab Setup Dummy");
        prefabSetup.SetActive(false);
        prefabSetup.hideFlags = HideFlags.HideAndDontSave;
        On.mainMenuCamScript.Start += MainMenuCamScript_Start;
    }

    private void MainMenuCamScript_Start(On.mainMenuCamScript.orig_Start orig, mainMenuCamScript self) {
        orig(self);
        if(worldLayerSparklePrefab == null) {
            worldLayerSparklePrefab = GameObject.Instantiate(GameObject.Find("Canvas/mainMenu/shop button").GetComponent<readPlayerMoneyAndActivate>().toActivate.GetComponent<instantiateRepeat>().GO, prefabSetup.transform);
            worldLayerSparklePrefab.gameObject.layer = 0;
            worldLayerSparklePrefab.transform.localScale = new(0.1f, 0.1f, 0.1f);
            worldLayerSparklePrefab.GetComponent<moveOverTimeDirection>().Speed /= 2f;
            worldLayerSparklePrefab.GetComponent<Animator>().speed *= 2f;
            worldLayerSparklePrefab.GetComponent<destructionRetard>().time /= 2f;
        }
    }
}
