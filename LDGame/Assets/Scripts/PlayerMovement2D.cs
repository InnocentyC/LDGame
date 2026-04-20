using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 5f;

    [Header("瞄准/动画方向")]
    public Camera targetCamera;

    [Header("动画资源")]
    public Animator animator;
    public string directionParameter = "Direction";
    public string isMovingParameter = "IsMoving";

    [Header("动画速度")]
    [Range(0.1f, 2f)] public float animationSpeed = 1f;

    [Header("Debug")]
    public bool debugMode = false;

    // 0=北, 1=东, 2=南, 3=西
    private int currentDirectionIndex = 0;
    private Vector2 aimDir = Vector2.up;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    // 4方向定义（度数）
    // 北(0°), 东(90°), 南(180°), 西(270°)
    private readonly Vector2[] directions = new Vector2[]
    {
        Vector2.up,    // 0 北
        Vector2.right, // 1 东
        Vector2.down,  // 2 南
        Vector2.left   // 3 西
    };

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        // 强制设置初始方向为北(0)
        currentDirectionIndex = 0;
        UpdateAnimator();
    }

    void Update()
    {
        // 移动输入
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        // 更新瞄准方向（echowave用）
        UpdateAimDirection();

        // 更新动画朝向
        UpdateAnimationDirection();

        // 更新动画状态
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
    }

    /// <summary>
    /// 更新瞄准方向（鼠标方向，用于echowave）
    /// </summary>
    private void UpdateAimDirection()
    {
        if (targetCamera == null)
            return;

        Vector3 mouseScreen = Input.mousePosition;
        Vector2 mouseWorld = targetCamera.ScreenToWorldPoint(mouseScreen);
        Vector2 dir = mouseWorld - (Vector2)transform.position;

        if (dir.sqrMagnitude > 0.0001f)
        {
            aimDir = dir.normalized;
        }
    }

    /// <summary>
    /// 根据移动方向更新动画朝向（4方向）
    /// </summary>
    private void UpdateAnimationDirection()
    {
        if (moveInput.sqrMagnitude < 0.01f)
            return;

        int newDirection;

        // 根据移动输入直接判断方向
        if (moveInput.x > 0)
            newDirection = 1; // 东
        else if (moveInput.x < 0)
            newDirection = 3; // 西
        else if (moveInput.y > 0)
            newDirection = 0; // 北
        else
            newDirection = 2; // 南

        if (newDirection != currentDirectionIndex)
        {
            currentDirectionIndex = newDirection;

            if (debugMode)
                Debug.Log(
                    $"[Input:{moveInput}] " +
                    $"→ {GetDirectionName(currentDirectionIndex)}"
                );
        }
    }

    /// <summary>
    /// 更新Animator参数
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator != null)
        {
            bool isMoving = moveInput.sqrMagnitude > 0.01f;
            
            animator.SetInteger(directionParameter, currentDirectionIndex);
            animator.SetBool(isMovingParameter, isMoving);
            animator.speed = animationSpeed;
        }
    }

    /// <summary>
    /// 获取瞄准方向（echowave使用）
    /// </summary>
    public Vector2 GetAimDirection()
    {
        return aimDir;
    }

    /// <summary>
    /// 获取当前朝向索引
    /// </summary>
    public int GetCurrentDirectionIndex()
    {
        return currentDirectionIndex;
    }

    private string GetDirectionName(int index)
    {
        string[] names = { "北", "东", "南", "西" };
        if (index >= 0 && index < names.Length)
            return names[index];
        return "未知";
    }

    private string GetKeyName(Vector2 input)
    {
        if (input.y > 0) return "W";
        if (input.y < 0) return "S";
        if (input.x > 0) return "D";
        if (input.x < 0) return "A";
        return "-";
    }
}
