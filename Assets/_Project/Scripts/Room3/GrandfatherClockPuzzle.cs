using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GrandfatherClockPuzzle : MonoBehaviour
{
    public static GrandfatherClockPuzzle Instance { get; private set; }

    [Header("1. Clock Hand Setup")]
    [Tooltip("Item name in InventoryManager")]
    [SerializeField] private string clockHandItemID = "ClockHand";

    [Tooltip("3D Minute hand on clock model")]
    [SerializeField] private GameObject minuteHandObject;

    [Tooltip("3D Hour hand Transform")]
    [SerializeField] private Transform hourHandTransform;

    [Tooltip("3D Minute hand Transform")]
    [SerializeField] private Transform minuteHandTransform;

    [Header("2. Target Alignment Time (From Memory Painting: 8:15)")]
    [SerializeField] private int targetHour = 8;
    [SerializeField] private int targetMinute = 15;

    [Header("3. Reward & Effects")]
    [SerializeField] private Animator cabinetAnimator;
    [SerializeField] private string cabinetOpenTrigger = "Open";
    [SerializeField] private GameObject cabinetLockObject;
    [SerializeField] private GameObject goldenKeyInCabinet;

    [SerializeField] private AudioClip insertHandSound;
    [SerializeField] private AudioClip tickSound;
    [SerializeField] private AudioClip clockChimeSuccessSound;

    // Internal state
    private bool isHandInstalled = false;
    private bool isPuzzleSolved = false;

    // Time state
    private int currentHour = 12;
    private int currentMinute = 0;

    // UI screen references
    private GameObject clockScreenCanvas;
    private RectTransform dialRectTransform;
    private RectTransform uiHourHandRT;
    private RectTransform uiMinuteHandRT;
    private TextMeshProUGUI uiDigitalTimeTMP;
    private TextMeshProUGUI uiStatusFeedbackTMP;
    private bool isScreenOpen = false;

    // Drag tracking
    private bool isDraggingHourHand = false;
    private bool isDraggingMinuteHand = false;
    private int lastPlayedMinuteTick = -1;

    // Floating Notification Banner
    private static GameObject notificationBannerObj;
    private static TextMeshProUGUI notificationTextTMP;
    private static Coroutine notificationCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadHandler()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name == "Room3")
            {
                if (FindAnyObjectByType<GrandfatherClockPuzzle>() == null)
                {
                    GameObject clockObj = FindClockInScene();
                    if (clockObj != null)
                    {
                        Debug.Log("<color=cyan>[GrandfatherClock]:</color> Auto-attaching GrandfatherClockPuzzle to " + clockObj.name);
                        clockObj.AddComponent<GrandfatherClockPuzzle>();
                    }
                    else
                    {
                        GameObject manager = new GameObject("Room3_GrandfatherClock_Manager");
                        manager.AddComponent<GrandfatherClockPuzzle>();
                    }
                }
            }
        };
    }

    private static GameObject FindClockInScene()
    {
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in all)
        {
            string n = go.name.ToLowerInvariant();
            if (n.Contains("grandfather_clock") || n == "clock" || n.Contains("clockface") || n.Contains("clockcase"))
            {
                Transform root = go.transform;
                while (root.parent != null && root.parent.name.ToLowerInvariant().Contains("clock"))
                {
                    root = root.parent;
                }
                return root.gameObject;
            }
        }
        return null;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        targetHour = 8;
        targetMinute = 15;
    }

    private void Start()
    {
        targetHour = 8;
        targetMinute = 15;

        isPuzzleSolved = PlayerPrefs.GetInt("Room3_ClockSolved", 0) == 1
                         || PlayerPrefs.GetInt("Room3_Task_clock_hand", 0) == 1;

        isHandInstalled = isPuzzleSolved || PlayerPrefs.GetInt("Room3_HandInstalled", 0) == 1;

        if (minuteHandObject != null)
        {
            minuteHandObject.SetActive(isHandInstalled);
        }

        if (isPuzzleSolved)
        {
            currentHour = targetHour;
            currentMinute = targetMinute;
            UnlockCabinetImmediate();
            HideClockWorldObject();
        }
        else
        {
            if (goldenKeyInCabinet != null)
            {
                goldenKeyInCabinet.SetActive(false);
            }
        }

        UpdateHandsVisuals();
        EnsureCollidersRecursively(gameObject);
        BuildClockScreenUI();
        BuildFloatingBannerUI();
    }

    private void EnsureCollidersRecursively(GameObject target)
    {
        if (target == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            foreach (Renderer r in renderers)
            {
                if (r.GetComponent<Collider>() == null)
                {
                    r.gameObject.AddComponent<BoxCollider>();
                }
            }
        }

        if (target.GetComponent<Collider>() == null)
        {
            BoxCollider col = target.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.2f, 0);
            col.size = new Vector3(1.2f, 2.4f, 1.2f);
        }
    }

    // Direct Interaction with the Clock in 3D
    public void InteractWithClock()
    {
        if (isPuzzleSolved)
        {
            ProceduralAudioGenerator.PlaySound("ui_click", 0.6f);
            return;
        }

        // 1. Mandatory requirement: Memory Painting inspected
        bool hasPainting = (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("MemoryPainting"))
                           || (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("painting"))
                           || PlayerPrefs.GetInt("Room3_Task_painting", 0) == 1;

        if (!hasPainting)
        {
            ProceduralAudioGenerator.PlaySound("door_locked", 0.7f);
            return;
        }

        // 2. إذا لم يتم تركيب العقرب بعد:
        if (!isHandInstalled)
        {
            // التحقق هل اللاعب يملك العقرب (سواء حدده بالحقيبة أو موجود معه تلقائياً)
            bool hasHandInInventory = (InventoryManager.Instance != null && 
                                      (InventoryManager.Instance.HasItem(clockHandItemID) || 
                                       InventoryManager.Instance.HasItem("HourHand") || 
                                       InventoryManager.Instance.HasItem("clock_hand")))
                                      || PlayerPrefs.GetInt("Room3_HourHandCollected", 0) == 1
                                      || PlayerPrefs.GetInt("HasItem_ClockHand", 0) == 1;

            if (hasHandInInventory)
            {
                // تركيب العقرب تلقائياً بدون إجبار اللاعب على فتح الحقيبة وتحديده يدوياً!
                InstallHandFromInventory();
                OpenClockScreen();
            }
            else
            {
                // لم يعثر على العقرب بعد
                ProceduralAudioGenerator.PlaySound("door_locked", 0.65f);
            }
        }
        else
        {
            // العقرب مركب مسبقاً -> تفتح شاشة الساعة مباشرة لضبطها على 8:15
            OpenClockScreen();
        }
    }

    private void InstallHandFromInventory()
    {
        if (InventoryManager.Instance != null)
        {
            if (InventoryManager.Instance.HasItem(clockHandItemID))
                InventoryManager.Instance.RemoveItem(clockHandItemID);
            else if (InventoryManager.Instance.HasItem("HourHand"))
                InventoryManager.Instance.RemoveItem("HourHand");
            else if (!string.IsNullOrEmpty(InventoryManager.Instance.SelectedItem))
                InventoryManager.Instance.RemoveItem(InventoryManager.Instance.SelectedItem);

            InventoryManager.Instance.ClearSelection();
        }

        isHandInstalled = true;
        PlayerPrefs.SetInt("Room3_HandInstalled", 1);
        PlayerPrefs.DeleteKey("HasItem_ClockHand");
        PlayerPrefs.Save();

        if (minuteHandObject != null) minuteHandObject.SetActive(true);

        if (insertHandSound != null)
            AudioSource.PlayClipAtPoint(insertHandSound, transform.position);
        else
            ProceduralAudioGenerator.PlaySound("pickup", 0.95f);

        UpdateHandsVisuals();
    }

    // ---------------- Interactive 2D Screen ----------------

    public void OpenClockScreen()
    {
        if (clockScreenCanvas != null)
        {
            clockScreenCanvas.SetActive(true);
            isScreenOpen = true;
            UpdateHandsVisuals();
            ProceduralAudioGenerator.PlaySound("dial_tick", 0.7f);
        }
    }

    public void CloseClockScreen()
    {
        if (clockScreenCanvas != null)
        {
            clockScreenCanvas.SetActive(false);
            isScreenOpen = false;
            isDraggingHourHand = false;
            isDraggingMinuteHand = false;
            ProceduralAudioGenerator.PlaySound("ui_click", 0.5f);
        }
    }

    private void UpdateHandsVisuals()
    {
        // 1. Digital Time (Clear English 24/12 display)
        if (uiDigitalTimeTMP != null)
        {
            uiDigitalTimeTMP.text = $"{currentHour:00}:{currentMinute:00}";
        }

        // 2. UI Hands Rotation
        if (uiHourHandRT != null)
        {
            float hourAngle = (currentHour % 12) * 30f + (currentMinute / 60f) * 30f;
            uiHourHandRT.localRotation = Quaternion.Euler(0f, 0f, -hourAngle);
        }

        if (uiMinuteHandRT != null)
        {
            float minuteAngle = currentMinute * 6f;
            uiMinuteHandRT.localRotation = Quaternion.Euler(0f, 0f, -minuteAngle);
        }

        // 3. 3D Scene Hands
        if (hourHandTransform != null)
        {
            float hourAngle = (currentHour % 12) * 30f + (currentMinute / 60f) * 30f;
            hourHandTransform.localRotation = Quaternion.Euler(0f, 0f, -hourAngle);
        }

        if (minuteHandTransform != null && isHandInstalled)
        {
            float minuteAngle = currentMinute * 6f;
            minuteHandTransform.localRotation = Quaternion.Euler(0f, 0f, -minuteAngle);
        }
    }

    private void CheckSolved()
    {
        if (isPuzzleSolved) return;

        bool hourMatches = (currentHour % 12) == (targetHour % 12);
        bool minuteMatches = (currentMinute == targetMinute);

        if (hourMatches && minuteMatches)
        {
            isPuzzleSolved = true;
            PlayerPrefs.SetInt("Room3_ClockSolved", 1);
            PlayerPrefs.SetInt("Room3_Task_clock_hand", 1);
            PlayerPrefs.Save();

            StartCoroutine(SolveAndRevealRoutine());
        }
    }

    private IEnumerator SolveAndRevealRoutine()
    {
        // 1. Instant visual feedback on digital display & status
        if (uiDigitalTimeTMP != null)
        {
            uiDigitalTimeTMP.color = new Color(0.25f, 1.0f, 0.45f); // Bright emerald green
        }

        if (uiStatusFeedbackTMP != null)
        {
            uiStatusFeedbackTMP.text = "<color=#47FF76>Time Aligned! Unlocking...</color>";
        }

        // 2. Play mechanical gears turning
        ProceduralAudioGenerator.PlaySound("clock_gears", 1.0f);
        yield return new WaitForSeconds(0.9f);

        // 3. Play chime bell
        if (clockChimeSuccessSound != null)
            AudioSource.PlayClipAtPoint(clockChimeSuccessSound, transform.position);
        else
            ProceduralAudioGenerator.PlaySound("clock_chime", 1.0f);

        yield return new WaitForSeconds(0.8f);

        // 4. Automatically close the 2D clock screen
        CloseClockScreen();

        // 5. Hide the clock completely (تختفي الساعة تماماً)
        HideClockWorldObject();

        // 6. Unlock cabinet & reveal golden key
        ProceduralAudioGenerator.PlaySound("cabinet_unlock", 0.95f);
        UnlockCabinetImmediate();

        if (Room3KeyPickup.Instance != null)
        {
            Room3KeyPickup.Instance.OnClockSolvedAndDisappeared();
        }

        LevelProgressGuide.ReportTask("clock_hand");
        Debug.Log("<color=green>[GrandfatherClock]:</color> Clock aligned and disappeared! Cabinet unlocked and key revealed!");
    }

    private void HideClockWorldObject()
    {
        // 1. Hide the entire clock gameObject or its mesh renderers / colliders
        GameObject clockGo = gameObject;
        if (clockGo.name.Contains("Manager"))
        {
            GameObject sceneClock = FindClockInScene();
            if (sceneClock != null) clockGo = sceneClock;
        }

        // Disable all renderers and colliders so it disappears seamlessly
        Renderer[] allRenderers = clockGo.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in allRenderers)
        {
            r.enabled = false;
        }

        Collider[] allCols = clockGo.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in allCols)
        {
            col.enabled = false;
        }

        // Also if minute hand or child objects exist
        if (minuteHandObject != null) minuteHandObject.SetActive(false);
    }

    private void UnlockCabinetImmediate()
    {
        // الخزانة / capinat ثابتة في مكانها تماماً ولا تتحرك أو تدور بالمرة
        if (cabinetLockObject != null)
        {
            cabinetLockObject.SetActive(false);
        }

        // إظهار مجسم المفتاح الذهبي الحقيقي في المشهد
        GameObject keyObj = goldenKeyInCabinet;
        if (keyObj == null)
        {
            KeyPickup[] allKeyPickups = Resources.FindObjectsOfTypeAll<KeyPickup>();
            foreach (var kp in allKeyPickups)
            {
                if (kp != null && kp.gameObject.scene.isLoaded)
                {
                    keyObj = kp.gameObject;
                    break;
                }
            }
        }

        if (keyObj == null)
        {
            keyObj = GameObject.Find("key") ?? GameObject.Find("ExitKey") ?? GameObject.Find("ChestKey") ?? GameObject.Find("GoldenKey");
        }

        if (keyObj != null)
        {
            keyObj.SetActive(true);
        }
    }

    // ---------------- Interactive Drag Handling for Clock Dial ----------------

    private void HandleDialTouchDrag(Vector2 screenPos, bool isInitialPress)
    {
        if (dialRectTransform == null || isPuzzleSolved) return;

        Camera cam = null;
        Canvas parentCanvas = dialRectTransform.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = parentCanvas.worldCamera ?? Camera.main;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dialRectTransform, screenPos, cam, out Vector2 localPoint))
        {
            return;
        }

        float distanceFromCenter = localPoint.magnitude;
        // Dial has radius of ~190 units (380x380)
        if (distanceFromCenter > 225f)
        {
            if (isInitialPress) return;
        }

        // Calculate continuous clockwise angle: 0 degrees at 12 o'clock, 90 at 3 o'clock, 180 at 6, 270 at 9
        float angle = Mathf.Atan2(localPoint.x, localPoint.y) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        if (isInitialPress)
        {
            // If inner radius (< 95) -> dragging Hour Hand
            // If outer radius (>= 95) -> dragging Minute Hand
            if (distanceFromCenter < 95f)
            {
                isDraggingHourHand = true;
                isDraggingMinuteHand = false;
            }
            else
            {
                isDraggingMinuteHand = true;
                isDraggingHourHand = false;
            }
        }

        if (isDraggingMinuteHand)
        {
            // Continuous fine minute rotation (0 to 59 minutes, every single minute!)
            int newMin = Mathf.RoundToInt((angle / 360f) * 60f) % 60;
            if (newMin != currentMinute)
            {
                currentMinute = newMin;
                if (currentMinute != lastPlayedMinuteTick)
                {
                    ProceduralAudioGenerator.PlaySound("dial_tick", 0.6f);
                    lastPlayedMinuteTick = currentMinute;
                }
                UpdateHandsVisuals();
                CheckSolved();
            }
        }
        else if (isDraggingHourHand)
        {
            // Continuous hour rotation (1 to 12)
            int newHour = Mathf.RoundToInt(angle / 30f);
            if (newHour == 0) newHour = 12;
            if (newHour != currentHour)
            {
                currentHour = newHour;
                ProceduralAudioGenerator.PlaySound("dial_tick", 0.75f);
                UpdateHandsVisuals();
                CheckSolved();
            }
        }
    }

    // ---------------- UI Construction ----------------

    private void BuildClockScreenUI()
    {
        if (clockScreenCanvas != null) return;

        // 1. Root Overlay Canvas
        clockScreenCanvas = new GameObject("GrandfatherClock_ScreenCanvas");
        Canvas c = clockScreenCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 95;

        CanvasScaler scaler = clockScreenCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        clockScreenCanvas.AddComponent<GraphicRaycaster>();

        // 2. Dark Overlay Backdrop (Clicking backdrop closes modal)
        GameObject bgObj = new GameObject("Backdrop");
        bgObj.transform.SetParent(clockScreenCanvas.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.90f);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // 3. Modal Panel Frame
        GameObject modal = new GameObject("ClockModal");
        modal.transform.SetParent(clockScreenCanvas.transform, false);
        Image modalImg = modal.AddComponent<Image>();
        modalImg.color = new Color(0.09f, 0.12f, 0.17f, 0.98f);
        RectTransform modalRT = modal.GetComponent<RectTransform>();
        modalRT.sizeDelta = new Vector2(650, 780);
        modalRT.anchoredPosition = Vector2.zero;

        Outline outline = modal.AddComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.68f, 0.28f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Titles in English (LiberationSans compatible - zero square glyphs!)
        CreateText(modal.transform, "Title", "Grandfather Clock Alignment", 26, new Vector2(0, 335), new Vector2(600, 45), new Color(0.98f, 0.88f, 0.60f));
        CreateText(modal.transform, "SubTitle", "Align the clock hands to the discovered memory time", 18, new Vector2(0, 295), new Vector2(600, 35), new Color(0.78f, 0.80f, 0.88f));

        // Digital Time Display
        uiDigitalTimeTMP = CreateText(modal.transform, "DigitalTime", "12:00", 42, new Vector2(0, 240), new Vector2(240, 50), new Color(1.0f, 0.85f, 0.35f));

        // Clock Dial (Face)
        GameObject dialObj = new GameObject("DialFace");
        dialObj.transform.SetParent(modal.transform, false);
        Image dialImg = dialObj.AddComponent<Image>();
        dialImg.color = new Color(0.95f, 0.91f, 0.82f, 1f); // Vintage Ivory
        dialRectTransform = dialObj.GetComponent<RectTransform>();
        dialRectTransform.sizeDelta = new Vector2(380, 380);
        dialRectTransform.anchoredPosition = new Vector2(0, 5);

        Outline dialOutline = dialObj.AddComponent<Outline>();
        dialOutline.effectColor = new Color(0.32f, 0.22f, 0.10f, 1f);
        dialOutline.effectDistance = new Vector2(5f, -5f);

        // Numbers 1 to 12 & 60-Minute Dial Ticks
        for (int m = 0; m < 60; m++)
        {
            if (m % 5 == 0) continue; // Skip minute lines where large hours sit
            float tickAngle = m * 6f * Mathf.Deg2Rad;
            float tickRadius = 175f;
            Vector2 tPos = new Vector2(Mathf.Sin(tickAngle) * tickRadius, Mathf.Cos(tickAngle) * tickRadius);

            GameObject tickObj = new GameObject($"Tick_{m}");
            tickObj.transform.SetParent(dialObj.transform, false);
            Image tImg = tickObj.AddComponent<Image>();
            tImg.color = new Color(0.45f, 0.40f, 0.32f, 0.85f);
            tImg.raycastTarget = false;
            RectTransform tRT = tickObj.GetComponent<RectTransform>();
            tRT.sizeDelta = new Vector2(2.5f, 8f);
            tRT.anchoredPosition = tPos;
            tRT.localRotation = Quaternion.Euler(0f, 0f, -m * 6f);
        }

        for (int h = 1; h <= 12; h++)
        {
            float ang = h * 30f * Mathf.Deg2Rad;
            float r = 148f;
            Vector2 numPos = new Vector2(Mathf.Sin(ang) * r, Mathf.Cos(ang) * r);
            var numTMP = CreateText(dialObj.transform, $"Num_{h}", h.ToString(), 24, numPos, new Vector2(40, 40), new Color(0.18f, 0.12f, 0.08f));
            if (numTMP != null) numTMP.raycastTarget = false;
        }

        // Center Pin
        GameObject pin = new GameObject("CenterPin");
        pin.transform.SetParent(dialObj.transform, false);
        Image pinImg = pin.AddComponent<Image>();
        pinImg.color = new Color(0.75f, 0.60f, 0.22f, 1f); // Gold pin
        pinImg.raycastTarget = false;
        RectTransform pinRT = pin.GetComponent<RectTransform>();
        pinRT.sizeDelta = new Vector2(24, 24);
        pinRT.anchoredPosition = Vector2.zero;

        // Hour Hand (Inner & Wider)
        GameObject hHand = new GameObject("HourHand");
        hHand.transform.SetParent(dialObj.transform, false);
        uiHourHandRT = hHand.AddComponent<RectTransform>();
        uiHourHandRT.sizeDelta = new Vector2(16, 110);
        uiHourHandRT.pivot = new Vector2(0.5f, 0f);
        uiHourHandRT.anchoredPosition = Vector2.zero;
        Image hImg = hHand.AddComponent<Image>();
        hImg.color = new Color(0.12f, 0.10f, 0.08f, 1f);
        hImg.raycastTarget = false;

        // Minute Hand (Longer & Fine - Rotates smoothly through all 60 minutes)
        GameObject mHand = new GameObject("MinuteHand");
        mHand.transform.SetParent(dialObj.transform, false);
        uiMinuteHandRT = mHand.AddComponent<RectTransform>();
        uiMinuteHandRT.sizeDelta = new Vector2(8, 160);
        uiMinuteHandRT.pivot = new Vector2(0.5f, 0f);
        uiMinuteHandRT.anchoredPosition = Vector2.zero;
        Image mImg = mHand.AddComponent<Image>();
        mImg.color = new Color(0.70f, 0.18f, 0.15f, 1f); // Crimson minute hand
        mImg.raycastTarget = false;

        // Quick Adjust Tap Buttons (+ / -) directly under dial for effortless fine control
        CreateButton(modal.transform, "BtnHourMinus", "- 1h", new Vector2(-220, -225), new Vector2(75, 44), () => AddHours(-1));
        CreateButton(modal.transform, "BtnHourPlus", "+ 1h", new Vector2(-135, -225), new Vector2(75, 44), () => AddHours(1));

        CreateButton(modal.transform, "BtnMinMinus5", "- 5m", new Vector2(-50, -225), new Vector2(75, 44), () => AddMinutes(-5));
        CreateButton(modal.transform, "BtnMinPlus5", "+ 5m", new Vector2(35, -225), new Vector2(75, 44), () => AddMinutes(5));

        CreateButton(modal.transform, "BtnMinMinus1", "- 1m", new Vector2(120, -225), new Vector2(75, 44), () => AddMinutes(-1));
        CreateButton(modal.transform, "BtnMinPlus1", "+ 1m", new Vector2(205, -225), new Vector2(75, 44), () => AddMinutes(1));

        // Interactive Touch Guide Badge
        CreateText(modal.transform, "TouchHint", "Touch & drag hands on clock, or tap buttons above to reach 8:15", 15, new Vector2(0, -270), new Vector2(600, 26), new Color(0.95f, 0.90f, 0.60f));

        // Status Feedback (Empty initially, shows success message upon solving)
        uiStatusFeedbackTMP = CreateText(modal.transform, "Status", "", 22, new Vector2(0, -305), new Vector2(580, 32), new Color(1.0f, 0.90f, 0.50f));

        // Close Button
        CreateButton(modal.transform, "BtnClose", "CLOSE", new Vector2(0, -350), new Vector2(160, 44), CloseClockScreen, new Color(0.35f, 0.15f, 0.15f));

        clockScreenCanvas.SetActive(false);
    }

    public void AddHours(int delta)
    {
        if (isPuzzleSolved) return;
        currentHour = ((currentHour - 1 + delta) % 12);
        if (currentHour < 0) currentHour += 12;
        currentHour += 1; // 1..12

        ProceduralAudioGenerator.PlaySound("dial_tick", 0.75f);
        UpdateHandsVisuals();
        CheckSolved();
    }

    public void AddMinutes(int delta)
    {
        if (isPuzzleSolved) return;
        currentMinute = (currentMinute + delta) % 60;
        if (currentMinute < 0) currentMinute += 60;

        ProceduralAudioGenerator.PlaySound("dial_tick", 0.6f);
        UpdateHandsVisuals();
        CheckSolved();
    }

    // ---------------- Floating Toast Notification Banner ----------------
    private void BuildFloatingBannerUI()
    {
        if (notificationBannerObj != null) return;

        GameObject bannerCanvasObj = new GameObject("GrandfatherClock_ToastCanvas");
        Canvas c = bannerCanvasObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 99;

        CanvasScaler scaler = bannerCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        notificationBannerObj = new GameObject("ToastPanel");
        notificationBannerObj.transform.SetParent(bannerCanvasObj.transform, false);

        Image img = notificationBannerObj.AddComponent<Image>();
        img.color = new Color(0.08f, 0.10f, 0.16f, 0.95f);

        Outline ol = notificationBannerObj.AddComponent<Outline>();
        ol.effectColor = new Color(0.85f, 0.70f, 0.25f, 0.9f);
        ol.effectDistance = new Vector2(2f, -2f);

        RectTransform rt = notificationBannerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.85f);
        rt.anchorMax = new Vector2(0.5f, 0.85f);
        rt.sizeDelta = new Vector2(760, 68);
        rt.anchoredPosition = Vector2.zero;

        notificationTextTMP = CreateText(notificationBannerObj.transform, "MessageText", "", 20, Vector2.zero, new Vector2(720, 60), Color.white);
        notificationBannerObj.SetActive(false);
    }

    public static void ShowOnScreenBanner(string message, Color? highlightColor = null)
    {
        if (Instance == null) return;

        if (notificationBannerObj == null || notificationTextTMP == null)
        {
            Instance.BuildFloatingBannerUI();
        }

        if (notificationBannerObj != null && notificationTextTMP != null)
        {
            notificationTextTMP.text = message;
            notificationTextTMP.color = highlightColor ?? Color.white;
            notificationBannerObj.SetActive(true);

            if (notificationCoroutine != null)
            {
                Instance.StopCoroutine(notificationCoroutine);
            }
            notificationCoroutine = Instance.StartCoroutine(Instance.HideBannerRoutine());
        }
    }

    private IEnumerator HideBannerRoutine()
    {
        yield return new WaitForSeconds(3.5f);
        if (notificationBannerObj != null)
        {
            notificationBannerObj.SetActive(false);
        }
        notificationCoroutine = null;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string content, int fontSize, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return tmp;
    }

    private void CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, Color? color = null)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = color ?? new Color(0.18f, 0.22f, 0.32f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Outline ol = btnObj.AddComponent<Outline>();
        ol.effectColor = new Color(0.8f, 0.7f, 0.3f, 0.6f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        CreateText(btnObj.transform, "Label", label, 18, Vector2.zero, size, Color.white);
    }

    private void OnMouseDown()
    {
        if (!isScreenOpen) InteractWithClock();
    }

    private void Update()
    {
        // 1. If 2D clock screen is open: Handle direct touch / drag on the clock dial!
        if (isScreenOpen)
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleDialTouchDrag(Input.mousePosition, true);
            }
            else if (Input.GetMouseButton(0) && (isDraggingHourHand || isDraggingMinuteHand))
            {
                HandleDialTouchDrag(Input.mousePosition, false);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingHourHand = false;
                isDraggingMinuteHand = false;
            }

            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    HandleDialTouchDrag(t.position, true);
                }
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    if (isDraggingHourHand || isDraggingMinuteHand)
                        HandleDialTouchDrag(t.position, false);
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    isDraggingHourHand = false;
                    isDraggingMinuteHand = false;
                }
            }
            return;
        }

        // 2. If 2D screen is closed: Detect click in 3D scene to open clock
        if (Input.GetMouseButtonDown(0))
        {
            CheckTouchRay(Input.mousePosition);
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            CheckTouchRay(Input.GetTouch(0).position);
        }
    }

    private bool IsPointerOverInteractiveUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject == null) continue;

            if (r.gameObject.GetComponentInParent<Button>() != null ||
                r.gameObject.GetComponentInParent<VirtualJoystick>() != null ||
                r.gameObject.GetComponentInParent<Slider>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private void CheckTouchRay(Vector2 screenPos)
    {
        if (IsPointerOverInteractiveUI(screenPos)) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 35f);
        foreach (var hit in hits)
        {
            if (hit.transform == null) continue;

            bool isClock = hit.transform == transform ||
                           hit.transform.IsChildOf(transform) ||
                           hit.transform.name.ToLowerInvariant().Contains("grandfather_clock") ||
                           hit.transform.name.ToLowerInvariant().Contains("clockface") ||
                           hit.transform.name.ToLowerInvariant().Contains("clockcase") ||
                           hit.transform.name.ToLowerInvariant().Contains("clock");

            if (isClock)
            {
                InteractWithClock();
                break;
            }
        }
    }
}
