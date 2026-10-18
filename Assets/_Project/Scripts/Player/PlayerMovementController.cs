using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement & Rotation Speed")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotateSpeed = 90f;

    [Header("Gravity & Ground")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Joystick Reference")]
    [SerializeField] private VirtualJoystick joystick;

    private CharacterController controller;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            // زيادة نصف قطر التصادم للاعب لمنع الكاميرا من الاقتراب الشديد من الجدران واختراقها
            if (controller.radius < 0.55f)
            {
                controller.radius = 0.55f;
            }
        }

        // ضبط الكاميرا لتجنب اختراق الجدران (Near Clip Plane)
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            cam.nearClipPlane = 0.01f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f); // لون داكن فخم بدلاً من السماء الفارغة
        }
    }

    private void Update()
    {
        Vector2 input = Vector2.zero;

        // قراءة الإدخال من الجويستيك
        if (joystick != null)
        {
            input = joystick.InputDirection;
        }

        // دعم أسهم الكيبورد (WASD) للاختبار
        if (input == Vector2.zero)
        {
            input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        // 1. الدوران لليمين واليسار
        if (Mathf.Abs(input.x) > 0.05f)
        {
            transform.Rotate(Vector3.up * input.x * rotateSpeed * Time.deltaTime);
        }

        // 2. الحركة للأمام والخلف مع منع اختراق الجدران
        Vector3 move = Vector3.zero;
        if (Mathf.Abs(input.y) > 0.05f)
        {
            move = transform.forward * input.y * moveSpeed;
        }

        // 3. الجاذبية للأرضية
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // تثبيت اللاعب فوق الأرض
        }

        velocity.y += gravity * Time.deltaTime;

        // تطبيق الحركة مع نظام الاصطدام
        controller.Move((move + velocity) * Time.deltaTime);
    }
}