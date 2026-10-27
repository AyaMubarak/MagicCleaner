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

    private void Start()
    {
        if (joystick == null)
        {
            joystick = FindAnyObjectByType<VirtualJoystick>();
        }

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        if (playerCamera != null)
        {
            cameraPitch = playerCamera.transform.localEulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;
        }
    }

    private Camera playerCamera;
    private float cameraPitch = 0f;
    private Vector2 lastLookPos;
    private int lookTouchId = -1;
    private bool isMouseLooking;

    private void OnDisable()
    {
        isMouseLooking = false;
        lookTouchId = -1;
        velocity = Vector3.zero;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            isMouseLooking = false;
            lookTouchId = -1;
        }
    }

    private void Update()
    {
        Vector2 input = Vector2.zero;

        if (joystick == null)
        {
            joystick = FindAnyObjectByType<VirtualJoystick>();
        }

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

        // 1. الدوران لليمين واليسار عبر الجويستيك أو الكيبورد
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

        // 3. دعم تدوير الكاميرا والحرية بالنظر باللمس والسحب (Swipe / Drag to Look)
        HandleCameraSwipeLook();

        // 4. الجاذبية للأرضية
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // تثبيت اللاعب فوق الأرض
        }

        velocity.y += gravity * Time.deltaTime;

        // تطبيق الحركة مع نظام الاصطدام
        controller.Move((move + velocity) * Time.deltaTime);
    }

    private void HandleCameraSwipeLook()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        // A touch can also synthesize mouse input. Process each drag only once.
        if (Input.touchCount > 0)
        {
            isMouseLooking = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && lookTouchId == -1 && CanLookAtPosition(touch.position))
                    lookTouchId = touch.fingerId;

                if (touch.fingerId != lookTouchId) continue;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    lookTouchId = -1;
                else if (touch.phase == TouchPhase.Moved)
                    ApplyLookDelta(touch.deltaPosition, 0.15f, 0.13f);
            }
            return;
        }

        lookTouchId = -1;
        if (Input.GetMouseButtonDown(0))
        {
            isMouseLooking = CanLookAtPosition(Input.mousePosition);
            lastLookPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0))
        {
            isMouseLooking = false;
        }
        else if (isMouseLooking)
        {
            Vector2 currentPos = Input.mousePosition;
            Vector2 delta = currentPos - lastLookPos;
            lastLookPos = currentPos;
            if (delta.sqrMagnitude > 0.01f && delta.sqrMagnitude < 2500f)
                ApplyLookDelta(delta, 0.16f, 0.14f);
        }
    }

    private void ApplyLookDelta(Vector2 delta, float yawSensitivity, float pitchSensitivity)
    {
        transform.Rotate(Vector3.up * (delta.x * yawSensitivity), Space.World);
        if (playerCamera == null) return;
        cameraPitch = Mathf.Clamp(cameraPitch - delta.y * pitchSensitivity, -40f, 50f);
        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }
    private bool CanLookAtPosition(Vector2 screenPos)
    {
        if (IsOverJoystick(screenPos)) return false;
        if (IsPointerOverInteractivePuzzle(screenPos)) return false;
        return true;
    }

    private bool IsOverJoystick(Vector2 screenPos)
    {
        if (joystick == null) return false;
        RectTransform rt = joystick.GetComponent<RectTransform>();
        if (rt == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos);
    }

    private bool IsPointerOverInteractivePuzzle(Vector2 screenPos)
    {
        // 1. فحص عناصر واجهة المستخدم النشطة (UI Canvas)
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = screenPos
            };
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                if (r.gameObject == null) continue;
                string n = r.gameObject.name.ToLower();

                // لوحات الفحص والغبار والأرقام والساعة
                if (n.Contains("dust") || n.Contains("book") || n.Contains("dial") || 
                    n.Contains("clock") || n.Contains("lock") || n.Contains("chest") ||
                    n.Contains("painting") || n.Contains("canvas") || n.Contains("panel") ||
                    r.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                    r.gameObject.GetComponentInParent<UnityEngine.UI.Slider>() != null ||
                    r.gameObject.GetComponentInParent<DustCleaner>() != null)
                {
                    return true;
                }
            }
        }

        // 2. فحص مجسمات الألغاز ثلاثية الأبعاد (الكتاب على الطاولة، جدار الرمز السري، أرقام الصندوق، الساعة)
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(ray, 20f);
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                string colName = hit.collider.name.ToLower();
                Transform p = hit.collider.transform.parent;
                string pName = p != null ? p.name.ToLower() : "";

                if (colName.Contains("book") || pName.Contains("book") ||
                    colName.Contains("wallsecret") || pName.Contains("wallsecret") ||
                    colName.Contains("dial") || pName.Contains("dial") ||
                    colName.Contains("chest") || pName.Contains("chest") ||
                    colName.Contains("clock") || pName.Contains("clock") ||
                    hit.collider.GetComponentInParent<ChestDialLock>() != null ||
                    hit.collider.GetComponentInParent<CandlePuzzle>() != null ||
                    hit.collider.GetComponentInParent<BookInteraction>() != null ||
                    hit.collider.GetComponentInParent<GrandfatherClockPuzzle>() != null ||
                    hit.collider.GetComponentInParent<Room3MemoryPainting>() != null)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
