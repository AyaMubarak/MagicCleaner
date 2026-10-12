using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ChestDialLock : MonoBehaviour
{
    [Header("Puzzle Requirements")]
    [Tooltip("تفعيل شرط وجود الكتاب وكود الجدار بالحقيبة")]
    [SerializeField] private bool requireItemsCollected = true;

    [Tooltip("اسحبي مجسم شعلة الشمعة أو كائن ضوء الشمعة هنا (اختياري)")]
    [SerializeField] private GameObject candleFlameOrLight;

    [Header("Lock Configuration")]
    [Tooltip("أرقام الرمز السري بالترتيب")]
    [SerializeField] private List<int> correctCode = new List<int>() { 7, 3, 9, 4, 7, 2, 9 };

    [Header("UI Panels & Animation")]
    [SerializeField] private GameObject lockUIPanel;
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private string openAnimationTrigger = "Open";

    [Header("Reward Key")]
    [Tooltip("اسحبي مجسم المفتاح الذهبي الموجود داخل الصندوق هنا")]
    [SerializeField] private GameObject keyInsideChest;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockSound;

    [Header("Success Events")]
    public UnityEvent onChestUnlocked;

    private List<int> currentEnteredCode = new List<int>();
    private bool isUnlocked = false;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;

        // التأكد من وجود كولايدر على مجسم الصندوق ليلتقط النقرة
        if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(1.5f, 1.5f, 1.5f);
        }
    }

    private void Start()
    {
        currentEnteredCode = new List<int>();
        for (int i = 0; i < correctCode.Count; i++)
        {
            currentEnteredCode.Add(0);
        }

        if (keyInsideChest != null)
        {
            keyInsideChest.SetActive(false);
        }
    }

    private void Update()
    {
        if (isUnlocked) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    OpenLockPanel();
                }
            }
        }
    }

    private void OnMouseDown()
    {
        OpenLockPanel();
    }

    public void OpenLockPanel()
    {
        if (isUnlocked) return;

        // الفحص المباشر من الحقيبة: هل الكتاب وكود الجدار مع اللاعب؟
        if (requireItemsCollected && InventoryManager.Instance != null)
        {
            bool hasBook = InventoryManager.Instance.HasItem("Book") ||
                           InventoryManager.Instance.HasItem("book") ||
                           InventoryManager.Instance.HasItem("CleanBook");

            bool hasCode = InventoryManager.Instance.HasItem("SecretCode") ||
                           InventoryManager.Instance.HasItem("code") ||
                           InventoryManager.Instance.HasItem("CandleCode");

            // إذا لم يتم جمع أحدهما أو لم تشتعل الشمعة بعد
            if (!hasBook && !hasCode)
            {
                Debug.LogWarning("<color=red>[ChestDialLock]:</color> الصندوق مغلق بإحكام! اجمعي الكتاب وكود الجدار أولاً.");
                return;
            }
        }

        // فتح لوحة الأرقام الدوارة فوراً
        if (lockUIPanel != null)
        {
            lockUIPanel.SetActive(true);
            CheckCode();
        }
    }

    public void SetDialValue(int dialIndex, int newValue)
    {
        UpdateDialValue(dialIndex, newValue);
    }

    public void UpdateDialValue(int dialIndex, int newValue)
    {
        if (isUnlocked) return;

        if (dialIndex >= 0 && dialIndex < currentEnteredCode.Count)
        {
            currentEnteredCode[dialIndex] = newValue;
            CheckCode();
        }
    }

    public void CheckCode()
    {
        if (isUnlocked) return;

        if (currentEnteredCode.Count != correctCode.Count) return;

        for (int i = 0; i < correctCode.Count; i++)
        {
            if (currentEnteredCode[i] != correctCode[i])
            {
                return;
            }
        }

        UnlockChest();
    }

    private void UnlockChest()
    {
        isUnlocked = true;
        Debug.Log("<color=green>[ChestDialLock]:</color> تم فك القفل وفتح الصندوق بنجاح!");

        if (lockUIPanel != null)
        {
            lockUIPanel.SetActive(false);
        }

        if (audioSource != null && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }

        // تشغيل أنيميشن الفتح
        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger(openAnimationTrigger);
        }

        // إظهار المفتاح داخل الصندوق فوراً للاعب
        if (keyInsideChest != null)
        {
            keyInsideChest.SetActive(true);
        }

        // إبلاغ دليل التقدم بإنجاز مهمة فتح الصندوق
        LevelProgressGuide.ReportTask("chest");

        onChestUnlocked?.Invoke();
    }

    public void CloseLockPanel()
    {
        if (lockUIPanel != null)
        {
            lockUIPanel.SetActive(false);
        }
    }
}