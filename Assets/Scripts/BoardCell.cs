using System.Collections.Generic;
using System.Linq;
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
    private float anim_omega;
    private readonly float anim_amplitude = 0.3f;
    Vector3 initPosition;

    public void Initialize(CellData data)
    {
        this.cellData = data;
        DisplayReward();
        transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 10f), 0);
        omega = 50 * Random.Range(0.9f, 1.5f);
        t = Random.Range(0, Mathf.PI);
        initPosition = transform.position;
        anim_omega = Random.Range(0.9f, 1.5f);
    }

    void Update()
    {
        transform.rotation *= Quaternion.Euler(0, omega * Time.deltaTime, 0);
        transform.position = initPosition + anim_amplitude * Mathf.Sin(anim_omega*t) * Vector3.up;

        t += Time.deltaTime;
    }

    public CellData GetData()
    {
        return cellData;
    }

    public Sprite[] potionSprites;
    public Sprite[] buffSprites;
    List<string> buffOrder = new List<string> { "DamageInc Potion", "DefenseInc Potion", "Heal Potion" };

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
            foreach(var t in  buffOrder)
            {
                print(t);
            }
            int id = buffOrder.IndexOf(cellData.potionReward.name);
            print(cellData.potionReward.name + " " + id.ToString() + " " + buffOrder.ToArray().ToString());
            transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = potionSprites[id];
            // TODO: Show an icon for the specific potion reward.
            Debug.Log("Spawning Enemy Cell with reward: " + cellData.potionReward.name);
            //transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = potionSprites[cellData.potionReward.name];
        }
    }


}