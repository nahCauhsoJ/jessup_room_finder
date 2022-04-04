using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapStructures : MonoBehaviour
{
    public string disp_name;
    public int map_layer; // This is to determine whether to zoom in or zoom out during transition. Higher means inner.
    public StructureType structure_type;
    public List<MapNodes> nodes_inside; // This is a list of nodes inside this structure. If omitted it means all of them.

    public enum StructureType {Outside, Inside, Upstairs, Downstairs}
}
