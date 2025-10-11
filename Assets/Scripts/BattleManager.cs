using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Cinemachine;
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
    private BattleHUDManager battleHUDManager;

    [Header("Area Abilities")]
    public Ability fireAreaAbility;
    public Ability snowAreaAbility;
    public Ability earthAreaAbility;
    public Ability lightningAreaAbility;

    [Header("Boss Fight")]
    [Tooltip("Ability to be awarded after boss fight")]
    public Ability bossRewardAbility;

    public enum BattleState { INACTIVE, STARTING, PLAYERTURN, ENEMYTURN, WON, LOST }
    public enum AreaType { Fire, Earth, Lightning, Snow }
    public BattleState currentState;

    private GameObject currentEnemyInstance;
    private CharacterStats playerStats;
    private CharacterStats enemyStats;
    private Vector3 playerBoardPosition;
    private Ability currentRewardOnWin;

    public ScreenTransition screenTransition;
    public void Start()
    {
        battleHUDManager = battleHUD.GetComponent<BattleHUDManager>();
    }

    // This is for NORMAL enemy battles
    public void StartBattle(AreaType area, Ability rewardOnWin)
    {

        currentRewardOnWin = rewardOnWin;
        playerBoardPosition = player.transform.position;

        

        player.transform.position = playerSpawnPoint.position;
        player.transform.rotation = playerSpawnPoint.rotation;

        int enemyIndex = Random.Range(0, enemyPrefabs.Length);
        currentEnemyInstance = Instantiate(enemyPrefabs[enemyIndex], enemySpawnPoint.position, enemySpawnPoint.rotation);
        enemyStats = currentEnemyInstance.GetComponent<CharacterStats>();
        enemyStats.characterAbilities.Clear();

        switch (area)
        {
            case AreaType.Fire: enemyStats.characterAbilities.Add(fireAreaAbility); break;
            case AreaType.Snow: enemyStats.characterAbilities.Add(snowAreaAbility); break;
            case AreaType.Earth: enemyStats.characterAbilities.Add(earthAreaAbility); break;
            case AreaType.Lightning: enemyStats.characterAbilities.Add(lightningAreaAbility); break;
        }
        print("Starting battle in area " + area.ToString());
        playerStats = player.GetComponent<CharacterStats>();

        currentState = BattleState.STARTING;
        battleHUDManager.UpdateWarriors();
        battleHUDManager.UpdateStats();
        StartCoroutine(BattleSequence());
    }

    // This is for BOSS battles
    public void StartBossBattle(GameObject bossPrefab, Ability skillToLearn)
    {
        bossRewardAbility = skillToLearn;
        playerBoardPosition = player.transform.position;

        boardHUD.SetActive(false);
        board.SetActive(false);
        battleHUD.SetActive(true);
        battleArena.SetActive(true);

        player.transform.position = playerSpawnPoint.position;
        player.transform.rotation = playerSpawnPoint.rotation;

        Quaternion rot = Quaternion.Euler(enemySpawnPoint.rotation.x -90, enemySpawnPoint.rotation.y, enemySpawnPoint.rotation.z);

        currentEnemyInstance = Instantiate(bossPrefab, enemySpawnPoint.position, rot);
        enemyStats = currentEnemyInstance.AddComponent<CharacterStats>();

        enemyStats.maxHealth = 200;
        enemyStats.currentHealth = 200;
        enemyStats.attackPower = 20;
        enemyStats.defense = 10;

        enemyStats.characterAbilities.Clear();
        enemyStats.characterAbilities.Add(skillToLearn);

        playerStats = player.GetComponent<CharacterStats>();

        boardVCam.Priority = 5;
        battleVCam.Priority = 10;

        currentState = BattleState.STARTING;
        battleHUDManager.UpdateWarriors();
        battleHUDManager.UpdateStats();
        StartCoroutine(BattleSequence());
    }

    IEnumerator BattleSequence()
    {
        screenTransition.StartTransition(true);
        dialogueText.text = "A wild " + currentEnemyInstance.name.Replace("(Clone)", "") + " appears!";
        yield return new WaitForSeconds(2f);
        currentState = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    void PlayerTurn()
    {
        dialogueText.text = "Player's Turn. Choose your move.";
        battleHUDManager.UpdateActionButtons(playerStats);
        playerStats.PrintPotionInventory();
    }

    public void OnAbilityButton(Ability ability)
    {
        if (currentState != BattleState.PLAYERTURN)
            return;
        StartCoroutine(PlayerAction(ability));
    }

    IEnumerator PlayerAction(Ability ability)
    {
        bool isPotion = playerStats.potions.ContainsKey(ability);
        if (isPotion)
        {
            if (playerStats.potions[ability] > 0)
            {
                ability.Execute(playerStats, playerStats);
                playerStats.UsePotion(ability);
            }
            else
            {
                dialogueText.text = "You don't have any " + ability.name + " left!";
                yield return new WaitForSeconds(2f);
                yield break;
            }
        }
        else
        {
            if (ability.targetType == Ability.TargetType.Self)
            {
                ability.Execute(playerStats, playerStats);
            }
            else
            {
                ability.Execute(playerStats, enemyStats);
            }
        }

        //aniamtion corutine

        battleHUDManager.UpdateStats();
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
        if (character == null) return;
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
        battleHUDManager.UpdateStats();
    }

    IEnumerator EnemyTurn()
    {
        dialogueText.text = "Enemy's Turn.";
        ProcessStatusEffects(enemyStats);
        yield return new WaitForSeconds(1f);

        if (enemyStats.isStunned)
        {
            dialogueText.text = "Enemy is stunned and cannot act!";
            yield return new WaitForSeconds(2f);
        }
        else
        {
            Ability abilityToUse = null;
            string actionText = "";

            // --- MODIFIED HEALING LOGIC ---
            // 1. Check if health is low AND it has a healing ability.
            bool shouldConsiderHealing = enemyStats.healingAbility != null &&
                                         enemyStats.currentHealth < (enemyStats.maxHealth * enemyStats.healAtHealthPercent);

            // 2. If it should consider healing, roll a 20% chance.
            if (shouldConsiderHealing && Random.Range(1, 101) <= 20)
            {
                abilityToUse = enemyStats.healingAbility;
                actionText = "Enemy heals itself!";
                abilityToUse.Execute(enemyStats, enemyStats); // Target is self for healing
            }
            // --- END OF MODIFIED LOGIC ---
            else // Otherwise, proceed to attack logic as normal.
            {
                int randomChance = Random.Range(0, 100);
                if (enemyStats.characterAbilities.Count > 0 && randomChance < enemyStats.specialAbilityChance)
                {
                    abilityToUse = enemyStats.characterAbilities[0];
                }
                else
                {
                    abilityToUse = enemyStats.normalAttack;
                }

                if (abilityToUse != null)
                {
                    actionText = "Enemy uses " + abilityToUse.abilityName + "!";
                    abilityToUse.Execute(enemyStats, playerStats);
                }
                else
                {
                    actionText = "Enemy has no moves!";
                }
            }

            dialogueText.text = actionText;
            yield return new WaitForSeconds(2f);
        }

        battleHUDManager.UpdateStats();
        if (playerStats.currentHealth <= 0)
        {
            currentState = BattleState.LOST;
            StartCoroutine(EndBattleSequence());
        }
        else
        {
            ProcessStatusEffects(playerStats);
            if (playerStats.isStunned)
            {
                dialogueText.text = "Player is stunned and cannot act!";
                yield return new WaitForSeconds(2f);
                StartCoroutine(EnemyTurn());
            }
            else
            {
                currentState = BattleState.PLAYERTURN;
                PlayerTurn();
            }
        }
    }

    IEnumerator EndBattleSequence()
    {
        if (currentState == BattleState.WON)
        {
            dialogueText.text = "You won the battle!";
            if (bossRewardAbility != null && !playerStats.characterAbilities.Contains(bossRewardAbility))
            {
                playerStats.characterAbilities.Add(bossRewardAbility);
                dialogueText.text += "\nYou learned " + bossRewardAbility.abilityName + "!";
            }
            else if (currentRewardOnWin != null)
            {
                playerStats.AddPotion(currentRewardOnWin, 1);
                dialogueText.text += "\nYou received a " + currentRewardOnWin.name + "!";
            }
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
        screenTransition.StartTransition(false);
        bossRewardAbility = null;
        currentRewardOnWin = null;

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

    public GameObject GetCurrentEnemyInstance()
    {
        return currentEnemyInstance;
    }
}