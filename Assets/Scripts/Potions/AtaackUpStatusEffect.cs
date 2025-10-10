[System.Serializable]
public class AttackUpStatusEffect : StatusEffect
{
    public int attackIncrease;

    public override void OnApply(CharacterStats target)
    {
        target.attackPower += attackIncrease;
    }

    public override void OnRemove(CharacterStats target)
    {
        target.attackPower -= attackIncrease;
    }
}