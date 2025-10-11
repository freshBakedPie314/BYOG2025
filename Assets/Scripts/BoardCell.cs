using System;
using System.Collections.Generic;
using UnityEngine;

public enum CellType { Ally, Enemy }
public enum PermaBuffType { Attack, Defense, Health }

[System.Serializable]
public class CellData
{
    public CellType type;
    public Ability potionReward;
    public PermaBuffType buffType;
    public Vector3 pos;
}

public class BoardCell : MonoBehaviour
{
    private CellData cellData;
    private float omega;
    private float t; //animation parameter
    Vector3 initPosition;

    public void Initialize(CellData data)
    {
        this.cellData = data;
        DisplayReward();
        transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 10f), 0);
        omega = Random.Range(0.1f, 0.3f);
        t = Random.Range(0, Mathf.PI);
        initPosition = transform.position;
    }

    void Update()
    {
        transform.rotation *= Quaternion.Euler(0, omega, 0);
        transform.position = initPosition + 0.3*Math.Sin(t)
    }

    public CellData GetData()
    {
        return cellData;
    }

    public Dictionary<string, Sprite> potionSprites;
    public Sprite[] buffSprites;

    void DisplayReward()
    {
        if (cellData.type == CellType.Ally)
        {
            // TODO: Show an icon for Attack, Defense, or Health buff.
            Debug.Log("Spawning Ally Cell with reward: " + cellData.buffType);
            transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = buffSprites[(int)cellData.buffType];
        }
        else if (cellData.type == CellType.Enemy)
        {
            // TODO: Show an icon for the specific potion reward.
            Debug.Log("Spawning Enemy Cell with reward: " + cellData.potionReward.name);
            //transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = potionSprites[cellData.potionReward.name];
        }
    }


}