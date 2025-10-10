[System.Serializable]
public class WeakenEffect : StatusEffect
{
    public int attackReduction;

    public override void OnApply(CharacterStats target)
    {
        target.attackPower -= attackReduction;
    }

    public override void OnRemove(CharacterStats target)
    {
        target.attackPower += attackReduction;
    }
}