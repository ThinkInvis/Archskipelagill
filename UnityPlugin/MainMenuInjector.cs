using Archskipelagill.ArchipelagoCompat;
using BepInEx;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Archskipelagill;

public class MainMenuInjector {
    SpriteState archiBtnDcState, archiBtnConnState;
    GameObject archiMenu, archiMenuBtn, archiConsoleGroup, archiConnGroup;
    InputField hostField, userField, passField, consoleField;
    Button wipeSaveBtn;
    Image wipeSaveProgress;
    Text consoleText;
    ScrollRect consoleScroll;
    static readonly List<string> logLines = [];
    const int MAX_LOG_LINES = 80;
    bool _logDirty = false;
    bool hasWiped = false;

    public MainMenuInjector() {
        On.mainMenuCamScript.Start += MainMenuCamScript_Start;
        On.mainMenuCamScript.Update += MainMenuCamScript_Update;
        archiBtnDcState = new SpriteState {
            highlightedSprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/archi-button-dc-selected.png"),
            selectedSprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/archi-button-dc-selected.png"),
            pressedSprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/archi-button-dc.png")
        };
        archiBtnConnState = new SpriteState {
            highlightedSprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/archi-button-conn-selected.png"),
            selectedSprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/archi-button-conn-selected.png"),
            pressedSprite = Plugin.resources.LoadAsset<Sprite>("Assets/Textures/archi-button-conn.png")
        };
    }

    private void MainMenuCamScript_Update(On.mainMenuCamScript.orig_Update orig, mainMenuCamScript self) {
        orig(self);
        if(self.menuMode == "archiMenu") {
            archiMenu.SetActive(true);
            if(Vector2.Distance(archiMenu.transform.position, Vector2.zero) != 0f) {
                if(Vector2.Distance(archiMenu.transform.position, Vector2.zero) < Time.deltaTime * 20f)
                    archiMenu.transform.position = Vector3.zero;
                else {
                    archiMenu.transform.position += Vector3.up * Time.deltaTime * 20f;
                    self.eventSystem.sendNavigationEvents = false;
                }
            }
            if(_logDirty) {
                _logDirty = false;
                UpdateLog();
            }
            if(wipeSaveBtn.IsPressed()) {
                if(!hasWiped) {
                    var fac = wipeSaveProgress.rectTransform.sizeDelta.x + Time.deltaTime * 132f / 5f;
                    if(fac > 132f) {
                        fac = 0f;
                        hasWiped = true;
                        Plugin.instance.customSaveLoad.Wipe();
                    }
                    wipeSaveProgress.rectTransform.sizeDelta = new(fac, 24f);
                }
            } else {
                hasWiped = false;
                wipeSaveProgress.rectTransform.sizeDelta = new(0f, 24f);
            }
        } else if(Vector2.Distance(archiMenu.transform.position, Vector2.down * 15f) != 0f) {
            if(Vector2.Distance(archiMenu.transform.position, Vector2.down * 15f) < Time.deltaTime * 20f) {
                archiMenu.transform.position = Vector3.down * 15f;
                archiMenu.SetActive(false);
            } else {
                archiMenu.transform.position += Vector3.down * Time.deltaTime * 20f;
                self.eventSystem.sendNavigationEvents = false;
            }
        }
    }

