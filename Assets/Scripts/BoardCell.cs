using UnityEngine;

public enum CellType { Ally, Enemy }
public enum PermaBuffType { None, Attack, Defense, Health }

[System.Serializable]
public class CellData
{
    public CellType type;
    public Ability potionReward; 
    public PermaBuffType buffType;
}

public class BoardCell : MonoBehaviour
{
    private CellData cellData;

    public void Initialize(CellData data)
    {
        this.cellData = data;
        DisplayReward();
    }

    public CellData GetData()
    {
        return cellData;
    }

    void DisplayReward()
    {
        if (cellData.type == CellType.Ally)
        {
            // TODO: Show an icon for Attack, Defense, or Health buff.
            Debug.Log("Spawning Ally Cell with reward: " + cellData.buffType);
        }
        else if (cellData.type == CellType.Enemy)
        {
            // TODO: Show an icon for the specific potion reward.
            Debug.Log("Spawning Enemy Cell with reward: " + cellData.potionReward.name);
        }
    }
}