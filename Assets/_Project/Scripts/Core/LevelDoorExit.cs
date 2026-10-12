using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelDoorExit : MonoBehaviour
{
    [Header("إعدادات المرحلة الحالية")]
    [Tooltip("مفتاح الحفظ الخاص بهذه الغرفة مثل Room1_Done")]
    public string roomDoneKey = "Room1_Done";

    [Header("المشهد المراد الانتقال إليه")]
    [Tooltip("اسم مشهد القائمة الرئيسية / المستويات")]
    public string menuSceneName = "MainMenu"; // تأكدي من كتابة اسم المشهد كما هو بالضبط

    private bool hasExited = false;

    // لما يدخل اللاعب في منطقة الباب (Trigger)
    private void OnTriggerEnter(Collider other)
    {
        // التحقق من أن الكائن الذي لمس الباب هو اللاعب
        if ((other.CompareTag("Player") || other.GetComponent<CharacterController>() != null) && !hasExited)
        {
            hasExited = true;
            CompleteLevelAndExit();
        }
    }

    public void CompleteLevelAndExit()
    {
        // 1. حفظ اكتمال المرحلة
        PlayerPrefs.SetInt(roomDoneKey, 1);
        PlayerPrefs.Save();

        // 2. الانتقال إلى مشهد القائمة
        SceneManager.LoadScene(menuSceneName);
    }
}