using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// مدير قائمة المستويات (Levels Menu Manager)
/// - يعرض زر START عند البداية.
/// - يعرض زر CONTINUE إذا بدأ اللاعب المرحلة ولم ينهها بعد.
/// - يعرض بانر Completed الأخضر عند إنهاء المرحلة بالكامل.
/// - يضمن فتح شاشة المستويات مباشرة عند الرجوع من الغرفة عبر QUIT.
/// </summary>
public class LevelsMenuManager : MonoBehaviour
{
    [Header("لوحة المستويات")]
    public GameObject levelsPanel;

    [Header("المستوى الأول (Level 1)")]
    public Button room1Button;
    public TextMeshProUGUI room1Text;
    public GameObject room1CompletedUI;
    public string room1SceneName = "Room1";

    [Header("المستوى الثاني (Level 2)")]
    public Button room2Button;
    public TextMeshProUGUI room2Text;
    public GameObject room2CompletedUI;
    public string room2SceneName = "Room2";

    [Header("المستوى الثالث (Level 3)")]
    public Button room3Button;
    public TextMeshProUGUI room3Text;
    public GameObject room3CompletedUI;
    public string room3SceneName = "Room3";

    [Header("ألوان نصوص الأزرار")]
    public Color startColor = Color.white;
    public Color continueColor = new Color(0.2f, 0.85f, 1f, 1f); // أزرق سماوي مضيء لـ CONTINUE
    public Color lockedColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoadedDirectCheck()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu") return;

