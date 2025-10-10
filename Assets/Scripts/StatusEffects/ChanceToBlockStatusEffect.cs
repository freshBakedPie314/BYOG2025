using UnityEngine;

[System.Serializable]
public class ChanceToBlockStatusEffect : StatusEffect
{
    [Tooltip("0 - 100 chance")]
    public int blockChance;
}