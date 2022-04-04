using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlternateNode : MapNodes
{
    public MapNodes alt_spot_of;

    protected override void Awake() {
        base.Awake();
        derived_obj = "alternate";
    }
}
