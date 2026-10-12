using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick References")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;

    [Header("Settings")]
    [SerializeField] private float handleRange = 70f;

    public Vector2 InputDirection { get; private set; } = Vector2.zero;

    private Canvas parentCanvas;

    private void Awake()
    {
        if (joystickBackground == null)
            joystickBackground = GetComponent<RectTransform>();

        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (joystickBackground == null || joystickHandle == null) return;

        Camera eventCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = eventData.pressEventCamera;
        }

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventCamera,
            out localPoint))
        {
            localPoint = Vector2.ClampMagnitude(localPoint, handleRange);
            joystickHandle.anchoredPosition = localPoint;
            InputDirection = localPoint / handleRange;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputDirection = Vector2.zero;
        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
        }
    }
}