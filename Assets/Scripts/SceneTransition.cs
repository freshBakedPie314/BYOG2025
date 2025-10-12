using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

using TMPro;

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
        RectTransform canvasRect = leftObject.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        float screenWidth = canvasRect.rect.width;
        float objectWidth = leftObject.rect.width;
        float offscreenX = (screenWidth / 2f) + (objectWidth / 2f);

        leftOffscreenPos = new Vector2(-offscreenX, centerPos.y);
        rightOffscreenPos = new Vector2(offscreenX, centerPos.y);

        leftObject.anchoredPosition = leftOffscreenPos;
        rightObject.anchoredPosition = rightOffscreenPos;
    }

    // --- MODIFIED --- Changed from 'void' to 'IEnumerator'
    public IEnumerator StartTransition(bool toBattle)
    {
        if (isTransitioning)
        {
            yield break; // Exit if a transition is already running
        }
        // --- MODIFIED --- We now wait for the routine to finish
        yield return StartCoroutine(TransitionRoutine(toBattle));
    }

    private IEnumerator TransitionRoutine(bool toBattle)
    {
        isTransitioning = true;

        // 1. Move objects from off-screen -> center
        yield return StartCoroutine(MoveObjects(leftOffscreenPos, centerPos, rightOffscreenPos, centerPos, moveDuration));

        // 2. Switch cameras and game state while the screen is covered
        if (toBattle)
        {
            boardVCam.Priority = 5;
            battleVCam.Priority = 10;
            battleManager.player.transform.position = battleManager.playerSpawnPoint.position;
            battleManager.player.transform.rotation = battleManager.playerSpawnPoint.rotation;
        }
        else
        {
            boardVCam.Priority = 10;
            battleVCam.Priority = 5;
            battleManager.player.transform.position = battleManager.playerBoardPosition;
        }

        boardHUD.SetActive(!toBattle);
        board.SetActive(!toBattle);
        battleHUD.SetActive(toBattle);
        battleArena.SetActive(toBattle);

        // 3. Wait while covered
        yield return new WaitForSeconds(pauseDuration);

        // 4. Move objects from center -> off-screen
        yield return StartCoroutine(MoveObjects(centerPos, leftOffscreenPos, centerPos, rightOffscreenPos, moveDuration));

        isTransitioning = false;
    }

    private float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }

    // --- MODIFIED --- Combined MoveObjectsIn and MoveObjectsOut into one flexible method
    private IEnumerator MoveObjects(Vector2 leftFrom, Vector2 leftTo, Vector2 rightFrom, Vector2 rightTo, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = EaseInOut(elapsed / duration);
            leftObject.anchoredPosition = Vector2.Lerp(leftFrom, leftTo, t);
            rightObject.anchoredPosition = Vector2.Lerp(rightFrom, rightTo, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        leftObject.anchoredPosition = leftTo;
        rightObject.anchoredPosition = rightTo;
    }
}
