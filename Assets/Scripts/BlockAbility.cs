using UnityEngine;

[CreateAssetMenu(fileName = "New Block Ability", menuName = "Abilities/Block Ability")]
public class BlockAbility : Ability
{
    public override void Execute(CharacterStats user, CharacterStats target)
    {
        ChanceToBlockStatusEffect effect = new ChanceToBlockStatusEffect();

        effect.blockChance = this.power;

        effect.duration = 2;

        user.activeStatusEffects.Add(effect);
        effect.OnApply(user);

        Debug.Log("APPLIED EFFECT: Added ChanceToBlockStatusEffect to " + user.name + ". Chance: " + effect.blockChance + "%");
    }
}