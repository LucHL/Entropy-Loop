using UnityEngine;

public interface IDataPersistence
{
    void SaveData(GameData data);
    void LoadData(GameData data);
}
