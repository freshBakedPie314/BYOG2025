using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public Vector3[] currentPath;
    public int currentPathIndex = 0;
    [SerializeField] private NextNSpawner nextNSpawner;

    [Header("UI References")]
    public TextMeshProUGUI healNo;
    public TextMeshProUGUI DmgNo;
    public TextMeshProUGUI DefNo;

    [Header("Data References")]
    public CharacterStats player;
    // Assign your 3 potion Ability assets here in the inspector
    public List<Ability> potionTypes = new List<Ability>();

    void Update()
    {
        if (player == null || potionTypes.Count < 3)
        {
            return;
        }

        Ability healPotion = potionTypes[0];
        Ability dmgPotion = potionTypes[1];
        Ability defPotion = potionTypes[2];

        int healCount = 0;
        if (player.potions.ContainsKey(healPotion))
        {
            healCount = player.potions[healPotion];
        }

        int dmgCount = 0;
        if (player.potions.ContainsKey(dmgPotion))
        {
            dmgCount = player.potions[dmgPotion];
        }

        int defCount = 0;
        if (player.potions.ContainsKey(defPotion))
        {
            defCount = player.potions[defPotion];
        }

        healNo.text = healCount.ToString();
        DmgNo.text = dmgCount.ToString();
        DefNo.text = defCount.ToString();
    }
}