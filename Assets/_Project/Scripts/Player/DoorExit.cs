using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorExit : MonoBehaviour
{
    [Header("Key Requirement")]
    [Tooltip("اسم معرف المفتاح")]
    [SerializeField] private string requiredItemID = "ChestKey";

    [Header("Scene Transition")]
    [Tooltip("اسم مشهد القائمة الرئيسية بدقة")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Feedback")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip lockedSound;

    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void OnMouseDown()
    {
        TryOpenDoor();
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
                    TryOpenDoor();
                }
            }
        }
    }

    private void TryOpenDoor()
    {
        // 1. التحقق من وجود المفتاح أولاً
        bool playerHasKey = KeyInteraction.HasKey;

        // التحقق من نظام الحقيبة إن وجد
        if (!playerHasKey && InventoryManager.Instance != null)
        {
            // فحص إضافي إذا لزم
        }

        // إذا لم يكن المفتاح بحوزة اللاعب: نمنع فتح الباب
        if (!playerHasKey)
        {
            Debug.Log("<color=red>[Door]:</color> الباب مقفل! تحتاج إلى المفتاح أولاً.");
            if (lockedSound != null)
            {
                AudioSource.PlayClipAtPoint(lockedSound, transform.position);
            }
            return;
        }

        // 2. إذا كان المفتاح موجوداً: يفتح الباب
        Debug.Log("<color=green>[Door]:</color> تم فتح الباب بنجاح بواسطة المفتاح!");

        if (doorOpenSound != null)
        {
            AudioSource.PlayClipAtPoint(doorOpenSound, transform.position);
        }

        // إبلاغ دليل التقدم بإتمام مهمة الباب
        LevelProgressGuide.ReportTask("door");

        // --- حفظ إنهاء المرحلة الأولى وتحديد فتح شاشة المستويات مباشرة ---
        PlayerPrefs.SetInt("Room1_Done", 1);
        PlayerPrefs.SetInt("Level1_Done", 1);
        PlayerPrefs.SetInt("OpenLevelsDirectly", 1); // حتى تفتح شاشة الـ Levels فوراً
        PlayerPrefs.Save();

        // الانتقال لمشهد القائمة
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}