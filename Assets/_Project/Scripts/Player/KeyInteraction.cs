using UnityEngine;

public class KeyInteraction : MonoBehaviour
{
    // هذا هو المتغير الذي يطلبه سكربت الباب
    public static bool HasKey { get; private set; } = false;

    [Header("Inventory Settings")]
    [SerializeField] private string itemID = "ChestKey";
    [SerializeField] private Sprite keyInventoryIcon;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip pickupSound;

    private Camera mainCam;
    private bool isCollected = false;

    private void Awake()
    {
        mainCam = Camera.main;
        HasKey = false; // إعادة ضبط المتغير عند بدء اللعبة
    }

    private void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool keyDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("key"))
                       || PlayerPrefs.GetInt($"{sceneName}_Task_key", 0) == 1
                       || PlayerPrefs.GetInt("HasItem_ChestKey", 0) == 1;

        bool doorDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("door"))
                        || PlayerPrefs.GetInt($"{sceneName}_Task_door", 0) == 1
                        || PlayerPrefs.GetInt("Room1_Done", 0) == 1;

        if (keyDone)
        {
            isCollected = true;
            HasKey = true;
            gameObject.SetActive(false); // إخفاء المفتاح من الغرفة لأن اللاعب أخذه مسبقاً

            // إذا لم يتم فتح الباب بعد، يبقى المفتاح داخل الحقيبة
            if (!doorDone && InventoryManager.Instance != null && !InventoryManager.Instance.HasItem(itemID) && !InventoryManager.Instance.HasItem("key"))
            {
                Sprite icon = keyInventoryIcon != null ? keyInventoryIcon : InventoryManager.GetKeySprite();
                InventoryManager.Instance.AddItem(itemID, icon, false);
            }
        }
    }

    private void OnMouseDown()
    {
        PickupKey();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    PickupKey();
                }
            }
        }
    }

    public void PickupKey()
    {
        if (isCollected) return;

        isCollected = true;
        HasKey = true; // تفعيل شرط فتح الباب هنا

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt($"{sceneName}_Task_key", 1);
        PlayerPrefs.SetInt("HasItem_ChestKey", 1);
        PlayerPrefs.Save();

        // إضافة المفتاح إلى الحقيبة
        if (InventoryManager.Instance != null)
        {
            Sprite icon = keyInventoryIcon != null ? keyInventoryIcon : InventoryManager.GetKeySprite();
            InventoryManager.Instance.AddItem(itemID, icon, true);
            Debug.Log("<color=yellow>[Key]:</color> تم التقاط المفتاح بنجاح وإضافته للحقيبة!");
        }

        // تشغيل صوت الالتقاط إن وجد أو توليد صوت بلوري سحري
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        else
        {
            ProceduralAudioGenerator.PlaySound("pickup", 0.9f, transform.position);
        }

        // إخفاء المفتاح من الغرفة
        gameObject.SetActive(false);

        // إبلاغ دليل التقدم بالتقاط المفتاح
        LevelProgressGuide.ReportTask("key");
    }
}