using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class KeyPickup : MonoBehaviour
{
    [Header("Item Info")]
    [Tooltip("معرف المفتاح - يجب أن يطابق الاسم المطلوب بالباب أو الساعة")]
    [SerializeField] private string itemID = "GoldenKey";

    [Tooltip("الأيقونة التي ستظهر في مربع الحقيبة")]
    [SerializeField] private Sprite itemIcon;

    [Header("Interaction Settings")]
    [Tooltip("أقصى مسافة للنقر أو اللمس")]
    [SerializeField] private float maxPickupDistance = 15f;

    [Tooltip("هل يلتقطه اللاعب بمجرد الاصطدام به دون الحاجة للنقر؟")]
    [SerializeField] private bool pickupOnTrigger = false;

    [Header("Feedback")]
    [Tooltip("الصوت الذي يشتغل عند التقاط المفتاح")]
    [SerializeField] private AudioClip pickupSound;

    [Tooltip("مؤثر جزيئات سحري يظهر عند الالتقاط")]
    [SerializeField] private GameObject pickupVFX;

    private bool isCollected = false;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        EnsureColliders();
    }

    private void Start()
    {
        EnsureColliders();
    }

    private void EnsureColliders()
    {
        // إذا كان هناك MeshCollider بدون Mesh معين (None Mesh) فلن يعمل Raycast أبداً، نستبدله بـ BoxCollider مناسب
        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc != null && mc.sharedMesh == null)
        {
            Destroy(mc);
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(8f, 8f, 8f);
        }
        else if (GetComponent<Collider>() == null)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(8f, 8f, 8f);
        }

        // التأكد من أن الأبناء (Plane.009 وغيرها التي تحتوي المش) لديها كولايدر مفعل
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            MeshFilter mf = child.GetComponent<MeshFilter>();
            if (mf != null && child.GetComponent<Collider>() == null)
            {
                child.gameObject.AddComponent<BoxCollider>();
            }
        }
    }

    // التقاط بالنقر المباشر على الكولايدر بالكمبيوتر
    private void OnMouseDown()
    {
        TryCollect();
    }

    // دعم اللمس والنقر على شاشات الموبايل والكمبيوتر
    private void Update()
    {
        if (isCollected || pickupOnTrigger) return;

        if (Input.GetMouseButtonDown(0))
        {
            CheckTouch(Input.mousePosition);
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            CheckTouch(Input.GetTouch(0).position);
        }
    }

    private void CheckTouch(Vector2 screenPos)
    {
        // تجنب الالتقاط إذا كان النقر على عنصر واجهة UI (مثل زر الحقيبة نفسه أو الجويستيك)
        if (EventSystem.current != null)
        {
            var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                if (r.gameObject != null && (r.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                    r.gameObject.GetComponentInParent<VirtualJoystick>() != null))
                {
                    return;
                }
            }
        }

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 50f);
        foreach (var hit in hits)
        {
            if (hit.transform == null) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform) ||
                hit.transform.name.ToLowerInvariant().Contains("plane.009") ||
                hit.transform.name.ToLowerInvariant().Contains("key"))
            {
                TryCollect();
                break;
            }
        }
    }

    // خيار الالتقاط عند اقتراب ومصادمة اللاعب إذا تم تفعيله
    private void OnTriggerEnter(Collider other)
    {
        if (!pickupOnTrigger || isCollected) return;

        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            TryCollect();
        }
    }

    public void TryCollect()
    {
        if (isCollected) return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[KeyPickup]: لم يتم العثور على InventoryManager في المشهد!");
            return;
        }

        // إضافة المفتاح للحقيبة
        Sprite icon = itemIcon != null ? itemIcon : InventoryManager.GetKeySprite();
        InventoryManager.Instance.AddItem(itemID, icon, true);
        InventoryManager.Instance.ShowInventoryTemporarily();

        isCollected = true;

        PlayerPrefs.SetInt("Room3_Task_key", 1);
        PlayerPrefs.SetInt("HasItem_Room3Key", 1);
        PlayerPrefs.SetInt("HasItem_GoldenKey", 1);
        PlayerPrefs.Save();

        // تشغيل صوت الالتقاط
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        else
        {
            ProceduralAudioGenerator.PlaySound("pickup", 0.95f);
        }

        // تشغيل المؤثر البصري إن وجد
        if (pickupVFX != null)
        {
            Instantiate(pickupVFX, transform.position, Quaternion.identity);
        }

        LevelProgressGuide.ReportTask("key");

        Debug.Log($"<color=gold>[KeyPickup]:</color> تم التقاط المفتاح ({itemID}) وحفظه في الحقيبة بنجاح!");

        // إخفاء مجسم المفتاح من الغرفة
        gameObject.SetActive(false);
    }
}