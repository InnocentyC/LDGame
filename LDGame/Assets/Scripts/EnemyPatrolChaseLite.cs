using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrolChaseLite : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Chase,
        Return
    }

    [Header("References")]
    public Transform player;
    public Transform patrolRoot;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float arriveDistance = 0.15f;
    public float obstacleCheckDistance = 0.8f;
    public LayerMask obstacleLayer;

    [Header("Vision")]
    public float scanRadius = 7f;
    [Range(0f, 360f)] public float scanAngle = 120f;
    public int scanRayCount = 11;
    public float scanInterval = 0.25f;
    public LayerMask playerLayer;

    [Header("Avoidance")]
    public float[] avoidAngles = new float[] { 0f, 30f, -30f, 60f, -60f, 90f, -90f };

    [Header("Patrol")]
    public bool pingPongPatrol = true;

    [Header("Chase")]
    public float losePlayerReturnDelay = 1.2f;

    [Header("Raycast Buffer")]
    [Tooltip("一次 Raycast 最多缓存多少个命中，通常 8 就够了")]
    public int raycastBufferSize = 8;

    public State currentState = State.Patrol;

    private List<Transform> patrolPoints = new List<Transform>();
    private int patrolIndex = 0;
    private int patrolDirection = 1;

    private Vector2 lastSeenPlayerPos;
    private float scanTimer = 0f;
    private float lostPlayerTimer = 0f;
    private bool canSeePlayer = false;

    private Rigidbody2D rb;
    private RaycastHit2D[] raycastBuffer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (raycastBufferSize < 1)
            raycastBufferSize = 1;

        raycastBuffer = new RaycastHit2D[raycastBufferSize];

        LoadPatrolPoints();
    }

    private void Update()
    {
        HandleScan();
        HandleStateLogic();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void LoadPatrolPoints()
    {
        patrolPoints.Clear();

        if (patrolRoot == null) return;

        foreach (Transform child in patrolRoot)
        {
            patrolPoints.Add(child);
        }
    }

    private void HandleScan()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        canSeePlayer = false;

        if (player == null) return;

        Vector2 origin = transform.position;
        Vector2 forward = GetForwardDirection();

        Vector2 toPlayer = (Vector2)player.position - origin;

        // 玩家不在扫描半径内
        if (toPlayer.magnitude > scanRadius) return;

        // 玩家不在视角内
        float angleToPlayer = Vector2.Angle(forward, toPlayer.normalized);
        if (angleToPlayer > scanAngle * 0.5f) return;

        float halfAngle = scanAngle * 0.5f;
        LayerMask scanMask = obstacleLayer | playerLayer;

        for (int i = 0; i < scanRayCount; i++)
        {
            float t = (scanRayCount == 1) ? 0.5f : (float)i / (scanRayCount - 1);
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector2 dir = Rotate(forward, angle);

            // 关键：这里只取“第一个非自己/非自己子物体”的有效命中
            if (TryGetFirstValidHit(origin, dir, scanRadius, scanMask, out RaycastHit2D hit))
            {
                if (IsInLayerMask(hit.collider.gameObject.layer, playerLayer))
                {
                    canSeePlayer = true;
                    lastSeenPlayerPos = player.position;
                    break;
                }

                // 如果第一个有效命中是障碍，那这条 ray 就被挡住
            }
        }
    }

    private void HandleStateLogic()
    {
        switch (currentState)
        {
            case State.Patrol:
                if (canSeePlayer)
                {
                    currentState = State.Chase;
                    lostPlayerTimer = 0f;
                }
                break;

            case State.Chase:
                if (canSeePlayer)
                {
                    lostPlayerTimer = 0f;
                    lastSeenPlayerPos = player.position;
                }
                else
                {
                    lostPlayerTimer += Time.deltaTime;
                    if (lostPlayerTimer >= losePlayerReturnDelay)
                    {
                        // 丢失玩家后，返回当前巡逻路径
                        currentState = State.Return;
                        patrolIndex = GetNearestPatrolIndex();
                    }
                }
                break;

            case State.Return:
                if (canSeePlayer)
                {
                    currentState = State.Chase;
                    lostPlayerTimer = 0f;
                }
                else
                {
                    if (patrolPoints.Count > 0)
                    {
                        Vector2 patrolTarget = patrolPoints[patrolIndex].position;
                        if (Vector2.Distance(transform.position, patrolTarget) <= arriveDistance)
                        {
                            currentState = State.Patrol;
                        }
                    }
                }
                break;
        }
    }

    private void HandleMovement()
    {
        if (patrolPoints.Count == 0 && currentState != State.Chase)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 targetPos = transform.position;

        switch (currentState)
        {
            case State.Chase:
                targetPos = lastSeenPlayerPos;
                break;

            case State.Patrol:
                targetPos = patrolPoints[patrolIndex].position;

                if (Vector2.Distance(transform.position, targetPos) <= arriveDistance)
                {
                    AdvancePatrolIndex();
                    targetPos = patrolPoints[patrolIndex].position;
                }
                break;

            case State.Return:
                targetPos = patrolPoints[patrolIndex].position;
                break;
        }

        Vector2 currentPos = transform.position;
        Vector2 desiredDir = targetPos - currentPos;

        if (desiredDir.magnitude <= arriveDistance)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        desiredDir.Normalize();

        Vector2 moveDir = GetAvoidedDirection(desiredDir);

        if (moveDir == Vector2.zero)
        {
            rb.velocity = Vector2.zero;
        }
        else
        {
            rb.velocity = moveDir * moveSpeed;
        }
    }

    private Vector2 GetAvoidedDirection(Vector2 desiredDir)
    {
        Vector2 origin = transform.position;

        for (int i = 0; i < avoidAngles.Length; i++)
        {
            Vector2 testDir = Rotate(desiredDir, avoidAngles[i]);

            // 关键：避障检测也改成“找到第一个非自己的有效命中”
            bool blocked = TryGetFirstValidHit(origin, testDir, obstacleCheckDistance, obstacleLayer, out _);

            if (!blocked)
            {
                return testDir.normalized;
            }
        }

        return Vector2.zero;
    }

    private bool TryGetFirstValidHit(Vector2 origin, Vector2 dir, float distance, LayerMask layerMask, out RaycastHit2D validHit)
    {
        validHit = default;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = layerMask;
        filter.useTriggers = false;

        int hitCount = Physics2D.Raycast(origin, dir, filter, raycastBuffer, distance);

        if (hitCount <= 0) return false;

        float closestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = raycastBuffer[i].collider;
            if (col == null) continue;

            // 忽略自己和自己所有子物体
            if (IsSelfOrChild(col.transform))
                continue;

            if (raycastBuffer[i].distance < closestDistance)
            {
                closestDistance = raycastBuffer[i].distance;
                validHit = raycastBuffer[i];
                found = true;
            }
        }

        return found;
    }

    private bool IsSelfOrChild(Transform target)
    {
        return target == transform || target.IsChildOf(transform);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return ((1 << layer) & mask.value) != 0;
    }

    private void AdvancePatrolIndex()
    {
        if (patrolPoints.Count <= 1) return;

        if (pingPongPatrol)
        {
            patrolIndex += patrolDirection;

            if (patrolIndex >= patrolPoints.Count)
            {
                patrolIndex = patrolPoints.Count - 2;
                patrolDirection = -1;
            }
            else if (patrolIndex < 0)
            {
                patrolIndex = 1;
                patrolDirection = 1;
            }
        }
        else
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
        }
    }

    private int GetNearestPatrolIndex()
    {
        if (patrolPoints.Count == 0) return 0;

        int nearest = 0;
        float bestDist = float.MaxValue;
        Vector2 pos = transform.position;

        for (int i = 0; i < patrolPoints.Count; i++)
        {
            float d = Vector2.Distance(pos, patrolPoints[i].position);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = i;
            }
        }

        return nearest;
    }

    private Vector2 GetForwardDirection()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
            return rb.velocity.normalized;

        // 如果角色正面不是朝上，可以改成 transform.right
        return transform.up;
    }

    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);

        float x = v.x * cos - v.y * sin;
        float y = v.x * sin + v.y * cos;

        return new Vector2(x, y).normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, scanRadius);

        Vector2 forward;
        if (Application.isPlaying)
            forward = GetForwardDirection();
        else
            forward = transform.up;

        Vector2 left = Rotate(forward, -scanAngle * 0.5f);
        Vector2 right = Rotate(forward, scanAngle * 0.5f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + left * scanRadius);
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + right * scanRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + forward * obstacleCheckDistance);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastSeenPlayerPos, 0.12f);
        }
    }
}