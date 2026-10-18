using UnityEngine;
using UnityEngine.UI;

public class TrashCleaner : MonoBehaviour
{
    [Header("Settings & Visuals")]
    [SerializeField] private int clicksToClean = 3;       // عدد النقرات لتنظيف القمامة
    [SerializeField] private Sprite flintIcon;            // صورة أيقونة القداحة في الحقيبة
    [SerializeField] private GameObject worldTrashObject; // مجسم القمامة في المشهد

    [Header("Item Data")]
    [SerializeField] private string itemName = "Flint";   // اسم العنصر ليتطابق مع الشمعة

    private int currentClicks = 0;
    private bool isCleaned = false;

    private void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isAlreadyDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("trash"))
                             || PlayerPrefs.GetInt($"{sceneName}_Task_trash", 0) == 1;

        if (isAlreadyDone)
        {
            isCleaned = true;
            if (worldTrashObject != null)
            {
                worldTrashObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            // إذا لم يتم استخدام القداحة لإشعال الشمعة بعد، إبقاء القداحة بالحقيبة
            bool candleDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("candle"))
                              || PlayerPrefs.GetInt($"{sceneName}_Task_candle", 0) == 1;

            if (!candleDone && InventoryManager.Instance != null && flintIcon != null && !InventoryManager.Instance.HasItem(itemName))
            {
                InventoryManager.Instance.AddItem(itemName, flintIcon);
            }
        }
    }

    private void OnMouseDown()
    {
        if (isCleaned) return;

        currentClicks++;
        Debug.Log("<color=yellow>[تنظيف]:</color> نقرة رقم: " + currentClicks + " من أصل " + clicksToClean);

        if (currentClicks >= clicksToClean)
        {
            CleanTrashAndGiveReward();
        }
    }

    private void CleanTrashAndGiveReward()
    {
        isCleaned = true;

        // 1. إضافة القداحة باسم "Flint" وصورتها إلى الحقيبة مع صوت جمع مبهج
        ProceduralAudioGenerator.PlaySound("pickup", 0.85f);
        if (InventoryManager.Instance != null && flintIcon != null)
        {
            InventoryManager.Instance.AddItem(itemName, flintIcon);
            Debug.Log("<color=green>[نجاح]:</color> تم تنظيف القمامة وأخذ " + itemName + " إلى الحقيبة بنجاح!");
        }
        else
        {
            Debug.LogWarning("لم يتم العثور على InventoryManager أو صورة flintIcon غير محددة!");
        }

        // 2. إخفاء مجسم القمامة من الغرفة
        if (worldTrashObject != null)
        {
            worldTrashObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // 3. إبلاغ دليل التقدم بإنجاز مهمة تنظيف القمامة
        LevelProgressGuide.ReportTask("trash");
    }
}