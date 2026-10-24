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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Room3") return;
        if (FindAnyObjectByType<Room3ClockHandPickup>() == null)
            new GameObject("Room3_ClockHandPickup").AddComponent<Room3ClockHandPickup>();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        cabinetObject = FindObject("Secret box for clock", "Secret box");
        handObject = FindObject("hour_hand_for_clock", "hour hand");
        EnsureCollider(cabinetObject);
        EnsureCollider(handObject);
        cabinetOpened = PlayerPrefs.GetInt("Room3_CabinetOpened", 0) == 1;
        handCollected = PlayerPrefs.GetInt("Room3_HourHandCollected", 0) == 1;
        if (cabinetOpened && cabinetObject != null)
            cabinetObject.SetActive(false);
        if (handCollected && handObject != null)
            handObject.SetActive(false);
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
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 20f);
        foreach (RaycastHit hit in hits)
        {
            if (IsSecretBoxHit(hit.transform) && !cabinetOpened)
            {
                // شرط: يجب أولاً تنظيف لوحة الذكريات وحفظها في الحقيبة
                bool isPaintingDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("painting"))
                                      || PlayerPrefs.GetInt("Room3_Task_painting", 0) == 1
                                      || (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("MemoryPainting"));

                if (!isPaintingDone)
                {
                    Debug.Log("<color=yellow>[الصندوق السري]:</color> مقفل بإحكام! افحص لوحة الذكريات ونظفها أولاً لمعرفة التوقيت.");
                    ProceduralAudioGenerator.PlaySound("door_locked", 0.7f);
                    return;
                }

                cabinetOpened = true;
                Transform boxRoot = FindSecretBoxRoot(hit.transform);
                if (boxRoot != null) boxRoot.gameObject.SetActive(false);
                PlayerPrefs.SetInt("Room3_CabinetOpened", 1);
                PlayerPrefs.Save();
                ProceduralAudioGenerator.PlaySound("door_open", 0.8f);
                return;
            }

            if (cabinetOpened && !handCollected && Matches(hit.transform, handObject))
            {
                handCollected = true;
                if (handObject != null)
                    handObject.SetActive(false);
                PlayerPrefs.SetInt("Room3_HourHandCollected", 1);
                PlayerPrefs.SetInt("HasItem_ClockHand", 1);
                PlayerPrefs.Save();

                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem("ClockHand", InventoryManager.GetClockTimeSprite(), true);
                    InventoryManager.Instance.ShowInventoryTemporarily();
                }

                LevelProgressGuide.ReportTask("cupboard");
                ProceduralAudioGenerator.PlaySound("pickup", 0.85f);
                return;
            }
        }
    }

    private static void EnsureCollider(GameObject target)
    {
        if (target == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.GetComponent<Collider>() == null)
                renderer.gameObject.AddComponent<BoxCollider>();
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
                if (candidateName.Contains(name.ToLowerInvariant())) return candidate;
        }
        return null;
    }
}
