using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Cinemachine;
using TMPro;
using UnityEngine.SceneManagement;

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
    public GameObject BossArena;

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
    public bool isUltimateBattle = false;
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
    private AudioSource audioPlayer;
    public bool isSpawningVFX = false;
    public bool inBossBattle = false;
    public void Start()
    {
        battleHUDManager = battleHUD.GetComponent<BattleHUDManager>();
        cameraShakeManager = CameraShakeManager.Instance;
        audioPlayer = GetComponent<AudioSource>();
    }

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

        enemyStats.maxHealth = 50;
        enemyStats.currentHealth = 50;
        enemyStats.attackPower = 12;
        enemyStats.defense = 5;

        enemyStats.characterAbilities.Clear();
        enemyStats.characterAbilities.Add(skillToLearn);
        enemyStats.normalAttack = slashAttack;
        playerStats = player.GetComponent<CharacterStats>();

        currentState = BattleState.STARTING;
        battleHUDManager.UpdateWarriors();
        battleHUDManager.UpdateStats();
        StartCoroutine(BattleSequence());
    }

    IEnumerator BattleSequence()
    {
        ActivateArena();
        yield return screenTransition.StartTransition(true);

        
        dialogueText.text = "A wild " + currentEnemyInstance.name.Replace("(Clone)", "") + " appears!";
        yield return new WaitForSeconds(1f); // Changed from 2f to 1f
        currentState = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    void ActivateArena()
    {
        FireArena.SetActive(false);
        IceArena.SetActive(false);
        ThunderARena.SetActive(false);
        EarthArena.SetActive(false);
        BossArena.SetActive(false);

        if (!inBossBattle)
        {
            switch (currentBattleArea)
            {
                case AreaType.Fire:
                    FireArena.SetActive(true);
                    break;
                case AreaType.Earth:
                    EarthArena.SetActive(true);
                    break;
                case AreaType.Snow:
                    IceArena.SetActive(true);
                    break;
                case AreaType.Lightning:
                    ThunderARena.SetActive(true);
                    break;
            }
        } else {
            BossArena.SetActive(true);
        }
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
            yield return MoveForwardRoutine(1, ability);
        }
        else if (ability.abilityName == "Block")
        {
            GameObject blockfx = Instantiate(blockVFX, player.transform);
            playSFX(ability);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Burn" )
        {
            GameObject blockfx = Instantiate(burnVFX, enemyStats.gameObject.transform);
            playSFX(ability);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Defense Inc")
        {
            GameObject blockfx = Instantiate(defVFX, player.transform);
            playSFX(ability);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Heal")
        {
            GameObject blockfx = Instantiate(healVFX, player.transform);
            playSFX(ability);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Frost")
        {
            GameObject blockfx = Instantiate(frostVFX, enemyStats.gameObject.transform);
            cameraShakeManager.Shake(.8f);
            playSFX(ability);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Stun")
        {
            GameObject blockfx = Instantiate(stunVFX, enemyStats.gameObject.transform);
            cameraShakeManager.Shake(.8f);
            playSFX(ability);
            yield return new WaitForSeconds(2.5f);
            Destroy(blockfx);
        }
        else if (ability.abilityName == "Quake")
        {
            GameObject blockfx = Instantiate(quakeVFX, enemyStats.gameObject.transform);
            cameraShakeManager.Shake(.8f);
            playSFX(ability);
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
                GameObject blockfx = Instantiate(healVFX, enemyStats.gameObject.transform);
                playSFX(abilityToUse);
                yield return new WaitForSeconds(1.5f);
                Destroy(blockfx);
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
                        yield return MoveForwardRoutine(-1, abilityToUse);
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
                    if (abilityToUse.abilityName != "Slash") playSFX(abilityToUse);
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

    IEnumerator SpawnSlashVFXnSFX(int direction, Transform pawn, Transform opp, Ability ability)
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
        playSFX(ability);
        yield return new WaitForSeconds(0.15f);
        GameObject impact = Instantiate(impactVFX, opp);
        if (cameraShakeManager != null) cameraShakeManager.Shake(5f);
        yield return new WaitForSeconds(0.5f);
        Destroy(impact);
        Destroy(vfx);

        isSpawningVFX = false;
    }

    private IEnumerator MoveForwardRoutine(int direction, Ability ability)
    {
        float moveDistance = 4f;
        float moveDuration = 0.5f;
        Transform pawn = (direction == 1) ? player.transform : enemyStats.gameObject.transform;
        Transform opp = (direction == 1) ? enemyStats.gameObject.transform : player.transform;
        Vector3 startPos = pawn.position;
        Vector3 endPos = startPos + pawn.right * direction * moveDistance;
        yield return MoveBetween(pawn, startPos, endPos, moveDuration);
        yield return SpawnSlashVFXnSFX(direction, pawn, opp, ability);
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
            yield return new WaitForSeconds(2f);
            SceneManager.LoadScene(2);
        }
        yield return new WaitForSeconds(3f);

        yield return screenTransition.StartTransition(false);
        EndBattle();
    }

    public void EndBattle()
    {
        bossRewardAbility = null;
        currentRewardOnWin = null;
        if (currentEnemyInstance != null) Destroy(currentEnemyInstance);
        if (player != null) player.GetComponent<PlayerController>().canMove = true;
        if (isUltimateBattle)
        {
            SceneManager.LoadScene(3);
        }

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

    public GameObject GetCurrentEnemyInstance() { return currentEnemyInstance; }

    public void StartUltimateBattle()
    {
        inBossBattle = true;
        currentBattleArea = AreaType.Fire;
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
        enemyStats.maxHealth = 100;
        enemyStats.currentHealth = 100;
        enemyStats.attackPower = 24;
        enemyStats.defense = 12;
        enemyStats.characterAbilities.Clear();
        enemyStats.characterAbilities.Add(fireAreaAbility);
        enemyStats.characterAbilities.Add(snowAreaAbility);
        enemyStats.characterAbilities.Add(earthAreaAbility);
        enemyStats.characterAbilities.Add(lightningAreaAbility);
        enemyStats.normalAttack = slashAttack;
        enemyStats.healAtHealthPercent = 20;
        enemyStats.healingAbility = healAbility;
        playerStats = player.GetComponent<CharacterStats>();
        currentState = BattleState.STARTING;
        battleHUDManager.UpdateWarriors();
        battleHUDManager.UpdateStats();
        isUltimateBattle = true;
        StartCoroutine(BattleSequence());
    }

    private void playSFX(Ability ability)
    {
        AudioClip audio = ability.sfx;
        audioPlayer.clip = audio;
        audioPlayer.pitch = Random.Range(0.8f, 1.2f);
        audioPlayer.Play();
    }
}

