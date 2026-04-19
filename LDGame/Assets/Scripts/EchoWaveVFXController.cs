using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EchoWaveVFXController : MonoBehaviour
{
    //扫描声波视觉效果设置
    [Header("Runtime")] 
    public float radius = 5f;
    public float angleDeg = 60f;
    public float duration = 0.6f;

    [Header("Wave Shape")]
    public float frontWidth = 0.35f;
    public float trailWidth = 1.2f;
    [Range(0f, 1f)] public float trailAlpha = 0.25f;

    [Header("Edge Soft")]
    public float edgeSoftDeg = 3f;
    [Range(0f, 1f)] public float edgeAlphaMin = 0.85f;

    [Header("Arc Look")]
    public float arcLineWidth = 0.18f;
    [Range(0f, 2f)] public float arcLineAlpha = 1.2f;

    [Header("Fade")]
    [Range(0f, 1f)] public float startAlpha = 1f;
    public AnimationCurve alphaOverLifetime = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Forward Axis")]
    public bool useTransformRightAsForward = true;

    private SpriteRenderer sr;
    private MaterialPropertyBlock mpb;
    private float birthTime;
    private bool initialized;

    static readonly int OriginWSID = Shader.PropertyToID("_OriginWS");
    static readonly int ForwardWSID = Shader.PropertyToID("_ForwardWS");
    static readonly int RadiusID = Shader.PropertyToID("_Radius");
    static readonly int AngleDegID = Shader.PropertyToID("_AngleDeg");
    static readonly int ProgressID = Shader.PropertyToID("_Progress");
    static readonly int FrontWidthID = Shader.PropertyToID("_FrontWidth");
    static readonly int TrailWidthID = Shader.PropertyToID("_TrailWidth");
    static readonly int TrailAlphaID = Shader.PropertyToID("_TrailAlpha");
    static readonly int EdgeSoftDegID = Shader.PropertyToID("_EdgeSoftDeg");
    static readonly int EdgeAlphaMinID = Shader.PropertyToID("_EdgeAlphaMin");
    static readonly int ArcLineWidthID = Shader.PropertyToID("_ArcLineWidth");
    static readonly int ArcLineAlphaID = Shader.PropertyToID("_ArcLineAlpha");
    static readonly int GlobalAlphaID = Shader.PropertyToID("_GlobalAlpha");

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    public void Init(
        float radius,
        float angleDeg,
        float duration,
        float frontWidth,
        float trailWidth,
        float trailAlpha,
        float edgeSoftDeg,
        float edgeAlphaMin,
        float arcLineWidth,
        float arcLineAlpha,
        float startAlpha
    )
    {
        this.radius = radius;
        this.angleDeg = angleDeg;
        this.duration = Mathf.Max(0.01f, duration);
        this.frontWidth = frontWidth;
        this.trailWidth = trailWidth;
        this.trailAlpha = trailAlpha;
        this.edgeSoftDeg = edgeSoftDeg;
        this.edgeAlphaMin = edgeAlphaMin;
        this.arcLineWidth = arcLineWidth;
        this.arcLineAlpha = arcLineAlpha;
        this.startAlpha = startAlpha;

        birthTime = Time.time;
        initialized = true;

        // 这里只是让 Sprite 覆盖足够大的区域供 shader 裁切。
        // 真正的扇形范围和角度现在都按世界坐标算，不靠 localPos 了。
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

        ApplyProperties(0f, startAlpha);
    }

    void Update()
    {
        if (!initialized) return;

        float age = Time.time - birthTime;
        float t = age / duration;

        if (t >= 1f)
        {
            ApplyProperties(1f, 0f);
            Destroy(gameObject);
            return;
        }

        float alpha = startAlpha * Mathf.Clamp01(alphaOverLifetime.Evaluate(t));
        ApplyProperties(t, alpha);
    }

    private void ApplyProperties(float progress, float alpha)
    {
        sr.GetPropertyBlock(mpb);

        Vector2 origin = transform.position;
        Vector2 forward = useTransformRightAsForward ? (Vector2)transform.right : (Vector2)transform.up;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector2.right;

        forward.Normalize();

        mpb.SetVector(OriginWSID, new Vector4(origin.x, origin.y, 0f, 0f));
        mpb.SetVector(ForwardWSID, new Vector4(forward.x, forward.y, 0f, 0f));

        mpb.SetFloat(RadiusID, radius);
        mpb.SetFloat(AngleDegID, angleDeg);
        mpb.SetFloat(ProgressID, Mathf.Clamp01(progress));

        mpb.SetFloat(FrontWidthID, frontWidth);
        mpb.SetFloat(TrailWidthID, trailWidth);
        mpb.SetFloat(TrailAlphaID, trailAlpha);

        mpb.SetFloat(EdgeSoftDegID, edgeSoftDeg);
        mpb.SetFloat(EdgeAlphaMinID, edgeAlphaMin);

        mpb.SetFloat(ArcLineWidthID, arcLineWidth);
        mpb.SetFloat(ArcLineAlphaID, arcLineAlpha);

        mpb.SetFloat(GlobalAlphaID, Mathf.Clamp01(alpha));

        sr.SetPropertyBlock(mpb);
    }
}