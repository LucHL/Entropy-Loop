using UnityEngine;
using System;
using System.IO;

public class FileDataHandler
{
    private string dataDirPath = "";
    private string dataFileName = "";
    private bool useEncryption = false;
    private readonly string encryptionCodeWord = "vraxx";

    public FileDataHandler(string dataDirPath, string dataFileName, bool useEncryption)
    {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName;
        this.useEncryption = useEncryption;

        BugTracker.Info("[FileDataHandler] Initialize");
        BugTracker.Info("[FileDataHandler] dataDirPath : '" + this.dataDirPath + "/" + this.dataFileName + "'.");
        BugTracker.Info("[FileDataHandler] useEncryption : '" + this.useEncryption + "'");
    }

    public GameData Load()
    {
        string jsonString = LoadJsonRaw();
        if (string.IsNullOrEmpty(jsonString))
            return null;

        try {
            return JsonUtility.FromJson<GameData>(jsonString);
        } catch (Exception e) {
            BugTracker.Error("Failed to parse GameData from JSON: " + e);
            return null;
        }
    }

    public string LoadJsonRaw()
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        string dataToLoad = "";

        if (File.Exists(fullPath)) {
            try {
                using (FileStream stream = new FileStream(fullPath, FileMode.Open)) {
                    using (StreamReader reader = new StreamReader(stream)) {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                if (useEncryption) {
                    dataToLoad = EncryptDecrypt(dataToLoad);
                }
            } catch (Exception e) {
                BugTracker.Error("Failed to load raw json from path: " + fullPath + "\n" + e);
            }
        }

        return dataToLoad;
    }

    public void Save(GameData data)
    {
        string dataToStore = JsonUtility.ToJson(data, true);
        SaveJsonRaw(dataToStore);
    }

    public void SaveJsonRaw(string jsonString)
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);

        try {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToStore = jsonString;

            if (useEncryption) {
                dataToStore = EncryptDecrypt(dataToStore);
            }

            using (FileStream stream = new FileStream(fullPath, FileMode.Create)) {
                using (StreamWriter writer = new StreamWriter(stream))  {
                    writer.Write(dataToStore);
                }
            }

        } catch (Exception e) {
            BugTracker.Error("Failed to save raw JSON Data: " + e);
        }
    }

    public void Delete()
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);

        try {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        } catch (Exception e) {
            BugTracker.Error("Failed to delete save data at path: " + fullPath + "\n" + e);
        }
    }

    private string EncryptDecrypt(string data) 
    {
        string modifiedData = "";

        for (int i = 0; i < data.Length; i++) 
            modifiedData += (char) (data[i] ^ encryptionCodeWord[i % encryptionCodeWord.Length]);

        return modifiedData;
    }
}
