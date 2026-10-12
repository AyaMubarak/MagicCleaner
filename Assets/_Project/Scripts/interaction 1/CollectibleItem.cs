using UnityEngine;

[RequireComponent(typeof(Collider))]
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

    private void Update()
    {
        if (isCollected) return;

        // استقبال اللمس على الموبايل أو نقرة الماوس
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
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
            Debug.Log($"<color=green>[الالتقاط]:</color> تم أخذ {itemID} وإضافته للحقيبة.");
            gameObject.SetActive(false); // إخفاء المجسم من الغرفة
        }
    }
}