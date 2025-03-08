using UnityEngine;
using System.Collections;

public class FoxWalk : MonoBehaviour
{
    public Animator animator;
    public LineRenderer pathLine;
    public Transform xrOrigin;  // Reference to XR Origin
    public float speed = 2.0f;
    public float stopThreshold = 0.3f;
    public float rotationSpeed = 5.0f;
    public float startOffset = 1f; // Distance in front of XR Origin

    private int currentPathIndex = 0;
    private bool isWalking = false;
    private bool reachedDestination = false;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (pathLine == null)
        {
            Debug.LogError("PathLine (LineRenderer) is not assigned!");
            return;
        }

        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin is not assigned!");
            return;
        }

        // **Scale Fix**: Reduce the size of the fox
        transform.localScale = Vector3.one * 0.5f;  // Adjust scale (change if needed)

        // **Position Fix**: Place the fox in front of XR Origin
        Vector3 startPosition = xrOrigin.position + (xrOrigin.forward * startOffset);
        startPosition.y = 0; // Ensure the fox starts at ground level
        transform.position = startPosition;

        // **Rotation Fix**: Face the first point in the path
        if (pathLine.positionCount > 0)
        {
            Vector3 firstTarget = pathLine.GetPosition(0);
            firstTarget.y = transform.position.y; // Keep fox at ground level
            transform.rotation = Quaternion.LookRotation(firstTarget - transform.position);
        }

        // Start sitting
        animator.SetTrigger("Sit");
    }

    void Update()
    {
        if (pathLine.positionCount == 0 || reachedDestination) return;

        Vector3 targetPosition = pathLine.GetPosition(currentPathIndex);
        targetPosition.y = transform.position.y; // Keep fox at ground level

        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance < stopThreshold)
        {
            currentPathIndex++;

            if (currentPathIndex >= pathLine.positionCount)
            {
                reachedDestination = true;
                isWalking = false;
                animator.SetTrigger("Sit"); // Sit at the destination
                return;
            }

            targetPosition = pathLine.GetPosition(currentPathIndex);
            targetPosition.y = transform.position.y;
        }

        MoveTowardsTarget(targetPosition);
    }

    private void MoveTowardsTarget(Vector3 target)
    {
        if (!isWalking)
        {
            isWalking = true;
            animator.SetTrigger("Stand"); // Stand up first
            StartCoroutine(StartWalkingAfterDelay(0.5f));
        }

        Vector3 direction = (target - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // Rotate smoothly towards movement direction
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
    }

    private IEnumerator StartWalkingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetTrigger("Walk");
    }

    public void StopFox()
    {
        isWalking = false;
        animator.SetTrigger("Sit"); // Sit when stopped
    }

    public void RestartPath()
    {
        currentPathIndex = 0;
        reachedDestination = false;
        isWalking = false;
        animator.SetTrigger("Sit");

        // Face the first target point again when restarting
        if (pathLine.positionCount > 0)
        {
            Vector3 firstTarget = pathLine.GetPosition(0);
            firstTarget.y = transform.position.y;
            transform.rotation = Quaternion.LookRotation(firstTarget - transform.position);
        }
    }
}
