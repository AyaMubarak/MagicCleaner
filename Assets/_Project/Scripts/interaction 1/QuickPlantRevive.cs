using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QuickPlantRevive : MonoBehaviour
{
    [Header("Item Check")]
    [SerializeField] private string requiredPotion = "AcidPotion";

    [Header("Plant Objects")]
    [Tooltip("مجسم أوراق النبتة القوية POTHOS_POTHOS_Mat_0")]
    [SerializeField] private GameObject healthyLeavesObject;

    [Tooltip("مجسم النبتة بالكامل plant ليختفي في النقرة الثانية")]
    [SerializeField] private GameObject wholePlantObject;

    [Header("Reward Key")]
    [Tooltip("مجسم المفتاح الذي سيظهر على الطاولة")]
    [SerializeField] private GameObject exitKey;

    [Header("Interaction Settings")]
    [SerializeField] private float maxDistance = 6f;

    // المراحل: 0 = ذابلة/فارغة، 1 = نمت وقوية، 2 = اختفت والمفتاح ظهر
    private int puzzleStage = 0;

    private void Start()
    {
        bool waterDone = PlayerPrefs.GetInt("Room2_Task_water", 0) == 1;
        bool keyDone = PlayerPrefs.GetInt("Room2_Task_key", 0) == 1 ||
                       (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("ExitKey"));

        if (keyDone)
        {
            // تم أخذ المفتاح والنبتة أزيحت مسبقاً: تختفي النبتة والمفتاح
            puzzleStage = 2;
            if (wholePlantObject != null) wholePlantObject.SetActive(false);
            else gameObject.SetActive(false);
            if (exitKey != null) exitKey.SetActive(false);
            return;
        }
        else if (waterDone)
        {
            // تم سقي النبتة وإزاحتها لكشف المفتاح، والمفتاح لم يؤخذ بعد
            puzzleStage = 2;
            if (wholePlantObject != null) wholePlantObject.SetActive(false);
            else gameObject.SetActive(false);
            if (exitKey != null) exitKey.SetActive(true);
            return;
        }

        // 1. إخفاء المفتاح في البداية
        if (exitKey != null)
        {
            exitKey.SetActive(false);
        }

        // 2. إخفاء الأوراق القوية في البداية لتبدو النبتة ميتة/فارغة
        if (healthyLeavesObject != null)
        {
            healthyLeavesObject.SetActive(false);
        }

        // ضبط الكولايدر
        Collider col = GetComponent<Collider>();
        if (col != null && col is MeshCollider meshCol)
        {
            meshCol.convex = true;
        }
    }

    private void Update()
    {
        if (puzzleStage >= 2) return;

        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    HandlePlantClick();
                }
            }
        }
    }

    private void HandlePlantClick()
    {
        // المرحلة 0: تحتاج الجرعة لتنمو وتصبح قوية
        if (puzzleStage == 0)
        {
            if (InventoryManager.Instance == null) return;

            string selected = InventoryManager.Instance.SelectedItem;

            if (string.IsNullOrEmpty(selected))
            {
                Debug.LogWarning("[النبتة]: النبتة ميتة! حددي الجرعة من الحقيبة واسقيها أولاً.");
                return;
            }

            if (selected.Trim().Equals(requiredPotion.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                // استهلاك الجرعة من الحقيبة
                InventoryManager.Instance.RemoveItem(selected);

                // إظهار الأوراق الخضراء القوية
                if (healthyLeavesObject != null)
                {
                    healthyLeavesObject.SetActive(true);
                }

                puzzleStage = 1;
                LevelProgressGuide.ReportTask("water");
                Debug.Log("<color=green>[النبتة]:</color> لقد أزهرت النبتة وأصبحت قوية! اضغطي عليها مرة أخرى لإزاحتها وأخذ المفتاح.");
            }
            else
            {
                Debug.LogWarning($"[النبتة]: مطلوب ({requiredPotion}) وليس ({selected})");
            }
        }
        // المرحلة 1: النبتة قوية واللاعب ينقر عليها مجدداً لتختفي ويظهر المفتاح
        else if (puzzleStage == 1)
        {
            puzzleStage = 2;

            // 1. إظهار المفتاح على الطاولة
            if (exitKey != null)
            {
                exitKey.SetActive(true);
            }

            Debug.Log("<color=cyan>[النبتة]:</color> تم إبعاد النبتة وظهر المفتاح على الطاولة!");

            // 2. إخفاء النبتة بالكامل
            if (wholePlantObject != null)
            {
                wholePlantObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}