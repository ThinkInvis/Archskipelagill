using Archskipelagill.ArchipelagoCompat;
using BepInEx;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Archskipelagill;

public class MainMenuInjector {
    SpriteState archiBtnDcState, archiBtnConnState;
    GameObject archiMenu, archiMenuBtn, archiConsoleGroup, archiConnGroup, pauseMenu;
    InputField hostField, userField, passField, consoleField;
    Button wipeSaveBtn;
    Image wipeSaveProgress;
    Text consoleText;
    ScrollRect consoleScroll;
    static readonly List<string> logLines = [];
    const int MAX_LOG_LINES = 80;
    bool _logDirty = false;
    bool hasWiped = false;
    bool _consoleStateDirty = false;
    bool _nextConsoleState = false;
    internal bool pauseMenuState = false;
    internal bool pauseMenuLeaving = false;

    public MainMenuInjector() {
        //for pause menu button: look for [#MainCamera]/Canvas/pauseMenu, /Quit, remove/replace EventTrigger and replace Button.onClick.PersistentCall[1]
        On.mainCameraScript.Start += MainCameraScript_Start;
        On.mainCameraScript.Update += MainCameraScript_Update;
        On.mainMenuCamScript.Start += MainMenuCamScript_Start;
        On.mainMenuCamScript.Update += MainMenuCamScript_Update;
        On.mainCameraScript.setQuitPause += MainCameraScript_setQuitPause;
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

    private void MainCameraScript_setQuitPause(On.mainCameraScript.orig_setQuitPause orig, mainCameraScript self) {
        orig(self);
        if(self.canPause && !self.paused) {
            pauseMenuState = false;
            pauseMenuLeaving = true;
        } else pauseMenuLeaving = false;
    }

    private void MainCameraScript_Update(On.mainCameraScript.orig_Update orig, mainCameraScript self) {
        orig(self);
        if(pauseMenuState) {
            archiMenu.SetActive(true);
            var targetPos = new Vector3(0f, 2f, 0f);
            if(Vector2.Distance(archiMenu.transform.localPosition, targetPos) != 0f) {
                if(Vector2.Distance(archiMenu.transform.localPosition, targetPos) < Time.unscaledDeltaTime * 20f) {
                    archiMenu.transform.localPosition = targetPos;
                    pauseMenu.SetActive(false);
                } else {
                    archiMenu.transform.localPosition += Vector3.up * Time.unscaledDeltaTime * 20f;
                    pauseMenu.transform.localPosition = new Vector3(-1.25f, -15f, 0f) - archiMenu.transform.localPosition;
                }
            }
            if(_consoleStateDirty) {
                _consoleStateDirty = false;
                archiConsoleGroup.SetActive(_nextConsoleState);
                archiConnGroup.SetActive(!_nextConsoleState);
            }
            if(_logDirty) {
                _logDirty = false;
                UpdateLog();
            }
        } else if(Vector2.Distance(archiMenu.transform.localPosition, Vector2.down * 15f) != 0f) {
            if(!pauseMenuLeaving)
                pauseMenu.SetActive(true);
            if(Vector2.Distance(archiMenu.transform.localPosition, Vector2.down * 15f) < Time.unscaledDeltaTime * 20f) {
                archiMenu.transform.localPosition = Vector3.down * 15f;
                pauseMenu.transform.localPosition = new(-1.25f, 0f, 0f);
                archiMenu.SetActive(false);
            } else {
                archiMenu.transform.localPosition += Vector3.down * Time.unscaledDeltaTime * 20f;
                if(pauseMenuLeaving)
                    pauseMenu.transform.localPosition = new Vector3(-1.25f, -15f, 0f);
                else
                pauseMenu.transform.localPosition = new Vector3(-1.25f, -15f, 0f) -archiMenu.transform.localPosition;
            }
        }
    }

    private void MainCameraScript_Start(On.mainCameraScript.orig_Start orig, mainCameraScript self) {
        orig(self);
        SetupMenu();

        pauseMenu = self.transform.Find("Canvas/pauseMenu").gameObject;

        archiMenu.transform.parent = self.transform.Find("Canvas").transform;
        archiMenu.transform.localPosition = new(0f, -13f, 0f);
        archiMenu.transform.localScale = new(1f, 1f, 1f);

        archiMenuBtn = GameObject.Instantiate(pauseMenu.transform.Find("Back").gameObject, pauseMenu.transform);
        archiMenuBtn.name = "archipelago button";
        archiMenuBtn.transform.position += new Vector3(10f, 0, 0);

        var backBtn = self.pauseMenu.transform.Find("Back");
        var newBackBtn = GameObject.Instantiate(backBtn, archiMenu.transform);
        newBackBtn.transform.localPosition = new(-12f, 3f, 0f);
        var calls = newBackBtn.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls;
        calls[0].m_Target = archiMenu.GetComponent<PauseMenuScrollHandler>();
        calls[0].m_MethodName = "DoCollapse";
        calls[1].m_Target = backBtn;

        var btnScript = archiMenuBtn.GetComponent<Button>();
        btnScript.spriteState = (Plugin.ArchipelagoClient.session != null) ? archiBtnConnState : archiBtnDcState;
        btnScript.image.sprite = btnScript.spriteState.pressedSprite;
        calls = btnScript.onClick.m_PersistentCalls.m_Calls;
        calls[0].m_Target = archiMenu.GetComponent<PauseMenuScrollHandler>();
        calls[0].m_MethodName = "DoExpand";
        calls[1].m_Target = newBackBtn;

        wipeSaveBtn.gameObject.SetActive(false);
        UpdateLog();
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
            if(_consoleStateDirty) {
                _consoleStateDirty = false;
                archiConsoleGroup.SetActive(_nextConsoleState);
                archiConnGroup.SetActive(!_nextConsoleState);
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
        SetupMenu();

        archiMenu.transform.parent = GameObject.Find("Canvas").transform;
        archiMenu.transform.position = new(0f, -13f, 0f);
        archiMenu.transform.localScale = new(16f, 16f, 16f);

        var menuBtns = GameObject.Find("Canvas/mainMenu");
        foreach(Transform btn in menuBtns.transform) {
            btn.position += new Vector3(0, 2f, 0);
        }
        archiMenuBtn = GameObject.Instantiate(GameObject.Find("Canvas/mainMenu/setting button"), menuBtns.transform);
        archiMenuBtn.name = "archipelago button";
        archiMenuBtn.transform.position += new Vector3(0, -4f, 0);

        var newBackBtn = GameObject.Instantiate(self.settingsMenu.transform.Find("Back"), archiMenu.transform);
        newBackBtn.transform.localPosition = new(-12f, 3f, 0f);

        var btnScript = archiMenuBtn.GetComponent<Button>();
        btnScript.spriteState = (Plugin.ArchipelagoClient.session != null) ? archiBtnConnState : archiBtnDcState;
        btnScript.image.sprite = btnScript.spriteState.pressedSprite;
        btnScript.onClick.m_PersistentCalls.m_Calls[0].arguments.stringArgument = "archiMenu";
        btnScript.onClick.m_PersistentCalls.m_Calls[2].m_Target = newBackBtn;
    }

    void SetupMenu() {
        archiMenu = GameObject.Instantiate(Plugin.resources.LoadAsset<GameObject>("Assets/Prefabs/ArchiMenu.prefab"));

        archiMenu.transform.Find("Title").GetComponent<Text>().text = Plugin.ModDisplayInfo;
        archiMenu.transform.Find("ConnInfo").GetComponent<Text>().text = Plugin.APDisplayInfo + (ArchipelagoClient.Authenticated ? " connected" : " disconnected");

        archiMenu.AddComponent<PauseMenuScrollHandler>();

        archiConsoleGroup = archiMenu.transform.Find("ConsoleGroup").gameObject;
        archiConnGroup = archiMenu.transform.Find("ConnectionGroup").gameObject;

        hostField = archiConnGroup.transform.Find("HostField/Backdrop/Field").GetComponent<InputField>();
        userField = archiConnGroup.transform.Find("UserField/Backdrop/Field").GetComponent<InputField>();
        passField = archiConnGroup.transform.Find("PassField/Backdrop/Field").GetComponent<InputField>();
        consoleField = archiConsoleGroup.transform.Find("ConsoleInputField/Backdrop/Field").GetComponent<InputField>();
        consoleText = archiConsoleGroup.transform.Find("ConsoleOutput/Backdrop/Scrollbox/Field").GetComponent<Text>();
        consoleScroll = archiConsoleGroup.transform.Find("ConsoleOutput/Backdrop/Scrollbox").GetComponent<ScrollRect>();

        hostField.text = Plugin.ArchipelagoClient.cfgAutoConnectHostname.Value;
        if(hostField.text == "") hostField.text = "localhost";
        userField.text = Plugin.ArchipelagoClient.cfgAutoConnectSlot.Value;
        if(userField.text == "") userField.text = "Player1";
        passField.text = Plugin.ArchipelagoClient.cfgAutoConnectPassword.Value;

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

        _nextConsoleState = Plugin.ArchipelagoClient.session != null;
        _consoleStateDirty = true;
    }

    public void OnConnect() {
        if(archiMenu == null) return;
        _nextConsoleState = true;
        _consoleStateDirty = true;
        archiMenu.transform.Find("ConnInfo").GetComponent<Text>().text = Plugin.APDisplayInfo + " connected";
        var btnScript = archiMenuBtn.GetComponent<Button>();
        btnScript.spriteState = archiBtnConnState;
        btnScript.image.sprite = btnScript.spriteState.pressedSprite;
    }

    public void OnDisconnect() {
        if(archiMenu == null) return;
        _nextConsoleState = false;
        _consoleStateDirty = true;
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
        Plugin.BepinLogger.LogMessage($"[APMsg] {msg}");
    }

    void UpdateLog() {
        if(archiMenu == null) return;
        consoleText.text = string.Join("\r\n", logLines);
        LayoutRebuilder.ForceRebuildLayoutImmediate(consoleScroll.rectTransform);
        consoleScroll.SetVerticalNormalizedPosition(0f);
    }

    public class PauseMenuScrollHandler : MonoBehaviour {
        public void DoCollapse() {
            Plugin.instance.mainMenuInjector.pauseMenuState = false;
        }
        public void DoExpand() {
            Plugin.instance.mainMenuInjector.pauseMenuState = true;
        }
    }
}
