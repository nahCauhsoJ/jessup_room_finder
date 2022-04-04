using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainNode : MapNodes
{
    public string disp_name; // If omitted, it'll use id as the name instead. For display and searching.
    [TextArea] public string desc;
    public string[] search_keys; // Some places have aliases, so this makes searching easier.

    protected override void Awake() {
        base.Awake();
        MapNodes.main_nodes.Add(this);
        derived_obj = "main";
    }
}
