using TMPro;
using UnityEditor.UI;
using UnityEngine;

public class BattleHUDManager : MonoBehaviour
{
    GameObject player;
    GameObject enemy;
    public BattleManager battleManager;
    public TextMeshProUGUI playerHealthText;
    public TextMeshProUGUI enemyHealthText;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateWarriors()
    {
        player = battleManager.player;
        enemy = battleManager.GetCurrentEnemyInstance();
    }
    
    public void UpdateStats()
    {
        playerHealthText.text = player.GetComponent<CharacterStats>().currentHealth.ToString();
        enemyHealthText.text = enemy.GetComponent<CharacterStats>().currentHealth.ToString();
    }
}
