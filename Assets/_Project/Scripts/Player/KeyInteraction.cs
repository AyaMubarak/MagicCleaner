using UnityEngine;

public class KeyInteraction : MonoBehaviour
{
    // هذا هو المتغير الذي يطلبه سكربت الباب
    public static bool HasKey { get; private set; } = false;

    [Header("Inventory Settings")]
    [SerializeField] private string itemID = "ChestKey";
    [SerializeField] private Sprite keyInventoryIcon;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip pickupSound;

    private Camera mainCam;
    private bool isCollected = false;

    private void Awake()
    {
        mainCam = Camera.main;
        HasKey = false; // إعادة ضبط المتغير عند بدء اللعبة
    }

    private void OnMouseDown()
    {
        PickupKey();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    PickupKey();
                }
            }
        }
    }

    public void PickupKey()
    {
        if (isCollected) return;

        isCollected = true;
        HasKey = true; // تفعيل شرط فتح الباب هنا

        // إضافة المفتاح إلى الحقيبة
        if (InventoryManager.Instance != null && keyInventoryIcon != null)
        {
            InventoryManager.Instance.AddItem(itemID, keyInventoryIcon);
            Debug.Log("<color=yellow>[Key]:</color> تم التقاط المفتاح بنجاح وإضافته للحقيبة!");
        }

        // تشغيل صوت الالتقاط إن وجد
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        // إخفاء المفتاح من الغرفة
        gameObject.SetActive(false);

        // إبلاغ دليل التقدم بالتقاط المفتاح
        LevelProgressGuide.ReportTask("key");
    }
}