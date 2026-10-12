using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [SerializeField] private LayerMask interactableLayer = ~0;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        TouchInputManager.OnTap += HandleTap;
    }

    private void OnDisable()
    {
        TouchInputManager.OnTap -= HandleTap;
    }

    private void HandleTap(Vector2 screenPosition)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayer))
        {
            InteractableObject interactable = hit.collider.GetComponentInParent<InteractableObject>();
            if (interactable != null)
            {
                interactable.OnInteract();
            }
        }
    }
}