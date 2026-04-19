using UnityEngine;

public class EchoRayActiveTarget : MonoBehaviour
{
    public enum RayBehavior
    {
        RevealOnly,       // 扫描会显示白色轮廓，但不挡后面的射线
        RevealAndBlock,   // 会显示，也会挡后面的射线
        BlockOnly         // 不显示，但会挡后面的射线
    }

    [Header("Ray Behavior")]// 选择是否显示和阻挡扫描
    public RayBehavior rayBehavior = RayBehavior.RevealOnly;

    [Header("Outline Control")]
    public SpriteRenderer outlineRenderer;// 拖入白色轮廓的obj
    public GameObject outlineObject;

    [Header("Timing")]//持续时间
    [Min(0.01f)] public float activeDuration = 2.0f;

    [Header("Debug")]
    public bool logStateChange = false;

    private float activeUntil = -999f;

    void Awake()
    {
        ApplyVisible(false, true);
    }

    void Update()
    {
        bool shouldShow = Time.time < activeUntil;
        ApplyVisible(shouldShow, false);
    }

    public void ActivateByRay()
    {
        if (rayBehavior == RayBehavior.BlockOnly)
            return;

        activeUntil = Mathf.Max(activeUntil, Time.time + activeDuration);
        ApplyVisible(true, false);

        if (logStateChange)
            Debug.Log($"[EchoRayActiveTarget] {name} activated until {activeUntil:F2}", this);
    }

    public bool BlocksRay()
    {
        return rayBehavior == RayBehavior.RevealAndBlock || rayBehavior == RayBehavior.BlockOnly;
    }

    private void ApplyVisible(bool visible, bool force)
    {
        if (outlineRenderer != null)
        {
            if (force || outlineRenderer.enabled != visible)
                outlineRenderer.enabled = visible;
        }

        if (outlineObject != null)
        {
            if (force || outlineObject.activeSelf != visible)
                outlineObject.SetActive(visible);
        }
    }
}