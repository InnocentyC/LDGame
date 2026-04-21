using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 门类型
/// </summary>
public enum DoorType
{
    [Tooltip("普通门：钥匙开门后渐出消失")]
    Normal,
    [Tooltip("通关门：钥匙开门后黑幕渐暗并切换场景")]
    Victory
}

/// <summary>
/// 门：需要钥匙才能打开
/// </summary>
public class Door : MonoBehaviour, IInteractable
{
    [Header("调试")]
    [SerializeField] private bool debugMode = false;

    [Header("交互设置")]
    [Tooltip("玩家可以交互的距离")]
    public float interactRange = 1.5f;

    [Header("门配置")]
    [Tooltip("匹配的钥匙ID")]
    public string requiredKeyId = "Key01";

    [Tooltip("门类型：普通门/通关门")]
    public DoorType doorType = DoorType.Normal;

    [Header("普通门渐出设置（仅 Normal 生效）")]
    [Tooltip("开门后停留延迟（秒）")]
    public float fadeDelay = 0.5f;

    [Tooltip("渐出时长（秒）")]
    public float fadeDuration = 0.5f;

    [Header("通关门设置（仅 Victory 生效）")]
    [Tooltip("要跳转到的场景名（需已加入 Build Settings）")]
    public string nextSceneName = "";

    [Tooltip("全屏黑幕遮罩 Image（建议放在 Canvas 最上层）")]
    public Image sceneFadeMask;

    [Tooltip("黑幕渐暗时长（秒）")]
    public float sceneFadeDuration = 1.2f;

    [Tooltip("完全变黑后，切场景前额外等待时间（秒）")]
    public float sceneLoadDelay = 0.2f;

    [Header("门状态")]
    [Tooltip("门是否已打开")]
    public bool isOpen = false;

    [Header("立绘切换")]
    public Sprite closedSprite;
    public Sprite openSprite;
    public SpriteRenderer spriteRenderer;

    private float fadeStartTime = -1f;
    private bool isFading = false;
    private bool isTransitioning = false;
    private Color originalColor;

    public string InteractionPrompt
    {
        get
        {
            if (isOpen)
            {
                if (doorType == DoorType.Victory)
                    return "[F] 门已开启";
                return "[F] 门已开启";
            }

            return PlayerInteraction.Instance != null &&
                   PlayerInteraction.Instance.HasKey(requiredKeyId)
                   ? $"[F] 开锁 ({requiredKeyId})"
                   : $"[F] 需要 {requiredKeyId}";
        }
    }

    public bool CanInteract() => !isOpen && !isTransitioning;
    public DoorType GetDoorType() => doorType;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // 初始化黑幕为透明
        if (sceneFadeMask != null)
        {
            Color c = sceneFadeMask.color;
            c.a = 0f;
            sceneFadeMask.color = c;
            sceneFadeMask.raycastTarget = false;
            sceneFadeMask.gameObject.SetActive(true);
        }

        UpdateVisual();
    }

    void Update()
    {
        if (!isFading || doorType == DoorType.Victory)
            return;

        float elapsed = Time.time - fadeStartTime;

        if (elapsed >= fadeDelay)
        {
            float fadeProgress = (elapsed - fadeDelay) / fadeDuration;
            fadeProgress = Mathf.Clamp01(fadeProgress);

            if (spriteRenderer != null)
            {
                Color color = originalColor;
                color.a = 1f - fadeProgress;
                spriteRenderer.color = color;
            }

            if (fadeProgress >= 1f)
            {
                if (debugMode)
                    Debug.Log($"[Door] {name} 渐出消失", this);

                Destroy(gameObject);
            }
        }
    }

    public void Interact()
    {
        if (isOpen || isTransitioning)
            return;

        if (PlayerInteraction.Instance != null &&
            PlayerInteraction.Instance.HasKey(requiredKeyId))
        {
            OpenDoor();
        }
        else
        {
            if (debugMode)
                Debug.Log($"[Door] {name} 需要钥匙: {requiredKeyId}", this);
        }
    }

    private void OpenDoor()
    {
        if (debugMode)
            Debug.Log($"[Door] {name} 开门！消耗钥匙: {requiredKeyId}", this);

        PlayerInteraction.Instance?.ConsumeKey(requiredKeyId);

        isOpen = true;
        UpdateVisual();

        GameEvents.OnDoorOpened?.Invoke(this);

        if (doorType == DoorType.Normal)
        {
            StartFadeOut();
        }
        else if (doorType == DoorType.Victory)
        {
            StartVictoryTransition();
        }
    }

    private void StartFadeOut()
    {
        if (fadeDuration <= 0f)
        {
            if (debugMode)
                Debug.Log($"[Door] {name} 直接消失（fadeDuration=0）", this);

            Destroy(gameObject);
            return;
        }

        fadeStartTime = Time.time;
        isFading = true;

        if (debugMode)
            Debug.Log($"[Door] {name} 开始渐出：延迟 {fadeDelay} 秒，时长 {fadeDuration} 秒", this);
    }

    private void StartVictoryTransition()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;
        StartCoroutine(VictoryTransitionRoutine());
    }

    private IEnumerator VictoryTransitionRoutine()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError($"[Door] {name} 的 Victory 门没有填写 nextSceneName！", this);
            yield break;
        }

        if (sceneFadeMask == null)
        {
            Debug.LogError($"[Door] {name} 的 Victory 门没有指定 sceneFadeMask！", this);
            yield break;
        }

        if (debugMode)
            Debug.Log($"[Door] {name} 开始通关过场，准备进入场景：{nextSceneName}", this);

        float timer = 0f;
        Color c = sceneFadeMask.color;

        while (timer < sceneFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / sceneFadeDuration);

            c.a = t;
            sceneFadeMask.color = c;

            yield return null;
        }

        c.a = 1f;
        sceneFadeMask.color = c;

        if (sceneLoadDelay > 0f)
            yield return new WaitForSeconds(sceneLoadDelay);

        SceneManager.LoadScene(nextSceneName);
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            if (isOpen && openSprite != null)
                spriteRenderer.sprite = openSprite;
            else if (closedSprite != null)
                spriteRenderer.sprite = closedSprite;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isOpen ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}

/// <summary>
/// 游戏事件静态类
/// </summary>
public static class GameEvents
{
    public static System.Action<Door> OnDoorOpened;
}