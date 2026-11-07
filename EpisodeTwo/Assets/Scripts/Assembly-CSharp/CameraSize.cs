using UnityEngine;

public class CameraSize : MonoBehaviour
{
    public float percent = 1f;
    public bool IsHud;
    [Range(0f, 1f)] public float q10Size = 0.5f;
    [Range(-50f, 200f)] public float q10Offset = 16f;
    public static float CameraAspect = LegacyScreenFit.ReferenceAspect;
    private Camera _camera;
    private bool _levelScene;
    private bool _mainMenu;
    private bool _loadingScene;
    private float _referenceSize;
    private Rect _lastViewport;
    private Rect _lastSafeArea;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _referenceSize = _camera.orthographicSize;
        _levelScene = gameObject.scene.path.Contains("/Scenes/Levels/");
        _mainMenu = gameObject.scene.name == "MainMenu";
        _loadingScene = gameObject.scene.name == "LoadLevel";
        UpdateViewport();
    }

    private void Start()
    {
        FixAspect();
        bool hasMenuBackdrop=false;
        foreach(Renderer backdrop in transform.root.GetComponentsInChildren<Renderer>(true))
            if(backdrop.name=="Background")hasMenuBackdrop|=MenuBackdropExtension.TryApply(_camera,backdrop);
        if (IsHud || !_levelScene)
        {
            foreach(MonoBehaviour panel in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if(panel==null || panel.gameObject.scene!=gameObject.scene)continue;
                string type=panel.GetType().Name;
                if(type!="PauseScreen" && type!="GameOverScreen" && type!="Alert" && type!="ConfirmScreen" && type!="OptionScreen" && type!="IngameOptionScreen" && type!="CalibrateScreen" && type!="SpheroConnectScreen")continue;
                var layout=panel.GetComponent<MenuContentFit>();
                if(layout==null)layout=panel.gameObject.AddComponent<MenuContentFit>();
                layout.Camera=_camera;
            }
        }
        if (_loadingScene)
        {
            _camera.clearFlags=CameraClearFlags.SolidColor;
            _camera.backgroundColor=Color.black;
            _camera.depth=1000f;
            var margins=gameObject.AddComponent<BackdropMargins>();margins.Camera=_camera;margins.CoverOriginal=true;
            foreach(LoadLevelScreen screen in UnityEngine.Object.FindObjectsByType<LoadLevelScreen>(FindObjectsInactive.Include))
            {
                var layout=screen.GetComponent<MenuContentFit>();
                if(layout==null)layout=screen.gameObject.AddComponent<MenuContentFit>();
                layout.Camera=_camera;
            }
        }
        if(!_levelScene && !_mainMenu && !_loadingScene)
        {
            var margins=gameObject.AddComponent<BackdropMargins>();margins.Camera=_camera;
        }
        if (_mainMenu)
        {
            if(!hasMenuBackdrop){var backdrop=gameObject.AddComponent<MenuBackdropExtension>();backdrop.Camera=_camera;}
            foreach(Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if(t.gameObject.scene != gameObject.scene || t.name != "MenuScreen") continue;
                var layout=t.GetComponent<MenuContentFit>();
                if(layout==null)layout=t.gameObject.AddComponent<MenuContentFit>();
                layout.Camera=_camera;
            }
        }
    }

    private void LateUpdate()
    {
        Rect viewport = LegacyScreenFit.FrameViewport();
        Rect safe = LegacyScreenFit.ContentRect();
        if (viewport != _lastViewport || safe != _lastSafeArea)
        {
            UpdateViewport();
            FixAspect();
        }
    }

    private void UpdateViewport()
    {
        _lastViewport = LegacyScreenFit.FrameViewport();
        _lastSafeArea = LegacyScreenFit.ContentRect();
        _camera.rect = _lastViewport;
        _camera.ResetProjectionMatrix();
        _camera.aspect = Screen.height > 0f ? Screen.width * _lastViewport.width / (Screen.height * _lastViewport.height) : LegacyScreenFit.ReferenceAspect;
    }

    private void FixAspect()
    {
        if (_levelScene)
        {
            float frameSize = LegacyScreenFit.LevelCameraSize(_camera, IsHud ? 256f : _referenceSize);
            if (IsHud) _camera.orthographicSize = frameSize;
            else
            {
                // Keep the authored 320-unit horizontal view. Tall displays
                // reveal additional scenery above and below the same center.
                _camera.orthographicSize = Mathf.Max(240f,160f / Mathf.Max(_camera.aspect,0.01f));
            }
        }
        else if (_loadingScene) _camera.orthographicSize=256f*LegacyScreenFit.ReferenceAspect/Mathf.Max(_camera.aspect,0.01f);
        else if (_mainMenu) _camera.orthographicSize = LegacyScreenFit.MainMenuCameraSize(_camera);
        else _camera.orthographicSize = 256f*LegacyScreenFit.ReferenceAspect/Mathf.Max(_camera.aspect,0.01f);
    }
}
