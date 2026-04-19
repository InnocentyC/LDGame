using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 5f;

    [Header("Mouse Aim")]
    public Camera targetCamera;
    public bool useTransformRightAsForward = true;
    // true: 角色朝向 = transform.right
    // false: 角色朝向 = transform.up

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 mouseWorld;
    private Vector2 aimDir = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        // WASD / 方向键输入
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        UpdateMouseAim();
    }

    void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
    }

    private void UpdateMouseAim()
    {
        if (targetCamera == null)
            return;

        Vector3 mouseScreen = Input.mousePosition;
        mouseWorld = targetCamera.ScreenToWorldPoint(mouseScreen);

        Vector2 dir = mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        aimDir = dir.normalized;

        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

        if (useTransformRightAsForward)
        {
            // 让 transform.right 指向鼠标
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            // 让 transform.up 指向鼠标
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    public Vector2 GetAimDirection()
    {
        return aimDir;
    }
}