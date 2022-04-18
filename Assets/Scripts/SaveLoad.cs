using System.IO;
using UnityEngine;

public static class SaveLoad
{
    // It only takes JSON files, make sure to include .json as well.
    public static string GetJson(string file_name)
    {
        string file_dir = Application.persistentDataPath + "/" + file_name;
        return File.Exists(file_dir) ? File.ReadAllText(file_dir) : "";
    }

    public static void SaveJson(string file_name, string json)
    {
        File.WriteAllText(Application.persistentDataPath + "/" + file_name, json);
    }
}
