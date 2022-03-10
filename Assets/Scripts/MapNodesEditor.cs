using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapNodes)), CanEditMultipleObjects]
public class MapNodesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        //EditorGUILayout.PropertyField(serializedObject.FindProperty("links"));
        //serializedObject.ApplyModifiedProperties();
    }
}
