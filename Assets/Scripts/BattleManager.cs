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
    public enum AreaType { None, Fire, Snow, Earth, Lightning }
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

        currentEnemyInstance = Instantiate(bossPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);
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
        if(ability.abilityName == "Slash")
        {
            yield return MoveForwardRoutine(1);
            
        }
        else if(ability.abilityName == "Block")
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
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Stun")
        {
            GameObject blockfx = Instantiate(stunVFX, enemyStats.gameObject.transform);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Quake")
        {
            GameObject blockfx = Instantiate(quakeVFX, enemyStats.gameObject.transform);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Frost")
        {
            GameObject blockfx = Instantiate(frostVFX, enemyStats.gameObject.transform);
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
                GameObject blockfx = Instantiate(healVFX, enemyStats.gameObject.transform);
                yield return new WaitForSeconds(1.5f);
                Destroy(blockfx);
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
                    if (abilityToUse.abilityName == "Slash")
                    {
                        yield return MoveForwardRoutine(-1);
                        GameObject impact = Instantiate(impactVFX, enemyStats.gameObject.transform);
                        yield return new WaitForSeconds(0.5f);
                        Destroy(impact);
                    }
                    else if (abilityToUse.abilityName == "Block")
                    {
                        GameObject blockfx = Instantiate(blockVFX, enemyStats.gameObject.transform);
                        yield return new WaitForSeconds(6f);
                        Destroy(blockfx);
                    }
                    else if (abilityToUse.abilityName == "Damge Inc")
                    {
                        GameObject blockfx = Instantiate(dmgVFX, enemyStats.gameObject.transform);
                        yield return new WaitForSeconds(2.5f);
                        Destroy(blockfx);
                    }
                    else if (abilityToUse.abilityName == "Defense Inc")
                    {
                        GameObject blockfx = Instantiate(defVFX, enemyStats.gameObject.transform);
                        yield return new WaitForSeconds(2.5f);
                        Destroy(blockfx);
                    }
                    else if (abilityToUse.abilityName == "Heal")
                    {
                        GameObject blockfx = Instantiate(healVFX, enemyStats.gameObject.transform);
                        yield return new WaitForSeconds(2.5f);
                        Destroy(blockfx);
                    }
                    else if (abilityToUse.abilityName == "Burn")
                    {
                        GameObject blockfx = Instantiate(burnVFX, player.transform);
                        yield return new WaitForSeconds(2.5f);
                        Destroy(blockfx);
                    }
                    else if (abilityToUse.abilityName == "Stun")
                    {
                        GameObject blockfx = Instantiate(stunVFX, player.transform);
                        yield return new WaitForSeconds(2.5f);
                        Destroy(blockfx);
                    }
                    else if (abilityToUse.abilityName == "Quake")
                    {
                        GameObject blockfx = Instantiate(quakeVFX, player.transform);
                        yield return new WaitForSeconds(2.5f);
                        Destroy(blockfx);
                    }
                    else if (abilityToUse.abilityName == "Frost")
                    {
                        GameObject blockfx = Instantiate(frostVFX, player.transform);
                        yield return new WaitForSeconds(2.5f);
                        Destroy(blockfx);
                    }
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

    IEnumerator SpawnSlashVFX(int direction,Transform pawn,Transform opp)
    {
        PlayerController.PlayerColor color = player.GetComponent<PlayerController>().startArea;
        if(pawn.transform != player.transform)
        {
            color = player.GetComponentInParent<PlayerController>().currentArea;
        }
        Vector3 spAngle = new Vector3(0, direction*90, 90);
        switch (color)
        {
            case PlayerController.PlayerColor.Red:
                GameObject VFXred = Instantiate(slashVFXfire, pawn.position,Quaternion.Euler(spAngle));
                yield return new WaitForSeconds(0.1f);
                GameObject impact = Instantiate(impactVFX, opp);
                yield return new WaitForSeconds(0.5f);
                Destroy(impact);
                Destroy(VFXred);
                break;
            case PlayerController.PlayerColor.Blue:
                GameObject VFXblue = Instantiate(slashVFXice, pawn.position, Quaternion.Euler(spAngle));
                yield return new WaitForSeconds(0.1f);
                GameObject impact1 = Instantiate(impactVFX, opp);
                yield return new WaitForSeconds(0.5f);
                Destroy(impact1);
                Destroy(VFXblue);
                break;
            case PlayerController.PlayerColor.Green:
                GameObject VFXgreen = Instantiate(slashVFXearth, pawn.position, Quaternion.Euler(spAngle));
                yield return new WaitForSeconds(0.1f);
                GameObject impact2 = Instantiate(impactVFX, opp);
                yield return new WaitForSeconds(0.5f);
                Destroy(impact2);
                Destroy(VFXgreen);
                break;
            default:
                GameObject VFXyellow = Instantiate(slashVFXthunder, pawn.position, Quaternion.Euler(spAngle));
                yield return new WaitForSeconds(0.1f);
                GameObject impact3 = Instantiate(impactVFX, opp);
                yield return new WaitForSeconds(0.5f);
                Destroy(impact3);
                Destroy(VFXyellow);
                break;
        }
        
    }

    private IEnumerator MoveForwardRoutine(int direction)
    {
        float moveDistance = 4f;   // How far to move forward
        float moveDuration = 0.5f;   // How long to take to move forward/back
                                     // How long to wait before returning
        Transform pawn = player.transform;
        Transform opp = enemyStats.gameObject.transform;
        if(direction == 1)
        {
            pawn = player.transform;
            opp = enemyStats.gameObject.transform;
        }
        else if(direction == -1)
        {
            pawn = enemyStats.gameObject.transform;
            opp = player.transform;
        }
        Vector3 startPos = pawn.transform.position;
        Vector3 endPos = startPos + pawn.transform.right*direction * moveDistance;

        // 1️⃣ Move forward
        yield return MoveBetween(pawn,startPos, endPos, moveDuration);

        // 2️⃣ Wait at the new position
        yield return SpawnSlashVFX(direction,pawn,opp);

        // 3️⃣ Move back
        yield return MoveBetween(pawn, endPos, startPos, moveDuration);
    }

    private IEnumerator MoveBetween(Transform pawn,Vector3 from, Vector3 to, float duration)
    {
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