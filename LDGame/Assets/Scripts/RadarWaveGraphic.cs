using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RadarWaveGraphic : MaskableGraphic
{
    public Transform player;
    public Transform target;

    [Header("Shape")]
    [Range(32, 512)] public int segments = 180;
    public float baseRadius = 80f;
    public float ringThickness = 10f;

    [Header("Noise")]
    public float noiseAmplitude = 6f;
    public float noiseFrequency = 4f;
    public float noiseSpeed = 2f;

    [Header("Peak")]
    public float peakHeight = 22f;
    public float peakWidth = 18f; // degrees
    public bool relativeToPlayerRotation = true;
    public float angleOffset = 0f; // 如果你的“前方”不是右侧，可在这里补偏移

    [Header("Distance")]
    public bool useDistanceEffect = false;
    public float maxDistance = 20f;
    public float minPeakScale = 0.35f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float targetAngle = 0f;
        bool hasTarget = player != null && target != null;

        if (hasTarget)
            targetAngle = GetTargetAngle();

        Vector2 center = rectTransform.rect.center;

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            float angleDegA = (float)i / segments * 360f;
            float angleDegB = (float)next / segments * 360f;

            float angleRadA = angleDegA * Mathf.Deg2Rad;
            float angleRadB = angleDegB * Mathf.Deg2Rad;

            float outerA = GetRadius(angleDegA, angleRadA, hasTarget, targetAngle);
            float outerB = GetRadius(angleDegB, angleRadB, hasTarget, targetAngle);

            float innerA = Mathf.Max(0f, outerA - ringThickness);
            float innerB = Mathf.Max(0f, outerB - ringThickness);

            Vector2 v0 = center + new Vector2(Mathf.Cos(angleRadA), Mathf.Sin(angleRadA)) * innerA;
            Vector2 v1 = center + new Vector2(Mathf.Cos(angleRadA), Mathf.Sin(angleRadA)) * outerA;
            Vector2 v2 = center + new Vector2(Mathf.Cos(angleRadB), Mathf.Sin(angleRadB)) * outerB;
            Vector2 v3 = center + new Vector2(Mathf.Cos(angleRadB), Mathf.Sin(angleRadB)) * innerB;

            int startIndex = vh.currentVertCount;

            vh.AddVert(v0, color, new Vector2(0f, 0f));
            vh.AddVert(v1, color, new Vector2(0f, 1f));
            vh.AddVert(v2, color, new Vector2(1f, 1f));
            vh.AddVert(v3, color, new Vector2(1f, 0f));

            vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);
        }
    }

    void Update()
    {
        SetVerticesDirty();
    }

    float GetTargetAngle()
    {
        Vector2 dir = (target.position - player.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (relativeToPlayerRotation)
            angle -= player.eulerAngles.z;

        angle += angleOffset;
        angle = (angle + 360f) % 360f;
        return angle;
    }

    float GetRadius(float angleDeg, float angleRad, bool hasTarget, float targetAngle)
    {
        float noise = Mathf.Sin(angleRad * noiseFrequency + Time.time * noiseSpeed) * noiseAmplitude;

        float peak = 0f;

        if (hasTarget)
        {
            float delta = Mathf.DeltaAngle(angleDeg, targetAngle);
            peak = Mathf.Exp(-(delta * delta) / (2f * peakWidth * peakWidth)) * peakHeight;

            if (useDistanceEffect)
            {
                float dist = Vector2.Distance(player.position, target.position);
                float t = Mathf.Clamp01(dist / maxDistance);
                float scale = Mathf.Lerp(1f, minPeakScale, t);
                peak *= scale;
            }
        }

        return baseRadius + noise + peak;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
