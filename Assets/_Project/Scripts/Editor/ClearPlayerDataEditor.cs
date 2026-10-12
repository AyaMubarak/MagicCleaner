#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ClearPlayerDataEditor
{
    [MenuItem("Tools/Clear All Save Data (PlayerPrefs)")]
    public static void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=green><b>[Save System] All PlayerPrefs saved data has been wiped clean! You can now test from scratch.</b></color>");
        EditorUtility.DisplayDialog("Clear Save Data", "All game save data (PlayerPrefs) has been wiped successfully!\n\nYou can now test from Level 1 fresh.", "OK");
    }
}
#endif
