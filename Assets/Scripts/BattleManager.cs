using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Text
using System.Collections;
using Unity.Cinemachine; // You might see an error if Cinemachine isn't imported
using TMPro;

public class BattleManager : MonoBehaviour
{
    [Header("Core References")]
    public GameObject player;
    public GameObject board;
    public GameObject battleArena;

    [Header("Spawning")]
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;
    public GameObject[] enemyPrefabs;

    [Header("Cameras")]
    public CinemachineCamera boardVCam;
    public CinemachineCamera battleVCam;

    [Header("HUDs & UI")]
    public GameObject boardHUD;
    public GameObject battleHUD;
    public TextMeshProUGUI dialogueText;

    [Header("Manager")]
    public NextNSpawner spawner;
    public GameManager gameManager;
    public enum BattleState { INACTIVE, STARTING, PLAYERTURN, ENEMYTURN, WON, LOST }
    public BattleState currentState;

    private GameObject currentEnemyInstance;
    private CharacterStats playerStats;
    private CharacterStats enemyStats;
    private Vector3 playerBoardPosition; // To remember where the player was

    public void StartBattle()
    {
        playerBoardPosition = player.transform.position;

        boardHUD.SetActive(false);
        board.SetActive(false);
        battleHUD.SetActive(true);
        battleArena.SetActive(true);

        player.transform.position = playerSpawnPoint.position;
        player.transform.rotation = playerSpawnPoint.rotation;

        //CHANGE: Dont spawn random emey byt dcide based on area
        int enemyIndex = Random.Range(0, enemyPrefabs.Length);
        currentEnemyInstance = Instantiate(enemyPrefabs[enemyIndex], enemySpawnPoint.position, enemySpawnPoint.rotation);

        playerStats = player.GetComponent<CharacterStats>();
        enemyStats = currentEnemyInstance.GetComponent<CharacterStats>();

        boardVCam.Priority = 5;
        battleVCam.Priority = 10;

        //Start the combat sequence
        currentState = BattleState.STARTING;
        StartCoroutine(BattleSequence());
    }

    IEnumerator BattleSequence()
    {
        dialogueText.text = "A wild " + enemyStats.name.Replace("(Clone)", "") + " appears!";
        yield return new WaitForSeconds(2f);

        currentState = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    void PlayerTurn()
    {
        dialogueText.text = "Player's Turn. Choose your move.";

        //TODO: Unlock player attack one by one 
    }

    public void OnAbilityButton(Ability ability)
    {
        if (currentState != BattleState.PLAYERTURN)
            return;

        StartCoroutine(PlayerAction(ability));
    }

    IEnumerator PlayerAction(Ability ability)
    {
        if (ability.targetType == Ability.TargetType.Self)
        {
            ability.Execute(playerStats, playerStats);
        }
        else
        {
            ability.Execute(playerStats, enemyStats);
        }

        dialogueText.text = "Player uses " + ability.abilityName + "!";
        yield return new WaitForSeconds(2f);

        if (enemyStats.currentHealth <= 0)
        {
            currentState = BattleState.WON;
            StartCoroutine(EndBattleSequence());
        }
        else
        {
            currentState = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
    }

    void ProcessStatusEffects(CharacterStats character)
    {
        for (int i = character.activeStatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = character.activeStatusEffects[i];

            effect.OnTurnTick(character);

            if (effect.duration <= 0)
            {
                effect.OnRemove(character);

                character.activeStatusEffects.RemoveAt(i);
            }
        }
    }

    //-------------ENEMY AI-------------//
    //TODO: Update AI
    IEnumerator EnemyTurn()
    {
        dialogueText.text = "Enemy's Turn.";
        ProcessStatusEffects(enemyStats);
        yield return new WaitForSeconds(1f);

        if (enemyStats.characterAbilities.Count > 0)
        {
            Ability enemyAbility = enemyStats.characterAbilities[0];
            enemyAbility.Execute(enemyStats, playerStats);
            dialogueText.text = "Enemy uses " + enemyAbility.abilityName + "!";
        }
        else
        {
            dialogueText.text = "Enemy has no moves!";
        }

        yield return new WaitForSeconds(2f);

        if (playerStats.currentHealth <= 0)
        {
            currentState = BattleState.LOST;
            StartCoroutine(EndBattleSequence());
        }
        else
        {
            ProcessStatusEffects(playerStats);
            currentState = BattleState.PLAYERTURN;
            PlayerTurn();
        }
    }

    IEnumerator EndBattleSequence()
    {
        if (currentState == BattleState.WON)
        {
            dialogueText.text = "You won the battle!";
        }
        else if (currentState == BattleState.LOST)
        {
            dialogueText.text = "You were defeated.";
        }

        yield return new WaitForSeconds(3f);

        EndBattle();
    }

    public void EndBattle()
    {
        battleVCam.Priority = 5;
        boardVCam.Priority = 10;
        player.transform.position = playerBoardPosition;

        if (currentEnemyInstance != null)
        {
            Destroy(currentEnemyInstance);
        }

        battleArena.SetActive(false);
        battleHUD.SetActive(false);
        board.SetActive(true);
        boardHUD.SetActive(true);

        currentState = BattleState.INACTIVE;
        spawner.SpawnNextNCells(gameManager.currentPathIndex);
    }
}