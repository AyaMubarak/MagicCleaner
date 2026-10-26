using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#endif

public class GameNotificationManager : MonoBehaviour
{
    public Slider notificationSlider;
    private const string ChannelId = "game_reminders";

    void Start()
    {
        AutoFindSlider();
        RequestNotificationPermission();
        SetupNotificationChannel();

        int isEnabled = PlayerPrefs.GetInt("NotificationsEnabled", 1);
        if (notificationSlider != null)
        {
            notificationSlider.value = isEnabled;
            notificationSlider.onValueChanged.RemoveListener(OnSwitchChanged);
            notificationSlider.onValueChanged.AddListener(OnSwitchChanged);
        }
    }

    private void AutoFindSlider()
    {
        if (notificationSlider == null)
        {
            Slider[] allSliders = Resources.FindObjectsOfTypeAll<Slider>();
            foreach (var s in allSliders)
            {
                if (s != null && s.gameObject.name.IndexOf("Notification", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    notificationSlider = s;
                    break;
                }
            }
        }
    }

    public void RequestNotificationPermission()
    {
#if UNITY_ANDROID
        // طلب إذن الإشعارات لأجهزة Android 13 وأحدث (POST_NOTIFICATIONS)
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        }
#endif
    }

    private void SetupNotificationChannel()
    {
#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = ChannelId,
            Name = "Magic Cleaner Reminders",
            Importance = Importance.High,
            Description = "Notifications and hints to return to the game",
            EnableVibration = true,
            CanShowBadge = true
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
    }

    public void OnSwitchChanged(float value)
    {
        int state = value >= 0.5f ? 1 : 0;
        PlayerPrefs.SetInt("NotificationsEnabled", state);
        PlayerPrefs.Save();

        // اهتزاز خفيف ليعطي ردة فعل ملموسة للاعب على التلفون
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif

#if UNITY_ANDROID
        if (state == 1)
        {
            RequestNotificationPermission();

            // إرسال إشعار تأكيدي فوري بعد 5 ثوانٍ عند تفعيل المفتاح ليشاهد اللاعب ردة الفعل على هاتفه
            ScheduleTestFeedbackNotification();
        }
        else
        {
            AndroidNotificationCenter.CancelAllNotifications();
        }
#endif
        Debug.Log($"[GameNotificationManager] Notification state changed to: {state}");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && PlayerPrefs.GetInt("NotificationsEnabled", 1) == 1)
        {
            ScheduleReturnNotification();
        }
    }

    private void ScheduleTestFeedbackNotification()
    {
#if UNITY_ANDROID
        try
        {
            var testNotification = new AndroidNotification
            {
                Title = "Magic Cleaner ✨",
                Text = "Notifications activated! We will notify you with magical secrets.",
                FireTime = DateTime.Now.AddSeconds(5)
            };
            AndroidNotificationCenter.SendNotification(testNotification, ChannelId);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameNotificationManager] Test notification: {ex.Message}");
        }
#endif
    }

    private void ScheduleReturnNotification()
    {
#if UNITY_ANDROID
        try
        {
            AndroidNotificationCenter.CancelAllNotifications();

            var notification = new AndroidNotification
            {
                Title = "We miss you! ✨",
                Text = "Mysterious puzzles await you. Return and play now!",
                FireTime = DateTime.Now.AddHours(24)
            };

            AndroidNotificationCenter.SendNotification(notification, ChannelId);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameNotificationManager] Schedule notification: {ex.Message}");
        }
#endif
    }
}