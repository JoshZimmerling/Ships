using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public class Save : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    private static bool isWebBuild = true;
#else
    private static bool isWebBuild = false;
#endif

    public static SaveData myGlobalSaveData;
    private static string saveFilePath;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        saveFilePath = isWebBuild ? "/idbfs/" : Application.persistentDataPath + "/";

        myGlobalSaveData = LoadFile("saveFile_ships.json");
    }

    public static void SaveMyData()
    {
        SaveFile("saveFile_ships.json", myGlobalSaveData);
    }

    private static void SaveFile(string fileName, SaveData data)
    {
        Debug.Log("Saving file");
        File.WriteAllText(saveFilePath + fileName, JsonConvert.SerializeObject(data));
    }

    private static SaveData LoadFile(string fileName)
    {
        SaveData loadedData;
        string fullPath = saveFilePath + fileName;
        if (File.Exists(fullPath))
        {
            loadedData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(fullPath));
            Debug.Log("Opening existing save file: " + fullPath);
        }
        else
        {
            Debug.Log("Creating new save file");
            loadedData = new SaveData();
        }

        ValidateSaveData(fileName, loadedData);
        return loadedData;
    }

    public static void DeleteFile(string fileName)
    {
        string fullPath = saveFilePath + fileName;
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Debug.Log("Deleted save file: " + fullPath);
        }
        else
        {
            Debug.Log("Tried to delete save file, but it does not exist: " + fullPath);
        }
    }

    private static void ValidateSaveData(string fileName, SaveData data)
    {
        if (data.uniqueID == null)
        {
            data.AssignUniqueId();
        }

        Debug.Log(data.ToString());
        SaveFile(fileName, data);
    }
}