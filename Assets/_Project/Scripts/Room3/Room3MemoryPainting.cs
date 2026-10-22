using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Memory Painting (لوحة الذكريات) - Room 3
/// Clicking directly on the memory painting while close to it triggers the video "Untitled design.mp4".
/// Requires the player to be within maxInteractDistance (3.5 meters).
/// </summary>
public class Room3MemoryPainting : MonoBehaviour
{
    public static Room3MemoryPainting Instance { get; private set; }

    [Header("Secret Time Clue")]
    public int revealedHour = 8;
    public int revealedMinute = 15;
    public string revealedTimeString = "8:15";

    [Header("Video Settings")]
    public string videoFileName = "Untitled design.mp4";

    [Header("Interaction Distance Requirement")]
    [Tooltip("Maximum distance in meters the player can be from the painting to inspect it")]
    public float maxInteractDistance = 3.5f;

    [Header("Specific Painting Target Collider")]
    public Collider targetCollider;

    private Camera mainCam;
    private bool isInspected = false;
    public bool IsInspected => isInspected;
    private bool isVideoPlaying = false;
    private readonly List<Collider> registeredPaintingColliders = new List<Collider>();

    // Tap tracking to distinguish clean clicks from dragging camera/joystick
    private Vector2 touchDownPos;
    private float touchDownTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitInRoom3()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Room3")
        {
            if (FindAnyObjectByType<Room3MemoryPainting>() == null)
            {
                GameObject go = new GameObject("Room3_MemoryPainting_Manager");
                go.AddComponent<Room3MemoryPainting>();
            }
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        mainCam = Camera.main;

        // Clean up any old prompt remnants
        GameObject oldPrompt = GameObject.Find("MemoryPainting_Prompt");
        if (oldPrompt != null) Destroy(oldPrompt);
    }

    private void Start()
    {
        GameObject oldPrompt = GameObject.Find("MemoryPainting_Prompt");
        if (oldPrompt != null) Destroy(oldPrompt);

        SetupPaintingCollidersAndTriggers();

        if (PlayerPrefs.GetInt("Room3_Task_painting", 0) == 1)
        {
            isInspected = true;
        }
    }

