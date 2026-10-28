using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("سرعة انتقال الكاميرا بين النقاط")]
    [SerializeField] private float transitionSpeed = 3.5f;

    [Header("Viewpoints Setup")]
    [Tooltip("قائمة النقاط التي تنتقل بينها الكاميرا داخل الغرفة")]
    public List<Transform> viewpoints = new List<Transform>();

    private Transform currentTargetViewpoint;
    private int currentIndex = 0;

    private void Start()
    {
        // نترك الهدف فارغاً حتى لا تُسحب الكاميرا إلى أي إحداثيات قديمة عند تشغيل اللعبة
        currentTargetViewpoint = null;
    }

    private void Update()
    {
        // التحريك يعمل فقط عند اختيار نقطة جديدة يدوياً
        if (currentTargetViewpoint != null)
        {
            transform.position = Vector3.Lerp(transform.position, currentTargetViewpoint.position, Time.deltaTime * transitionSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, currentTargetViewpoint.rotation, Time.deltaTime * transitionSpeed);
        }

        // تحكم تجريبي سريع عبر الكيبورد
        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            NextViewpoint();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            PreviousViewpoint();
        }
    }

    public void MoveToViewpoint(Transform targetPoint)
    {
        if (targetPoint == null) return;
        currentTargetViewpoint = targetPoint;

        int index = viewpoints.IndexOf(targetPoint);
        if (index != -1)
        {
            currentIndex = index;
        }
    }

    public void MoveToViewpointIndex(int index)
    {
        if (index >= 0 && index < viewpoints.Count)
        {
            currentIndex = index;
            currentTargetViewpoint = viewpoints[currentIndex];
        }
    }

    public void NextViewpoint()
    {
        if (viewpoints == null || viewpoints.Count == 0) return;
        currentIndex = (currentIndex + 1) % viewpoints.Count;
        currentTargetViewpoint = viewpoints[currentIndex];
    }

    public void PreviousViewpoint()
    {
        if (viewpoints == null || viewpoints.Count == 0) return;
        currentIndex = (currentIndex - 1 + viewpoints.Count) % viewpoints.Count;
        currentTargetViewpoint = viewpoints[currentIndex];
    }
}
