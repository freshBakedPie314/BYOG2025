// BurnEffect.cs
using UnityEngine;

[System.Serializable]
public class BurnEffect : StatusEffect
{
    public int damageOverTime;

    public override void OnTurnTick(CharacterStats target)
    {
        base.OnTurnTick(target); // This calls duration-- from the base class
        target.TakeDamage(damageOverTime);
        Debug.Log(target.name + " is burning!");
    }
}