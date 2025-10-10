[System.Serializable]
public class StunEffect : StatusEffect
{
    public override void OnApply(CharacterStats target)
    {
        target.isStunned = true;
    }

    public override void OnRemove(CharacterStats target)
    {
        target.isStunned = false;
    }
}