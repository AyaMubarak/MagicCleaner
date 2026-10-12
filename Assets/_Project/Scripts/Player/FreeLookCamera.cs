using UnityEngine;

public class FreeLookCamera : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [SerializeField] private float lookSpeed = 0.2f;

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 60f;

    private float yaw;
    private float pitch;
    private Vector2 lastMousePosition;
    private bool isDragging = false;

    private void Start()
    {
        // إصلاح اختراق الكاميرا للجدران وظهور اللون الأزرق
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            cam.nearClipPlane = 0.02f; // تقليل مسافة القص القريب لتجنب اختراق الجدران
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f); // لون داكن فخم بدلاً من الأزرق الفاقع
        }

        Vector3 angles = transform.eulerAngles;
        pitch = angles.x;
        yaw = angles.y;

        if (pitch > 180f) pitch -= 360f;
    }

    private void Update()
    {
        // استخدام الزر الأيسر للماوس مباشرة ليعمل داخل نافذة Simulator بدون قيود
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 currentMousePosition = Input.mousePosition;
            Vector2 delta = currentMousePosition - lastMousePosition;
            lastMousePosition = currentMousePosition;

            // تطبيق التدوير فوراً
            yaw += delta.x * lookSpeed;
            pitch -= delta.y * lookSpeed;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}