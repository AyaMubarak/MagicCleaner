using System;
using UnityEngine;

public class TouchInputManager : MonoBehaviour
{
    public static TouchInputManager Instance { get; private set; }

    public static event Action<Vector2> OnTap;
    public static event Action<Vector2, Vector2> OnSwipe; // StartPos, Delta/Direction

    [SerializeField] private float minSwipeDistance = 25f;
    private Vector2 touchStartPosition;
    private bool isTrackingSwipe = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            touchStartPosition = touch.position;
            isTrackingSwipe = true;
        }
        else if (touch.phase == TouchPhase.Moved && isTrackingSwipe)
        {
            Vector2 delta = touch.position - touchStartPosition;
            if (delta.magnitude >= minSwipeDistance)
            {
                OnSwipe?.Invoke(touch.position, delta);
            }
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            Vector2 delta = touch.position - touchStartPosition;
            if (delta.magnitude < minSwipeDistance)
            {
                OnTap?.Invoke(touch.position);
            }
            isTrackingSwipe = false;
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPosition = Input.mousePosition;
            isTrackingSwipe = true;
        }
        else if (Input.GetMouseButton(0) && isTrackingSwipe)
        {
            Vector2 currentPos = (Vector2)Input.mousePosition;
            Vector2 delta = currentPos - touchStartPosition;
            if (delta.magnitude >= minSwipeDistance)
            {
                OnSwipe?.Invoke(currentPos, delta);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Vector2 endPos = (Vector2)Input.mousePosition;
            Vector2 delta = endPos - touchStartPosition;
            if (delta.magnitude < minSwipeDistance)
            {
                OnTap?.Invoke(endPos);
            }
            isTrackingSwipe = false;
        }
    }
}