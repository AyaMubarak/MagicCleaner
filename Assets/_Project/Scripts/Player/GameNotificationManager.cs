using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class GameNotificationManager : MonoBehaviour
{
    public Slider notificationSlider;
    private const string ChannelId = "game_reminders";

    void Start()
    {
        #if UNITY_ANDROID
        // إنشاء قناة الإشعارات (مطلوبة لأندرويد 8 وأحدث)
        var channel = new AndroidNotificationChannel()
        {
            Id = ChannelId,
            Name = "Game Reminders",
            Importance = Importance.High,
            Description = "Notifications to return to the game",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        #endif

        int isEnabled = PlayerPrefs.GetInt("NotificationsEnabled", 1);
        if (notificationSlider != null)
        {
            notificationSlider.value = isEnabled;
            notificationSlider.onValueChanged.AddListener(OnSwitchChanged);
        }
    }

    public void OnSwitchChanged(float value)
    {
        int state = value >= 0.5f ? 1 : 0;
        PlayerPrefs.SetInt("NotificationsEnabled", state);
        PlayerPrefs.Save();

        #if UNITY_ANDROID
        if (state == 0)
        {
            AndroidNotificationCenter.CancelAllNotifications();
        }
        #endif
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && PlayerPrefs.GetInt("NotificationsEnabled", 1) == 1)
        {
            ScheduleReturnNotification();
        }
    }

    private void ScheduleReturnNotification()
    {
        #if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();

        var notification = new AndroidNotification
        {
            Title = "We miss you! ✨",
            Text = "Mysterious puzzles await you. Return and play now!",
            FireTime = DateTime.Now.AddDays(1),
            SmallIcon = "icon_0",
            LargeIcon = "icon_1"
        };

        AndroidNotificationCenter.SendNotification(notification, ChannelId);
        #endif
    }
}