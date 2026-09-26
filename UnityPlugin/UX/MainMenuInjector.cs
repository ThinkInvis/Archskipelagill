using Archskipelagill.ArchipelagoCompat;
using Archskipelagill.GameDataAccess;
using BepInEx;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Archskipelagill.UX;

public class MainMenuInjector : Module<MainMenuInjector> {

    ////// Initializer/Fields/Properties //////

    private SpriteState _archiBtnDcState, _archiBtnConnState;
    private GameObject _archiMenu, _archiMenuBtn, _archiConsoleGroup, _archiConnGroup, _pauseMenu;
    private InputField _hostField, _userField, _passField, _consoleField;
    private Button _wipeSaveBtn;
    private Image _wipeSaveProgress;
    private Text _consoleText;
    private ScrollRect _consoleScroll;
    private static readonly List<string> _logLines = [];
    private const int _MAX_LOG_LINES = 80;
    private bool _logDirty = false;
    private bool _hasWiped = false;
    private bool _consoleStateDirty = false;
    private bool _nextConsoleState = false;
    private bool _pauseMenuState = false;
    private bool _pauseMenuLeaving = false;

    public MainMenuInjector() {
        //for pause menu button: look for [#MainCamera]/Canvas/pauseMenu, /Quit, remove/replace EventTrigger and replace Button.onClick.PersistentCall[1]
        On.mainCameraScript.Start += On_MainCameraScript_Start;
        On.mainCameraScript.Update += On_MainCameraScript_Update;
        On.mainMenuCamScript.Start += On_MainMenuCamScript_Start;
        On.mainMenuCamScript.Update += On_MainMenuCamScript_Update;
        On.mainCameraScript.setQuitPause += On_MainCameraScript_setQuitPause;
        _archiBtnDcState = new SpriteState {
            highlightedSprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/archi-button-dc-selected.png"),
            selectedSprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/archi-button-dc-selected.png"),
            pressedSprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/archi-button-dc.png")
        };
        _archiBtnConnState = new SpriteState {
            highlightedSprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/archi-button-conn-selected.png"),
            selectedSprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/archi-button-conn-selected.png"),
            pressedSprite = Plugin.Resources.LoadAsset<Sprite>("Assets/Textures/archi-button-conn.png")
        };
    }


    ////// MonoMod Hooks //////
    #region MonoMod Hooks
    private void On_MainCameraScript_setQuitPause(On.mainCameraScript.orig_setQuitPause orig, mainCameraScript self) {
        orig(self);
        if(self.canPause && !self.paused) {
            _pauseMenuState = false;
            _pauseMenuLeaving = true;
        } else _pauseMenuLeaving = false;
    }

    private void On_MainCameraScript_Update(On.mainCameraScript.orig_Update orig, mainCameraScript self) {
        orig(self);
        if(self.ended) {
            _pauseMenu.SetActive(false);
            return;
        }
        if(_pauseMenuState) {
            _archiMenu.SetActive(true);
            var targetPos = new Vector3(0f, 2f, 0f);
            if(Vector2.Distance(_archiMenu.transform.localPosition, targetPos) != 0f) {
                if(Vector2.Distance(_archiMenu.transform.localPosition, targetPos) < Time.unscaledDeltaTime * 20f) {
                    _archiMenu.transform.localPosition = targetPos;
                    _pauseMenu.SetActive(false);
                } else {
                    _archiMenu.transform.localPosition += Vector3.up * Time.unscaledDeltaTime * 20f;
                    _pauseMenu.transform.localPosition = new Vector3(-1.25f, -15f, 0f) - _archiMenu.transform.localPosition;
                }
            }
            if(_consoleStateDirty) {
                _consoleStateDirty = false;
                _archiConsoleGroup.SetActive(_nextConsoleState);
                _archiConnGroup.SetActive(!_nextConsoleState);
            }
            if(_logDirty) {
                _logDirty = false;
                UpdateLog();
            }
        } else if(Vector2.Distance(_archiMenu.transform.localPosition, Vector2.down * 15f) != 0f) {
            if(!_pauseMenuLeaving)
                _pauseMenu.SetActive(true);
            if(Vector2.Distance(_archiMenu.transform.localPosition, Vector2.down * 15f) < Time.unscaledDeltaTime * 20f) {
                _archiMenu.transform.localPosition = Vector3.down * 15f;
                _pauseMenu.transform.localPosition = new(-1.25f, 0f, 0f);
                _archiMenu.SetActive(false);
            } else {
                _archiMenu.transform.localPosition += Vector3.down * Time.unscaledDeltaTime * 20f;
                if(_pauseMenuLeaving)
                    _pauseMenu.transform.localPosition = new Vector3(-1.25f, -15f, 0f);
                else
                _pauseMenu.transform.localPosition = new Vector3(-1.25f, -15f, 0f) -_archiMenu.transform.localPosition;
            }
        }
    }

