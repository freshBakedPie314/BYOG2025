using UnityEngine;
using System.Collections;

public class ScreenTransition : MonoBehaviour
{
    [Header("Assign the 2 GameObjects to move")]
    public Transform leftObject;
    public Transform rightObject;

    [Header("Transition Settings")]
    public float moveDuration = 1f;      // Time to reach the center
    public float pauseDuration = 1f;     // Time to stay in center
    public float offset = 8f;            // How far off-screen (world units)

    private Vector3 leftStartPos;
    private Vector3 rightStartPos;
    private Vector3 leftCenterPos;
    private Vector3 rightCenterPos;
    private bool isTransitioning = false;

    void Start()
    {
        // Cache starting positions
        leftStartPos = leftObject.position;
        rightStartPos = rightObject.position;

        // Move them off-screen initially (optional)
        leftObject.position = new Vector3(-offset, leftStartPos.y, leftStartPos.z);
        rightObject.position = new Vector3(offset, rightStartPos.y, rightStartPos.z);

        // Compute center positions (center of screen)
        leftCenterPos = new Vector3(0f, leftStartPos.y, leftStartPos.z);
        rightCenterPos = new Vector3(0f, rightStartPos.y, rightStartPos.z);
        InvokeRepeating(nameof(StartTransition), 2f,5f);
    }

    public void StartTransition()
    {
        if (!isTransitioning)
            StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        isTransitioning = true;

        // Move to center
        yield return MoveObjects(
            leftObject, rightObject,
            leftObject.position, rightObject.position,
            leftCenterPos, rightCenterPos,
            moveDuration
        );

        // Wait in center
        yield return new WaitForSeconds(pauseDuration);

        // Move back to original off-screen positions
        yield return MoveObjects(
            leftObject, rightObject,
            leftCenterPos, rightCenterPos,
            new Vector3(-offset, leftStartPos.y, leftStartPos.z),
            new Vector3(offset, rightStartPos.y, rightStartPos.z),
            moveDuration
        );

        isTransitioning = false;
    }

    // Add this helper method to create an ease-in-out effect
    private float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private IEnumerator MoveObjects(
        Transform left, Transform right,
        Vector3 leftFrom, Vector3 rightFrom,
        Vector3 leftTo, Vector3 rightTo,
        float duration
    )
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;
            float t = EaseInOut(normalizedTime);

            left.position = Vector3.Lerp(leftFrom, leftTo, t);
            right.position = Vector3.Lerp(rightFrom, rightTo, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        left.position = leftTo;
        right.position = rightTo;
    }
}
