using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketUI : MonoBehaviour
{
    public static MarketUI Instance;

    [SerializeField] private Transform characterSlotsParent;
    [SerializeField] private GameObject characterSlotPrefab;
    [SerializeField] private TextMeshProUGUI currencyDisplay;
    // Remove this line: [SerializeField] private CharacterDatabase characterDatabase;

    private List<CharacterMarketSlot> characterSlots = new List<CharacterMarketSlot>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        CreateSlots();
        UpdateCurrencyDisplay();
    }

    private void CreateSlots()
    {
        // Clear existing slots
        foreach (Transform child in characterSlotsParent)
        {
            Destroy(child.gameObject);
        }
        characterSlots.Clear();

        // Access database through CharacterManager
        foreach (var character in CharacterManager.Instance.GetAllCharacters())
        {
            GameObject slotObj = Instantiate(characterSlotPrefab, characterSlotsParent);
            CharacterMarketSlot slot = slotObj.GetComponent<CharacterMarketSlot>();
            slot.SetupSlot(character);
            characterSlots.Add(slot);
        }
    }

    public void RefreshCharacterSlots()
    {
        foreach (var slot in characterSlots)
        {
            // You'll need to add this method to CharacterMarketSlot
            slot.RefreshSlot();
        }
    }

    public void OnCharacterPurchased()
    {
        AudioManager.instance.PlayUI(AudioManager.instance.uiPurchase);
        UpdateCurrencyDisplay();
        GameManager.instance.SpawnSelectedSlingshot();
        // Maybe play purchase sound/effect
    }

    private void UpdateCurrencyDisplay()
    {
        currencyDisplay.text = GameManager.instance.GetCurrency().ToString();
    }
}