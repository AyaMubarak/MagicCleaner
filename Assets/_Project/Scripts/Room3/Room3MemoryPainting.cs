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
    public Vector3 PaintingTargetPosition => targetCollider != null ? targetCollider.bounds.center : transform.position;
    private bool isVideoPlaying = false;
    private readonly List<Collider> registeredPaintingColliders = new List<Collider>();
    private readonly List<Renderer> paintingRenderers = new List<Renderer>();

    [Header("Swipe Interaction")]
    public float minimumSwipeDistance = 45f;
    public int swipesRequiredToClean = 1;

    // Tap/swipe tracking to distinguish painting interaction from camera movement
    private Vector2 touchDownPos;
    private float touchDownTime;
    private bool swipeStartedOnPainting;
    private int completedSwipes;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadHandler()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name == "Room3")
            {
                if (FindAnyObjectByType<Room3MemoryPainting>() == null)
                {

                    
                    GameObject go = new GameObject("Room3_MemoryPainting_Manager");
                    go.AddComponent<Room3MemoryPainting>();
                }
            }
        };
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
            RevealPainting();
        }
    }

    /// <summary>
    /// Scans the scene STRICTLY for the painting objects (SM_Tablo / M_Tablo_P1).
    /// Explicitly excludes wall mouldings (SM_Duvar_Cercevesi) and other room elements.
    /// </summary>
    public void SetupPaintingCollidersAndTriggers()
    {
        registeredPaintingColliders.Clear();
        paintingRenderers.Clear();

        Renderer preferredPainting = null;
        Renderer[] sceneRenderers = FindObjectsByType<Renderer>();
        foreach (Renderer candidate in sceneRenderers)
        {
            if (candidate == null) continue;
            Transform parent = candidate.transform;
            while (parent != null)
            {
                string candidateName = parent.name.ToLowerInvariant();
                if (candidateName.Contains("secret art") || candidateName.Contains("secretart"))
                {
                    preferredPainting = candidate;
                    break;
                }
                parent = parent.parent;
            }
            if (preferredPainting != null) break;
        }

        if (preferredPainting != null)
        {
            Collider preferredCollider = preferredPainting.GetComponent<Collider>();
            if (preferredCollider != null)
            {
                targetCollider = preferredCollider;
            }
        }

        if (targetCollider != null && !registeredPaintingColliders.Contains(targetCollider))
        {
            registeredPaintingColliders.Add(targetCollider);
        }

        Renderer[] allRenderers = sceneRenderers;
        foreach (Renderer rend in allRenderers)
        {
            if (rend == null) continue;

            if (preferredPainting != null && rend != preferredPainting) continue;

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
                if (n.Contains("sm_tablo") || n.Contains("painting0") || n.Contains("memorypainting") ||
                    n.Contains("secret art") || n.Contains("secretart"))
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
                    if (mName.Contains("m_tablo") || mName.Contains("tablo_p1") || mName.Contains("secret art") || mName.Contains("secretart"))
                    {
                        isPainting = true;
                        break;
                    }
                }
            }

            if (isPainting)
            {
                if (!paintingRenderers.Contains(rend))
                {
                    paintingRenderers.Add(rend);
                }

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
            swipeStartedOnPainting = IsPaintingAtScreenPosition(touchDownPos);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            float duration = Time.time - touchDownTime;
            float distance = Vector2.Distance(touchDownPos, (Vector2)Input.mousePosition);
            if (swipeStartedOnPainting && distance >= minimumSwipeDistance)
            {
                TrySwipePainting(Input.mousePosition);
            }
            else if ((swipeStartedOnPainting || IsPaintingAtScreenPosition(Input.mousePosition)) && duration < 0.5f && distance < 40f)
            {
                TryClickPainting(Input.mousePosition);
            }
            swipeStartedOnPainting = false;
        }

        // 2. Mobile touch tap detection
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                touchDownPos = t.position;
                touchDownTime = Time.time;
                swipeStartedOnPainting = IsPaintingAtScreenPosition(t.position);
            }
            else if (t.phase == TouchPhase.Ended)
            {
                float duration = Time.time - touchDownTime;
                float distance = Vector2.Distance(touchDownPos, t.position);
                if (swipeStartedOnPainting && distance >= minimumSwipeDistance)
                {
                    TrySwipePainting(t.position);
                }
                else if ((swipeStartedOnPainting || IsPaintingAtScreenPosition(t.position)) && distance < 45f)
                {
                    TryClickPainting(t.position);
                }
                swipeStartedOnPainting = false;
            }
        }
    }

    private bool IsPaintingAtScreenPosition(Vector2 screenPos)
    {
        if (IsPointerOverInteractiveUI(screenPos)) return false;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return false;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 30f);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (registeredPaintingColliders.Contains(hit.collider) ||
                hit.collider == targetCollider ||
                hit.collider.GetComponentInParent<PaintingClickTrigger>() != null)
            {
                return true;
            }

            string hName = hit.collider.name.ToLower();
            if ((hName.Contains("sm_tablo") || hName.Contains("painting") || hName.Contains("secret art") || hName.Contains("secretart")) &&
                !hName.Contains("duvar") && !hName.Contains("cercevesi"))
            {
                return true;
            }
        }
        return false;
    }

    private void TrySwipePainting(Vector2 screenPos)
    {
        completedSwipes++;
        Debug.Log($"<color=yellow>[MemoryPainting]:</color> Dust swipe {completedSwipes}/{swipesRequiredToClean}");

        if (completedSwipes >= swipesRequiredToClean)
        {
            Debug.Log("<color=green>[MemoryPainting]:</color> Dust cleared; revealing the secret art.");
            InspectPainting();
        }
    }

    private void TryClickPainting(Vector2 screenPos)
    {
        if (IsPointerOverInteractiveUI(screenPos)) return;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);

        RaycastHit[] hits = Physics.RaycastAll(ray, 30f);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            bool isHitPainting = false;

            if (registeredPaintingColliders.Contains(hit.collider) || hit.collider == targetCollider)
            {
                isHitPainting = true;
            }
            else if (hit.collider.GetComponentInParent<PaintingClickTrigger>() != null)
            {
                isHitPainting = true;
            }
            else
            {
                string hName = hit.collider.name.ToLower();
                if ((hName.Contains("sm_tablo") || hName.Contains("painting") || hName.Contains("secret art") || hName.Contains("secretart")) &&
                    !hName.Contains("duvar") && !hName.Contains("cercevesi"))
                {
                    isHitPainting = true;
                }
            }

            if (isHitPainting)
            {
                Debug.Log("<color=green>[MemoryPainting]:</color> Successfully clicked painting! Distance: " + Vector3.Distance(mainCam.transform.position, hit.point).ToString("F1") + "m");
                InspectPainting();
                return;
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
        if (isVideoPlaying || isInspected) return;

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

        RevealPainting();

        Debug.Log("<color=green>[MemoryPainting]:</color> Video completed, revealed time: " + revealedTimeString);

        ProceduralAudioGenerator.PlaySound("level_complete", 0.75f);

        // Add clue item to inventory with revealed time icon (8:15)
        if (InventoryManager.Instance != null)
        {
            Sprite paintingIcon = InventoryManager.GetMemoryPaintingSprite();
            bool added = InventoryManager.Instance.AddItem("MemoryPainting", paintingIcon, false);
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

    }

    private void RevealPainting()
    {
        MaterialPropertyBlock clearDustBlock = new MaterialPropertyBlock();
        foreach (Renderer paintingRenderer in paintingRenderers)
        {
            if (paintingRenderer != null)
            {
                paintingRenderer.enabled = true;
                paintingRenderer.GetPropertyBlock(clearDustBlock);
                clearDustBlock.SetColor("_Color", Color.white);
                paintingRenderer.SetPropertyBlock(clearDustBlock);
            }
        }

        Debug.Log("<color=green>[MemoryPainting]:</color> Secret art revealed after video.");
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