    private void MainMenuCamScript_Start(On.mainMenuCamScript.orig_Start orig, mainMenuCamScript self) {
        orig(self);

        archiMenu = GameObject.Instantiate(Plugin.resources.LoadAsset<GameObject>("Assets/Prefabs/ArchiMenu.prefab"));
        archiMenu.transform.parent = GameObject.Find("Canvas").transform;
        archiMenu.transform.position = new(0f, -13f, 0f);
        archiMenu.transform.localScale = new(16f, 16f, 16f);
        var newBackBtn = GameObject.Instantiate(self.settingsMenu.transform.Find("Back"), archiMenu.transform);
        newBackBtn.transform.localPosition = new(-12f, 3f, 0f);

        archiMenu.transform.Find("Title").GetComponent<Text>().text = Plugin.ModDisplayInfo;
        archiMenu.transform.Find("ConnInfo").GetComponent<Text>().text = Plugin.APDisplayInfo + " disconnected";

        archiConsoleGroup = archiMenu.transform.Find("ConsoleGroup").gameObject;
        archiConnGroup = archiMenu.transform.Find("ConnectionGroup").gameObject;

        hostField = archiConnGroup.transform.Find("HostField/Backdrop/Field").GetComponent<InputField>();
        userField = archiConnGroup.transform.Find("UserField/Backdrop/Field").GetComponent<InputField>();
        passField = archiConnGroup.transform.Find("PassField/Backdrop/Field").GetComponent<InputField>();
        consoleField = archiConsoleGroup.transform.Find("ConsoleInputField/Backdrop/Field").GetComponent<InputField>();
        consoleText = archiConsoleGroup.transform.Find("ConsoleOutput/Backdrop/Scrollbox/Field").GetComponent<Text>();
        consoleScroll = archiConsoleGroup.transform.Find("ConsoleOutput/Backdrop/Scrollbox").GetComponent<ScrollRect>();

        wipeSaveBtn = archiConnGroup.transform.Find("WipeSaveBtn").GetComponent<Button>();
        wipeSaveProgress = wipeSaveBtn.transform.Find("Progress").GetComponent<Image>();

        archiConnGroup.transform.Find("ConnectBtn").GetComponent<Button>().onClick.AddListener(() => {
            if(userField.text.IsNullOrWhiteSpace()) return;
            ArchipelagoClient.ServerData.Uri = hostField.text;
            ArchipelagoClient.ServerData.SlotName = userField.text;
            ArchipelagoClient.ServerData.Password = passField.text;
            Plugin.ArchipelagoClient.Connect();
        });

        consoleField.onSubmit.AddListener(content => {
            Plugin.ArchipelagoClient.SendMessage(content);
            consoleField.text = "";
        });

        var menuBtns = GameObject.Find("Canvas/mainMenu");
        foreach(Transform btn in menuBtns.transform) {
            btn.position += new Vector3(0, 2f, 0);
        }
        archiMenuBtn = GameObject.Instantiate(GameObject.Find("Canvas/mainMenu/setting button"), menuBtns.transform);
        archiMenuBtn.name = "archipelago button";
        archiMenuBtn.transform.position += new Vector3(0, -4f, 0);
        var btnScript = archiMenuBtn.GetComponent<Button>();
        bool isConnected = Plugin.ArchipelagoClient.session != null;
        btnScript.spriteState = isConnected ? archiBtnConnState : archiBtnDcState;
        btnScript.image.sprite = btnScript.spriteState.pressedSprite;
        btnScript.onClick.m_PersistentCalls.m_Calls[0].arguments.stringArgument = "archiMenu";
        btnScript.onClick.m_PersistentCalls.m_Calls[2].m_Target = newBackBtn;

        archiConsoleGroup.SetActive(isConnected);
        archiConnGroup.SetActive(!isConnected);
    }

    public void OnConnect() {
        if(archiMenu == null) return;
        archiConnGroup.SetActive(false);
        archiConsoleGroup.SetActive(true);
        archiMenu.transform.Find("ConnInfo").GetComponent<Text>().text = Plugin.APDisplayInfo + " connected";
        var btnScript = archiMenuBtn.GetComponent<Button>();
        btnScript.spriteState = archiBtnConnState;
        btnScript.image.sprite = btnScript.spriteState.pressedSprite;
    }

    public void OnDisconnect() {
        if(archiMenu == null) return;
        archiConsoleGroup.SetActive(false);
        archiConnGroup.SetActive(true);
        archiMenu.transform.Find("ConnInfo").GetComponent<Text>().text = Plugin.APDisplayInfo + " disconnected";
        var btnScript = archiMenuBtn.GetComponent<Button>();
        btnScript.spriteState = archiBtnDcState;
        btnScript.image.sprite = btnScript.spriteState.pressedSprite;
    }

    public void OnConnectFail() {
        if(archiMenu == null) return;
    }

    public void ReceiveMessage(string msg) {
        logLines.Add(msg);
        if(logLines.Count > MAX_LOG_LINES)
            logLines.RemoveAt(0);
        _logDirty = true;
    }

    void UpdateLog() {
        if(archiMenu == null) return;
        consoleText.text = string.Join("\r\n", logLines);
        LayoutRebuilder.ForceRebuildLayoutImmediate(consoleScroll.rectTransform);
        consoleScroll.SetVerticalNormalizedPosition(0f);
    }
}
