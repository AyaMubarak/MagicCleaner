using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MagicPlantPuzzle : MonoBehaviour
{
    [Header("Required Item")]
    [Tooltip("اسم الجرعة المطلوبة لسقاية النبتة من الحقيبة")]
    [SerializeField] private string requiredPotionID = "AcidPotion";

    [Header("Plant Visuals")]
    [Tooltip("مجسم النبتة الميتة / الذابلة")]
    [SerializeField] private GameObject deadPlantObject;
    [Tooltip("مجسم النبتة الحية المورقة")]
    [SerializeField] private GameObject alivePlantObject;

    [Header("Key Reward")]
    [Tooltip("مجسم المفتاح الذي سيظهر للاعب")]
    [SerializeField] private GameObject rewardKeyObject;

    [Header("Effects & Audio")]
    [SerializeField] private ParticleSystem bloomParticles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip reviveSound;

    [Header("Interaction Settings")]
    [SerializeField] private float maxDistance = 4f;

    private bool isRevived = false;

    private void Start()
    {
        // تهيئة الحالة الأولية
        if (deadPlantObject != null) deadPlantObject.SetActive(true);
        if (alivePlantObject != null) alivePlantObject.SetActive(false);
        if (rewardKeyObject != null) rewardKeyObject.SetActive(false);
    }

    private void Update()
    {
        if (isRevived) return;

        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    TryWaterPlant();
                }
            }
        }
    }

    private void TryWaterPlant()
    {
        if (isRevived || InventoryManager.Instance == null) return;

        string selected = InventoryManager.Instance.SelectedItem;

        if (string.IsNullOrEmpty(selected))
        {
            Debug.Log("<color=yellow>[النبتة]:</color> النبتة يابسة وتحتاج إلى سائل لإحيائها! حدد الجرعة من الحقيبة.");
            return;
        }

        if (selected.Equals(requiredPotionID, System.StringComparison.OrdinalIgnoreCase))
        {
            // استهلاك الجرعة من الحقيبة
            InventoryManager.Instance.RemoveItem(selected);
            StartCoroutine(RevivePlantRoutine());
        }
        else
        {
            Debug.LogWarning("<color=orange>[النبتة]:</color> هذا السائل لا يناسب النبتة.");
        }
    }

    private IEnumerator RevivePlantRoutine()
    {
        isRevived = true;

        if (bloomParticles != null) bloomParticles.Play();
        if (audioSource && reviveSound) audioSource.PlayOneShot(reviveSound);

        // تبديل النبتة من ميتة إلى حية
        if (deadPlantObject != null) deadPlantObject.SetActive(false);
        if (alivePlantObject != null) alivePlantObject.SetActive(true);

        Debug.Log("<color=green>[النبتة]:</color> لقد عادت النبتة للحياة وتفتحت!");

        // انتظار ثانية ونصف لتظهر النبتة أولاً ثم يظهر المفتاح
        yield return new WaitForSeconds(1.5f);

        if (rewardKeyObject != null)
        {
            rewardKeyObject.SetActive(true);
            Debug.Log("<color=cyan>[النبتة]:</color> ظهر مفتاح الخروج بين الأوراق!");
        }
    }
}