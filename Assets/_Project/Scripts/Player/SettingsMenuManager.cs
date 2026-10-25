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
    public Button shareButton;

    void Start()
    {
        AutoResolveReferences();

        // التأكد من إخفاء الكتاب عند بدء تشغيل اللعبة
        if (settingsBookPanel != null)
        {
            settingsBookPanel.SetActive(false);
        }

        // تحميل القيم وتطبيقها فوراً على الصوت والموسيقى
        LoadSavedSettings();
        ProceduralAudioGenerator.RefreshAudioSettings();
    }

    private void AutoResolveReferences()
    {
        if (settingsBookPanel == null)
        {
            settingsBookPanel = GameObject.Find("SettingsBookPanel");
        }

        if (settingsBookPanel != null)
        {
            if (masterVolumeSlider == null || musicSlider == null || sfxSlider == null || muteSwitch == null || notificationSwitch == null)
            {
                Slider[] sliders = settingsBookPanel.GetComponentsInChildren<Slider>(true);
                foreach (var s in sliders)
                {
                    string sName = s.gameObject.name;
                    if (sName.Contains("Master")) masterVolumeSlider = s;
                    else if (sName.Contains("Music")) musicSlider = s;
                    else if (sName.Contains("SFX")) sfxSlider = s;
                    else if (sName.Contains("Mute")) muteSwitch = s;
                    else if (sName.Contains("Notification")) notificationSwitch = s;
                }
            }

            if (notificationSwitch != null)
            {
                notificationSwitch.onValueChanged.RemoveListener(OnNotificationSwitchChanged);
                notificationSwitch.onValueChanged.AddListener(OnNotificationSwitchChanged);
            }

            // ربط الأزرار (Apply Changes و Share) داخل الكتاب برمجياً لضمان استجابتها
            Button[] buttons = settingsBookPanel.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string bName = b.gameObject.name.ToLower();
                TMPro.TextMeshProUGUI tmp = b.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                string textContent = (tmp != null) ? tmp.text.ToLower() : "";

                if (bName.Contains("apply") || textContent.Contains("apply"))
                {
                    b.onClick.RemoveListener(OnApplyChangesClicked);
                    b.onClick.AddListener(OnApplyChangesClicked);
                }
                else if (bName.Contains("share") || textContent.Contains("share"))
                {
                    shareButton = b;
                    b.onClick.RemoveListener(ShareGame);
                    b.onClick.AddListener(ShareGame);
                }
            }
        }
    }

    /// <summary>
    /// يتم استدعاؤها عند الضغط على زر SETTINGS في الشاشة الرئيسية
    /// </summary>
    public void OpenSettings()
    {
        AutoResolveReferences();
        LoadSavedSettings();

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

        Debug.Log("تم حفظ الإعدادات وتطبيق التغييرات الصوتية وإغلاق الكتاب بنجاح!");
    }

    /// <summary>
    /// دالة حفظ الإعدادات في الذاكرة وتطبيقها الفوري
    /// </summary>
    private void SaveSettings()
    {
        if (masterVolumeSlider != null) PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        if (musicSlider != null) PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        if (sfxSlider != null) PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
        if (muteSwitch != null) PlayerPrefs.SetInt("IsMuted", muteSwitch.value >= 0.5f ? 1 : 0);
        if (notificationSwitch != null) PlayerPrefs.SetInt("NotificationsEnabled", notificationSwitch.value >= 0.5f ? 1 : 0);

        PlayerPrefs.Save();

        // تحديث فوري للموسيقى والصوت العام
        ProceduralAudioGenerator.RefreshAudioSettings();
    }

    /// <summary>
    /// يتم استدعاؤها فور تحريك مفتاح الإشعارات
    /// </summary>
    public void OnNotificationSwitchChanged(float value)
    {
        int state = value >= 0.5f ? 1 : 0;
        PlayerPrefs.SetInt("NotificationsEnabled", state);
        PlayerPrefs.Save();

        GameNotificationManager notifManager = UnityEngine.Object.FindAnyObjectByType<GameNotificationManager>();
        if (notifManager != null)
        {
            notifManager.OnSwitchChanged(value);
        }
    }

    /// <summary>
    /// مشاركة اللعبة عبر شاشة المشاركة الرسمية (Android Share Sheet / Clipboard)
    /// </summary>
    public void ShareGame()
    {
        string shareSubject = "Magic Cleaner - Escape Mystery";
        string shareMessage = "Join the mystery in Magic Cleaner! Solve ancient puzzles and unlock magical rooms. ✨\nPlay now: https://play.google.com/store/apps/details?id=com.AyaMubarak.MagicCleaner";

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
            using (AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent"))
            {
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intentObject.Call<AndroidJavaObject>("setType", "text/plain");
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), shareSubject);
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), shareMessage);

                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "Share Magic Cleaner"))
                {
                    currentActivity.Call("startActivity", chooser);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShareGame Error]: {ex.Message}");
        }
#else
        GUIUtility.systemCopyBuffer = shareMessage;
        Debug.Log($"[ShareGame]: Link copied to clipboard:\n{shareMessage}");
#endif
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