        if (PlayerPrefs.GetInt("OpenLevelsDirectly", 0) == 1)
        {
            PlayerPrefs.DeleteKey("OpenLevelsDirectly");
            PlayerPrefs.Save();

            LevelsMenuManager[] managers = Object.FindObjectsByType<LevelsMenuManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (managers != null && managers.Length > 0)
            {
                LevelsMenuManager mgr = managers[0];
                if (mgr.levelsPanel != null)
                {
                    mgr.levelsPanel.SetActive(true);
                }
                else
                {
                    mgr.gameObject.SetActive(true);
                }
                mgr.OpenLevelsMenu();
            }
        }
    }

    private void Awake()
    {
        ResolveTextReferences();
        CheckDirectOpen();
    }

    private void Start()
    {
        CheckDirectOpen();
        UpdateLevelsState();
    }

    private void OnEnable()
    {
        ResolveTextReferences();
        UpdateLevelsState();
    }

    private void CheckDirectOpen()
    {
        if (PlayerPrefs.GetInt("OpenLevelsDirectly", 0) == 1)
        {
            PlayerPrefs.DeleteKey("OpenLevelsDirectly");
            PlayerPrefs.Save();
            OpenLevelsMenu();
        }
    }

    private void ResolveTextReferences()
    {
        if (room1Button != null)
        {
            var txt = room1Button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) room1Text = txt;
        }

        if (room2Button != null)
        {
            var txt = room2Button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) room2Text = txt;
        }

        if (room3Button != null)
        {
            var txt = room3Button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) room3Text = txt;
        }
    }

    public void UpdateLevelsState()
    {
        ResolveTextReferences();

        bool isRoom1Done = PlayerPrefs.GetInt("Room1_Done", 0) == 1 || PlayerPrefs.GetInt("Level1_Done", 0) == 1;
        bool isRoom2Done = PlayerPrefs.GetInt("Room2_Done", 0) == 1 || PlayerPrefs.GetInt("Level2_Done", 0) == 1;
        bool isRoom3Done = PlayerPrefs.GetInt("Room3_Done", 0) == 1 || PlayerPrefs.GetInt("Level3_Done", 0) == 1;

        // ================= LEVEL 1 =================
        SetupLevelSlot(
            isCompleted: isRoom1Done,
            isUnlocked: true,
            button: room1Button,
            buttonText: room1Text,
            completedUI: room1CompletedUI,
            sceneName: room1SceneName
        );

        // ================= LEVEL 2 =================
        SetupLevelSlot(
            isCompleted: isRoom2Done,
            isUnlocked: isRoom1Done,
            button: room2Button,
            buttonText: room2Text,
            completedUI: room2CompletedUI,
            sceneName: room2SceneName
        );

        // ================= LEVEL 3 =================
        SetupLevelSlot(
            isCompleted: isRoom3Done,
            isUnlocked: isRoom2Done,
            button: room3Button,
            buttonText: room3Text,
            completedUI: room3CompletedUI,
            sceneName: room3SceneName
        );
    }

    private void SetupLevelSlot(bool isCompleted, bool isUnlocked, Button button, TextMeshProUGUI buttonText, GameObject completedUI, string sceneName)
    {
        float progressPct = PlayerPrefs.GetFloat($"{sceneName}_ProgressPct", 0f);
        bool hasStarted = PlayerPrefs.GetInt($"{sceneName}_Started", 0) == 1;

        if (button != null)
        {
            button.gameObject.SetActive(true);
        }

        if (isCompleted)
        {
            // المرحلة مكتملة: إظهار بانر Completed والإطار الذهبي
            if (completedUI != null)
            {
                completedUI.SetActive(true);

                // التأكد من تفعيل الإطار الذهبي الجانبي الخاص بالبطاقة
                Transform goldenBorder = completedUI.transform.Find("Image");
                if (goldenBorder != null) goldenBorder.gameObject.SetActive(true);

                Button replayBtn = completedUI.GetComponent<Button>();
                if (replayBtn == null) replayBtn = completedUI.AddComponent<Button>();
                replayBtn.interactable = true;
                replayBtn.onClick.RemoveAllListeners();
                replayBtn.onClick.AddListener(() => StartCoroutine(LoadSceneFast(sceneName, replayBtn)));
            }

            if (buttonText != null)
            {
                buttonText.gameObject.SetActive(false);
            }
        }
        else if (isUnlocked)
        {
            // المرحلة مفتوحة ولم تكتمل بعد: إخفاء بانر Completed والإطار الذهبي تماماً
            if (completedUI != null)
            {
                completedUI.SetActive(false);
                Transform goldenBorder = completedUI.transform.Find("Image");
                if (goldenBorder != null) goldenBorder.gameObject.SetActive(false);
            }

            if (button != null)
            {
                button.interactable = true;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => StartCoroutine(LoadSceneFast(sceneName, button)));
            }

            if (buttonText != null)
            {
                buttonText.gameObject.SetActive(true);

                // Make sure text doesn't wrap or get cut off
                buttonText.textWrappingMode = TextWrappingModes.NoWrap;
                buttonText.overflowMode = TextOverflowModes.Ellipsis;
                buttonText.alignment = TextAlignmentOptions.Center;

                // إذا بدأ اللاعب ولم ينهِ -> CONTINUE بلون أبيض ناصع، وإلا -> START
                if (hasStarted || progressPct > 0f)
                {
                    buttonText.text = "CONTINUE";
                    buttonText.color = Color.white; // كقيمة بيضاء ناصعة كما طلب
                    buttonText.fontSize = Mathf.Min(buttonText.fontSize, 20f); // حجم مناسب لا ينكسر لسطرين
                }
                else
                {
                    buttonText.text = "START";
                    buttonText.color = Color.white;
                }
            }
        }
        else
        {
            // المرحلة مقفلة LOCKED
            if (completedUI != null) completedUI.SetActive(false);

            if (button != null)
            {
                button.interactable = false;
            }

            if (buttonText != null)
            {
                buttonText.gameObject.SetActive(true);
                buttonText.text = "LOCKED";
                buttonText.color = lockedColor;
            }
        }

        UpdateMiniProgressBar(button, sceneName, progressPct, isCompleted, isUnlocked);
    }

    private void UpdateMiniProgressBar(Button button, string sceneName, float progressPct, bool isCompleted, bool isUnlocked)
    {
        if (button == null) return;

        string barName = "MiniProgressHUD_" + sceneName;
        Transform existing = button.transform.Find(barName);

        // إظهار شريط التقدم الصغير فقط عندما تكون المرحلة بدأت وبها تقدم ولم تكتمل
        bool shouldShow = isUnlocked && !isCompleted && progressPct > 0f;

        if (!shouldShow)
        {
            if (existing != null) existing.gameObject.SetActive(false);
            return;
        }

        GameObject barObj;
        Image fillImg = null;
        TextMeshProUGUI labelText = null;

        if (existing == null)
        {
            barObj = new GameObject(barName);
            barObj.transform.SetParent(button.transform, false);

            RectTransform rt = barObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -14); // مسافة مريحة أسفل الزر تماماً
            rt.sizeDelta = new Vector2(230, 22); // بار عريض وواضح

            Image bg = barObj.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);

            fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.0f, 0.88f, 0.58f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;

            GameObject textObj = new GameObject("PctText");
            textObj.transform.SetParent(barObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            labelText = textObj.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 13;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
        }
        else
        {
            barObj = existing.gameObject;
            barObj.SetActive(true);

            RectTransform rt = barObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0, -14);
                rt.sizeDelta = new Vector2(230, 22);
            }

            Transform fillT = barObj.transform.Find("Fill");
            if (fillT != null) fillImg = fillT.GetComponent<Image>();

            Transform textT = barObj.transform.Find("PctText");
            if (textT != null)
            {
                labelText = textT.GetComponent<TextMeshProUGUI>();
                if (labelText != null) labelText.fontSize = 13;
            }
        }

        if (fillImg != null) fillImg.fillAmount = progressPct;
        if (labelText != null) labelText.text = $"{Mathf.RoundToInt(progressPct * 100f)}% COMPLETED";
    }

    private IEnumerator LoadSceneFast(string sceneName, Button btn)
    {
        if (btn != null) btn.interactable = false;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public void OpenLevelsMenu()
    {
        if (levelsPanel != null)
        {
            levelsPanel.SetActive(true);
            UpdateLevelsState();
        }

        // إضاءة زر LEVELS في القائمة وإطفاء باقي التوهجات
        MainMenuNavigation nav = FindFirstObjectByType<MainMenuNavigation>();
        if (nav != null)
        {
            nav.SelectLevels();
        }
    }

    public void CloseLevelsMenu()
    {
        if (levelsPanel != null)
        {
            levelsPanel.SetActive(false);
        }

        MainMenuNavigation nav = FindFirstObjectByType<MainMenuNavigation>();
        if (nav != null)
        {
            nav.ClearAllGlows();
        }
    }

    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=cyan>[LevelsMenu]:</color> All saved data wiped clean! Starting fresh.");
        UpdateLevelsState();
    }
}