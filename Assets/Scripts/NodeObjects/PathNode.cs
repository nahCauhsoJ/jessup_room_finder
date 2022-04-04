using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode : MapNodes
{
    protected override void Awake() {
        base.Awake();
        derived_obj = "path";
    }
}
