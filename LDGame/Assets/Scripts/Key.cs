using UnityEngine;

/// <summary>
/// 钥匙：玩家靠近后按F拾取
/// </summary>
public class Key : MonoBehaviour, IInteractable
{
    [Header("调试")]
    [SerializeField] private bool debugMode = false;

    [Header("交互设置")]
    [Tooltip("玩家可以拾取钥匙的距离")]
    public float pickupRange = 1.5f;

    [Header("钥匙配置")]
    [Tooltip("钥匙ID，相同ID的钥匙和门匹配")]
    public string keyId = "Key01";

    private bool isCollected = false;

    public string InteractionPrompt => $"[F] 拾取 {keyId}";
    public bool CanInteract() => !isCollected;

    public void Interact()
    {
        if (isCollected) return;

        isCollected = true;

        if (debugMode)
            Debug.Log($"[Key] 拾取了钥匙: {keyId}", this);

        // 通知玩家获得钥匙
        PlayerInteraction.Instance?.OnKeyCollected(this);

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
