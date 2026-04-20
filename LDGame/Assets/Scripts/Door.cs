using UnityEngine;

/// <summary>
/// 门类型
/// </summary>
public enum DoorType
{
    [Tooltip("普通门：钥匙开门后渐出消失")]
    Normal,
    [Tooltip("通关门：通关所需的门，不会消失")]
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

    [Header("渐出设置（仅普通门生效）")]
    [Tooltip("开门后停留延迟（秒）")]
    public float fadeDelay = 0.5f;

    [Tooltip("渐出时长（秒）")]
    public float fadeDuration = 0.5f;

    [Header("门状态")]
    [Tooltip("门是否已打开")]
    public bool isOpen = false;

    [Header("立绘切换")]
    public Sprite closedSprite;
    public Sprite openSprite;
    public SpriteRenderer spriteRenderer;

    private float fadeStartTime = -1f;
    private bool isFading = false;
    private Color originalColor;

    public string InteractionPrompt
    {
        get
        {
            if (isOpen)
                return "[F] 门已开启";
            return PlayerInteraction.Instance != null &&
                   PlayerInteraction.Instance.HasKey(requiredKeyId)
                   ? $"[F] 开锁 ({requiredKeyId})"
                   : $"[F] 需要 {requiredKeyId}";
        }
    }

    public bool CanInteract() => !isOpen;
    public DoorType GetDoorType() => doorType;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        UpdateVisual();
    }

    void Update()
    {
        if (!isFading || doorType == DoorType.Victory)
            return;

        float elapsed = Time.time - fadeStartTime;

        if (elapsed >= fadeDelay)
        {
            // 计算渐出进度
            float fadeProgress = (elapsed - fadeDelay) / fadeDuration;
            fadeProgress = Mathf.Clamp01(fadeProgress);

            // 应用透明度
            if (spriteRenderer != null)
            {
                Color color = originalColor;
                color.a = 1f - fadeProgress;
                spriteRenderer.color = color;
            }

            // 渐出完成，销毁
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
        if (isOpen) return;

        // 检查玩家是否有钥匙
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

        // 消耗钥匙
        PlayerInteraction.Instance?.ConsumeKey(requiredKeyId);

        // 切换立绘
        isOpen = true;
        UpdateVisual();

        // 通知游戏系统门已打开
        GameEvents.OnDoorOpened?.Invoke(this);

        // 普通门开始渐出
        if (doorType == DoorType.Normal)
        {
            StartFadeOut();
        }
    }

    private void StartFadeOut()
    {
        if (fadeDuration <= 0f)
        {
            // 直接销毁
            if (debugMode)
                Debug.Log($"[Door] {name} 直接消失（fadeDuration=0）", this);
            Destroy(gameObject);
            return;
        }

        fadeStartTime = Time.time;
        isFading = true;

        if (debugMode)
            Debug.Log($"[Door] {name} 开始渐出：延迟{fadeDelay}秒，时长{fadeDuration}秒", this);
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
