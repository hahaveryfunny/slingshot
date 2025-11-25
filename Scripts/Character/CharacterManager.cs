using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance;
    public ItemDatabase characterDatabase;
    [SerializeField] Slingshot slingshotScript;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        var saveData = SaveManager.Instance.GetSaveData();

        // If no character is selected, select the first owned one
        if (string.IsNullOrEmpty(saveData.selectedCharacterId))
        {
            var firstOwned = GetOwnedCharacters().FirstOrDefault();
            if (firstOwned != null)
            {
                saveData.selectedCharacterId = firstOwned.id;
                SaveManager.Instance.SaveGame();
            }
        }
    }

    public bool IsCharacterOwned(string characterId)
    {
        return SaveManager.Instance.GetSaveData().ownedCharacterIds.Contains(characterId);
    }

    public List<ItemData> GetAllCharacters()
    {
        return characterDatabase.characters;
    }

    public Slingshot GetSlingshotController()
    {
        return slingshotScript;
    }


    public bool PurchaseCharacter(string characterId)
    {
        var character = characterDatabase.GetCharacterById(characterId);
        var saveData = SaveManager.Instance.GetSaveData();

        if (character != null && !IsCharacterOwned(characterId) && saveData.currency >= character.price)
        {
            saveData.selectedCharacterId = characterId;
            saveData.currency -= character.price;
            saveData.ownedCharacterIds.Add(characterId);
            SaveManager.Instance.SaveGame();
            MarketUI.Instance.RefreshCharacterSlots();
            return true;
        }
        return false;
    }

    public void SelectCharacter(string characterId)
    {
        if (IsCharacterOwned(characterId))
        {
            AudioManager.instance.PlayUI(AudioManager.instance.uiPick);
            SaveManager.Instance.GetSaveData().selectedCharacterId = characterId;
            SaveManager.Instance.SaveGame();
            
        }
    }

    public ItemData GetSelectedCharacter()
    {
        string selectedId = SaveManager.Instance.GetSaveData().selectedCharacterId;
        if (string.IsNullOrEmpty(selectedId))
            return null;

        return characterDatabase.GetCharacterById(selectedId);
    }

    public List<ItemData> GetOwnedCharacters()
    {
        var ownedIds = SaveManager.Instance.GetSaveData().ownedCharacterIds;
        return characterDatabase.characters.Where(c => ownedIds.Contains(c.id)).ToList();
    }

    public GameObject GetCharacterPrefab(string characterId)
    {
        var character = characterDatabase.GetCharacterById(characterId);
        return character?.prefab;
    }
}
