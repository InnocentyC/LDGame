using UnityEngine;

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

    [Header("门状态")]
    [Tooltip("门是否已打开")]
    public bool isOpen = false;

    [Header("立绘切换")]
    public Sprite closedSprite; 
    public Sprite openSprite;
    public SpriteRenderer spriteRenderer;

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

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateVisual();
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
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null && openSprite != null)
        {
            spriteRenderer.sprite = isOpen ? openSprite : closedSprite;
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
