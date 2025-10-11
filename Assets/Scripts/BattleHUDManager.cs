using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BattleHUDManager : MonoBehaviour
{
    [Header("Health Displays")]
    public TextMeshProUGUI playerHealthText;
    public TextMeshProUGUI enemyHealthText;

    [Header("Action Buttons")]
    [Tooltip("Drag all of your action buttons (Stun, Heal, etc.) here.")]
    public List<ActionButtonUI> actionButtons;

    // References
    private GameObject player;
    private GameObject enemy;
    public BattleManager battleManager;

    void Start()
    {
        foreach (ActionButtonUI button in actionButtons)
        {
            button.Initialize(battleManager);
        }
    }

    public void UpdateActionButtons(CharacterStats playerStats)
    {
        foreach (ActionButtonUI button in actionButtons)
        {
            button.UpdateState(playerStats);
        }
    }

    public void UpdateWarriors()
    {
        player = battleManager.player;
        enemy = battleManager.GetCurrentEnemyInstance();
    }

    public void UpdateStats()
    {
        if (player != null)
            playerHealthText.text = player.GetComponent<CharacterStats>().currentHealth.ToString();
        if (enemy != null)
            enemyHealthText.text = enemy.GetComponent<CharacterStats>().currentHealth.ToString();
    }
}