using System.Linq; // For ClearList()
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoxMessageNoRepeatData : MonoBehaviour
{
    static BoxMessageNoRepeatData main;
    public List<string> message_id_list = new List<string>();

    public static void Add(string message_id)
    {
        main.message_id_list.Add(message_id);
        SaveLoad.SaveJson("boxMessageNoRepeat.json", JsonUtility.ToJson(main));
    }

    public static bool Contains(string message_id)
    {
        return main.message_id_list.Contains(message_id);
    }

    void Awake()
    {
        main = this;
        JsonUtility.FromJsonOverwrite(SaveLoad.GetJson("boxMessageNoRepeat.json"), this);
    }

    // Just in case there are duplicates. Honestly not worried but good practice.
    void CleanList()
    {
        message_id_list = message_id_list.Distinct().ToList();
    }
}