    private void On_MainCameraScript_Start(On.mainCameraScript.orig_Start orig, mainCameraScript self) {
        orig(self);
        SetupMenu();

        _pauseMenu = self.transform.Find("Canvas/pauseMenu").gameObject;

        _archiMenu.transform.parent = self.transform.Find("Canvas").transform;
        _archiMenu.transform.localPosition = new(0f, -13f, 0f);
        _archiMenu.transform.localScale = new(1f, 1f, 1f);

        _archiMenuBtn = GameObject.Instantiate(_pauseMenu.transform.Find("Back").gameObject, _pauseMenu.transform);
        _archiMenuBtn.name = "archipelago button";
        _archiMenuBtn.transform.position += new Vector3(10f, 0, 0);

        var backBtn = self.pauseMenu.transform.Find("Back");
        var newBackBtn = GameObject.Instantiate(backBtn, _archiMenu.transform);
        newBackBtn.transform.localPosition = new(-12f, 3f, 0f);
        var calls = newBackBtn.GetComponent<Button>().onClick.m_PersistentCalls.m_Calls;
        calls[0].m_Target = _archiMenu.GetComponent<PauseMenuScrollHandler>();
        calls[0].m_MethodName = "DoCollapse";
        calls[1].m_Target = backBtn;

        var btnScript = _archiMenuBtn.GetComponent<Button>();
        btnScript.spriteState = ArchipelagoClient.Instance.Authenticated ? _archiBtnConnState : _archiBtnDcState;
        btnScript.image.sprite = btnScript.spriteState.pressedSprite;
        calls = btnScript.onClick.m_PersistentCalls.m_Calls;
        calls[0].m_Target = _archiMenu.GetComponent<PauseMenuScrollHandler>();
        calls[0].m_MethodName = "DoExpand";
        calls[1].m_Target = newBackBtn;

        _wipeSaveBtn.gameObject.SetActive(false);
        UpdateLog();
    }

    private void On_MainMenuCamScript_Update(On.mainMenuCamScript.orig_Update orig, mainMenuCamScript self) {
        orig(self);
        if(self.menuMode == "archiMenu") {
            _archiMenu.SetActive(true);
            if(Vector2.Distance(_archiMenu.transform.position, Vector2.zero) != 0f) {
                if(Vector2.Distance(_archiMenu.transform.position, Vector2.zero) < Time.deltaTime * 20f)
                    _archiMenu.transform.position = Vector3.zero;
                else {
                    _archiMenu.transform.position += Vector3.up * Time.deltaTime * 20f;
                    self.eventSystem.sendNavigationEvents = false;
                }
            }
            if(_consoleStateDirty) {
                _consoleStateDirty = false;
                _archiConsoleGroup.SetActive(_nextConsoleState);
                _archiConnGroup.SetActive(!_nextConsoleState);
            }
            if(_logDirty) {
                _logDirty = false;
                UpdateLog();
            }
            if(_wipeSaveBtn.IsPressed()) {
                if(!_hasWiped) {
                    var fac = _wipeSaveProgress.rectTransform.sizeDelta.x + Time.deltaTime * 132f / 5f;
                    if(fac > 132f) {
                        fac = 0f;
                        _hasWiped = true;
                        SaveFileRedirect.Instance.Wipe();
                    }
                    _wipeSaveProgress.rectTransform.sizeDelta = new(fac, 24f);
                }
            } else {
                _hasWiped = false;
                _wipeSaveProgress.rectTransform.sizeDelta = new(0f, 24f);
            }
        } else if(Vector2.Distance(_archiMenu.transform.position, Vector2.down * 15f) != 0f) {
            if(Vector2.Distance(_archiMenu.transform.position, Vector2.down * 15f) < Time.deltaTime * 20f) {
                _archiMenu.transform.position = Vector3.down * 15f;
                _archiMenu.SetActive(false);
            } else {
                _archiMenu.transform.position += Vector3.down * Time.deltaTime * 20f;
                self.eventSystem.sendNavigationEvents = false;
            }
        }
    }

    private void On_MainMenuCamScript_Start(On.mainMenuCamScript.orig_Start orig, mainMenuCamScript self) {
        orig(self);
        SetupMenu();

        _archiMenu.transform.parent = GameObject.Find("Canvas").transform;
        _archiMenu.transform.position = new(0f, -13f, 0f);
        _archiMenu.transform.localScale = new(16f, 16f, 16f);

        var menuBtns = GameObject.Find("Canvas/mainMenu");
        foreach(Transform btn in menuBtns.transform) {
            btn.position += new Vector3(0, 2f, 0);
        }
        _archiMenuBtn = GameObject.Instantiate(GameObject.Find("Canvas/mainMenu/setting button"), menuBtns.transform);
        _archiMenuBtn.name = "archipelago button";
        _archiMenuBtn.transform.position += new Vector3(0, -4f, 0);

        var newBackBtn = GameObject.Instantiate(self.settingsMenu.transform.Find("Back"), _archiMenu.transform);
        newBackBtn.transform.localPosition = new(-12f, 3f, 0f);

        var btnScript = _archiMenuBtn.GetComponent<Button>();
        btnScript.spriteState = ArchipelagoClient.Instance.Authenticated ? _archiBtnConnState : _archiBtnDcState;
        btnScript.image.sprite = btnScript.spriteState.pressedSprite;
        btnScript.onClick.m_PersistentCalls.m_Calls[0].arguments.stringArgument = "archiMenu";
        btnScript.onClick.m_PersistentCalls.m_Calls[2].m_Target = newBackBtn;
    }
    #endregion

