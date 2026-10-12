using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DustCleaner : MonoBehaviour, IDragHandler
{
    [Header("UI Reference")]
    [SerializeField] private Image dustImage;

    [Header("Settings")]
    [SerializeField] private float cleanSpeed = 0.015f; // سرعة اختفاء الغبار مع كل مسحة
    private float currentAlpha = 1f;
    private bool isCleaned = false;

    [Header("Book Inventory Reward")]
    [SerializeField] private Sprite bookIcon;          // اسحبي صورة الكتاب هنا لتظهر في الحقيبة
    [SerializeField] private GameObject worldBookObject; // اسحبي مجسم الكتاب الموجود في الغرفة هنا

    private void Start()
    {
        if (dustImage == null)
            dustImage = GetComponent<Image>();

        if (dustImage != null)
            currentAlpha = dustImage.color.a;
    }

    // يتم استدعاؤه تلقائياً أثناء تحريك الإصبع أو الماوس فوق الغبار
    public void OnDrag(PointerEventData eventData)
    {
        if (isCleaned || dustImage == null) return;

        // تقليل الشفافية تدريجياً مع المسح
        currentAlpha -= cleanSpeed;

        Color c = dustImage.color;
        c.a = Mathf.Clamp01(currentAlpha);
        dustImage.color = c;

        // إذا تم مسح معظم الغبار
        if (currentAlpha <= 0.15f)
        {
            CompleteCleaning();
        }
    }

    private void CompleteCleaning()
    {
        isCleaned = true;
        gameObject.SetActive(false); // إخفاء طبقة الغبار بالكامل
        Debug.Log("<color=yellow>[نجاح اللغز]:</color> تم تنظيف الغبار بالكامل وكشف الرمز السري!");

        // 1. إضافة الكتاب إلى شريط الحقيبة
        if (InventoryManager.Instance != null && bookIcon != null)
        {
            InventoryManager.Instance.AddItem("SecretBook", bookIcon);
            Debug.Log("<color=green>[الحقيبة]:</color> تم حفظ الكتاب السري داخل الحقيبة.");
        }

        // 2. إخفاء مجسم الكتاب الحقيقي من الغرفة
        if (worldBookObject != null)
        {
            worldBookObject.SetActive(false);
        }

        // 3. إبلاغ دليل التقدم بإنجاز مهمة مسح الغبار
        LevelProgressGuide.ReportTask("dust");
    }
}