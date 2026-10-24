using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Room 3 Door Exit Controller:
/// 1. Self-contained specifically for Level 3 / Room 3.
/// 2. Requires selecting the Room3Key from the inventory bag before clicking the door.
/// 3. Upon unlocking: plays celebration sound, congratulation banner ("★ LEVEL 3 COMPLETED! ★"),
///    brightens the room lighting, launches fireworks, marks Room3_Done and Level3_Done,
///    and returns cleanly to the Levels Menu with Level 3 marked COMPLETED and replayable!
/// </summary>
public class Room3DoorExit : MonoBehaviour
{
    public static Room3DoorExit Instance { get; private set; }

    [Header("Key Requirement")]
    [SerializeField] private string requiredItemID = "Room3Key";

    [Header("Transition")]
    [SerializeField] private string menuSceneName = "MainMenu";

    private Camera mainCam;
    private bool isOpened = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitInRoom3()
    {
        if (SceneManager.GetActiveScene().name != "Room3") return;

        if (FindAnyObjectByType<Room3DoorExit>() == null)
        {
            GameObject doorObj = FindDoorObject();
            if (doorObj != null)
            {
                doorObj.AddComponent<Room3DoorExit>();
            }
            else
            {
                GameObject mgr = new GameObject("Room3_DoorExit_Manager");
                mgr.AddComponent<Room3DoorExit>();
            }
        }
    }