    ////// Public API //////

    public void OnConnect() {
        if(_archiMenu == null) return;
        _nextConsoleState = true;
        _consoleStateDirty = true;
        _archiMenu.transform.Find("ConnInfo").GetComponent<Text>().text = Plugin.AP_DISPLAY_INFO + " connected";
        var btnScript = _archiMenuBtn.GetComponent<Button>();
        btnScript.spriteState = _archiBtnConnState;
        btnScript.image.sprite = btnScript.spriteState.pressedSprite;
    }

    public void OnDisconnect() {
        if(_archiMenu == null) return;
        _nextConsoleState = false;
        _consoleStateDirty = true;
        _archiMenu.transform.Find("ConnInfo").GetComponent<Text>().text = Plugin.AP_DISPLAY_INFO + " disconnected";
        var btnScript = _archiMenuBtn.GetComponent<Button>();
        btnScript.spriteState = _archiBtnDcState;
        btnScript.image.sprite = btnScript.spriteState.pressedSprite;
    }

    public void OnConnectFail() {
        if(_archiMenu == null) return;
    }

    public void ReceiveMessage(string msg) {
        _logLines.Add(msg);
        if(_logLines.Count > _MAX_LOG_LINES)
            _logLines.RemoveAt(0);
        _logDirty = true;
        Plugin.BepinLogger.LogMessage($"[APMsg] {msg}");
    }


    ////// Private API //////

    private void SetupMenu() {
        _archiMenu = GameObject.Instantiate(Plugin.Resources.LoadAsset<GameObject>("Assets/Prefabs/ArchiMenu.prefab"));

        _archiMenu.transform.Find("Title").GetComponent<Text>().text = Plugin.MOD_DISPLAY_INFO;
        _archiMenu.transform.Find("ConnInfo").GetComponent<Text>().text = Plugin.AP_DISPLAY_INFO + (ArchipelagoClient.Instance.Authenticated ? " connected" : " disconnected");

        _archiMenu.AddComponent<PauseMenuScrollHandler>();

        _archiConsoleGroup = _archiMenu.transform.Find("ConsoleGroup").gameObject;
        _archiConnGroup = _archiMenu.transform.Find("ConnectionGroup").gameObject;

        _hostField = _archiConnGroup.transform.Find("HostField/Backdrop/Field").GetComponent<InputField>();
        _userField = _archiConnGroup.transform.Find("UserField/Backdrop/Field").GetComponent<InputField>();
        _passField = _archiConnGroup.transform.Find("PassField/Backdrop/Field").GetComponent<InputField>();
        _consoleField = _archiConsoleGroup.transform.Find("ConsoleInputField/Backdrop/Field").GetComponent<InputField>();
        _consoleText = _archiConsoleGroup.transform.Find("ConsoleOutput/Backdrop/Scrollbox/Field").GetComponent<Text>();
        _consoleScroll = _archiConsoleGroup.transform.Find("ConsoleOutput/Backdrop/Scrollbox").GetComponent<ScrollRect>();

        _hostField.text = ArchipelagoClient.Instance.AutoConnectHostname;
        if(_hostField.text == "") _hostField.text = "localhost";
        _userField.text = ArchipelagoClient.Instance.AutoConnectSlot;
        if(_userField.text == "") _userField.text = "Player1";
        _passField.text = ArchipelagoClient.Instance.AutoConnectPassword;

        _wipeSaveBtn = _archiConnGroup.transform.Find("WipeSaveBtn").GetComponent<Button>();
        _wipeSaveProgress = _wipeSaveBtn.transform.Find("Progress").GetComponent<Image>();

        _archiConnGroup.transform.Find("ConnectBtn").GetComponent<Button>().onClick.AddListener(() => {
            if(_userField.text.IsNullOrWhiteSpace()) return;
            ArchipelagoClient.Instance.Connect(_hostField.text, _userField.text, _passField.text);
        });

        _consoleField.onSubmit.AddListener(content => {
            ArchipelagoClient.Instance.SendMessage(content);
            _consoleText.text += "\r\n" + content;
            _consoleField.text = "";
            LayoutRebuilder.ForceRebuildLayoutImmediate(_consoleScroll.rectTransform);
            _consoleScroll.SetVerticalNormalizedPosition(0f);
        });

        _nextConsoleState = ArchipelagoClient.Instance.Authenticated;
        _consoleStateDirty = true;
    }

    private void UpdateLog() {
        if(_archiMenu == null) return;
        _consoleText.text = string.Join("\r\n", _logLines);
    }

    private class PauseMenuScrollHandler : MonoBehaviour {
        public void DoCollapse() {
            MainMenuInjector.Instance._pauseMenuState = false;
        }
        public void DoExpand() {
            MainMenuInjector.Instance._pauseMenuState = true;
        }
    }
}
