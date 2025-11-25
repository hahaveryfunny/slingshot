using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterMarketSlot : MonoBehaviour
{
    [SerializeField] private Image characterIcon;
    //[SerializeField]
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button actionButton;
    //[SerializeField] private Text buttonText;
    [SerializeField] private Image selectedIndicator;

    private ItemData characterData;

    public void SetupSlot(ItemData data)
    {
        Debug.Log($"Setting up slot - Data: {data}, Icon: {characterIcon}");

        characterData = data;
        characterIcon.sprite = data.icon;
        UpdateSlotAppearance();

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnSlotClicked);
    }

    public void RefreshSlot()
    {
        Debug.Log($"CharacterManager: {CharacterManager.Instance}, GameManager: {GameManager.instance}");

        UpdateSlotAppearance();
    }

    public void UpdateSlotAppearance()
    {
        if (CharacterManager.Instance.IsCharacterOwned(characterData.id))
        {
            priceText.gameObject.SetActive(false);
            characterIcon.enabled = true;
            // Show if this is the currently selected character
            bool isSelected = CharacterManager.Instance.GetSelectedCharacter()?.id == characterData.id;
            selectedIndicator.enabled = isSelected;
            actionButton.interactable = !isSelected;
        }
        else
        {
            priceText.gameObject.SetActive(true);
            characterIcon.enabled = false;
            priceText.text = characterData.price.ToString();
            selectedIndicator.enabled = false;
            // Check if player can afford
            actionButton.interactable = GameManager.instance.GetCurrency() >= characterData.price;
        }
    }

    private void OnSlotClicked()
    {
        if (CharacterManager.Instance.IsCharacterOwned(characterData.id))
        {
            CharacterManager.Instance.SelectCharacter(characterData.id);
            // Refresh all slots to update selection indicators
            MarketUI.Instance.RefreshCharacterSlots();
            GameManager.instance.SpawnSelectedSlingshot();
        }
        else
        {
            if (CharacterManager.Instance.PurchaseCharacter(characterData.id))
            {
                // Purchase successful
                UpdateSlotAppearance();
                MarketUI.Instance.OnCharacterPurchased();
            }
            else
            {
                print("NOT ENOUGH MONEY");
                // Show insufficient funds message
                //UIManager.Instance.ShowMessage("Insufficient funds!");
            }
        }
    }
}