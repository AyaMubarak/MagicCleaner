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

    private void Awake()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        bool hasUsableCollider = false;
        foreach (Collider currentCollider in colliders)
        {
            MeshCollider meshCollider = currentCollider as MeshCollider;
            if (currentCollider.enabled && (meshCollider == null || meshCollider.sharedMesh != null))
            {
                hasUsableCollider = true;
                break;
            }
        }

        if (!hasUsableCollider)
        {
            BoxCollider fallbackCollider = gameObject.AddComponent<BoxCollider>();
            Bounds bounds = new Bounds(transform.position, Vector3.one * 0.5f);
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer currentRenderer in renderers)
            {
                bounds.Encapsulate(currentRenderer.bounds);
            }

            fallbackCollider.center = transform.InverseTransformPoint(bounds.center);
            fallbackCollider.size = bounds.size;
        }
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

    private void Update()
    {
        if (isCollected) return;

        // استقبال اللمس على الموبايل أو نقرة الماوس
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                CollectibleItem hitItem = hit.collider.GetComponentInParent<CollectibleItem>();
                if (hitItem == this)
                {
                    Collect();
                }
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
