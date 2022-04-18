using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlternateNode : MapNodes
{
    public MainNode alt_spot_of;

    protected override void Awake() {
        base.Awake();
        derived_obj = "alternate";

        if (main_node_alts.ContainsKey(alt_spot_of)) main_node_alts[alt_spot_of].Add(this);
        else main_node_alts[alt_spot_of] = new List<AlternateNode>(){this};
    }
}
