[System.Serializable]
public class VulnerableEffect : StatusEffect
{
    public int defenseReduction;

    public override void OnApply(CharacterStats target)
    {
        target.defense -= defenseReduction;
    }

    public override void OnRemove(CharacterStats target)
    {
        target.defense += defenseReduction;
    }
}