using UnityEngine;

[System.Serializable]
public class StatusEffect
{
    public string effectName;
    public int duration;

    public virtual void OnApply(CharacterStats target) { }
    public virtual void OnTurnTick(CharacterStats target)
    {
        duration--;
    }
    public virtual void OnRemove(CharacterStats target) { }
}