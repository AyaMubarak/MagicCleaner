using UnityEngine;

public class MainMenuNavigation : MonoBehaviour
{
    [Header("توهجات الأزرار الثلاثة")]
    public GameObject glowPlay;
    public GameObject glowLevels;
    public GameObject glowSettings;

    // دالة تُستدعى عند الضغط على PLAY
    public void SelectPlay()
    {
        SetGlowState(glowPlay);
    }

    // دالة تُستدعى عند الضغط على LEVELS
    public void SelectLevels()
    {
        SetGlowState(glowLevels);
    }

    // دالة تُستدعى عند الضغط على SETTINGS
    public void SelectSettings()
    {
        SetGlowState(glowSettings);
    }

    // دالة لإطفاء التوهج عن كل الأزرار (تُستدعى عند الرجوع أو إغلاق الإعدادات)
    public void ClearAllGlows()
    {
        if (glowPlay != null) glowPlay.SetActive(false);
        if (glowLevels != null) glowLevels.SetActive(false);
        if (glowSettings != null) glowSettings.SetActive(false);
    }

    // دالة مساعدة لتفعيل الزر المختار فقط وإطفاء الباقي
    private void SetGlowState(GameObject activeGlow)
    {
        ClearAllGlows();
        if (activeGlow != null)
        {
            activeGlow.SetActive(true);
        }
    }
}