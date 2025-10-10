using UnityEngine;

[CreateAssetMenu(fileName = "New Status Effect Ability", menuName = "Abilities/Status Effect Ability")]
public class StatusEffectAbility : Ability
{
    public enum EffectType { Burn, Stun, Vulnerable, Weaken }
    public EffectType effectType;
    public int effectDuration;
    public int effectPower; // Can be damage for burn, stat reduction, etc.

    public override void Execute(CharacterStats user, CharacterStats target)
    {
        StatusEffect effectToApply = null;

        switch (effectType)
        {
            case EffectType.Burn:
                effectToApply = new BurnEffect { duration = effectDuration, damageOverTime = effectPower };
                break;

            case EffectType.Stun:
                effectToApply = new StunEffect { duration = effectDuration };
                break;

            case EffectType.Vulnerable: // Increases damage taken
                effectToApply = new VulnerableEffect { duration = effectDuration, defenseReduction = effectPower };
                break;

            case EffectType.Weaken: // Decreases damage given
                effectToApply = new WeakenEffect { duration = effectDuration, attackReduction = effectPower };
                break;
        }

        if (effectToApply != null)
        {
            target.activeStatusEffects.Add(effectToApply);
            effectToApply.OnApply(target);
        }
    }
}