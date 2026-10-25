using UnityEngine;

public class Room3ClockHandPickup : MonoBehaviour
{
    private static bool cabinetOpened;
    private static bool handCollected;
    private Camera mainCamera;
    private GameObject cabinetObject;
    private GameObject handObject;
    public Vector3 CabinetTargetPosition => cabinetObject != null ? cabinetObject.transform.position : transform.position;
    public bool IsCabinetOpened => cabinetOpened;
    public bool IsHandCollected => handCollected;
    public Vector3 HourHandTargetPosition => handObject != null ? handObject.transform.position : transform.position;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadHandler()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name == "Room3")
            {
                if (FindAnyObjectByType<Room3ClockHandPickup>() == null)
                    new GameObject("Room3_ClockHandPickup").AddComponent<Room3ClockHandPickup>();
            }
        };
    }

    private void Start()
    {
        mainCamera = Camera.main;
        cabinetOpened = PlayerPrefs.GetInt("Room3_CabinetOpened", 0) == 1;
        handCollected = PlayerPrefs.GetInt("Room3_HourHandCollected", 0) == 1;

        FindObjectsAndSetup();
    }

    private void FindObjectsAndSetup()
    {
        cabinetObject = FindObject("Secret box for clock", "Secret box");
        handObject = FindObject("hour_hand_for_clock", "hour hand");

        EnsureCollider(cabinetObject);
        EnsureCollider(handObject);

        if (cabinetOpened && cabinetObject != null)
        {
            HideSecretBox(cabinetObject);
        }

        if (handCollected && handObject != null)
        {
            handObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryClick(Input.mousePosition);
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
            TryClick(Input.GetTouch(0).position);
    }

    private void TryClick(Vector2 screenPosition)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (cabinetObject == null || handObject == null)
        {
            FindObjectsAndSetup();
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 30f);
        foreach (RaycastHit hit in hits)
        {
            // 1. Click on Secret Box (or its door) to open/disappear it
            if (!cabinetOpened && IsSecretBoxHit(hit.transform))
            {
                cabinetOpened = true;
                PlayerPrefs.SetInt("Room3_CabinetOpened", 1);
                PlayerPrefs.Save();

                HideSecretBox(hit.transform.gameObject);
                if (cabinetObject != null) HideSecretBox(cabinetObject);

                // Make sure the hour hand is visible and has a collider ready to pick up
                if (handObject != null)
                {
                    handObject.SetActive(true);
                    EnsureCollider(handObject);
                }

                ProceduralAudioGenerator.PlaySound("door_open", 0.85f);
                Debug.Log("<color=green>[Room3]:</color> Secret box opened/disappeared! You can now take the hour hand.");
                return;
            }

            // 2. Click on the hour hand to pick it up into bag
            if (!handCollected && (IsHourHandHit(hit.transform) || (cabinetOpened && Matches(hit.transform, handObject))))
            {
                CollectHourHand();
                return;
            }
        }
    }

    private void HideSecretBox(GameObject obj)
    {
        Transform boxRoot = FindSecretBoxRoot(obj.transform);
        if (boxRoot != null)
        {
            // Disable mesh renderer or the object itself so it disappears cleanly
            boxRoot.gameObject.SetActive(false);
        }
        else if (obj != null)
        {
            obj.SetActive(false);
        }
    }

    private void CollectHourHand()
    {
        handCollected = true;
        PlayerPrefs.SetInt("Room3_HourHandCollected", 1);
        PlayerPrefs.SetInt("HasItem_ClockHand", 1);
        PlayerPrefs.Save();

        if (handObject != null)
            handObject.SetActive(false);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem("ClockHand", InventoryManager.GetClockTimeSprite(), true);
            InventoryManager.Instance.ShowInventoryTemporarily();
        }

        LevelProgressGuide.ReportTask("cupboard");
        ProceduralAudioGenerator.PlaySound("pickup", 0.9f);
        Debug.Log("<color=green>[Room3]:</color> Hour hand collected into inventory bag!");
    }

    private static void EnsureCollider(GameObject target)
    {
        if (target == null) return;

        Collider mainCol = target.GetComponent<Collider>();
        if (mainCol == null)
        {
            target.AddComponent<BoxCollider>();
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.GetComponent<Collider>() == null)
            {
                BoxCollider bc = renderer.gameObject.AddComponent<BoxCollider>();
                bc.isTrigger = false;
            }
        }
    }

    private static bool Matches(Transform hit, GameObject target)
    {
        return target != null && (hit == target.transform || hit.IsChildOf(target.transform));
    }

    private bool IsSecretBoxHit(Transform hit)
    {
        if (Matches(hit, cabinetObject)) return true;
        return FindSecretBoxRoot(hit) != null;
    }

    private bool IsHourHandHit(Transform hit)
    {
        if (Matches(hit, handObject)) return true;
        Transform curr = hit;
        while (curr != null)
        {
            string n = curr.name.ToLowerInvariant();
            if (n.Contains("hour_hand") || n.Contains("hour hand") || n.Contains("clock_hand"))
                return true;
            curr = curr.parent;
        }
        return false;
    }

    private static Transform FindSecretBoxRoot(Transform current)
    {
        while (current != null)
        {
            string objectName = current.name.ToLowerInvariant();
            if (objectName.Contains("secret box for clock") || objectName.Contains("secret box"))
                return current;
            current = current.parent;
        }
        return null;
    }

    private static GameObject FindObject(params string[] names)
    {
        GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject candidate in objects)
        {
            string candidateName = candidate.name.ToLowerInvariant();
            foreach (string name in names)
            {
                if (candidateName.Contains(name.ToLowerInvariant()))
                    return candidate;
            }
        }
        return null;
    }
}
