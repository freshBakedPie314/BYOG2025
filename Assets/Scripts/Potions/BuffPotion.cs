using UnityEngine;

[CreateAssetMenu(fileName = "New Buff Potion", menuName = "Abilities/Buff Potion")]
public class BuffPotionAbility : Ability
{
    public enum BuffType { Attack, Defense }
    public BuffType buffType;
    public int duration;

    public override void Execute(CharacterStats user, CharacterStats target)
    {
        if (buffType == BuffType.Attack)
        {
            AttackUpStatusEffect effect = new AttackUpStatusEffect();
            effect.duration = this.duration;
            effect.attackIncrease = this.power;
            target.activeStatusEffects.Add(effect);
            effect.OnApply(target);
        }
        else if (buffType == BuffType.Defense)
        {
            DefenseUpStatusEffect effect = new DefenseUpStatusEffect();
            effect.duration = this.duration;
            effect.defenseIncrease = this.power;
            target.activeStatusEffects.Add(effect);
            effect.OnApply(target);
        }
    }
}
