using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite cardImage;
    public GameObject unitPrefab;
}
