using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Room 3 Key Pickup:
/// 1. Spawns golden key inside/at the cabinet upon clock alignment completion.
/// 2. Clicking the key plays pickup sound, stores it in InventoryManager, and reports task "key".
/// 3. The Fairy companion automatically targets this key next!
/// </summary>
public class Room3KeyPickup : MonoBehaviour
{
    public static Room3KeyPickup Instance { get; private set; }

    [Header("Item Config")]
    public string itemID = "GoldenKey";
    public Vector3 spawnPosition = new Vector3(-0.5f, 1.15f, 3.2f);

    private bool isCollected = false;
    private GameObject keyVisualObj;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitInRoom3()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Room3") return;

        if (FindAnyObjectByType<Room3KeyPickup>() == null)
        {
            GameObject go = new GameObject("Room3_KeyPickup_Manager");
            go.AddComponent<Room3KeyPickup>();
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        bool doorDone = PlayerPrefs.GetInt("Room3_Done", 0) == 1
                        || PlayerPrefs.GetInt("Level3_Done", 0) == 1
                        || PlayerPrefs.GetInt("Room3_Task_door", 0) == 1;

        bool keyDone = PlayerPrefs.GetInt("Room3_Task_key", 0) == 1
                       || PlayerPrefs.GetInt("HasItem_GoldenKey", 0) == 1
                       || PlayerPrefs.GetInt("HasItem_Room3Key", 0) == 1;

        bool clockDone = PlayerPrefs.GetInt("Room3_ClockSolved", 0) == 1
                         || PlayerPrefs.GetInt("Room3_Task_clock_hand", 0) == 1;

        if (keyDone)
        {
            isCollected = true;
            // If key collected but door not unlocked yet, ensure it is in the inventory bag (only one key!)
            if (!doorDone && InventoryManager.Instance != null && !InventoryManager.Instance.HasItem(itemID) && !InventoryManager.Instance.HasItem("key"))
            {
                InventoryManager.Instance.AddItem(itemID, InventoryManager.GetKeySprite(), false);
            }
        }
        else if (clockDone)
        {
            // Clock was already solved previously in this session
            SpawnKeyVisual();
        }
    }

    public void OnClockSolvedAndDisappeared()
    {
        if (isCollected) return;
        SpawnKeyVisual();
    }

    private void SpawnKeyVisual()
    {
        if (keyVisualObj != null || isCollected) return;

        // Find Grandfather Clock position to spawn key exactly where the clock was
        Vector3 pos = spawnPosition;
        GameObject clock = GameObject.Find("grandfather_clock") ?? GameObject.Find("GrandfatherClock") ?? GameObject.Find("Clock") ?? GameObject.Find("ClockPuzzle");
        if (clock == null && GrandfatherClockPuzzle.Instance != null)
        {
            clock = GrandfatherClockPuzzle.Instance.gameObject;
        }

        if (clock != null)
        {
            pos = clock.transform.position + Vector3.up * 1.1f;
        }
        else
        {
            GameObject cabinet = GameObject.Find("capinat") ?? GameObject.Find("Cabinet") ?? GameObject.Find("Cupboard");
            if (cabinet != null) pos = cabinet.transform.position + Vector3.up * 1.0f;
        }

        spawnPosition = pos;

        // Try finding existing key in scene or cabinet
        GameObject existing = GameObject.Find("key") ?? GameObject.Find("ExitKey") ?? GameObject.Find("ChestKey") ?? GameObject.Find("GoldenKey");
        if (existing != null && existing.transform.parent != null && !existing.name.Contains("Door"))
        {
            keyVisualObj = existing;
            keyVisualObj.transform.position = pos;
            keyVisualObj.SetActive(true);
            EnsureColliderAndScript(keyVisualObj);
            return;
        }

        keyVisualObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        keyVisualObj.name = "Room3_GoldenKey";
        keyVisualObj.transform.position = pos;
        keyVisualObj.transform.localScale = new Vector3(0.12f, 0.03f, 0.35f);
        keyVisualObj.transform.rotation = Quaternion.Euler(0f, 45f, 90f);

        Renderer r = keyVisualObj.GetComponent<Renderer>();
        if (r != null)
        {
            Material m = new Material(Shader.Find("Standard"));
            m.color = new Color(1.0f, 0.85f, 0.20f, 1f); // Shiny gold
            m.SetFloat("_Metallic", 0.9f);
            m.SetFloat("_Glossiness", 0.85f);
            r.material = m;
        }

        // Add small decorative teeth to look unmistakably like a classic key
        GameObject bit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bit.name = "KeyTeeth";
        bit.transform.SetParent(keyVisualObj.transform, false);
        bit.transform.localPosition = new Vector3(0.5f, 0f, 0.3f);
        bit.transform.localScale = new Vector3(0.6f, 0.8f, 0.4f);
        if (r != null && bit.GetComponent<Renderer>() != null)
        {
            bit.GetComponent<Renderer>().material = r.material;
        }

        EnsureColliderAndScript(keyVisualObj);
    }

    private void EnsureColliderAndScript(GameObject go)
    {
        if (go.GetComponent<Collider>() == null)
        {
            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.6f, 0.6f, 0.6f);
        }
    }

    private void Update()
    {
        if (isCollected || keyVisualObj == null) return;

        // Subtle floating / spinning animation for the key so the player notices it easily
        keyVisualObj.transform.Rotate(Vector3.up * (45f * Time.deltaTime), Space.World);

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
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 35f);
        foreach (var hit in hits)
        {
            if (hit.transform == null) continue;
            if (hit.transform == keyVisualObj.transform || hit.transform.IsChildOf(keyVisualObj.transform) ||
                hit.transform.name.ToLowerInvariant().Contains("key"))
            {
                PickupKey();
                break;
            }
        }
    }

    public void PickupKey()
    {
        if (isCollected) return;

        isCollected = true;
        PlayerPrefs.SetInt("Room3_Task_key", 1);
        PlayerPrefs.SetInt("HasItem_GoldenKey", 1);
        PlayerPrefs.SetInt("HasItem_Room3Key", 1);
        PlayerPrefs.Save();

        // 1. Add key to inventory bag (only if no key already present)
        if (InventoryManager.Instance != null && !InventoryManager.Instance.HasItem(itemID) && !InventoryManager.Instance.HasItem("key"))
        {
            InventoryManager.Instance.AddItem(itemID, InventoryManager.GetKeySprite(), true);
            InventoryManager.Instance.ShowInventoryTemporarily();
            Debug.Log("<color=green>[Room3Key]:</color> Golden key added to inventory bag!");
        }

        // 2. Play sound
        ProceduralAudioGenerator.PlaySound("pickup", 0.95f);

        // 3. Hide key from room
        if (keyVisualObj != null)
        {
            keyVisualObj.SetActive(false);
        }

        // 4. Report task to advance guide and guide the Fairy companion toward the exit door!
        LevelProgressGuide.ReportTask("key");
    }

    public Vector3 KeyTargetPosition
    {
        get
        {
            if (keyVisualObj != null && keyVisualObj.activeInHierarchy)
                return keyVisualObj.transform.position + Vector3.up * 0.25f;
            return spawnPosition + Vector3.up * 0.25f;
        }
    }
}
