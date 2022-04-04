using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RepairMode : EditorWindow
{
    [MenuItem("Window/Repair Mode")]
    public static void ShowWindow()
    {
        GetWindow<RepairMode>("Repair Stuff");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Fix Node Links")) FixNodeLinks(new List<MapNodes>(Object.FindObjectsOfType<MapNodes>()));
    }

    // I'm too lazy. Let's have this function fix everything.
    // Note that it only fix links by making one-sided links two-sided and remove duplicate / empty links.
    // It also overwrites all the affected nodes's links.
    void FixNodeLinks(List<MapNodes> nodes)
    {
        foreach (var i in nodes)
        {
            List<MapNodes> used_links = new List<MapNodes>();
            foreach (var j in i.links)
            {
                if (j == null) continue;
                if (used_links.Contains(j)) continue;
                used_links.Add(j);
                if (!j.links.Contains(i)) j.links.Add(i);
            }

            SerializedObject so_node = new SerializedObject(i);
            SerializedProperty node_links = so_node.FindProperty("links");
            node_links.ClearArray();
            for (var j = 0; j < used_links.Count; j++)
            {
                node_links.InsertArrayElementAtIndex(j);
                node_links.GetArrayElementAtIndex(j).objectReferenceValue = used_links[j];
            }
            so_node.ApplyModifiedProperties();
        }
    }
}