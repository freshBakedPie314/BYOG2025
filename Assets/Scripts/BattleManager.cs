using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Cinemachine;
using TMPro;
using UnityEditor.Playables;

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
    private CameraShakeManager cameraShakeManager;

    [Header("Area Abilities")]
    public Ability fireAreaAbility;
    public Ability snowAreaAbility;
    public Ability earthAreaAbility;
    public Ability lightningAreaAbility;

    [Header("VFX")]
    public GameObject slashVFXfire;
    public GameObject slashVFXice;
    public GameObject slashVFXearth;
    public GameObject slashVFXthunder;
    public GameObject blockVFX;
    public GameObject burnVFX;
    public GameObject stunVFX;
    public GameObject frostVFX;
    public GameObject quakeVFX;
    public GameObject healVFX;
    public GameObject dmgVFX;
    public GameObject defVFX;
    public GameObject impactVFX;

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
    public GameObject ultimateBossPrefab;
    public Ability slashAttack;
    public Ability healAbility;

    private bool isAttackInProgress = false;
    public void Start()
    {
        battleHUDManager = battleHUD.GetComponent<BattleHUDManager>();
        cameraShakeManager = CameraShakeManager.Instance;
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

        player.transform.position = playerSpawnPoint.position;
        player.transform.rotation = playerSpawnPoint.rotation;

        Quaternion rot = Quaternion.Euler(enemySpawnPoint.rotation.x - 90, enemySpawnPoint.rotation.y, enemySpawnPoint.rotation.z);

        currentEnemyInstance = Instantiate(bossPrefab, enemySpawnPoint.position, rot);
        enemyStats = currentEnemyInstance.AddComponent<CharacterStats>();

        enemyStats.maxHealth = 200;
        enemyStats.currentHealth = 200;
        enemyStats.attackPower = 20;
        enemyStats.defense = 10;

        enemyStats.characterAbilities.Clear();
        enemyStats.characterAbilities.Add(skillToLearn);

        playerStats = player.GetComponent<CharacterStats>();

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
        isAttackInProgress = false; // Ensure player can always act at the start of their turn
        dialogueText.text = "Player's Turn. Choose your move.";
        battleHUDManager.UpdateActionButtons(playerStats);
        playerStats.PrintPotionInventory();
    }

    public void OnAbilityButton(Ability ability)
    {
        if (isAttackInProgress) return;
        isAttackInProgress = true;
        if (currentState != BattleState.PLAYERTURN)
        {
            isAttackInProgress = false; // Unlock if clicked at wrong time
            return;
        }
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
                isAttackInProgress = false; // Unlock controls if action fails
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

        //animation coroutine
        if (ability.abilityName == "Slash")
        {
            yield return MoveForwardRoutine(1);
        }
        else if (ability.abilityName == "Block")
        {
            GameObject blockfx = Instantiate(blockVFX, player.transform);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Damge Inc")
        {
            GameObject blockfx = Instantiate(dmgVFX, player.transform);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Defense Inc")
        {
            GameObject blockfx = Instantiate(defVFX, player.transform);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Heal")
        {
            GameObject blockfx = Instantiate(healVFX, player.transform);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Burn")
        {
            GameObject blockfx = Instantiate(burnVFX, enemyStats.gameObject.transform);
            cameraShakeManager.Shake(.8f);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Stun")
        {
            GameObject blockfx = Instantiate(stunVFX, enemyStats.gameObject.transform);
            cameraShakeManager.Shake(.8f);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Quake")
        {
            GameObject blockfx = Instantiate(quakeVFX, enemyStats.gameObject.transform);
            cameraShakeManager.Shake(.8f);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Frost")
        {
            GameObject blockfx = Instantiate(frostVFX, enemyStats.gameObject.transform);
            cameraShakeManager.Shake(.8f);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }

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

    // --- MODIFIED --- Changed from 'void' to 'IEnumerator' to allow for pauses
    IEnumerator ProcessStatusEffects(CharacterStats character)
    {
        if (character == null) yield break;

        // Loop backwards in case effects are removed from the list during iteration
        for (int i = character.activeStatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = character.activeStatusEffects[i];
            if (effect == null) continue;

            // --- ADDED --- Logic to display text for burn damage
            int healthBeforeTick = character.currentHealth;
            effect.OnTurnTick(character);

            if (!string.IsNullOrEmpty(effect.effectName) && effect.effectName.Contains("Burn") && character.currentHealth < healthBeforeTick)
            {
                string characterName = (character == playerStats) ? "Player" : "The enemy";
                dialogueText.text = $"{characterName} takes damage from the burn!";
                yield return new WaitForSeconds(1.5f); // Pause to let the player read the message
            }
            // --- END ADDED ---

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
        // --- MODIFIED --- Now calling this as a coroutine
        yield return StartCoroutine(ProcessStatusEffects(enemyStats));
        yield return new WaitForSeconds(1f);

        if (enemyStats.isStunned)
        {
            dialogueText.text = "Enemy is stunned and cannot act!";
            yield return new WaitForSeconds(2f);
        }
        else
        {
            // ... (rest of enemy attack logic is unchanged) ...
            Ability abilityToUse = null;
            string actionText = "";

            bool shouldConsiderHealing = enemyStats.healingAbility != null &&
                                         enemyStats.currentHealth < (enemyStats.maxHealth * enemyStats.healAtHealthPercent);

            if (shouldConsiderHealing && Random.Range(1, 101) <= 20)
            {
                abilityToUse = enemyStats.healingAbility;
                actionText = "Enemy heals itself!";
                GameObject blockfx = Instantiate(healVFX, enemyStats.gameObject.transform);
                yield return new WaitForSeconds(1.5f);
                Destroy(blockfx);
                abilityToUse.Execute(enemyStats, enemyStats);
            }
            else
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
                    if (abilityToUse.abilityName == "Slash")
                    {
                        yield return MoveForwardRoutine(-1);
                    }
                    else if (abilityToUse.abilityName == "Block")
                    {
                        GameObject blockfx = Instantiate(blockVFX, enemyStats.gameObject.transform);
                        yield return new WaitForSeconds(6f);
                        Destroy(blockfx);
                    }
                    // ... other ability animations
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

        isAttackInProgress = false; // Reset for the player's next turn

        battleHUDManager.UpdateStats();
        if (playerStats.currentHealth <= 0)
        {
            currentState = BattleState.LOST;
            StartCoroutine(EndBattleSequence());
        }
        else
        {
            // --- MODIFIED --- Also calling this as a coroutine
            yield return StartCoroutine(ProcessStatusEffects(playerStats));

            if (playerStats.isStunned)
            {
                dialogueText.text = "Player is stunned and cannot act!";
                yield return new WaitForSeconds(2f);
                StartCoroutine(EnemyTurn()); // Enemy gets another turn if player is stunned
            }
            else
            {
                currentState = BattleState.PLAYERTURN;
                PlayerTurn();
            }
        }
    }

    IEnumerator SpawnSlashVFX(int direction, Transform pawn, Transform opp)
    {
        // ... (this method is unchanged) ...
        PlayerController.PlayerColor color = player.GetComponent<PlayerController>().startArea;
        if (pawn.transform != player.transform)
        {
            color = player.GetComponentInParent<PlayerController>().currentArea;
        }
        Vector3 spAngle = new Vector3(0, direction * 90, 90);
        GameObject vfxToSpawn = slashVFXfire; // default
        switch (color)
        {
            case PlayerController.PlayerColor.Red: vfxToSpawn = slashVFXfire; break;
            case PlayerController.PlayerColor.Blue: vfxToSpawn = slashVFXice; break;
            case PlayerController.PlayerColor.Green: vfxToSpawn = slashVFXearth; break;
            default: vfxToSpawn = slashVFXthunder; break;
        }

        GameObject vfx = Instantiate(vfxToSpawn, pawn.position, Quaternion.Euler(spAngle));
        yield return new WaitForSeconds(0.1f);
        GameObject impact = Instantiate(impactVFX, opp);
        cameraShakeManager.Shake(0.8f);
        yield return new WaitForSeconds(0.5f);
        Destroy(impact);
        Destroy(vfx);
    }

    private IEnumerator MoveForwardRoutine(int direction)
    {
        // ... (this method is mostly unchanged) ...
        float moveDistance = 4f;
        float moveDuration = 0.5f;
        Transform pawn = (direction == 1) ? player.transform : enemyStats.gameObject.transform;
        Transform opp = (direction == 1) ? enemyStats.gameObject.transform : player.transform;

        Vector3 startPos = pawn.position;
        Vector3 endPos = startPos + pawn.right * direction * moveDistance;

        yield return MoveBetween(pawn, startPos, endPos, moveDuration);
        yield return SpawnSlashVFX(direction, pawn, opp);
        yield return MoveBetween(pawn, endPos, startPos, moveDuration);
    }

    private IEnumerator MoveBetween(Transform pawn, Vector3 from, Vector3 to, float duration)
    {
        // ... (this method is unchanged) ...
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // smoothstep ease
            pawn.position = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        pawn.position = to;
    }
    IEnumerator EndBattleSequence()
    {
        // ... (this method is unchanged) ...
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
        // ... (this method is unchanged) ...
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
        player.GetComponent<PlayerController>().canMove = true;
        currentState = BattleState.INACTIVE;
        spawner.SpawnNextNCells(gameManager.currentPathIndex);
    }

    public GameObject GetCurrentEnemyInstance()
    {
        return currentEnemyInstance;
    }

    public void StartUltimateBattle()
    {
        // ... (this method is unchanged) ...
        playerBoardPosition = player.transform.position;
        boardHUD.SetActive(false);
        board.SetActive(false);
        battleHUD.SetActive(true);
        battleArena.SetActive(true);
        player.transform.position = playerSpawnPoint.position;
        player.transform.rotation = playerSpawnPoint.rotation;
        Quaternion rot = Quaternion.Euler(enemySpawnPoint.rotation.x, enemySpawnPoint.rotation.y, enemySpawnPoint.rotation.z);
        currentEnemyInstance = Instantiate(ultimateBossPrefab, enemySpawnPoint.position, rot);
        enemyStats = currentEnemyInstance.AddComponent<CharacterStats>();
        enemyStats.maxHealth = 6969;
        enemyStats.currentHealth = 6969;
        enemyStats.attackPower = 69;
        enemyStats.defense = 69;
        enemyStats.characterAbilities.Clear();
        enemyStats.characterAbilities.Add(fireAreaAbility);
        enemyStats.characterAbilities.Add(snowAreaAbility);
        enemyStats.characterAbilities.Add(earthAreaAbility);
        enemyStats.characterAbilities.Add(lightningAreaAbility);
        enemyStats.normalAttack = slashAttack;
        enemyStats.healAtHealthPercent = 20;
        enemyStats.healingAbility = healAbility;
        playerStats = player.GetComponent<CharacterStats>();
        boardVCam.Priority = 5;
        battleVCam.Priority = 10;
        currentState = BattleState.STARTING;
        battleHUDManager.UpdateWarriors();
        battleHUDManager.UpdateStats();
        StartCoroutine(BattleSequence());
    }
}