    /// <summary>
    /// Scans the scene STRICTLY for the painting objects (SM_Tablo / M_Tablo_P1).
    /// Explicitly excludes wall mouldings (SM_Duvar_Cercevesi) and other room elements.
    /// </summary>
    public void SetupPaintingCollidersAndTriggers()
    {
        registeredPaintingColliders.Clear();

        if (targetCollider != null && !registeredPaintingColliders.Contains(targetCollider))
        {
            registeredPaintingColliders.Add(targetCollider);
        }

        Renderer[] allRenderers = FindObjectsByType<Renderer>();
        foreach (Renderer rend in allRenderers)
        {
            if (rend == null) continue;

            string objName = rend.gameObject.name.ToLower();

            // Strictly exclude walls, floors, ceilings, mouldings, furniture
            if (objName.Contains("duvar") || objName.Contains("wall") ||
                objName.Contains("zemin") || objName.Contains("floor") ||
                objName.Contains("tavan") || objName.Contains("ceiling") ||
                objName.Contains("cercevesi")) // SM_Duvar_Cercevesi is wall moulding, not a painting!
            {
                continue;
            }

            bool isPainting = false;

            // 1. Check parent chain strictly for "sm_tablo" or "painting"
            Transform curr = rend.transform;
            while (curr != null)
            {
                string n = curr.name.ToLower();
                if (n.Contains("duvar") || n.Contains("cercevesi"))
                {
                    isPainting = false;
                    break;
                }
                if (n.Contains("sm_tablo") || n.Contains("painting0") || n.Contains("memorypainting"))
                {
                    isPainting = true;
                    break;
                }
                curr = curr.parent;
            }

            // 2. Check material specifically for M_Tablo_P1
            if (!isPainting && rend.sharedMaterials != null)
            {
                foreach (Material mat in rend.sharedMaterials)
                {
                    if (mat == null) continue;
                    string mName = mat.name.ToLower();
                    if (mName.Contains("m_tablo") || mName.Contains("tablo_p1"))
                    {
                        isPainting = true;
                        break;
                    }
                }
            }

            if (isPainting)
            {
                Collider col = rend.GetComponent<Collider>();
                if (col == null)
                {
                    MeshFilter mf = rend.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        MeshCollider mc = rend.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        col = mc;
                    }
                    else
                    {
                        BoxCollider bc = rend.gameObject.AddComponent<BoxCollider>();
                        col = bc;
                    }
                }

                if (col != null && !registeredPaintingColliders.Contains(col))
                {
                    registeredPaintingColliders.Add(col);
                }

                if (targetCollider == null)
                {
                    targetCollider = col;
                    transform.position = rend.transform.position;
                }

                var trigger = rend.GetComponent<PaintingClickTrigger>();
                if (trigger == null) trigger = rend.gameObject.AddComponent<PaintingClickTrigger>();
                trigger.parentPainting = this;

                Debug.Log("<color=cyan>[MemoryPainting]:</color> Registered exact painting collider: " + rend.gameObject.name + " at " + rend.transform.position);
            }
        }
    }

    private void Update()
    {
        if (isVideoPlaying) return;

        // 1. Mouse click detection (Editor / PC)
        if (Input.GetMouseButtonDown(0))
        {
            touchDownPos = Input.mousePosition;
            touchDownTime = Time.time;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Only trigger on short, stationary clicks (not camera dragging)
            float duration = Time.time - touchDownTime;
            float distance = Vector2.Distance(touchDownPos, (Vector2)Input.mousePosition);
            if (duration < 0.4f && distance < 20f)
            {
                TryClickPainting(Input.mousePosition);
            }
        }

        // 2. Mobile touch tap detection
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                touchDownPos = t.position;
                touchDownTime = Time.time;
            }
            else if (t.phase == TouchPhase.Ended)
            {
                float duration = Time.time - touchDownTime;
                float distance = Vector2.Distance(touchDownPos, t.position);
                if (duration < 0.4f && distance < 25f)
                {
                    TryClickPainting(t.position);
                }
            }
        }
    }

    private void TryClickPainting(Vector2 screenPos)
    {
        if (IsPointerOverInteractiveUI(screenPos)) return;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);

        // Raycast only up to a reasonable forward distance (max 15m)
        if (Physics.Raycast(ray, out RaycastHit hit, 15f))
        {
            if (hit.collider == null) return;

            bool isHitPainting = false;

            // Check if hit collider is one of the verified painting colliders
            if (registeredPaintingColliders.Contains(hit.collider) || hit.collider == targetCollider)
            {
                isHitPainting = true;
            }
            else
            {
                PaintingClickTrigger trigger = hit.collider.GetComponentInParent<PaintingClickTrigger>();
                if (trigger != null)
                {
                    isHitPainting = true;
                }
                else
                {
                    string hName = hit.collider.name.ToLower();
                    if ((hName.Contains("sm_tablo") || hName.Contains("painting")) && !hName.Contains("duvar") && !hName.Contains("cercevesi"))
                    {
                        isHitPainting = true;
                    }
                }
            }

            if (isHitPainting)
            {
                // CRITICAL REQUIREMENT: Check player distance!
                // Player must be physically close to the painting to inspect it
                Vector3 playerPos = mainCam.transform.position;
                float distToPainting = Vector3.Distance(playerPos, hit.point);

                if (distToPainting > maxInteractDistance)
                {
                    Debug.Log($"<color=yellow>[MemoryPainting]:</color> Too far from painting ({distToPainting:F1}m > {maxInteractDistance}m). Walk closer first!");
                    return;
                }

                Debug.Log("<color=green>[MemoryPainting]:</color> Successfully clicked painting within range! Distance: " + distToPainting.ToString("F1") + "m");
                InspectPainting();
            }
        }
    }

    private bool IsPointerOverInteractiveUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
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

    /// <summary>
    /// Called when the player clicks the painting within the required interaction distance
    /// </summary>
    public void InspectPainting()
    {
        if (isVideoPlaying) return;

        // Double check distance from player position to painting
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null && targetCollider != null)
        {
            float d = Vector3.Distance(mainCam.transform.position, targetCollider.bounds.center);
            if (d > maxInteractDistance + 1f)
            {
                Debug.Log($"<color=yellow>[MemoryPainting]:</color> Player is too far ({d:F1}m). Move closer to inspect!");
                return;
            }
        }

        Debug.Log("<color=yellow>[MemoryPainting]:</color> Starting memory video playback!");
        isVideoPlaying = true;

        ProceduralAudioGenerator.PlaySound("ui_click", 0.7f);

        if (UniversalVideoPlayer.Instance == null)
        {
            UniversalVideoPlayer uvp = FindAnyObjectByType<UniversalVideoPlayer>();
            if (uvp == null)
            {
                GameObject vObj = new GameObject("UniversalVideoPlayer");
                uvp = vObj.AddComponent<UniversalVideoPlayer>();
            }
        }

        if (UniversalVideoPlayer.Instance != null)
        {
            UniversalVideoPlayer.Instance.PlayMemoryPaintingVideo(OnVideoCompleted);
        }
        else
        {
            StartCoroutine(FallbackDirectVideoRoutine());
        }
    }

    private IEnumerator FallbackDirectVideoRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        OnVideoCompleted();
    }

    private void OnVideoCompleted()
    {
        isVideoPlaying = false;
        isInspected = true;

        Debug.Log("<color=green>[MemoryPainting]:</color> Video completed, revealed time: " + revealedTimeString);

        ProceduralAudioGenerator.PlaySound("level_complete", 0.75f);

        // Add clue item to inventory with revealed time icon (8:15)
        if (InventoryManager.Instance != null)
        {
            Sprite paintingIcon = InventoryManager.GetMemoryPaintingSprite();
            bool added = InventoryManager.Instance.AddItem("MemoryPainting", paintingIcon, true);
            if (added)
            {
                InventoryManager.Instance.ShowInventoryTemporarily();
            }
        }

        // Report task completion
        LevelProgressGuide.ReportTask("painting");
        PlayerPrefs.SetInt("Room3_Task_painting", 1);
        PlayerPrefs.SetInt("HasItem_MemoryPainting", 1);
        PlayerPrefs.Save();

        // Show English banner on screen
        ShowDiscoveredTimeBanner();
    }

    private void ShowDiscoveredTimeBanner()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject banner = new GameObject("MemoryTimeDiscoveredBanner", typeof(RectTransform));
        banner.transform.SetParent(canvas.transform, false);

        RectTransform rt = banner.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 50f);
        rt.sizeDelta = new Vector2(580f, 200f);

        Image bg = banner.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.15f, 0.96f);

        Outline outline = banner.AddComponent<Outline>();
        outline.effectColor = new Color(0.96f, 0.82f, 0.35f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(banner.transform, false);
        RectTransform tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(20, 20);
        tRt.offsetMax = new Vector2(-20, -20);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "<b><color=#FFD166>★ MEMORY PAINTING REVEALED! ★</color></b>\n" +
                   $"<size=38><color=#38BDF8><b>TIME: {revealedTimeString}</b></color></size>\n" +
                   "<size=16><color=#CBD5E1>Clue added to your Bag. Align the Grandfather Clock hands to this exact time!</color></size>";
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        Destroy(banner, 5.0f);
    }
}

public class PaintingClickTrigger : MonoBehaviour
{
    public Room3MemoryPainting parentPainting;

    private void OnMouseDown()
    {
        if (parentPainting != null)
        {
            parentPainting.InspectPainting();
        }
        else if (Room3MemoryPainting.Instance != null)
        {
            Room3MemoryPainting.Instance.InspectPainting();
        }
    }
}
