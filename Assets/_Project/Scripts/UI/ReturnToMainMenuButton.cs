using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// سكربت زر الرجوع العام للقائمة الرئيسية / قائمة المستويات
/// يضمن عمل الزر بنقرة واحدة وحفظ التقدم دائماً
/// </summary>
public class ReturnToMainMenuButton : MonoBehaviour
{
    [Tooltip("اسم مشهد القائمة الرئيسية")]
    public string menuSceneName = "MainMenu";

    [Tooltip("إذا كان مفعلاً، يفتح شاشة المستويات مباشرة عند دخول القائمة")]
    public bool openLevelsDirectly = true;

    private void Awake()
    {
        // ربط الزر تلقائياً إن وُجد
        Button btn = GetComponent<Button>();
        if (btn == null) btn = GetComponentInChildren<Button>();

        if (btn != null)
        {
            btn.onClick.RemoveListener(ExecuteReturn);
            btn.onClick.AddListener(ExecuteReturn);
        }

        // إيقاف RaycastTarget عن كل الصور والنصوص الفرعية حتى لا تحجب نقرة الزر
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            if (btn != null && g.gameObject == btn.gameObject) continue;
            g.raycastTarget = false;
        }
    }

    public void ExecuteReturn()
    {
        Debug.Log("<color=cyan>[Back Button]:</color> تم الضغط على زر الرجوع! حفظ والعودة للقائمة...");

        if (openLevelsDirectly)
        {
            PlayerPrefs.SetInt("OpenLevelsDirectly", 1);
            PlayerPrefs.Save();
        }

        if (LevelProgressGuide.Instance != null)
        {
            LevelProgressGuide.Instance.SaveProgress();
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SaveInventoryToDisk();
        }

        SceneManager.LoadScene(menuSceneName);
    }
}
