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

    // استجابة للمس المباشر بالكولايدر (PC) عند النقر على السطح حصراً
    private void OnMouseDown()
    {
        TryExit();
    }

    // استجابة للمس عند الاقتراب المادي باللاعب إذا صعد للأعلى
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

        // فحص النقر أو اللمس لشاشات الهواتف والكمبيوتر
        if (Input.GetMouseButtonDown(0))
        {
            // تجنب التفاعل فقط إذا نقر اللاعب على زر فعلي في الواجهة (مثل زر الحقيبة أو زر الرجوع)
            if (EventSystem.current != null)
            {
                var curSelected = EventSystem.current.currentSelectedGameObject;
                if (curSelected != null && curSelected.GetComponent<Button>() != null)
                    return;
            }

            Camera cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, clickDistance);

            foreach (var hit in hits)
            {
                if (hit.transform == null) continue;

                string tName = hit.transform.name;
                string pName = hit.transform.parent != null ? hit.transform.parent.name : "";

                // التحقق من أن النقر على السطح حصراً (وليس السلم!)
                bool isRoof = hit.transform == transform || hit.transform.IsChildOf(transform) ||
                              tName.IndexOf("TOP", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                              pName.IndexOf("TOP", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                              tName.IndexOf("Ceiling", System.StringComparison.OrdinalIgnoreCase) >= 0;

                // استبعاد السلم تماماً
                bool isStairs = tName.IndexOf("Stairs", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                pName.IndexOf("Stairs", System.StringComparison.OrdinalIgnoreCase) >= 0;

                if (isRoof && !isStairs)
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

        // فحص وجود المفتاح في الحقيبة (سواء كان ExitKey أو أي مفتاح آخر أو تم التقاطه في المهمة)
        bool hasKey = (PlayerPrefs.GetInt("Room2_Task_key", 0) == 1) ||
                      (PlayerPrefs.GetInt("HasItem_ExitKey", 0) == 1);

        if (!hasKey && InventoryManager.Instance != null)
        {
            hasKey = InventoryManager.Instance.HasItem(requiredKey) ||
                     InventoryManager.Instance.HasItem("key") ||
                     (!string.IsNullOrEmpty(InventoryManager.Instance.SelectedItem) &&
                      (InventoryManager.Instance.SelectedItem.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       InventoryManager.Instance.SelectedItem.Trim().Equals(requiredKey.Trim(), System.StringComparison.OrdinalIgnoreCase)));
        }

        if (hasKey)
        {
            isTransitioning = true;

            // تشغيل صوت النجاح إن وُجد
            if (openSound != null)
            {
                AudioSource.PlayClipAtPoint(openSound, transform.position);
            }

            // إزالة المفتاح من الحقيبة عند الاستخدام
            if (InventoryManager.Instance != null)
            {
                if (InventoryManager.Instance.HasItem(requiredKey))
                {
                    InventoryManager.Instance.RemoveItem(requiredKey);
                }
                else if (InventoryManager.Instance.HasItem("key"))
                {
                    InventoryManager.Instance.RemoveItem("key");
                }
                else if (!string.IsNullOrEmpty(InventoryManager.Instance.SelectedItem))
                {
                    InventoryManager.Instance.RemoveItem(InventoryManager.Instance.SelectedItem);
                }
            }

            Debug.Log("<color=green>[السطح]:</color> تم فتح السطح بنجاح! حفظ المرحلة والانتقال للقائمة...");

            // 1. حفظ إنهاء الغرفة
            PlayerPrefs.SetInt(roomDoneKey, 1);
            PlayerPrefs.SetInt("Room2_Done", 1);
            PlayerPrefs.SetInt("Level2_Done", 1);
            PlayerPrefs.SetInt("Room2_Task_roof", 1);
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

        Canvas canvas = null;
        GameObject guideCanvasObj = GameObject.Find("ProgressGuideCanvas_HUD");
        if (guideCanvasObj != null) canvas = guideCanvasObj.GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        GameObject banner = null;
        if (canvas != null)
        {
            banner = new GameObject("Room2Completion", typeof(RectTransform));
            banner.transform.SetParent(canvas.transform, false);
            banner.transform.SetAsLastSibling();

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

        yield return new WaitForSeconds(3f);
        if (banner != null) Destroy(banner);

        bool videoDone = false;
        if (UniversalVideoPlayer.Instance != null)
        {
            UniversalVideoPlayer.Instance.PlayRoom2EndVideo(() => videoDone = true);
        }
        else
        {
            var uvp = FindAnyObjectByType<UniversalVideoPlayer>();
            if (uvp != null) uvp.PlayRoom2EndVideo(() => videoDone = true);
            else videoDone = true;
        }

        while (!videoDone)
        {
            yield return null;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}
