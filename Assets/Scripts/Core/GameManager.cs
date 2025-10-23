// Assets/Scripts/Core/GameManager.cs
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)] // ensure GM initializes before binders/UI
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("Config")]
    public GameConstants cfg;

    [Header("UI Hooks")]
    [Tooltip("Optional: will be auto-bound by ResultsPanelBinder in the Run scene.")]
    public GameObject resultsPanel;

    public float Speed { get; private set; }
    public float Distance { get; private set; }
    public int Coins { get; private set; }
    public bool Alive { get; private set; } = true;

    float _startTime;
    bool _manualSpeed;

    public void SetDistance(float d) => Distance = Mathf.Max(0f, d);

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        EnsureResultsPanel();
        ResetRun();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        var panel = EnsureResultsPanel();
        ShowWithParents(panel, true);
    }

    // -------- Results panel resolution --------
    GameObject EnsureResultsPanel()
    {
        if (resultsPanel == null)
        {
            ResultsController rc = null;
#if UNITY_2021_3_OR_NEWER
            rc = UnityEngine.Object.FindFirstObjectByType<ResultsController>(FindObjectsInactive.Include);
#else
            rc = FindObjectOfType<ResultsController>(true);
#endif
            if (rc) resultsPanel = rc.gameObject;
            if (resultsPanel == null) resultsPanel = GameObject.Find("ResultsPanel");
        }
        return resultsPanel;
    }

    static void ShowWithParents(GameObject go, bool visible)
    {
        if (!go) return;
        if (visible)
        {
            Transform t = go.transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
        }
        else
        {
            if (go.activeSelf) go.SetActive(false);
        }
    }

    // -------- Run lifecycle --------
    public void ResetRun()
    {
        _manualSpeed = false;
        Speed = cfg ? cfg.startSpeed : 6f;
        Distance = 0;
        Coins = 0;
        Alive = true;
        _startTime = Time.time;

        // Make sure the results UI is hidden at run start
        var panel = EnsureResultsPanel();
        ShowWithParents(panel, false);
    }

    public void OverrideSpeed(float s) { _manualSpeed = true; Speed = s; }

    void Update()
    {
        if (!Alive) return;

        if (!_manualSpeed)
        {
            float t = Time.time - _startTime;
            int ramps = Mathf.FloorToInt(t / (cfg ? Mathf.Max(0.0001f, cfg.rampEverySec) : 5f));
            float start = cfg ? cfg.startSpeed : 6f;
            float delta = cfg ? cfg.rampDelta : 0.5f;
            float cap = cfg ? cfg.speedCap : 20f;
            Speed = Mathf.Min(cap, start + ramps * delta);
        }
        Distance += Speed * Time.deltaTime;
    }

    public void AddCoin(int n = 1) { Coins += n; }

    public void PlayerDied() => KillPlayer();

    public void KillPlayer()
    {
        if (!Alive) return;
        Alive = false;

        var panel = EnsureResultsPanel();
        if (!panel)
        {
            return;
        }

        // Make visible (including all parents)
        ShowWithParents(panel, true);

        // Bring to front within its parent canvas
        panel.transform.SetAsLastSibling();

        // Ensure it is visible and can receive input
        var cg = panel.GetComponent<CanvasGroup>();
        if (!cg) cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // Give the panel its own Canvas that overrides sorting so it draws above HUD
        var c = panel.GetComponent<Canvas>();
        if (!c) c = panel.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 1000; // higher than any HUD canvas

    }

    static string PathOf(Transform t)
    {
        if (t == null) return "<null>";
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }

}