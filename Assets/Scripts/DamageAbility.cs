// DamageAbility.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Damage Ability", menuName = "Abilities/Damage Ability")]
public class DamageAbility : Ability
{
    public override void Execute(CharacterStats user, CharacterStats target)
    {
        target.TakeDamage(user.attackPower + power);
    }
}