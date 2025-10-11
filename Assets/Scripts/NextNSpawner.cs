// NextNSpawner.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NextNSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    public int diceFace = 6;
    [Tooltip("The minimum number of Ally cells that will be spawned.")]
    [Range(0, 6)]
    public int minAllyCells = 2;

    [Header("Cell Prefabs")]
    public GameObject enemyCellPrefab;
    public GameObject allyCellPrefab;

    [Header("Possible Rewards")]
    [Tooltip("Drag all possible potion ability assets here.")]
    public List<Ability> possiblePotionRewards;

    [Header("References")]
    public GameManager gameManager;

    private List<GameObject> spawnedReferences = new List<GameObject>();

    public void SpawnNextNCells(int playerCellIndex)
    {
        // 1. Clear old cells
        foreach (GameObject go in spawnedReferences)
        {
            if (go != null) Destroy(go);
        }
        spawnedReferences.Clear();

        // 2. Generate a random spawn plan
        List<CellData> spawnPlan = GenerateSpawnPlan();

        // 3. Loop through the plan and spawn the cells
        for (int i = 0; i < diceFace; i++)
        {
            CellData dataForThisCell = spawnPlan[i];
            GameObject prefabToSpawn = (dataForThisCell.type == CellType.Ally) ? allyCellPrefab : enemyCellPrefab;

            if (prefabToSpawn != null)
            {
                int pathIndex = Mathf.Min(playerCellIndex + i + 1, gameManager.currentPath.Length - 1);
                Vector3 spawnPosition = gameManager.currentPath[pathIndex];
                GameObject spawnedCellObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                dataForThisCell.pos = spawnPosition;
                spawnedCellObject.GetComponent<BoardCell>().Initialize(dataForThisCell);

                spawnedReferences.Add(spawnedCellObject);
            }
        }
    }

    private List<CellData> GenerateSpawnPlan()
    {
        List<CellData> plan = new List<CellData>();

        var buffTypes = new List<PermaBuffType> { PermaBuffType.Attack, PermaBuffType.Defense, PermaBuffType.Health };

        for (int i = 0; i < minAllyCells; i++)
        {
            plan.Add(new CellData
            {
                type = CellType.Ally,
                buffType = buffTypes[Random.Range(0, buffTypes.Count)]
            });
        }

        int remainingCells = diceFace - minAllyCells;
        for (int i = 0; i < remainingCells; i++)
        {
            plan.Add(new CellData
            {
                type = CellType.Enemy,
                potionReward = possiblePotionRewards[Random.Range(0, possiblePotionRewards.Count)]
            });
        }

        return plan.OrderBy(x => Random.value).ToList();
    }
}