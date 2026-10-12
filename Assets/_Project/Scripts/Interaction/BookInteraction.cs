using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BookInteraction : MonoBehaviour
{
    public static BookInteraction Instance;

    [Header("UI & Clue")]
    [SerializeField] private GameObject bookInspectUI;    // لوحة Book_Background
    [SerializeField] private TextMeshProUGUI codeDisplayText; // اسحبي Secret_Code_Text هنا
    [SerializeField] private string secretCode = "739";       // الرمز السري المطلوب

    [Header("Inventory & World Object")]
    [SerializeField] private Sprite bookInventoryIcon;   // صورة أيقونة الكتاب للحقيبة
    [SerializeField] private GameObject worldBookObject; // مجسم الكتاب في الغرفة

    private bool isOpen = false;
    private bool isCollected = false;
    public static bool HasDiscoveredCode { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // ضبط نص الكود تلقائياً ليكون 739
        if (codeDisplayText != null)
        {
            codeDisplayText.text = secretCode;
        }
    }

    private void OnMouseDown()
    {
        // منع الفتح إذا كان مفتوحاً مسبقاً أو تم التقاط الكتاب
        if (isOpen || isCollected) return;

        OpenBookView();
    }

    public void OpenBookView()
    {
        isOpen = true;

        if (codeDisplayText != null)
        {
            codeDisplayText.text = secretCode;
        }

        if (bookInspectUI != null)
        {
            bookInspectUI.SetActive(true);
        }

        Debug.Log("<color=yellow>[الكتاب]:</color> تم فتح واجهة فحص الكتاب.");
    }

    // يتم استدعاء هذه الدالة عند اكتمال تنظيف الغبار
    public void OnDustCleanedComplete()
    {
        HasDiscoveredCode = true;
        Debug.Log("<color=yellow>[الكتاب]:</color> تم كشف الرمز السري بنجاح: " + secretCode);

        // 1. إضافة الكتاب إلى الحقيبة
        if (!isCollected && InventoryManager.Instance != null && bookInventoryIcon != null)
        {
            InventoryManager.Instance.AddItem("SecretBook", bookInventoryIcon);
            isCollected = true;
        }

        // 2. إخفاء مجسم الكتاب الحقيقي من الغرفة
        if (worldBookObject != null)
        {
            worldBookObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // يتم ربط هذه الدالة بزر الإغلاق (Close_Button)
    public void CloseBookView()
    {
        isOpen = false;

        if (bookInspectUI != null)
        {
            bookInspectUI.SetActive(false);
        }

        Debug.Log("<color=yellow>[الكتاب]:</color> تم إغلاق الكتاب والعودة للغرفة.");
    }

    private void Update()
    {
        // إغلاق سريع بالزر الأيمن للفأرة
        if (isOpen && Input.GetMouseButtonDown(1))
        {
            CloseBookView();
        }
    }
}