    private static GameObject FindDoorObject()
    {
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in all)
        {
            string n = go.name.ToLowerInvariant();
            if (n.Contains("door") || n.Contains("exitdoor") || n.Contains("gate"))
            {
                return go;
            }
        }
        return null;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        mainCam = Camera.main;
    }

    private void Start()
    {
        isOpened = PlayerPrefs.GetInt("Room3_Done", 0) == 1
                   || PlayerPrefs.GetInt("Level3_Done", 0) == 1
                   || PlayerPrefs.GetInt("Room3_Task_door", 0) == 1;

        EnsureDoorCollider();
    }

    private void EnsureDoorCollider()
    {
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(1.5f, 2.5f, 0.5f);
            col.center = new Vector3(0f, 1.25f, 0f);
        }
    }

    private void OnMouseDown()
    {
        TryOpenDoor();
    }

    private void Update()
    {
        if (isOpened) return;

        if (Input.GetMouseButtonDown(0))
        {
            CheckTouch(Input.mousePosition);
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            CheckTouch(Input.GetTouch(0).position);
        }
    }

    private void CheckTouch(Vector2 screenPos)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 35f);
        foreach (var hit in hits)
        {
            if (hit.transform == null) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform) ||
                hit.transform.name.ToLowerInvariant().Contains("door"))
            {
                TryOpenDoor();
                break;
            }
        }
    }

    public void TryOpenDoor()
    {
        if (isOpened) return;

        // 1. Check if key was collected
        bool hasKeyInInventory = (InventoryManager.Instance != null && (InventoryManager.Instance.HasItem(requiredItemID) || InventoryManager.Instance.HasItem("key")))
                                 || PlayerPrefs.GetInt("Room3_Task_key", 0) == 1;

        if (!hasKeyInInventory)
        {
            Debug.Log("<color=yellow>[Room3 Door]:</color> Door is locked! Find and take the golden key first.");
            ProceduralAudioGenerator.PlaySound("door_locked", 0.8f);
            return;
        }

        // 2. Check if the player clicked on the key in the inventory bag (Selected Item)
        bool isKeySelected = false;
        if (InventoryManager.Instance != null)
        {
            string sel = InventoryManager.Instance.SelectedItem;
            if (!string.IsNullOrEmpty(sel) &&
                (sel.Equals(requiredItemID, System.StringComparison.OrdinalIgnoreCase) ||
                 sel.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                isKeySelected = true;
            }
        }

        if (!isKeySelected)
        {
            Debug.Log("<color=yellow>[Room3 Door]:</color> Click the key from your inventory bag first, then touch the door!");
            ProceduralAudioGenerator.PlaySound("door_locked", 0.8f);
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ShowInventoryTemporarily();
            }
            return;
        }

        // 3. Success: Unlock door & celebrate!
        isOpened = true;
        Debug.Log("<color=green>[Room3 Door]:</color> Door unlocked successfully with the selected key!");

        ProceduralAudioGenerator.PlaySound("door_open", 0.95f);
        ProceduralAudioGenerator.PlaySound("level_complete", 1.0f);

        // Mark Room 3 done
        LevelProgressGuide.ReportTask("door");
        PlayerPrefs.SetInt("Room3_DoorOpened", 1);
        PlayerPrefs.SetInt("Room3_Done", 1);
        PlayerPrefs.SetInt("Level3_Done", 1);
        PlayerPrefs.SetInt("OpenLevelsDirectly", 1);
        PlayerPrefs.Save();

        // Remove key from bag
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(requiredItemID);
            InventoryManager.Instance.RemoveItem("key");
            InventoryManager.Instance.ClearSelection();
        }

        // Brighten room completely & launch fireworks
        BrightenHouseCompletely();
        StartCoroutine(LaunchFireworksCelebration());
        ShowLevelCompleteBanner();

        StartCoroutine(WaitAndProceedToMenu());
    }

    private void BrightenHouseCompletely()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientIntensity = 1.50f;
        RenderSettings.ambientLight = new Color(1f, 0.98f, 0.94f);

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.intensity = 1.50f;
                l.color = new Color(1f, 0.96f, 0.90f);
            }
            else if (l.type == LightType.Point)
            {
                l.intensity = 1.8f;
                l.range = Mathf.Max(l.range, 12f);
            }
        }

        if (RoomDustAtmosphere.Instance != null)
        {
            RoomDustAtmosphere.Instance.ApplyProgressiveRoomLighting();
        }
    }

    private IEnumerator LaunchFireworksCelebration()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            for (int f = 0; f < 6; f++)
            {
                ProceduralAudioGenerator.PlaySound("firework_pop", 0.9f);
                SpawnConfettiFireworkBurst(canvas.transform);
                yield return new WaitForSeconds(0.35f);
            }
        }
    }

    private void SpawnConfettiFireworkBurst(Transform parent)
    {
        GameObject burst = new GameObject("FireworkBurst", typeof(RectTransform));
        burst.transform.SetParent(parent, false);

        RectTransform bRt = burst.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.5f, 0.5f);
        bRt.anchorMax = new Vector2(0.5f, 0.5f);
        bRt.anchoredPosition = new Vector2(Random.Range(-350f, 350f), Random.Range(-100f, 320f));

        Color[] colors = new Color[] {
            new Color(1f, 0.84f, 0f),      // Gold
            new Color(0.2f, 0.85f, 0.4f),  // Emerald
            new Color(1f, 0.25f, 0.45f),   // Crimson
            new Color(0.3f, 0.75f, 1f),    // Cyan
            new Color(1f, 0.55f, 0.1f)     // Orange
        };

        for (int i = 0; i < 18; i++)
        {
            GameObject spark = new GameObject("Spark", typeof(RectTransform));
            spark.transform.SetParent(burst.transform, false);

            RectTransform sRt = spark.GetComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(Random.Range(10f, 22f), Random.Range(10f, 22f));

            Image img = spark.AddComponent<Image>();
            img.color = colors[Random.Range(0, colors.Length)];

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(80f, 220f);
            Vector2 target = new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);

            StartCoroutine(AnimateSpark(sRt, img, target));
        }

        Destroy(burst, 1.8f);
    }

    private IEnumerator AnimateSpark(RectTransform rt, Image img, Vector2 targetPos)
    {
        float elapsed = 0f;
        float duration = 1.2f;
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.Sin(t * Mathf.PI * 0.5f));
                rt.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.zero, t * t);
            }
            if (img != null)
            {
                Color c = img.color;
                c.a = 1f - (t * t);
                img.color = c;
            }
            yield return null;
        }
    }

    private void ShowLevelCompleteBanner()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject banner = new GameObject("LevelCompleteBanner", typeof(RectTransform));
        banner.transform.SetParent(canvas.transform, false);
        Destroy(banner, 5f);

        RectTransform rt = banner.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(620f, 170f);

        Image img = banner.AddComponent<Image>();
        img.color = new Color(0.04f, 0.07f, 0.14f, 0.94f);

        Outline outline = banner.AddComponent<Outline>();
        outline.effectColor = new Color(0.98f, 0.82f, 0.25f, 0.98f);
        outline.effectDistance = new Vector2(3f, -3f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(banner.transform, false);
        RectTransform tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "★ LEVEL 3 COMPLETED! ★\n<size=24><color=#4ADE80>CONGRATULATIONS!</color></size>";
        tmp.fontSize = 32;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.98f, 0.88f, 0.40f, 1f);
    }

    private IEnumerator WaitAndProceedToMenu()
    {
        yield return new WaitForSeconds(2.0f);
        SceneManager.LoadScene(menuSceneName);
    }
}
