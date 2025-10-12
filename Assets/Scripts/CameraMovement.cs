using UnityEngine;

/// <summary>
/// Third-person camera controller (no Cinemachine vCam required).
/// Attach to your Camera (or CameraRig). Assign player Transform.
/// Smooth follow, filtered vertical movement to avoid jump jitter,
/// player orientation control (mouse / right stick), zoom, and optional collision.
/// </summary>
[AddComponentMenu("Camera/ThirdPersonCamera_NoCinemachine")]
public class ThirdPersonCamera_NoCinemachine : MonoBehaviour
{
    [Header("References")]
    public Transform player;                    // the player root (required)
    public Vector3 followOffset = new Vector3(0f, 1.6f, 0f); // look-at point offset from player position

    [Header("Orbit / Rotation")]
    public float sensitivityX = 120f;
    public float sensitivityY = 100f;
    public string mouseXInput = "Mouse X";
    public string mouseYInput = "Mouse Y";
    public bool invertY = false;
    public float rotationSmoothTime = 0.08f;
    public float minPitch = -35f;
    public float maxPitch = 60f;

    [Header("Follow Smoothing")]
    public float followSmoothTimeXZ = 0.06f;  // horizontal smoothing
    public float followSmoothTimeY = 0.25f;   // vertical smoothing (larger avoids jumpiness)

    [Header("Distance / Zoom")]
    public float distance = 4f;
    public float minDistance = 1.5f;
    public float maxDistance = 8f;
    public bool enableZoom = true;
    public float zoomSpeed = 2f;

    [Header("Collision (optional)")]
    public bool enableCollision = true;
    public float collisionRadius = 0.3f;
    public LayerMask collisionMask = ~0; // everything by default
    public float collisionSmoothTime = 0.05f;

    // internals
    float yaw = 0f;
    float pitch = 10f;
    float smoothYaw = 0f;
    float smoothPitch = 0f;
    float yawVel = 0f;
    float pitchVel = 0f;

    Vector3 followVelocityXZ = Vector3.zero;
    float followVelocityY = 0f;

    float currentDistance;
    float distanceVelocity = 0f;

    void Awake()
    {
        if (player == null)
            Debug.LogError("[ThirdPersonCamera_NoCinemachine] Player not assigned.");

        currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);

        // initialize yaw/pitch from current transform
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        smoothYaw = yaw;
        smoothPitch = pitch;
    }

    void Update()
    {
        // Input: look (mouse or right stick)
        float inputX = Input.GetAxis(mouseXInput);
        float inputY = Input.GetAxis(mouseYInput);

        yaw += inputX * sensitivityX * Time.deltaTime;
        float invert = invertY ? 1f : -1f;
        pitch += inputY * sensitivityY * Time.deltaTime * invert;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Zoom
        if (enableZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                float target = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minDistance, maxDistance);
                currentDistance = target;
            }
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Smooth rotation
        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawVel, rotationSmoothTime);
        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchVel, rotationSmoothTime);

        // Calculate desired camera position (world)
        Vector3 lookAtWorld = player.position + followOffset;

        Quaternion rot = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
        Vector3 desiredCameraPos = lookAtWorld + rot * new Vector3(0f, 0f, -currentDistance);

        // Separate XZ and Y smoothing to prevent jumpy vertical following
        Vector3 currentPos = transform.position;

        // XZ smoothing: keep Y as current (we'll smooth Y separately)
        Vector3 desiredXZ = new Vector3(desiredCameraPos.x, currentPos.y, desiredCameraPos.z);
        Vector3 newXZ = Vector3.SmoothDamp(currentPos, desiredXZ, ref followVelocityXZ, followSmoothTimeXZ);

        // Y smoothing
        float newY = Mathf.SmoothDamp(currentPos.y, desiredCameraPos.y, ref followVelocityY, followSmoothTimeY);

        Vector3 smoothedPos = new Vector3(newXZ.x, newY, newXZ.z);

        // Collision: spherecast from lookAtWorld to smoothedPos; if hit, move camera to hit point minus a small offset
        Vector3 finalPos = smoothedPos;
        if (enableCollision)
        {
            Vector3 dir = (smoothedPos - lookAtWorld);
            float dirLen = dir.magnitude;
            if (dirLen > 0.001f)
            {
                dir /= dirLen;
                RaycastHit hit;
                // SphereCast from lookAtWorld towards desired position
                if (Physics.SphereCast(lookAtWorld, collisionRadius, dir, out hit, dirLen, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    float targetDist = Mathf.Max(0.1f, hit.distance - 0.05f); // small offset
                    Vector3 collisionPos = lookAtWorld + dir * targetDist;
                    // Smoothly move camera toward collisionPos
                    finalPos = Vector3.SmoothDamp(transform.position, collisionPos, ref followVelocityXZ, collisionSmoothTime);
                }
                else
                {
                    // no hit, smoothly move towards smoothedPos (we already have smoothedPos)
                    finalPos = smoothedPos;
                }
            }
        }

        transform.position = finalPos;

        // Look at the player lookAt point
        transform.rotation = Quaternion.LookRotation((lookAtWorld - transform.position).normalized, Vector3.up);
    }

    /// <summary>
    /// Call this to instantly snap camera to current desired (useful on scene start or teleport).
    /// </summary>
    public void SnapToTargetImmediate()
    {
        if (player == null) return;
        Vector3 lookAtWorld = player.position + followOffset;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredCameraPos = lookAtWorld + rot * new Vector3(0f, 0f, -currentDistance);
        transform.position = desiredCameraPos;
        transform.rotation = Quaternion.LookRotation((lookAtWorld - transform.position).normalized, Vector3.up);

        // reset smoothing velocities
        followVelocityXZ = Vector3.zero;
        followVelocityY = 0f;
        yawVel = pitchVel = 0f;
    }
}
