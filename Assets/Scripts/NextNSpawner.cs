using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class NextNSpawner : MonoBehaviour
{
    public int diceFace = 6;
    List<GameObject> spawnedReferences = new List<GameObject>();
    public GameManager gameManager;
    public GameObject temp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnNextNCells(int cellIndex)
    {
        foreach(GameObject gameObject in spawnedReferences)
        {
            if(gameObject != null) Destroy(gameObject);
        }
        spawnedReferences.Clear();
        for(int i = 1; i <= diceFace; i++)
        {
            Vector3 spawnPosition = gameManager.currentPath[Mathf.Min(cellIndex + i , gameManager.currentPath.Length-1)];
            GameObject go = Instantiate(temp, spawnPosition, Quaternion.identity);
            spawnedReferences.Add(go);
        }
    }
}
