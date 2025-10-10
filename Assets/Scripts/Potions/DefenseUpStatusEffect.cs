using UnityEngine;

[System.Serializable]
public class DefenseUpStatusEffect : StatusEffect
{
    public int defenseIncrease;
    public override void OnApply(CharacterStats target)
    {
        target.defense += defenseIncrease;
    }

    public override void OnRemove(CharacterStats target)
    {
        target.defense -= defenseIncrease;
    }
}
