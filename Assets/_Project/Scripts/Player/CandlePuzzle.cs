using UnityEngine;

public class CandlePuzzle : MonoBehaviour
{
    [Header("Candle Fire & Light")]
    [Tooltip("اسحبي Point Light أو مجسم شعلة الشمعة هنا")]
    [SerializeField] private GameObject flameEffect;

    [Header("Optional Cutscene Video")]
    [Tooltip("اختياري: اسحبي فيديو إشعال الشمعة بالقداحة هنا ليعرض عند الإشعال")]
    [SerializeField] private UnityEngine.Video.VideoClip lightCandleVideoClip;
    [SerializeField] private string lighterItemID = "Flint";

    private bool isLit = false;

    private void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isAlreadyDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("candle"))
                             || PlayerPrefs.GetInt($"{sceneName}_Task_candle", 0) == 1;

        if (isAlreadyDone)
        {
            isLit = true;
            if (flameEffect != null)
            {
                flameEffect.SetActive(true);
            }
        }
        else
        {
            // إطفاء الشعلة والضوء في البداية
            if (flameEffect != null)
            {
                flameEffect.SetActive(false);
            }
        }
    }

    private void OnMouseDown()
    {
        TryLightCandle();
    }

    private void Update()
    {
        // دعم النقر باللمس والماوس
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    TryLightCandle();
                }
            }
        }
    }

    private void TryLightCandle()
    {
        if (isLit) return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[CandlePuzzle]: InventoryManager غير موجود في المشهد!");
            return;
        }

        // الشرط: الضغط وتحديد الـ Flint بالحقيبة أولاً
        if (InventoryManager.Instance.SelectedItem.Equals(lighterItemID, System.StringComparison.OrdinalIgnoreCase))
        {
            LightCandle();
        }
        else
        {
            if (InventoryManager.Instance.HasItem(lighterItemID))
            {
                Debug.Log("<color=yellow>[الشمعة]:</color> انقري على القداحة بالحقيبة أولاً لتحديدها، ثم انقري على الشمعة!");
            }
            else
            {
                Debug.Log("<color=orange>[الشمعة]:</color> الشمعة مطفأة! يجب إيجاد القداحة أولاً.");
            }
        }
    }

    private void LightCandle()
    {
        isLit = true;

        // 1. حذف الـ Flint من الحقيبة ومسح تحديده حتى تختفي خانته تماماً بدون بياض
        InventoryManager.Instance.RemoveItem(lighterItemID);
        InventoryManager.Instance.ClearSelection();

        // 2. إشعال شعلة وضوء الشمعة مع صوت نار واشتعال سحري
        ProceduralAudioGenerator.PlaySound("candle_ignite", 0.85f);
        if (flameEffect != null)
        {
            flameEffect.SetActive(true);
        }

        // 3. استدعاء الكود ليظهر ع الجدار ثم ينحفظ بعد ثانيتين بالحقيبة تلقائياً
        SecretCodeWall secretWall = Object.FindAnyObjectByType<SecretCodeWall>();
        if (secretWall != null)
        {
            secretWall.RevealCode();
        }

        // 4. إبلاغ دليل التقدم بإنجاز مهمة إشعال الشمعة
        LevelProgressGuide.ReportTask("candle");

        // 5. تشغيل فيديو الجدار والشمعة (gemini_generated_video_ff76d4f1)
        if (UniversalVideoPlayer.Instance != null)
        {
            UniversalVideoPlayer.Instance.TryPlayItemVideo("Candle");
        }

        Debug.Log("<color=green>[CandlePuzzle]:</color> تم إشعال الشمعة بنجاح!");
    }
}