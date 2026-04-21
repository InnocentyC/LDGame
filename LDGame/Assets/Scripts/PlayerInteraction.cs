using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家交互控制器：管理交互范围检测和按键响应
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    // 单例（如果需要全局访问）
    public static PlayerInteraction Instance { get; private set; }

    [Header("调试")]
    [SerializeField] private bool debugMode = false;

    [Header("交互设置")]
    [Tooltip("检测交互物的半径")]
    public float detectionRadius = 1.5f;

    [Tooltip("交互按键")]
    public KeyCode interactKey = KeyCode.F;

    [Header("UI提示")]
    public bool showInteractionPrompt = true;
    public Vector2 promptOffset = new Vector2(0, 1f);

    // 已收集的钥匙列表
    private readonly HashSet<string> collectedKeys = new HashSet<string>();
    
    // 当前范围内可交互的物体
    private IInteractable nearestInteractable;
    private Collider2D[] nearbyColliders;
    private const int MaxColliders = 16;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        nearbyColliders = new Collider2D[MaxColliders];
    }

    void Update()
    {
        DetectNearbyInteractables();

        // 按键交互
        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    /// <summary>
    /// 检测附近的可交互物体
    /// </summary>
    private void DetectNearbyInteractables()
    {
        nearestInteractable = null;
        float nearestDist = float.MaxValue;

        // 使用OverlapCircleAll检测附近所有物体
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, 
            detectionRadius, 
            nearbyColliders
        );

        for (int i = 0; i < count; i++)
        {
            IInteractable interactable = nearbyColliders[i].GetComponent<IInteractable>();
            
            if (interactable != null && interactable.CanInteract())
            {
                float dist = Vector2.Distance(
                    transform.position, 
                    nearbyColliders[i].transform.position
                );

                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestInteractable = interactable;
                }
            }
        }

        // 更新UI提示
        UpdatePromptUI();
    }

    /// <summary>
    /// 尝试交互
    /// </summary>
    private void TryInteract()
    {
        if (nearestInteractable != null && nearestInteractable.CanInteract())
        {
            if (debugMode)
                Debug.Log($"[PlayerInteraction] 交互: {nearestInteractable.InteractionPrompt}", this);

            nearestInteractable.Interact();
        }
        else
        {
            if (debugMode)
                Debug.Log("[PlayerInteraction] 范围内没有可交互的物体", this);
        }
    }

    /// <summary>
    /// 更新UI提示
    /// </summary>
    private void UpdatePromptUI()
    {
        // 这里可以连接UI系统
        
        if (showInteractionPrompt && nearestInteractable != null)
        {
            // 简单的屏幕空间提示（需要配合Canvas和UI系统）
            // 这里只是Debug展示
            Debug.Log($"提示: {nearestInteractable.InteractionPrompt}");
        }
    }

    /// <summary>
    /// 钥匙被收集时调用
    /// </summary>
    public void OnKeyCollected(Key key)
    {
        collectedKeys.Add(key.keyId);
        KeyUI.Instance?.ShowKey(key.keyId);

        if (debugMode)
            Debug.Log($"[PlayerInteraction] 获得钥匙: {key.keyId} (已收集: {collectedKeys.Count})", this);
    }

    /// <summary>
    /// 消耗钥匙（开门时调用）
    /// </summary>
    public void ConsumeKey(string keyId)
    {
        if (collectedKeys.Contains(keyId))
        {
            collectedKeys.Remove(keyId);

            if (debugMode)
                Debug.Log($"[PlayerInteraction] 消耗钥匙: {keyId} (剩余: {collectedKeys.Count})", this);
        }
    }

    /// <summary>
    /// 检查是否拥有指定钥匙
    /// </summary>
    public bool HasKey(string keyId)
    {
        return collectedKeys.Contains(keyId);
    }

    /// <summary>
    /// 获取已收集钥匙数量
    /// </summary>
    public int CollectedKeyCount => collectedKeys.Count;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
