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

    public static string ListToString<T>(ICollection<T> l)
    {
        if (l.Count == 0) return "[]";
        string result = "[";
        foreach (var i in l) result += i.ToString() + ", ";
        result = result.Substring(0,result.Length - 2) + "]";
        return result;
    }

    public static void Dict<K,V>(Dictionary<K,V> d)
    {
        if (d.Count == 0)
        {
            Debug.Log("{}");
            return;
        }

        string result = "{";
        foreach (KeyValuePair<K,V> i in d)
        {
            result += string.Format("{0}: {1}, ", i.Key.ToString(), i.Value.ToString());
        }
        result = result.Substring(0,result.Length - 2) + "}";
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
