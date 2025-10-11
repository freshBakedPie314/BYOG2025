using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TMPro;
using static BattleManager;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    private Dictionary<PlayerColor, Vector3[]> paths = new Dictionary<PlayerColor, Vector3[]>();
    private Vector3[] currentPath;
    private int currentPathIndex = 0;
    private bool isMoving = false;

    public PlayerColor currentArea;
    public PlayerColor startArea = PlayerColor.Red;
    public PlayerColor previousArea;
    private PlayerColor[] areas = new PlayerColor[] { PlayerColor.Red, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Blue };

    [Header("Serialized References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private NextNSpawner nextNSpawner;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private Image diceImageUI;
    [SerializeField] private List<Sprite> diceImages;
    [SerializeField] private BattleHUDManager battleHUDManager;

    [Header("Boss Fights")]
    [Tooltip("Assign skill rewards in order: Red(Fire), Blue(Snow), Yellow(Lightning), Green(Earth)")]
    public List<Ability> bossSkillRewards = new List<Ability>();

    [Header("Physics")]
    public LayerMask boardCellLayer;

    [Header("Player Prefabs")]
    [Tooltip("Assign prefabs in order: Red, Blue, Yellow, Green")]
    public List<GameObject> playerPrefabs = new List<GameObject>();

    [Header("UI")]
    public GameObject chohiceMenu;
    public TextMeshProUGUI rewardDescription;
    public Image rewardImage;

    public Sprite healImage;
    public Sprite dmgImage;
    public Sprite defImage;

    public string healDesc;
    public string dmgDesc;
    public string defDesc;

    public bool canMove = true;
    public enum PlayerColor { Red, Green, Yellow, Blue } // Order matters for indexing
    private BoardCell currentLandedCell;

    void Awake()
    {
        GenerateAllPaths();
    }

    public void StartGame(int col_id)
    {
        PlayerColor colour = (PlayerColor)col_id;
        GameObject go = Instantiate(playerPrefabs[col_id], gameObject.transform);
        if (col_id == 1) go.transform.rotation = Quaternion.Euler(go.transform.rotation.eulerAngles.x - 90f,
            go.transform.rotation.eulerAngles.y,
            go.transform.rotation.eulerAngles.z);
        CharacterStats playerStats = GetComponent<CharacterStats>();

        currentArea = PlayerColor.Red + col_id;
        SetPlayerColor(colour);
        previousArea = startArea;

        if (playerStats != null && bossSkillRewards.Count > col_id)
        {
            playerStats.characterAbilities.Add(bossSkillRewards[col_id]);
            battleHUDManager.UpdateActionButtons(playerStats);
            battleHUDManager.gameObject.SetActive(false);
        }

        gameManager.currentPath = currentPath;
        gameManager.currentPathIndex = currentPathIndex;
        nextNSpawner.SpawnNextNCells(0);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1) && !isMoving && currentPath != null && canMove)
        {
            StartCoroutine(AnimateAndMove());
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentPathIndex = currentPath.Length - 2;
        }
    }

    public void SetPlayerColor(PlayerColor color)
    {
        if (!paths.ContainsKey(color)) return;
        startArea = color;
        currentPath = paths[color];
        currentPathIndex = 0;
        transform.position = currentPath[0];
        isMoving = false;
    }
    public PlayerColor areaBeforeMove;
    public PlayerColor areaAfterMove;
    IEnumerator AnimateAndMove()
    {
        isMoving = true;
        float animationTime = 1f;
        float timer = 0f;
        while (timer < animationTime)
        {
            diceImageUI.sprite = diceImages[Random.Range(0, 6)];
            timer += Time.deltaTime;
            yield return null;
        }

        int steps = Random.Range(1, 7);
        diceImageUI.sprite = diceImages[steps - 1];

        areaBeforeMove = GetCurrentAreaColor();

        yield return StartCoroutine(Move(steps));

        this.currentArea = GetCurrentAreaColor();

        areaAfterMove = GetCurrentAreaColor();

        if (areaAfterMove != areaBeforeMove && areaBeforeMove != startArea)
        {
            Debug.Log("NEW AREA ENTERED! Spawning boss from previous area: " + areaBeforeMove);
            int previousAreaIndex = (int)areaBeforeMove;
            GameObject bossToFight = playerPrefabs[previousAreaIndex];
            Ability skillToLearn = bossSkillRewards[previousAreaIndex];

            if (bossToFight != null && skillToLearn != null)
            {
                battleManager.StartBossBattle(bossToFight, skillToLearn);
            }
        }
        else
        {
            CheckLandedCell();
        }

        isMoving = false;
        gameManager.currentPathIndex = currentPathIndex;
    }

    public IEnumerator Move(int steps)
    {
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
    }

    private void CheckLandedCell()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 3f, Vector3.down, out hit, 5f, boardCellLayer))
        {
            BoardCell landedCell = hit.collider.GetComponent<BoardCell>();
            if (landedCell != null)
            {
                if (currentPathIndex == currentPath.Length - 1)
                {
                    battleManager.StartUltimateBattle();
                    return;
                }
                ProcessCellAction(landedCell);
            }
        }
        else
        {
            nextNSpawner.SpawnNextNCells(currentPathIndex);
        }
    }

    void ProcessCellAction(BoardCell cell)
    {
        CharacterStats playerStats = GetComponent<CharacterStats>();
        CellData landedCellData = cell.GetData();
        switch (landedCellData.type)
        {
            // case PlayerColor.Red: return AreaType.Fire;
            // case PlayerColor.Green: return AreaType.Earth;
            // case PlayerColor.Blue: return AreaType.Snow;
            // case PlayerColor.Yellow: return AreaType.Lightning;
            // default: return AreaType.None;
            case CellType.Enemy:
                currentLandedCell = cell;
                ShowChoiceMenu(landedCellData.potionReward.abilityName);
                break;
            case CellType.Ally:
                switch (landedCellData.buffType)
                {
                    case PermaBuffType.Attack: playerStats.attackPower += 1; break;
                    case PermaBuffType.Defense: playerStats.defense += 1; break;
                    case PermaBuffType.Health: playerStats.maxHealth += 5; playerStats.Heal(5); break;
                }
                nextNSpawner.SpawnNextNCells(currentPathIndex);
                break;
        }
    }

    private PlayerColor GetCurrentAreaColor()
    {
        int pathLength = currentPath.Length - 5;
        int segmentLength = pathLength / 4;
        int currentSegment = currentPathIndex / segmentLength;
        int startID = (int)startArea;
        int currentAreaID = (startID + currentSegment) % 4;
        return (PlayerColor)currentAreaID;
    }

    public void ShowChoiceMenu(string reward)
    {
        if(reward == "Heal")
        {
            rewardImage.sprite = healImage;
            rewardDescription.text = healDesc;
        }
        else if(reward == "Damge Inc")
        {
            rewardImage.sprite = dmgImage;
            rewardDescription.text = dmgDesc;
        }
        else if(reward == "Defense Inc")
        {
            rewardImage.sprite = defImage;
            rewardDescription.text = defDesc;
        }
        
        chohiceMenu.SetActive(true);
        canMove = false;
    }

    public void StartBattle()
    {
        chohiceMenu.SetActive(false);

        if (currentLandedCell != null)
        {
            CellData data = currentLandedCell.GetData();
            AreaType area = (AreaType)GetCurrentAreaColor();
            battleManager.StartBattle(area, data.potionReward);
        }
    }

    public void SkipBatlle()
    {
        chohiceMenu.SetActive(false);
        canMove = true;
        nextNSpawner.SpawnNextNCells(gameManager.currentPathIndex);
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
        List<Vector2Int> blueExtras = new List<Vector2Int> { new Vector2Int(8, 14), new Vector2Int(14, 6), new Vector2Int(6, 0) };
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
        paths[PlayerColor.Blue] = GenerateFullPath(mainPath, blueHome, blueStart, blueExtras);
        paths[PlayerColor.Red] = GenerateFullPath(mainPath, redHome, redStart, redExtras);
        paths[PlayerColor.Yellow] = GenerateFullPath(mainPath, yellowHome, yellowStart, yellowExtras);
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
            else if (i == 35)
                fullPathGrid.Add(extras[2]);

        }

        // Add home column
        fullPathGrid.AddRange(home);

        // Convert the grid coordinates to world coordinates
        List<Vector3> worldPath = new List<Vector3>();
        foreach (var point in fullPathGrid)
        {
            worldPath.Add(new Vector3(7f - point.x, 0.1f, 7f - point.y));
        }

        return worldPath.ToArray();
    }
}