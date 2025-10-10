using UnityEngine;

[CreateAssetMenu(fileName = "New Heal Potion", menuName = "Abilities/Heal")]
public class Heal : Ability
{
    public override void Execute(CharacterStats user, CharacterStats target)
    {
        target.Heal(power);
    }
}