using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TMPro;
using System.Security.Cryptography;
using UnityEngine.Rendering;
using static BattleManager;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;

    //This dic holds generated paths for each class
    private Dictionary<PlayerColor, Vector3[]> paths = new Dictionary<PlayerColor, Vector3[]>();

    private Vector3[] currentPath;
    private int currentPathIndex = 0;
    private bool isMoving = false;
    public PlayerColor currentArea = PlayerColor.Red;
    private PlayerColor startArea = PlayerColor.Red;
    private PlayerColor[] areas = new PlayerColor[] { PlayerColor.Red, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Blue };

    [SerializeField] private GameManager gameManager;
    [SerializeField] private NextNSpawner nextNSpawner;
    [SerializeField] private BattleManager battleManager;

    [SerializeField] private TextMeshProUGUI textMeshProUGUI;

    [Header("Physics")]
    public LayerMask boardCellLayer;

    [Header("Player Prefabs")]
    public List<GameObject> playerPrefabs = new List<GameObject>();

    [Header("Managers")]
    public NextNSpawner spawner;

    [Header("UI")]
    public GameObject chohiceMenu;
    public enum PlayerColor { Red, Green, Blue, Yellow }
    private BoardCell currentLandedCell;
    void Awake()
    {
        GenerateAllPaths();
    }

    void Start()
    {
        //SetPlayerColor(PlayerColor.Yellow);
        
    }

    public void StartGame(int col_id)
    {
        PlayerColor colour = PlayerColor.Red;
        switch (col_id)
        {
            case 0:
                Instantiate(playerPrefabs[0] , gameObject.transform);
                colour = PlayerColor.Red;
                break;
            case 1:
                Instantiate(playerPrefabs[1], gameObject.transform);
                colour = PlayerColor.Blue;
                break;
            case 2:
                Instantiate(playerPrefabs[2], gameObject.transform);
                colour = PlayerColor.Yellow;
                break;
            default:
                Instantiate(playerPrefabs[3], gameObject.transform);
                colour = PlayerColor.Green;
                break;
        }
        //
        SetPlayerColor(colour);
        gameManager.currentPath = currentPath;
        gameManager.currentPathIndex = currentPathIndex;


        nextNSpawner.SpawnNextNCells(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isMoving && currentPath != null)
        {
            StartCoroutine(AnimateAndMove());
        }
    }


    public void SetPlayerColor(PlayerColor color)
    {
        //
        if (!paths.ContainsKey(color))
        {
            Debug.LogError("Path for " + color + " not generated!");
            return;
        }
        currentArea = color;
        startArea = color;
        currentPath = paths[color];
        currentPathIndex = 0;
        transform.position = currentPath[0];
        isMoving = false;
    }

    IEnumerator AnimateAndMove()
    {
        float animationTime = 1f;
        float timer = 0f;

        // Animate random numbers for 1 second.
        while (timer < animationTime)
        {
            textMeshProUGUI.text = Random.Range(1, 7).ToString();
            timer += Time.deltaTime;
            yield return null;
        }

        int steps = Random.Range(1, 7);
        textMeshProUGUI.text = steps.ToString();

        StartCoroutine(Move(steps));
        int currentAreaID = currentPathIndex * 4 / (currentPath.Length - 5);
        int startID = System.Array.IndexOf(areas, startArea);
        currentAreaID += startID;
        currentAreaID %= 4;
        currentArea = areas[currentAreaID];
        print(currentArea);

        
    }

    public IEnumerator Move(int steps)
    {
        if (isMoving) yield break;
        isMoving = true;

        for (int i = 0; i < steps; i++)
        {
            if (currentPathIndex + 1 < currentPath.Length)
            {
                currentPathIndex++;
                Vector3 targetPosition = currentPath[currentPathIndex];

                while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = targetPosition;
            }
        }

        Vector3 rayOrigin = transform.position + Vector3.up * 3f;
        RaycastHit hit;

        //Debug.DrawRay(rayOrigin, Vector3.down * 5f, Color.red, 500f);

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 5f, boardCellLayer))
        {
            Debug.Log("Raycast HIT something! Object name: " + hit.collider.name);

            BoardCell landedCell = hit.collider.GetComponent<BoardCell>();
            if (landedCell != null)
            {
                Debug.Log("SUCCESS: Found BoardCell. Processing action...");
                gameManager.currentPathIndex = currentPathIndex;
                ProcessCellAction(landedCell);
            }
            else
            {
                Debug.Log("ERROR: The object hit does NOT have a BoardCell script attached.");
            }
        }
        else
        {
            Debug.Log("Raycast FAILED. The ray did not hit any colliders.");
        }

        isMoving = false;
        gameManager.currentPathIndex = currentPathIndex;

    }

    private BattleManager.AreaType GetCurrentArea()
    {
        int currentAreaID = currentPathIndex * 4 / (currentPath.Length - 5);
        int startID = System.Array.IndexOf(areas, startArea);
        currentAreaID = (currentAreaID + startID) % 4;
        currentArea = areas[currentAreaID];
        print("Current Area is: " + currentArea);

        switch (currentArea)
        {
            case PlayerColor.Red: return AreaType.Fire;
            case PlayerColor.Green: return AreaType.Earth;
            case PlayerColor.Blue: return AreaType.Snow;
            case PlayerColor.Yellow: return AreaType.Lightning;
            default: return AreaType.None;
        }
    }


    public void ShowChoiceMenu()
    {
        chohiceMenu.SetActive(true);
    }

    public void StartBattle()
    {
        chohiceMenu.SetActive(false);
        if (currentLandedCell != null)
        {
            CellData data = currentLandedCell.GetData();

            // Get the area and the reward, then start the battle
            AreaType area = GetCurrentArea();
            battleManager.StartBattle(area, data.potionReward); // Pass both parameters
        }
    }

    public void SkipBatlle()
    {
        spawner.SpawnNextNCells(gameManager.currentPathIndex);
    }

    //------------------------- PATH GENERATION LOGIC --------------------------------//
    private void GenerateAllPaths()
    {
        // Define the main path and home columns using 15x15 grid coordinates
        // (0,0) bottom-left, (14,14) top-right.

        // The 52 squares of the main outer track (non colored squares)
        List<Vector2Int> mainPath = new List<Vector2Int>
        {
            new Vector2Int(6, 1),
            new Vector2Int(6, 2),
            new Vector2Int(6, 3),
            new Vector2Int(6, 4),
            new Vector2Int(6, 5),
            new Vector2Int(5, 6),
            new Vector2Int(4, 6),
            new Vector2Int(3, 6),
            new Vector2Int(2, 6),
            new Vector2Int(1, 6),
            new Vector2Int(0, 6),
            new Vector2Int(0, 7),

            

            new Vector2Int(1, 8),
            new Vector2Int(2, 8),
            new Vector2Int(3, 8),
            new Vector2Int(4, 8),
            new Vector2Int(5, 8),
            //new Vector2Int(6, 8),
            new Vector2Int(6, 9),
            new Vector2Int(6, 10),
            new Vector2Int(6, 11),
            new Vector2Int(6, 12),
            new Vector2Int(6, 13),
            new Vector2Int(6, 14),
            new Vector2Int(7, 14),

            

            new Vector2Int(8, 13),
            new Vector2Int(8, 12),
            new Vector2Int(8, 11),
            new Vector2Int(8, 10),
            new Vector2Int(8, 9),
            //new Vector2Int(8, 8),
            new Vector2Int(9, 8),
            new Vector2Int(10, 8),
            new Vector2Int(11, 8),
            new Vector2Int(12, 8),
            new Vector2Int(13, 8),
            new Vector2Int(14, 8),
            new Vector2Int(14, 7),

            

            new Vector2Int(13, 6), 
            new Vector2Int(12, 6), 
            new Vector2Int(11, 6), 
            new Vector2Int(10, 6), 
            new Vector2Int(9, 6), 
            //new Vector2Int(8, 6),
            new Vector2Int(8, 5), 
            new Vector2Int(8, 4), 
            new Vector2Int(8, 3), 
            new Vector2Int(8, 2), 
            new Vector2Int(8, 1), 
            new Vector2Int(8, 0),
            new Vector2Int(7, 0)
        };

        //additional points
        List<Vector2Int> yellowExtras = new List<Vector2Int> { new Vector2Int(0, 8), new Vector2Int(8, 14), new Vector2Int(14, 6), };
        List<Vector2Int> blueExtras = new List<Vector2Int> { new Vector2Int(8, 14), new Vector2Int(14, 6), new Vector2Int(6, 0)};
        List<Vector2Int> redExtras = new List<Vector2Int> { new Vector2Int(14, 6), new Vector2Int(6, 0), new Vector2Int(0, 8) };
        List<Vector2Int> greenExtras = new List<Vector2Int> { new Vector2Int(6, 0), new Vector2Int(0, 8), new Vector2Int(8, 14) };

        // 4 home paths
        List<Vector2Int> yellowHome = new List<Vector2Int> { new Vector2Int(7, 1), new Vector2Int(7, 2), new Vector2Int(7, 3), new Vector2Int(7, 4), new Vector2Int(7, 5), new Vector2Int(7, 6) };
        List<Vector2Int> blueHome = new List<Vector2Int> { new Vector2Int(1, 7), new Vector2Int(2, 7), new Vector2Int(3, 7), new Vector2Int(4, 7), new Vector2Int(5, 7), new Vector2Int(6, 7) };
        List<Vector2Int> greenHome = new List<Vector2Int> { new Vector2Int(13, 7), new Vector2Int(12, 7), new Vector2Int(11, 7), new Vector2Int(10, 7), new Vector2Int(9, 7), new Vector2Int(8, 7) };
        List<Vector2Int> redHome = new List<Vector2Int> { new Vector2Int(7, 13), new Vector2Int(7, 12), new Vector2Int(7, 11), new Vector2Int(7, 10), new Vector2Int(7, 9), new Vector2Int(7, 8) };

        // Start Positions
        Vector2Int yellowStart = new Vector2Int(6, 1);
        Vector2Int blueStart = new Vector2Int(1, 8);
        Vector2Int greenStart = new Vector2Int(13, 6);
        Vector2Int redStart = new Vector2Int(8, 13);

        // Generate and store the full path for each color
        paths[PlayerColor.Green] = GenerateFullPath(mainPath, greenHome, greenStart, greenExtras);
        paths[PlayerColor.Blue] = GenerateFullPath(mainPath, blueHome, blueStart , blueExtras);
        paths[PlayerColor.Red] = GenerateFullPath(mainPath, redHome, redStart, redExtras);
        paths[PlayerColor.Yellow] = GenerateFullPath(mainPath, yellowHome, yellowStart, yellowExtras);
    }

    void ProcessCellAction(BoardCell cell)
    {
        CharacterStats playerStats = GetComponent<CharacterStats>();
        CellData landedCellData = cell.GetData();

        switch (landedCellData.type)
        {
            case CellType.Enemy:
                Debug.Log("Landed on an Enemy cell!");
                currentLandedCell = cell;
                ShowChoiceMenu();
                break;

            case CellType.Ally:
                Debug.Log("Landed on an Ally cell!");

                switch (landedCellData.buffType)
                {
                    case PermaBuffType.Attack:
                        playerStats.attackPower += 1;
                        Debug.Log("Player attack permanently increased to " + playerStats.attackPower);
                        break;
                    case PermaBuffType.Defense:
                        playerStats.defense += 1;
                        Debug.Log("Player defense permanently increased to " + playerStats.defense);
                        break;
                    case PermaBuffType.Health:
                        playerStats.maxHealth += 5;
                        playerStats.Heal(5); // Also heal the player for the new max health
                        Debug.Log("Player max health permanently increased to " + playerStats.maxHealth);
                        break;
                }

                nextNSpawner.SpawnNextNCells(gameManager.currentPathIndex);
                break;
        }
    }

    private Vector3[] GenerateFullPath(List<Vector2Int> main, List<Vector2Int> home, Vector2Int start, List<Vector2Int> extras)
    {
        List<Vector2Int> fullPathGrid = new List<Vector2Int>();
        int startIndex = main.IndexOf(start);

        // Loop through the main path from the start index, wrapping around
        for (int i = 0; i < main.Count; i++)
        {
            fullPathGrid.Add(main[(startIndex + i) % main.Count]);

            if (i == 11)
                fullPathGrid.Add(extras[0]);
            else if (i == 23)
                fullPathGrid.Add(extras[1]);
            else if(i == 35)
                fullPathGrid.Add(extras[2]);
            
        }

        // Add home column
        fullPathGrid.AddRange(home);

        // Convert the grid coordinates to world coordinates
        List<Vector3> worldPath = new List<Vector3>();
        foreach (var point in fullPathGrid)
        {
            // This assumes your 15x15 plane is centered at (0,0,0) and (1.5, 1.5, 1.5) scale
            worldPath.Add(new Vector3(point.x - 7f, 0.1f, point.y - 7f));
        }

        return worldPath.ToArray();
    }
}