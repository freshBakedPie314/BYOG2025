using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Abilities/Ability")]
public class Ability : ScriptableObject
{
    public string abilityName;
    public int power;
    public enum TargetType { Enemy, Self }
    public TargetType targetType;

    public virtual void Execute(CharacterStats user, CharacterStats target)
    {
        Debug.Log(user.name + " used " + abilityName + " on " + target.name);
    }
}