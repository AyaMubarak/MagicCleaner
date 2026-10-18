using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StairsClimber : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("نقطة وصول اللاعب في الأعلى")]
    [SerializeField] private Transform topPoint;

    [Header("Key Requirement")]
    [Tooltip("اسم المفتاح في الحقيبة")]
    [SerializeField] private string requiredKeyID = "ExitKey";

    [Header("Roof Object")]
    [Tooltip("مجسم فتحة السطح")]
    [SerializeField] private GameObject ceilingTrapdoor;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "Room3";

    private bool isAtTop = false;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // زيادة مسافة الرايكاست لتصل للسقف من أي مكان
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                // إذا نقر على السلم
                if (!isAtTop && (hit.transform == transform || hit.transform.IsChildOf(transform)))
                {
                    TryClimb();
                }
                // إذا نقر على السطح
                else if (ceilingTrapdoor != null && (hit.transform == ceilingTrapdoor.transform || hit.transform.IsChildOf(ceilingTrapdoor.transform)))
                {
                    TryOpenRoof();
                }
            }
        }
    }

    public void TryClimb()
    {
        // 1. فحص وجود المفتاح في الحقيبة
        if (!PlayerHasKey())
        {
            Debug.LogWarning($"<color=yellow>[السلم]:</color> اجمعي المفتاح ({requiredKeyID}) أولاً!");
            return;
        }

        if (topPoint == null)
        {
            Debug.LogError("<color=red>[السلم]:</color> خانة Top Point فارغة في الـ Inspector!");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = FindAnyObjectByType<CharacterController>()?.gameObject;

        if (player != null)
        {
            // إيقاف الـ CharacterController وسكربت الحركة مؤقتاً لمنع الجاذبية من إسقاطه
            CharacterController cc = player.GetComponent<CharacterController>();
            MonoBehaviour movementScript = player.GetComponent("PlayerMovementController") as MonoBehaviour;

            if (movementScript != null) movementScript.enabled = false;
            if (cc != null) cc.enabled = false;

            // نقل اللاعب فوراً إلى الإحداثيات العلوية
            player.transform.position = topPoint.position;
            player.transform.rotation = topPoint.rotation;

            // إعادة تشغيل المكونات بعد تثبيت الموقع
            if (cc != null) cc.enabled = true;

            isAtTop = true;
            Debug.Log("<color=green>[السلم]:</color> تم نقل اللاعب للأعلى بنجاح! انقري على فتحة السقف.");
        }
    }

    public void TryOpenRoof()
    {
        if (!PlayerHasKey())
        {
            Debug.LogWarning("<color=orange>[السطح]:</color> حددي المفتاح من الحقيبة أولاً!");
            return;
        }

        // حذف المفتاح والانتقال
        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKeyID))
        {
            InventoryManager.Instance.RemoveItem(requiredKeyID);
        }

        Debug.Log("<color=green>[السطح]:</color> مبروك! جاري الانتقال إلى المشهد التالي: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }

    private bool PlayerHasKey()
    {
        if (InventoryManager.Instance == null) return true;

        bool hasInBag = InventoryManager.Instance.HasItem(requiredKeyID);
        bool isSelected = !string.IsNullOrEmpty(InventoryManager.Instance.SelectedItem) &&
                          InventoryManager.Instance.SelectedItem.Trim().Equals(requiredKeyID.Trim(), System.StringComparison.OrdinalIgnoreCase);

        return hasInBag || isSelected;
    }
}