using UnityEngine;

[RequireComponent(typeof(Collider))]
// Collectible item for puzzle progression
public class CollectibleItem : MonoBehaviour
{
    [Header("Item Details")]
    [Tooltip("يجب أن يطابق تماماً أحد الأسماء في الوعاء: Herb أو WaterFlask")]
    [SerializeField] private string itemID = "WaterFlask";

    [Tooltip("صورة الأيقونة التي ستظهر في شريط الحقيبة")]
    [SerializeField] private Sprite itemIcon;

    [Header("Interaction Distance")]
    [SerializeField] private float maxDistance = 4f;

    private bool isCollected = false;

    public string ItemID => itemID;
    public Sprite ItemIcon => itemIcon;

    private void Awake()
    {
        EnsureGenerousHitCollider();
    }

    private void EnsureGenerousHitCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider currentCollider in colliders)
        {
            if (currentCollider is MeshCollider meshCollider && meshCollider.sharedMesh == null)
            {
                meshCollider.enabled = false;
            }
        }

        // دائماً نضيف أو نضبط BoxCollider واسع وواضح على الكائن الأب لتسهيل الالتقاط باللمس أو الماوس من أي زاوية
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }
        boxCollider.enabled = true;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds worldBounds = new Bounds(transform.position, Vector3.one * 0.45f);
        bool hasRenderer = false;
        foreach (Renderer currentRenderer in renderers)
        {
            if (currentRenderer == null) continue;
            if (!hasRenderer)
            {
                worldBounds = currentRenderer.bounds;
                hasRenderer = true;
            }
            else
            {
                worldBounds.Encapsulate(currentRenderer.bounds);
            }
        }

        // ضمان حجم أدنى مريح جداً للنقر في الفضاء العالمي (على الأقل 0.65 متر في كل اتجاه)
        Vector3 minWorldSize = new Vector3(0.65f, 0.65f, 0.65f);
        Vector3 targetWorldSize = Vector3.Max(worldBounds.size * 1.45f, minWorldSize);

        Vector3 lossy = transform.lossyScale;
        float sx = Mathf.Max(Mathf.Abs(lossy.x), 0.001f);
        float sy = Mathf.Max(Mathf.Abs(lossy.y), 0.001f);
        float sz = Mathf.Max(Mathf.Abs(lossy.z), 0.001f);

        boxCollider.center = transform.InverseTransformPoint(worldBounds.center);
        boxCollider.size = new Vector3(targetWorldSize.x / sx, targetWorldSize.y / sy, targetWorldSize.z / sz);
    }

    private void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool alreadyTaken = false;

        if (sceneName == "Room2")
        {
            if (itemID.Equals("Herb", System.StringComparison.OrdinalIgnoreCase))
            {
                alreadyTaken = PlayerPrefs.GetInt("Room2_Task_herb", 0) == 1 ||
                               PlayerPrefs.GetInt("HasItem_Herb", 0) == 1 ||
                               (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("Herb")) ||
                               PlayerPrefs.GetInt("Room2_Task_alchemy", 0) == 1;
            }
            else if (itemID.Equals("WaterFlask", System.StringComparison.OrdinalIgnoreCase))
            {
                alreadyTaken = PlayerPrefs.GetInt("Room2_Task_flask", 0) == 1 ||
                               PlayerPrefs.GetInt("HasItem_WaterFlask", 0) == 1 ||
                               (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("WaterFlask")) ||
                               PlayerPrefs.GetInt("Room2_Task_alchemy", 0) == 1;
            }
            else if (itemID.Equals("ExitKey", System.StringComparison.OrdinalIgnoreCase))
            {
                alreadyTaken = PlayerPrefs.GetInt("Room2_Task_key", 0) == 1 ||
                               PlayerPrefs.GetInt("HasItem_ExitKey", 0) == 1 ||
                               (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("ExitKey"));
            }
        }
        else
        {
            alreadyTaken = (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(itemID)) ||
                           PlayerPrefs.GetInt($"{sceneName}_Task_{itemID.ToLower()}", 0) == 1;
        }

        if (alreadyTaken)
        {
            isCollected = true;
            gameObject.SetActive(false);
        }
    }

    public Vector3 TargetPosition
    {
        get
        {
            Collider targetCollider = GetComponentInChildren<Collider>();
            return targetCollider != null ? targetCollider.bounds.center : transform.position;
        }
    }

    private void OnMouseDown()
    {
        if (isCollected) return;
        if (IsPointerOverUIButton(Input.mousePosition)) return;

        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            float effectiveDist = Mathf.Max(maxDistance, 12f);
            if (Vector3.Distance(cam.transform.position, TargetPosition) <= effectiveDist)
            {
                Collect();
            }
        }
    }

    private void Update()
    {
        if (isCollected) return;

        // استقبال اللمس على الموبايل أو نقرة الماوس
        if (Input.GetMouseButtonDown(0))
        {
            TryCollectFromScreenPoint(Input.mousePosition);
        }
        else if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                TryCollectFromScreenPoint(touch.position);
            }
        }
    }

    private bool IsPointerOverUIButton(Vector2 screenPos)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;
        var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = screenPos
        };
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            if (r.gameObject != null && r.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null)
                return true;
        }
        return false;
    }

    private void TryCollectFromScreenPoint(Vector2 screenPos)
    {
        if (isCollected) return;
        if (IsPointerOverUIButton(screenPos)) return;

        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null) return;

        float effectiveDist = Mathf.Max(maxDistance, 12f);
        Ray ray = cam.ScreenPointToRay(screenPos);

        // 1. RaycastAll لاختراق أي عوائق أو طاولات تحجب العنصر
        RaycastHit[] hits = Physics.RaycastAll(ray, effectiveDist);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<CollectibleItem>() == this)
            {
                Collect();
                return;
            }
        }

        // 2. SphereCastAll بنصف قطر مريح لالتقاط العناصر الصغيرة بسهولة عند اللمس القريب منها
        RaycastHit[] sphereHits = Physics.SphereCastAll(ray, 0.25f, effectiveDist);
        foreach (var hit in sphereHits)
        {
            if (hit.collider == null) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<CollectibleItem>() == this)
            {
                Collect();
                return;
            }
        }
    }

    public void Collect()
    {
        if (isCollected) return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[CollectibleItem]: InventoryManager غير موجود بالمشهد!");
            return;
        }

        // إضافة العنصر للحقيبة
        bool added = InventoryManager.Instance.AddItem(itemID, itemIcon);
        if (added)
        {
            isCollected = true;
            gameObject.SetActive(false); // أخفِ العنصر قبل تحديث هدف الجنية
            PlayerPrefs.SetInt($"HasItem_{itemID}", 1);
            PlayerPrefs.Save();
            if (itemID.Equals("ExitKey", System.StringComparison.OrdinalIgnoreCase))
            {
                LevelProgressGuide.ReportTask("key");
            }
            else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Room2")
            {
                if (itemID.Equals("WaterFlask", System.StringComparison.OrdinalIgnoreCase)) LevelProgressGuide.ReportTask("flask");
                else if (itemID.Equals("Herb", System.StringComparison.OrdinalIgnoreCase)) LevelProgressGuide.ReportTask("herb");
            }
            Debug.Log($"<color=green>[الالتقاط]:</color> تم أخذ {itemID} وإضافته للحقيبة.");
        }
    }
}
