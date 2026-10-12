using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuManager : MonoBehaviour
{
    [Header("قائمة الإعدادات (Book UI Panel)")]
    [Tooltip("اسحبي كائن SettingsBookPanel هنا")]
    public GameObject settingsBookPanel;

    [Header("عناصر الإعدادات (اختياري للتحكم الإضافي)")]
    public Slider masterVolumeSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider muteSwitch;
    public Slider notificationSwitch;

    void Start()
    {
        // التأكد من إخفاء الكتاب عند بدء تشغيل اللعبة
        if (settingsBookPanel != null)
        {
            settingsBookPanel.SetActive(false);
        }

        // تحميل القيم المحفوظة مسبقاً (إن وجدت)
        LoadSavedSettings();
    }

    /// <summary>
    /// يتم استدعاؤها عند الضغط على زر SETTINGS في الشاشة الرئيسية
    /// </summary>
    public void OpenSettings()
    {
        if (settingsBookPanel != null)
        {
            settingsBookPanel.SetActive(true);
        }
    }

    /// <summary>
    /// يتم استدعاؤها عند الضغط على زر Apply Changes داخل الكتاب
    /// </summary>
    public void OnApplyChangesClicked()
    {
        // 1. حفظ جميع التعديلات في PlayerPrefs
        SaveSettings();

        // 2. إخفاء الكتاب والعودة للشاشة الرئيسية
        if (settingsBookPanel != null)
        {
            settingsBookPanel.SetActive(false);
        }

        Debug.Log("تم حفظ الإعدادات وإغلاق الكتاب بنجاح!");
    }

    /// <summary>
    /// دالة حفظ الإعدادات في الذاكرة
    /// </summary>
    private void SaveSettings()
    {
        if (masterVolumeSlider != null) PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        if (musicSlider != null) PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        if (sfxSlider != null) PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
        if (muteSwitch != null) PlayerPrefs.SetInt("IsMuted", muteSwitch.value >= 0.5f ? 1 : 0);
        if (notificationSwitch != null) PlayerPrefs.SetInt("NotificationsEnabled", notificationSwitch.value >= 0.5f ? 1 : 0);

        PlayerPrefs.Save();
    }

    /// <summary>
    /// دالة استرجاع الإعدادات المحفوظة
    /// </summary>
    private void LoadSavedSettings()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (muteSwitch != null) muteSwitch.value = PlayerPrefs.GetInt("IsMuted", 0);
        if (notificationSwitch != null) notificationSwitch.value = PlayerPrefs.GetInt("NotificationsEnabled", 1);
    }
}