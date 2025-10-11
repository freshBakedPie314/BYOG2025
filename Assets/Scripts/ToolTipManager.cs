using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    public RectTransform canvasRectTransform;
    public Vector2 offset = new Vector2(30f, -30f);

    private RectTransform panelRectTransform;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            tooltipPanel.SetActive(false);
            panelRectTransform = tooltipPanel.GetComponent<RectTransform>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowTooltip(string text)
    {
        tooltipText.text = text;
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform);
        UpdatePosition(); // Set initial position
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
        tooltipText.text = string.Empty;
    }

    // --- MODIFIED --- This is now called only when the mouse moves
    public void UpdatePosition()
    {
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            Input.mousePosition,
            null,
            out anchoredPos);

        tooltipPanel.GetComponent<RectTransform>().anchoredPosition = anchoredPos + offset;
    }
}