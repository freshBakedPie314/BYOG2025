using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionButtonUI : MonoBehaviour
{
    public Ability associatedAbility;

    private Button button;
    private TextMeshProUGUI buttonText;

    public void UpdateState(CharacterStats playerStats)
    {
        if (associatedAbility == null) return;

        bool isPotion = playerStats.potions.ContainsKey(associatedAbility);

        if (isPotion)
        {
            int potionCount = playerStats.potions[associatedAbility];
            Debug.Log("Potion Count for " + associatedAbility + " is " + potionCount);
            button.interactable = potionCount > 0;
            buttonText.text = associatedAbility.name + " (x" + potionCount + ")";
        }
        else
        {
            button.interactable = playerStats.characterAbilities.Contains(associatedAbility);
            buttonText.text = associatedAbility.name;
            if (buttonText.text == "Vunerable") buttonText.text = "Quake";
            else if (buttonText.text == "Weaken") buttonText.text = "Frost";
        }
    }

    public void Initialize(BattleManager battleManager)
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();

        // Clear any old listeners and add a new one
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            battleManager.OnAbilityButton(associatedAbility);
        });
    }
}