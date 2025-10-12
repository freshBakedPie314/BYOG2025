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

    [Header("Arena Prefabs")]
    public GameObject FireArena;
    public GameObject IceArena;
    public GameObject ThunderARena;
    public GameObject EarthArena;

    [Header("HUDs & UI")]
    public GameObject boardHUD;
    public GameObject battleHUD;
    public TextMeshProUGUI dialogueText;

    [Header("Manager")]
    public NextNSpawner spawner;
    public GameManager gameManager;
    private BattleHUDManager battleHUDManager;
    private CameraShakeManager cameraShakeManager; // Re-enabled this

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
    private AreaType currentBattleArea;

    private GameObject currentEnemyInstance;
    private CharacterStats playerStats;
    private CharacterStats enemyStats;
    public Vector3 playerBoardPosition;
    private Ability currentRewardOnWin;

    public ScreenTransition screenTransition;
    public GameObject ultimateBossPrefab;
    public Ability slashAttack;
    public Ability healAbility;

    private bool isAttackInProgress = false;
    private static bool isSpawningVFX = false;

    public void Start()
    {
        battleHUDManager = battleHUD.GetComponent<BattleHUDManager>();
        cameraShakeManager = CameraShakeManager.Instance; // Re-enabled this
    }

    // This is for NORMAL enemy battles
    public void StartBattle(AreaType area, Ability rewardOnWin)
    {
        currentBattleArea = area;
        currentRewardOnWin = rewardOnWin;
        playerBoardPosition = player.transform.position;

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
        if (skillToLearn == fireAreaAbility) currentBattleArea = AreaType.Fire;
        else if (skillToLearn == snowAreaAbility) currentBattleArea = AreaType.Snow;
        else if (skillToLearn == earthAreaAbility) currentBattleArea = AreaType.Earth;
        else if (skillToLearn == lightningAreaAbility) currentBattleArea = AreaType.Lightning;

        bossRewardAbility = skillToLearn;
        playerBoardPosition = player.transform.position;

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
        // --- MODIFIED --- Now waits for the transition to finish
        yield return screenTransition.StartTransition(true);

        // This code will now run AFTER the transition has finished and the screen is visible
        ActivateArena();
        dialogueText.text = "A wild " + currentEnemyInstance.name.Replace("(Clone)", "") + " appears!";
        yield return new WaitForSeconds(2f);
        currentState = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    void ActivateArena()
    {
        // This logic is now part of the screen transition itself
    }

    void PlayerTurn()
    {
        isAttackInProgress = false;
        dialogueText.text = "Player's Turn. Choose your move.";
        battleHUDManager.UpdateActionButtons(playerStats);
        playerStats.PrintPotionInventory();
    }

    public void OnAbilityButton(Ability ability)
    {
        if (isAttackInProgress) return;
        isAttackInProgress = true;
        if (currentState != BattleState.PLAYERTURN) { isAttackInProgress = false; return; }
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
                isAttackInProgress = false;
                yield break;
            }
        }
        else
        {
            if (ability.targetType == Ability.TargetType.Self) ability.Execute(playerStats, playerStats);
            else ability.Execute(playerStats, enemyStats);
        }

        if (ability.abilityName == "Slash")
        {
            yield return MoveForwardRoutine(1);
        }
        else if (ability.abilityName == "Block")
        {
            GameObject vfx = Instantiate(blockVFX, player.transform);
            yield return new WaitForSeconds(2.5f); Destroy(vfx);
        }
        else if (ability.abilityName == "Burn" || ability.abilityName == "Stun" || ability.abilityName == "Frost")
        {
            GameObject vfxPrefab = (ability.abilityName == "Stun") ? stunVFX : (ability.abilityName == "Frost") ? frostVFX : burnVFX;
            GameObject vfx = Instantiate(vfxPrefab, enemyStats.gameObject.transform);
            if (cameraShakeManager != null) cameraShakeManager.Shake(5f);
            yield return new WaitForSeconds(2.5f); Destroy(vfx);
        }
        else if (ability.abilityName == "Quake")
        {
            GameObject vfx = Instantiate(quakeVFX, enemyStats.gameObject.transform);
            if (cameraShakeManager != null) cameraShakeManager.Shake(8f);
            yield return new WaitForSeconds(2.5f); Destroy(vfx);
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

    IEnumerator ProcessStatusEffects(CharacterStats character)
    {
        if (character == null) yield break;
        for (int i = character.activeStatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = character.activeStatusEffects[i];
            if (effect == null) continue;
            int healthBeforeTick = character.currentHealth;
            effect.OnTurnTick(character);
            if (!string.IsNullOrEmpty(effect.effectName) && effect.effectName.Contains("Burn") && character.currentHealth < healthBeforeTick)
            {
                dialogueText.text = $"{character.name} takes damage from the burn!";
                yield return new WaitForSeconds(1.5f);
            }
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
        yield return StartCoroutine(ProcessStatusEffects(enemyStats));
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
            bool shouldConsiderHealing = enemyStats.healingAbility != null && (enemyStats.currentHealth < (enemyStats.maxHealth * enemyStats.healAtHealthPercent));
            if (shouldConsiderHealing && Random.Range(1, 101) <= 20)
            {
                abilityToUse = enemyStats.healingAbility;
                actionText = "Enemy heals itself!";
                GameObject vfx = Instantiate(healVFX, enemyStats.gameObject.transform);
                yield return new WaitForSeconds(1.5f); Destroy(vfx);
                abilityToUse.Execute(enemyStats, enemyStats);
            }
            else
            {
                abilityToUse = (enemyStats.characterAbilities.Count > 0 && Random.Range(0, 100) < enemyStats.specialAbilityChance) ? enemyStats.characterAbilities[0] : enemyStats.normalAttack;
                if (abilityToUse != null)
                {
                    actionText = "Enemy uses " + abilityToUse.abilityName + "!";
                    if (abilityToUse.abilityName == "Slash")
                    {
                        yield return MoveForwardRoutine(-1);
                    }
                    else if (abilityToUse.abilityName == "Burn" || abilityToUse.abilityName == "Stun" || abilityToUse.abilityName == "Frost")
                    {
                        GameObject vfxPrefab = (abilityToUse.abilityName == "Stun") ? stunVFX : (abilityToUse.abilityName == "Frost") ? frostVFX : burnVFX;
                        GameObject vfx = Instantiate(vfxPrefab, player.transform);
                        if (cameraShakeManager != null) cameraShakeManager.Shake(5f);
                        yield return new WaitForSeconds(2.5f); Destroy(vfx);
                    }
                    else if (abilityToUse.abilityName == "Quake")
                    {
                        GameObject vfx = Instantiate(quakeVFX, player.transform);
                        if (cameraShakeManager != null) cameraShakeManager.Shake(8f);
                        yield return new WaitForSeconds(2.5f); Destroy(vfx);
                    }
                    abilityToUse.Execute(enemyStats, playerStats);
                }
                else { actionText = "Enemy has no moves!"; }
            }
            dialogueText.text = actionText;
            yield return new WaitForSeconds(2f);
        }
        isAttackInProgress = false;
        battleHUDManager.UpdateStats();
        if (playerStats.currentHealth <= 0)
        {
            currentState = BattleState.LOST;
            StartCoroutine(EndBattleSequence());
        }
        else
        {
            yield return StartCoroutine(ProcessStatusEffects(playerStats));
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

    IEnumerator SpawnSlashVFX(int direction, Transform pawn, Transform opp)
    {
        if (isSpawningVFX) yield break;
        isSpawningVFX = true;

        PlayerController.PlayerColor color;
        switch (currentBattleArea)
        {
            case AreaType.Fire: color = PlayerController.PlayerColor.Red; break;
            case AreaType.Snow: color = PlayerController.PlayerColor.Blue; break;
            case AreaType.Earth: color = PlayerController.PlayerColor.Green; break;
            case AreaType.Lightning: color = PlayerController.PlayerColor.Yellow; break;
            default: color = player.GetComponent<PlayerController>().startArea; break;
        }

        Vector3 spAngle = new Vector3(0, direction * 90, 90);
        GameObject vfxToSpawn = slashVFXfire;
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
        if (cameraShakeManager != null) cameraShakeManager.Shake(5f);
        yield return new WaitForSeconds(0.5f);
        Destroy(impact);
        Destroy(vfx);

        isSpawningVFX = false;
    }

    private IEnumerator MoveForwardRoutine(int direction)
    {
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
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
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

        // --- MODIFIED --- Now waits for the transition to finish
        yield return screenTransition.StartTransition(false);
        EndBattle();
    }

    public void EndBattle()
    {
        bossRewardAbility = null;
        currentRewardOnWin = null;
        if (currentEnemyInstance != null) Destroy(currentEnemyInstance);
        if (player != null) player.GetComponent<PlayerController>().canMove = true;
        currentState = BattleState.INACTIVE;
        spawner.SpawnNextNCells(gameManager.currentPathIndex);
    }

    public GameObject GetCurrentEnemyInstance() { return currentEnemyInstance; }

    public void StartUltimateBattle()
    {
        currentBattleArea = AreaType.Fire;
        playerBoardPosition = player.transform.position;

        StartCoroutine(BattleSequence());
    }
}

