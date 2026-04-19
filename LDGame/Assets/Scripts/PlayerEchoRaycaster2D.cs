using System;
using UnityEngine;

public class PlayerEchoRaycaster2D : MonoBehaviour
{
    [Header("Input")]// 检测射线按键（需要统一）
    public KeyCode scanKey = KeyCode.Space;

    [Header("Direction")]
    public bool useTransformRightAsForward = true;

    [Header("Sector Raycast")]// 检测射线半径和角度（需要和扫描统一）
    [Min(0.1f)] public float sectorRadius = 10f;
    [Range(1f, 180f)] public float sectorAngleDeg = 60f;
    [Range(1, 360)] public int rayCount = 80;// 检测射线数

    [Header("Timing")]// 检测射线冷却时间
    [Min(0f)] public float scanCooldown = 0f;

    [Header("Layers")]
    public LayerMask raycastMask;   // 所有会被射线检测到的层
    public bool includeTriggers = false;

    [Header("Debug")] //检测射线debug相关，可以忽视
    public bool drawDebugRays = true;
    public bool logHits = false;
    public Color debugRevealOnlyColor = Color.green;
    public Color debugRevealBlockColor = Color.cyan;
    public Color debugBlockOnlyColor = Color.red;
    public Color debugMissColor = Color.yellow;

    private float lastScanTime = -999f;

    void Update()
    {
        if (Input.GetKeyDown(scanKey) && Time.time >= lastScanTime + scanCooldown)
        {
            CastSectorRays();
            lastScanTime = Time.time;
        }
    }

    private void CastSectorRays()
    {
        Vector2 origin = transform.position;
        Vector2 forward = GetForward();

        int count = Mathf.Max(1, rayCount);
        float halfAngle = sectorAngleDeg * 0.5f;

        if (logHits)
        {
            Debug.Log($"[PlayerEchoRaycaster2D] scan start | origin={origin} forward={forward} radius={sectorRadius} angle={sectorAngleDeg} rayCount={count}", this);
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = raycastMask;
        filter.useTriggers = includeTriggers;

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0.5f : (float)i / (count - 1);
            float angleOffset = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector2 dir = Rotate(forward, angleOffset);

            // 用 RaycastAll 更直观，拿到一条射线上的所有命中
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, sectorRadius, raycastMask);

            if (hits == null || hits.Length == 0)
            {
                if (drawDebugRays)
                    Debug.DrawLine(origin, origin + dir * sectorRadius, debugMissColor, 0.25f);
                continue;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool blocked = false;
            float rayEndDistance = sectorRadius;
            Color rayColor = debugMissColor;

            for (int h = 0; h < hits.Length; h++)
            {
                RaycastHit2D hit = hits[h];
                if (hit.collider == null) continue;
                if (!includeTriggers && hit.collider.isTrigger) continue;

                Collider2D hitCol = hit.collider;
                EchoRayActiveTarget target = hitCol.GetComponentInParent<EchoRayActiveTarget>();

                if (target == null)
                {
                    // 没挂脚本的 collider：默认当纯阻挡体
                    blocked = true;
                    rayEndDistance = hit.distance;
                    rayColor = debugBlockOnlyColor;

                    if (logHits)
                        Debug.Log($"[Ray] blocked by plain collider: {hitCol.name}", hitCol);

                    break;
                }

                switch (target.rayBehavior)
                {
                    case EchoRayActiveTarget.RayBehavior.RevealOnly:
                        target.ActivateByRay();
                        rayColor = debugRevealOnlyColor;

                        if (logHits)
                            Debug.Log($"[Ray] reveal only: collider={hitCol.name}, target={target.name}", hitCol);
                        break;

                    case EchoRayActiveTarget.RayBehavior.RevealAndBlock:
                        target.ActivateByRay();
                        blocked = true;
                        rayEndDistance = hit.distance;
                        rayColor = debugRevealBlockColor;

                        if (logHits)
                            Debug.Log($"[Ray] reveal and block: collider={hitCol.name}, target={target.name}", hitCol);
                        break;

                    case EchoRayActiveTarget.RayBehavior.BlockOnly:
                        blocked = true;
                        rayEndDistance = hit.distance;
                        rayColor = debugBlockOnlyColor;

                        if (logHits)
                            Debug.Log($"[Ray] block only: collider={hitCol.name}, target={target.name}", hitCol);
                        break;
                }

                if (blocked)
                    break;
            }

            if (drawDebugRays)
            {
                Vector2 endPoint = origin + dir * rayEndDistance;
                Debug.DrawLine(origin, endPoint, blocked ? rayColor : debugRevealOnlyColor, 0.25f);

                if (!blocked && hits.Length == 0)
                    Debug.DrawLine(origin, origin + dir * sectorRadius, debugMissColor, 0.25f);
            }
        }
    }

    private Vector2 GetForward()
    {
        Vector2 forward = useTransformRightAsForward ? (Vector2)transform.right : (Vector2)transform.up;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector2.right;

        return forward.normalized;
    }

    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        ).normalized;
    }
}