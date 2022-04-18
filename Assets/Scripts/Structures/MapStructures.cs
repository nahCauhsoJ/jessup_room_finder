using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapStructures : MonoBehaviour
{
    public static MapStructures master; // Whoever runs MapStructuresMaster is the default structure.
    public static MapStructures current; // Started with MapStructuresMaster, but can be changed by MapStructureTransition.
    
    public string disp_name;
    public StructureLayer structure_layer = StructureLayer.Inside;
    public StructureFloor structure_floor = StructureFloor.Downstairs;
    public List<MapNodes> nodes_inside; // This is a list of nodes inside this structure. If omitted it means all of them.
    public float map_scale_inside; // This is the new map_scale shoved into Map.main.ZoomForced().
    

    public enum StructureLayer{Outside, Inside}
    public enum StructureFloor{Upstairs, Downstairs}

    void Awake()
    {
        // This lets the structure claim the nodes, so the pathfinding system knows if it needs a transition when arriving at this node.
        // If null, it means outside.
        foreach (var i in nodes_inside) i.structure_belong = this;
        // gameObject.SetActive(false); // Again, Awake() needs the object to be active at the start to run.
    }

    void Start()
    {
        // There's no reason to zoom to 0 z-offset. Hence 0 means it'll be provided with Outside's default.
        if (map_scale_inside == 0) map_scale_inside = MapScroller.main.map_scale;
        gameObject.SetActive(false); // This is supposed to be in Awake(), but Start() needs to run.
    }
}
