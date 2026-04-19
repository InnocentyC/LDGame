using System.Collections.Generic;
using UnityEngine;

public class PlayerEchoScanner : MonoBehaviour
{
    [Header("Input")] // 发射扫描的按键
    public KeyCode scanKey = KeyCode.Space;

    [Header("Direction")] // 设置初始默认方向
    public bool useTransformRightAsForward = true;

    [Header("Reveal Sector")]// 扫描波范围和宽度（角度）
    [Min(0.1f)] public float sectorRadius = 5f;
    [Range(1f, 180f)] public float sectorAngleDeg = 60f;

    [Header("Reveal Timing")]// 扫描持续时间和暗淡时间
    [Min(0f)] public float holdTime = 2f;
    [Min(0.01f)] public float fadeTime = 1f;

    [Header("Reveal Edge Soft")]// 扫描边界模糊
    [Range(0f, 20f)] public float edgeSoftDeg = 3f;
    [Range(0f, 1f)] public float edgeAlphaMin = 0.85f;

    [Header("Reveal History")]// 同时存在的扫描区域上限
    [Range(1, 16)] public int maxScans = 8;

    [Header("Wave VFX Prefab")] // 扫描波视觉效果
    public EchoWaveVFXController waveVFXPrefab;
    public Transform waveSpawnRoot;
    public int waveSortingOrder = 20;

    [Header("Wave VFX Settings")]// 扫描宽度强度视觉效果
    [Min(0.01f)] public float waveDuration = 0.6f;
    [Min(0.01f)] public float waveFrontWidth = 0.35f;
    [Min(0f)] public float waveTrailWidth = 1.2f;
    [Range(0f, 1f)] public float waveTrailAlpha = 0.25f;
    [Range(0f, 1f)] public float waveStartAlpha = 1f;
    
    [Header("Wave Arc Look")]// 扫描弧度视觉效果
    public float waveArcLineWidth = 0.18f;
    [Range(0f, 2f)] public float waveArcLineAlpha = 1.2f;

    private struct ScanEvent
    {
        public Vector2 origin;
        public Vector2 forward;
        public float birthTime;
    }

    private readonly List<ScanEvent> scans = new List<ScanEvent>();

    static readonly int EchoScanCountID = Shader.PropertyToID("_EchoScanCount");
    static readonly int EchoOriginsID = Shader.PropertyToID("_EchoOrigins");
    static readonly int EchoForwardsID = Shader.PropertyToID("_EchoForwards");
    static readonly int EchoBirthTimesID = Shader.PropertyToID("_EchoBirthTimes");
    static readonly int EchoRadiusID = Shader.PropertyToID("_EchoRadius");
    static readonly int EchoCosHalfAngleID = Shader.PropertyToID("_EchoCosHalfAngle");
    static readonly int EchoHoldTimeID = Shader.PropertyToID("_EchoHoldTime");
    static readonly int EchoFadeTimeID = Shader.PropertyToID("_EchoFadeTime");
    static readonly int EchoEdgeSoftCosID = Shader.PropertyToID("_EchoEdgeSoftCos");
    static readonly int EchoEdgeAlphaMinID = Shader.PropertyToID("_EchoEdgeAlphaMin");

    const int MaxShaderScans = 16;
    private readonly Vector4[] originsArray = new Vector4[MaxShaderScans];
    private readonly Vector4[] forwardsArray = new Vector4[MaxShaderScans];
    private readonly float[] birthTimesArray = new float[MaxShaderScans];

    void Update()
    {
        if (Input.GetKeyDown(scanKey))
        {
            EmitScan();
        }

        CleanupExpiredScans();
        PushGlobals();
    }

    private void EmitScan()
    {
        Vector2 origin = transform.position;
        Vector2 forward = GetCurrentForward();

        scans.Add(new ScanEvent
        {
            origin = origin,
            forward = forward,
            birthTime = Time.time
        });

        if (scans.Count > maxScans)
            scans.RemoveAt(0);

        SpawnWaveVFX(origin, forward);
    }

    private Vector2 GetCurrentForward()
    {
        Vector2 fwd = useTransformRightAsForward ? (Vector2)transform.right : (Vector2)transform.up;

        if (fwd.sqrMagnitude < 0.0001f)
            fwd = Vector2.right;

        return fwd.normalized;
    }

    private void SpawnWaveVFX(Vector2 origin, Vector2 forward)
    {
        if (waveVFXPrefab == null)
            return;

        Transform parent = waveSpawnRoot != null ? waveSpawnRoot : null;

        EchoWaveVFXController vfx = Instantiate(waveVFXPrefab, origin, Quaternion.identity, parent);

        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        vfx.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer sr = vfx.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = waveSortingOrder;
        }

        float waveRadius = sectorRadius * 5f;
        vfx.Init(
            sectorRadius,
            sectorAngleDeg,
            waveDuration,
            waveFrontWidth,
            waveTrailWidth,
            waveTrailAlpha,
            edgeSoftDeg,
            edgeAlphaMin,
            waveArcLineWidth,
        waveArcLineAlpha,
        waveStartAlpha
        );
    }

    private void CleanupExpiredScans()
    {
        float maxLife = holdTime + fadeTime;

        for (int i = scans.Count - 1; i >= 0; i--)
        {
            if (Time.time - scans[i].birthTime > maxLife)
                scans.RemoveAt(i);
        }
    }

    private void PushGlobals()
    {
        int count = Mathf.Min(scans.Count, MaxShaderScans);

        for (int i = 0; i < count; i++)
        {
            originsArray[i] = new Vector4(scans[i].origin.x, scans[i].origin.y, 0f, 0f);
            forwardsArray[i] = new Vector4(scans[i].forward.x, scans[i].forward.y, 0f, 0f);
            birthTimesArray[i] = scans[i].birthTime;
        }

        for (int i = count; i < MaxShaderScans; i++)
        {
            originsArray[i] = Vector4.zero;
            forwardsArray[i] = Vector4.zero;
            birthTimesArray[i] = -99999f;
        }

        float halfAngle = sectorAngleDeg * 0.5f;
        float cosHalfAngle = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

        float innerHalfAngle = Mathf.Max(0f, halfAngle - edgeSoftDeg);
        float edgeSoftCos = Mathf.Cos(innerHalfAngle * Mathf.Deg2Rad);

        Shader.SetGlobalInt(EchoScanCountID, count);
        Shader.SetGlobalVectorArray(EchoOriginsID, originsArray);
        Shader.SetGlobalVectorArray(EchoForwardsID, forwardsArray);
        Shader.SetGlobalFloatArray(EchoBirthTimesID, birthTimesArray);

        Shader.SetGlobalFloat(EchoRadiusID, sectorRadius);
        Shader.SetGlobalFloat(EchoCosHalfAngleID, cosHalfAngle);

        Shader.SetGlobalFloat(EchoHoldTimeID, holdTime);
        Shader.SetGlobalFloat(EchoFadeTimeID, fadeTime);

        Shader.SetGlobalFloat(EchoEdgeSoftCosID, edgeSoftCos);
        Shader.SetGlobalFloat(EchoEdgeAlphaMinID, edgeAlphaMin);
    }
}