using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
            if (cam == null) cam = FindAnyObjectByType<Camera>();
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
            LevelProgressGuide.ReportTask("roof");

            // 2. راية فتح شاشة المستويات مباشرة عند دخول القائمة
            PlayerPrefs.SetInt("OpenLevelsDirectly", 1);
            PlayerPrefs.Save();

            StartCoroutine(ShowRoom2Completion());
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

    private IEnumerator ShowRoom2Completion()
    {
        ProceduralAudioGenerator.PlaySound("level_complete", 1.0f);

        Canvas canvas = FindAnyObjectByType<Canvas>();
        GameObject banner = null;
        if (canvas != null)
        {
            banner = new GameObject("Room2Completion", typeof(RectTransform));
            banner.transform.SetParent(canvas.transform, false);
            RectTransform bannerRect = banner.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.sizeDelta = new Vector2(650f, 180f);
            Image background = banner.AddComponent<Image>();
            background.color = new Color(0.04f, 0.07f, 0.14f, 0.96f);
            Outline outline = banner.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.82f, 0.2f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(banner.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = "★ LEVEL 2 COMPLETED! ★\n<size=26><color=#4ADE80>CONGRATULATIONS!</color></size>";
            text.fontSize = 34f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.88f, 0.35f, 1f);

            for (int i = 0; i < 36; i++)
            {
                GameObject confetti = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
                confetti.transform.SetParent(canvas.transform, false);
                RectTransform confettiRect = confetti.GetComponent<RectTransform>();
                confettiRect.anchorMin = new Vector2(0.5f, 0.5f);
                confettiRect.anchorMax = new Vector2(0.5f, 0.5f);
                confettiRect.anchoredPosition = new Vector2(Random.Range(-500f, 500f), Random.Range(-300f, 300f));
                confettiRect.sizeDelta = new Vector2(12f, 12f);
                confetti.GetComponent<Image>().color = Color.HSVToRGB(Random.value, 0.8f, 1f);
                Destroy(confetti, 4f);
            }
        }

        yield return new WaitForSeconds(4f);
        if (banner != null) Destroy(banner);
        SceneManager.LoadScene(menuSceneName);
    }
}
