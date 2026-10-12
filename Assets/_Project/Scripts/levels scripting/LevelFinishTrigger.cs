using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFinishTrigger : MonoBehaviour
{
    [Tooltip("اسم مشهد القائمة الرئيسية للعودة إليها")]
    public string mainMenuSceneName = "MainMenu";

    /// <summary>
    /// استدعي هذه الدالة عند إنهاء المرحلة الأولى
    /// </summary>
    public void OnLevel1Complete()
    {
        // حفظ إكمال المرحلة الأولى بكلا المسميين للتوافق
        PlayerPrefs.SetInt("Level1_Done", 1);
        PlayerPrefs.SetInt("Room1_Done", 1);
        PlayerPrefs.SetInt("OpenLevelsDirectly", 1);
        PlayerPrefs.Save();

        // إبلاغ دليل التقدم بإتمام الباب
        LevelProgressGuide.ReportTask("door");

        Debug.Log("تم حفظ إنهاء Level 1 بنجاح والانتقال للقائمة!");

        // العودة للقائمة الرئيسية
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // زر تجريبي / للمطورين لإعادة تصفير التقدم واختبار START مجدداً
    [ContextMenu("Reset Progress (للتجربة فقط)")]
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey("Level1_Done");
        PlayerPrefs.DeleteKey("Room1_Done");
        PlayerPrefs.DeleteKey("OpenLevelsDirectly");
        PlayerPrefs.Save();
        Debug.Log("تم تصفير التقدم لاختبار زر START من جديد");
    }
}