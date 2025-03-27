using UnityEngine;
using System.Collections;

public class FoxWalk : MonoBehaviour
{
    public Animator animator;
    public LineRenderer pathLine;
    public Transform xrOrigin;
    private Camera arCamera;
    public float speed = 2.5f; // Increased speed slightly
    public float stopThreshold = 0.5f;
    public float rotationSpeed = 5.0f;
    public float startOffset = 1.0f;
    public float followDistance = 2.0f; 
    private bool isWalking = false;
    private bool hasJumped = false;
    private Vector3 lastCameraPosition;
    private float stillTime = 0f;
    private float stillThreshold = 0.2f; // Time before switching to sit
    private Vector3 xrDelta;

    void Start()
    {
        Debug.Log("debug log: Fox enabled! Positioning...");

        arCamera = xrOrigin.GetComponentInChildren<Camera>();

        // Position fox after being enabled
        Vector3 startPosition = arCamera.transform.position + (arCamera.transform.forward * startOffset);
        startPosition.y = arCamera.transform.position.y + 0.5f;

        // Ensure fox is above ground
        if (Physics.Raycast(startPosition + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
        {
            startPosition.y = hit.point.y + 0.1f; // Adjust to be slightly above ground
        }

        transform.position = startPosition;
        Debug.Log($"debug log: Fox enabled! Positioning... {startPosition}, {startPosition.y}, {transform.position}");

        lastCameraPosition = arCamera.transform.position;
        StartFoxWalking();
    }

    void Update()
    {
        if (pathLine.positionCount == 0) return;

        xrDelta = arCamera.transform.position - lastCameraPosition;
        lastCameraPosition = arCamera.transform.position;

        Vector3 targetPosition = arCamera.transform.position + (arCamera.transform.forward * followDistance);

        // Check if Fox reached the last point in the path
        if (Vector3.Distance(transform.position, pathLine.GetPosition(pathLine.positionCount - 1)) < stopThreshold)
        {
            if (!hasJumped)
            {
                PlayJumpAnimation();
                hasJumped = true;
            }
            return;
        }

        // Check movement
        if (xrDelta.magnitude > 0f) // Even the smallest movement should trigger walk
        {
            stillTime = 0f; // Reset still timer
            StartFoxWalking();
            MoveTowardsTarget(targetPosition);
        }
        else
        {
            stillTime += Time.deltaTime;
            if (stillTime >= stillThreshold) // Sit only after a small delay
            {
                StopFox();
            }
        }
    }

    private void MoveTowardsTarget(Vector3 target)
    {
        float dynamicDistance = Mathf.Lerp(5f, 10f, xrDelta.magnitude * 5f);
        Vector3 adjustedTarget = arCamera.transform.position + (arCamera.transform.forward * dynamicDistance) + (arCamera.transform.right * 2f);

        // 🔹 Use Raycasting to find ground level
        RaycastHit hit;
        if (Physics.Raycast(adjustedTarget + Vector3.up * 2f, Vector3.down, out hit, 5f))
        {
            adjustedTarget.y = hit.point.y; // Snap fox to the detected ground
        }
        else
        {
            adjustedTarget.y = arCamera.transform.position.y; // Default fallback
        }

        transform.position = Vector3.Lerp(transform.position, adjustedTarget, speed * Time.deltaTime);

        Vector3 direction = (adjustedTarget - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void StartFoxWalking()
    {
        if (!isWalking)
        {
            isWalking = true;
            animator.ResetTrigger("Fox sit");
            animator.ResetTrigger("Fox jump");
            animator.SetTrigger("Fox walk");
        }
    }

    private void StopFox()
    {
        if (isWalking)
        {
            isWalking = false;
            animator.ResetTrigger("Fox walk");
            animator.SetTrigger("Fox sit");
        }
    }

    private void PlayJumpAnimation()
    {
        animator.ResetTrigger("Fox walk");
        animator.ResetTrigger("Fox sit");
        animator.SetTrigger("Fox jump");
    }
}
