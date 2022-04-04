using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLog
{
    public static void List<T>(ICollection<T> l)
    {
        if (l.Count == 0)
        {
            Debug.Log("[]");
            return;
        }
        string result = "[";
        foreach (var i in l) result += i.ToString() + ", ";
        result = result.Substring(0,result.Length - 2) + "]";
        Debug.Log(result);
    }

    public static void Array<T>(T[] a)
    {
        if (a.Length == 0)
        {
            Debug.Log("[]");
            return;
        }
        string result = "[";
        foreach (var i in a) result += i.ToString() + ", ";
        result = result.Substring(0,result.Length - 2) + "]";
        Debug.Log(result);
    }
}
