using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemData
{
    public string id;
    public Sprite icon;
    public GameObject prefab;
    public int price;
}

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> characters = new List<ItemData>();

    public ItemData GetCharacterById(string id)
    {
        return characters.Find(c => c.id == id);
    }
}