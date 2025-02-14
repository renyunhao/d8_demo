using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 25f;

    [Header("缩放参数")]
    public float zoomSpeed = 30f;
    public float minFOV = 20f;
    public float maxFOV = 60f;

    [Header("可视范围")]
    public float minX = -50f;
    public float maxX = 50f;
    public float minZ = -50f;
    public float maxZ = 50f;

    private Camera cam;
    private float baseHeight; // 初始高度，用于计算相对位置

    void Start()
    {
        cam = GetComponent<Camera>();
        baseHeight = transform.position.y;
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    void LateUpdate()
    {
        ApplyBoundaryRestrictions();
    }

    void HandleMovement()
    {
        // 键盘输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            cam.fieldOfView = Mathf.Clamp(
                cam.fieldOfView - scroll * zoomSpeed,
                minFOV,
                maxFOV
            );
        }
    }

    void ApplyBoundaryRestrictions()
    {
        // 计算当前视口尺寸
        float currentHeight = transform.position.y;

        float verticalFOV = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float visibleHeight = 2 * currentHeight * Mathf.Tan(verticalFOV);
        float visibleWidth = visibleHeight * cam.aspect;

        // 计算动态边界
        float effectiveMinX = minX + visibleWidth / 2;
        float effectiveMaxX = maxX - visibleWidth / 2;
        float effectiveMinZ = minZ + visibleHeight / 2;
        float effectiveMaxZ = maxZ - visibleHeight / 2;

        // 限制摄像机位置
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, effectiveMinX, effectiveMaxX);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, effectiveMinZ, effectiveMaxZ);

        transform.position = clampedPosition;
    }
}