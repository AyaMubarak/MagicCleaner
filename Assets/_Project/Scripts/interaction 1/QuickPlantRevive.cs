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

    private void Awake()
    {
        EnsureGenerousHitCollider();
    }

    private void EnsureGenerousHitCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders)
        {
            if (c is MeshCollider mc)
            {
                if (mc.sharedMesh == null) mc.enabled = false;
                else mc.convex = true;
            }
        }

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) box = gameObject.AddComponent<BoxCollider>();
        box.enabled = true;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds worldBounds = new Bounds(transform.position, Vector3.one * 0.65f);
        bool hasRenderer = false;
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (!hasRenderer)
            {
                worldBounds = r.bounds;
                hasRenderer = true;
            }
            else
            {
                worldBounds.Encapsulate(r.bounds);
            }
        }

        Vector3 minWorldSize = new Vector3(0.75f, 0.75f, 0.75f);
        Vector3 targetWorldSize = Vector3.Max(worldBounds.size * 1.35f, minWorldSize);
        Vector3 lossy = transform.lossyScale;
        float sx = Mathf.Max(Mathf.Abs(lossy.x), 0.001f);
        float sy = Mathf.Max(Mathf.Abs(lossy.y), 0.001f);
        float sz = Mathf.Max(Mathf.Abs(lossy.z), 0.001f);

        box.center = transform.InverseTransformPoint(worldBounds.center);
        box.size = new Vector3(targetWorldSize.x / sx, targetWorldSize.y / sy, targetWorldSize.z / sz);
    }

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
        EnsureGenerousHitCollider();
    }

    private void OnMouseDown()
    {
        if (puzzleStage >= 2) return;

        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            float effectiveDist = Mathf.Max(maxDistance, 12f);
            if (Vector3.Distance(cam.transform.position, transform.position) <= effectiveDist)
            {
                HandlePlantClick();
            }
        }
    }

    private void Update()
    {
        if (puzzleStage >= 2) return;

        if (Input.GetMouseButtonDown(0))
        {
            TryClickPlant(Input.mousePosition);
        }
        else if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                TryClickPlant(t.position);
            }
        }
    }

    private void TryClickPlant(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null) return;

        float effectiveDist = Mathf.Max(maxDistance, 12f);
        Ray ray = cam.ScreenPointToRay(screenPos);

        RaycastHit[] hits = Physics.RaycastAll(ray, effectiveDist);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<QuickPlantRevive>() == this)
            {
                HandlePlantClick();
                return;
            }
        }

        RaycastHit[] sphereHits = Physics.SphereCastAll(ray, 0.25f, effectiveDist);
        foreach (var hit in sphereHits)
        {
            if (hit.collider == null) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<QuickPlantRevive>() == this)
            {
                HandlePlantClick();
                return;
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