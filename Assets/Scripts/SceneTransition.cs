using UnityEngine;
using System.Collections;
using Unity.Cinemachine;


public class ScreenTransition : MonoBehaviour
{
    [Header("Assign the 2 UI Objects to move")]
    public RectTransform leftObject;
    public RectTransform rightObject;

    [Header("Transition Settings")]
    public float moveDuration = 1f;
    public float pauseDuration = 1f;

    [Header("Cameras")]
    public CinemachineCamera boardVCam;
    public CinemachineCamera battleVCam;

    private Vector2 leftOffscreenPos;
    private Vector2 rightOffscreenPos;
    private Vector2 centerPos;

    public bool isTransitioning = false;
    public BattleManager battleManager;

    [Header("HUDs & UI")]
    public GameObject boardHUD;
    public GameObject battleHUD;

    [Header("Core References")]
    public GameObject board;
    public GameObject battleArena;

    void Start()
    {
        centerPos = Vector2.zero;

        // ✅ Correct: use canvas width
        RectTransform canvasRect = leftObject.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        float screenWidth = canvasRect.rect.width;
        float objectWidth = leftObject.rect.width;

        float offscreenX = (screenWidth / 2f) + (objectWidth / 2f);

        leftOffscreenPos = new Vector2(-offscreenX, centerPos.y);
        rightOffscreenPos = new Vector2(offscreenX, centerPos.y);

        leftObject.anchoredPosition = leftOffscreenPos;
        rightObject.anchoredPosition = rightOffscreenPos;

        Debug.Log($"[ScreenTransition] ScreenWidth={screenWidth}, ObjectWidth={objectWidth}, OffscreenX={offscreenX}");
        Debug.Log($"[ScreenTransition] LeftOffscreen={leftOffscreenPos}, RightOffscreen={rightOffscreenPos}");
    }


    public void StartTransition(bool toBattle)
    {
        Debug.Log($"<color=cyan>[ScreenTransition]</color> StartTransition called | ToBattle = {toBattle}, IsTransitioning = {isTransitioning}");
        if (!isTransitioning)
            StartCoroutine(TransitionRoutine(toBattle));
        else
            Debug.LogWarning("[ScreenTransition] Transition already in progress!");
    }

    public IEnumerator TransitionRoutine(bool toBattle)
    {
        isTransitioning = true;
        Debug.Log("<color=magenta>[ScreenTransition]</color> Transition started...");

        // 1️ Move objects from off-screen → center
        Debug.Log("[ScreenTransition] Moving objects ON screen...");
        yield return StartCoroutine(MoveObjectsIn(leftOffscreenPos, rightOffscreenPos, centerPos, centerPos, moveDuration));
        Debug.Log("[ScreenTransition] Objects reached center. Screen fully covered.");

        // 2️ Switch cameras
        if (toBattle)
        {
            Debug.Log("[ScreenTransition] Switching to Battle Camera...");
            boardVCam.Priority = 5;
            battleVCam.Priority = 10;
        }
        else
        {
            Debug.Log("[ScreenTransition] Switching to Board Camera...");
            boardVCam.Priority = 10;
            battleVCam.Priority = 5;
        }
        if (toBattle)
        {
            battleManager.player.transform.position = battleManager.playerSpawnPoint.position;
            battleManager.player.transform.rotation = battleManager.playerSpawnPoint.rotation;
        }
        else
        {
            battleManager.player.transform.position = battleManager.playerBoardPosition;
        }

            // 3️ Wait while covered
            Debug.Log($"[ScreenTransition] Pausing for {pauseDuration} seconds...");
        yield return new WaitForSeconds(pauseDuration);

        boardHUD.SetActive(!toBattle);
        board.SetActive(!toBattle);
        battleHUD.SetActive(toBattle);
        battleArena.SetActive(toBattle);

        // 4️ Move objects from center → off-screen
        Debug.Log("[ScreenTransition] Moving objects OFF screen...");
        yield return StartCoroutine(MoveObjectsOut(centerPos, centerPos, leftOffscreenPos, rightOffscreenPos, moveDuration));
        Debug.Log("<color=lime>[ScreenTransition]</color> Transition complete.");

        

        isTransitioning = false;
    }

    private float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private IEnumerator MoveObjectsIn(Vector2 leftFrom, Vector2 rightFrom, Vector2 leftTo, Vector2 rightTo, float duration)
    {
        Debug.Log($"[ScreenTransition] MoveObjects | Duration: {duration}s");
        Debug.Log($"    Left from {leftFrom} → {leftTo}");
        Debug.Log($"    Right from {rightFrom} → {rightTo}");

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = EaseInOut(elapsed / duration);
            leftObject.anchoredPosition = Vector2.Lerp(new Vector2(-1280,0), leftTo, t);
            rightObject.anchoredPosition = Vector2.Lerp(new Vector2(1280, 0), rightTo, t);

            if (elapsed == 0f || Mathf.Abs(elapsed - duration / 2f) < Time.deltaTime)
                Debug.Log($"[ScreenTransition] Progress t={t:F2}, LeftPos={leftObject.anchoredPosition}, RightPos={rightObject.anchoredPosition}");

            elapsed += Time.deltaTime;
            yield return null;
        }

        leftObject.anchoredPosition = leftTo;
        rightObject.anchoredPosition = rightTo;

        Debug.Log($"[ScreenTransition] Move complete | Left={leftTo}, Right={rightTo}");
    }
    private IEnumerator MoveObjectsOut(Vector2 leftFrom, Vector2 rightFrom, Vector2 leftTo, Vector2 rightTo, float duration)
    {
        Debug.Log($"[ScreenTransition] MoveObjects | Duration: {duration}s");
        Debug.Log($"    Left from {leftFrom} → {leftTo}");
        Debug.Log($"    Right from {rightFrom} → {rightTo}");

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = EaseInOut(elapsed / duration);
            leftObject.anchoredPosition = Vector2.Lerp(leftFrom, new Vector2(-1280, 0), t);
            rightObject.anchoredPosition = Vector2.Lerp(rightFrom, new Vector2(1280, 0), t);

            if (elapsed == 0f || Mathf.Abs(elapsed - duration / 2f) < Time.deltaTime)
                Debug.Log($"[ScreenTransition] Progress t={t:F2}, LeftPos={leftObject.anchoredPosition}, RightPos={rightObject.anchoredPosition}");

            elapsed += Time.deltaTime;
            yield return null;
        }

        leftObject.anchoredPosition = new Vector2(-1280, 0);
        rightObject.anchoredPosition = new Vector2(1280, 0);

        Debug.Log($"[ScreenTransition] Move complete | Left={leftTo}, Right={rightTo}");
    }
}
