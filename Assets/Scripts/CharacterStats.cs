using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Playables;

public class CharacterStats : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;

    public int attackPower = 10;
    public int defense = 5;

    public List<Ability> characterAbilities = new List<Ability>();
    public List<StatusEffect> activeStatusEffects = new List<StatusEffect>();

    [Header("AI Configuration")]
    public Ability normalAttack;

    [Range(0, 100)]
    public int specialAbilityChance = 50;

    public Ability healingAbility;
    [Range(0.1f, 1f)]
    public float healAtHealthPercent = 0.3f;

    public bool isStunned = false;
    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        Debug.Log("TAKE DAMAGE CALLED on " + name + ". Checking for block... Active effects: " + activeStatusEffects.Count);
        ChanceToBlockStatusEffect blockEffect = activeStatusEffects.Find(e => e is ChanceToBlockStatusEffect) as ChanceToBlockStatusEffect;
        if (blockEffect != null)
        {
            activeStatusEffects.Remove(blockEffect);

            int roll = Random.Range(1, 101);

            if (roll <= blockEffect.blockChance)
            {
                Debug.Log(transform.name + " BLOCKED the attack! (Rolled " + roll + " vs " + blockEffect.blockChance + "%)");
                return;
            }
            else
            {
                Debug.Log(transform.name + " failed to block. (Rolled " + roll + " vs " + blockEffect.blockChance + "%)");
            }
        }

        int damageToTake = Mathf.Max(damage - defense, 1);
        currentHealth -= damageToTake;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        Debug.Log(transform.name + " takes " + damageToTake + " damage.");
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
}