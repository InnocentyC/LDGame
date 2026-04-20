using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器：管理场景切换和关卡胜利
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("场景名称")]
    public string startSceneName = "StartScene";
    public string tutorialSceneName = "TutorialLevel";
    public string level1SceneName = "Level1";
    public string level2SceneName = "Level2";

    [Header("关卡配置")]
    [Tooltip("当前关卡需要打开的门数量")]
    public int doorsRequiredForVictory = 1;

    [Header("当前状态")]
    [SerializeField] private int doorsOpened = 0;
    [SerializeField] private bool isVictory = false;

    // 事件
    public System.Action OnVictory;
    public System.Action<int, int> OnDoorProgressChanged; // 当前/需要

    public bool IsVictory => isVictory;
    public int DoorsOpened => doorsOpened;
    public int DoorsRequired => doorsRequiredForVictory;

    void Awake()
    {
        // 单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        GameEvents.OnDoorOpened += OnDoorOpened;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        GameEvents.OnDoorOpened -= OnDoorOpened;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 门被打开时调用
    /// </summary>
    private void OnDoorOpened(Door door)
    {
        if (isVictory) return;

        doorsOpened++;
        OnDoorProgressChanged?.Invoke(doorsOpened, doorsRequiredForVictory);
        Debug.Log($"[GameManager] 门已打开: {doorsOpened}/{doorsRequiredForVictory}");

        // 检查胜利条件
        if (doorsOpened >= doorsRequiredForVictory)
        {
            TriggerVictory();
        }
    }

    /// <summary>
    /// 触发胜利
    /// </summary>
    private void TriggerVictory()
    {
        if (isVictory) return;

        isVictory = true;
        Debug.Log("[GameManager] 关卡胜利！");
        OnVictory?.Invoke();
    }

    /// <summary>
    /// 场景加载完成
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 重置状态
        doorsOpened = 0;
        isVictory = false;
        Debug.Log($"[GameManager] 加载场景: {scene.name}");
    }

    /// <summary>
    /// 加载开始场景
    /// </summary>
    public void LoadStartScene()
    {
        LoadScene(startSceneName);
    }

    /// <summary>
    /// 加载教学关
    /// </summary>
    public void LoadTutorialScene()
    {
        LoadScene(tutorialSceneName);
    }

    /// <summary>
    /// 加载第一关
    /// </summary>
    public void LoadLevel1()
    {
        LoadScene(level1SceneName);
    }

    /// <summary>
    /// 加载第二关
    /// </summary>
    public void LoadLevel2()
    {
        LoadScene(level2SceneName);
    }

    /// <summary>
    /// 加载指定场景
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GameManager] 场景名称为空！");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 获取当前场景名称
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// 检查是否为开始场景
    /// </summary>
    public bool IsStartScene()
    {
        return SceneManager.GetActiveScene().name == startSceneName;
    }

    /// <summary>
    /// 检查是否为教程关
    /// </summary>
    public bool IsTutorialScene()
    {
        return SceneManager.GetActiveScene().name == tutorialSceneName;
    }

    /// <summary>
    /// 设置胜利需要的门数量
    /// </summary>
    public void SetDoorsRequired(int count)
    {
        doorsRequiredForVictory = Mathf.Max(1, count);
    }
}
