using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class RoofExitTrigger : MonoBehaviour
{
    [Header("Save & Scene Settings")]
    [Tooltip("مفتاح حفظ إكمال هذه المرحلة")]
    [SerializeField] private string roomDoneKey = "Room2_Done";

    [Tooltip("اسم مشهد القائمة الرئيسية بدقة")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Key Check")]
    [Tooltip("اسم معرف المفتاح في الحقيبة")]
    [SerializeField] private string requiredKey = "ExitKey";

    [Header("Interaction Distance")]
    [Tooltip("أقصى مسافة لالتقاط نقرة الماوس أو اللمس")]
    [SerializeField] private float clickDistance = 100f;

    [Header("Audio Feedback")]
    [Tooltip("صوت فتح السطح / الباب بنجاح")]
    [SerializeField] private AudioClip openSound;
    [Tooltip("صوت الرفض عند محاولة الفتح بدون مفتاح")]
    [SerializeField] private AudioClip lockedSound;

    private bool isTransitioning = false;

    // استجابة للمس المباشر بالكولايدر (PC)
    private void OnMouseDown()
    {
        TryExit();
    }

    // استجابة للمس عند الاقتراب المادي باللاعب إذا كان الكائن Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            TryExit();
        }
    }

    private void Update()
    {
        if (isTransitioning) return;

        // فحص النقر أو اللمس لشاشات الهواتف
        if (Input.GetMouseButtonDown(0))
        {
            // تجنب التفاعل إذا نقر اللاعب على عنصر واجهة UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, clickDistance);

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform) ||
                    hit.transform.name.Contains("Ceiling") || hit.transform.name.Contains("TOP"))
                {
                    TryExit();
                    break;
                }
            }
        }
    }

    public void TryExit()
    {
        if (isTransitioning) return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[السطح]: لم يتم العثور على InventoryManager في المشهد!");
            return;
        }

        // فحص وجود المفتاح في الحقيبة (سواء كان محدداً أو موجوداً ضمن الأغراض)
        bool hasKey = InventoryManager.Instance.HasItem(requiredKey) ||
                      (!string.IsNullOrEmpty(InventoryManager.Instance.SelectedItem) &&
                       InventoryManager.Instance.SelectedItem.Trim().Equals(requiredKey.Trim(), System.StringComparison.OrdinalIgnoreCase));

        if (hasKey)
        {
            isTransitioning = true;

            // تشغيل صوت النجاح إن وُجد
            if (openSound != null)
            {
                AudioSource.PlayClipAtPoint(openSound, transform.position);
            }

            // إزالة المفتاح من الحقيبة عند الاستخدام
            if (InventoryManager.Instance.HasItem(requiredKey))
            {
                InventoryManager.Instance.RemoveItem(requiredKey);
            }
            else if (!string.IsNullOrEmpty(InventoryManager.Instance.SelectedItem) &&
                     InventoryManager.Instance.SelectedItem.Trim().Equals(requiredKey.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                InventoryManager.Instance.RemoveItem(InventoryManager.Instance.SelectedItem);
            }

            Debug.Log("<color=green>[السطح]:</color> تم فتح السطح بنجاح! حفظ المرحلة والانتقال للقائمة...");

            // 1. حفظ إنهاء الغرفة
            PlayerPrefs.SetInt(roomDoneKey, 1);

            // 2. راية فتح شاشة المستويات مباشرة عند دخول القائمة
            PlayerPrefs.SetInt("OpenLevelsDirectly", 1);
            PlayerPrefs.Save();

            // 3. الانتقال إلى مشهد القائمة الرئيسية
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            // تشغيل صوت القفل المغلق إن وُجد
            if (lockedSound != null)
            {
                AudioSource.PlayClipAtPoint(lockedSound, transform.position);
            }

            Debug.LogWarning("<color=orange>[السطح]:</color> السطح مقفل بإحكام! يجب الحصول على المفتاح أولاً.");
        }
    }
}