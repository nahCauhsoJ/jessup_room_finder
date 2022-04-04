using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureTransition : MonoBehaviour
{
    public static int current_layer; // This is the layer defined in MapStructures. Outside should be 0.
    public static Dictionary<MapNodes, StructureTransition> nodes = new Dictionary<MapNodes, StructureTransition>();
    public static List<MapNodes> not_trans = new List<MapNodes>();
    public static StructureTransition GetObj(MapNodes node)
    {
        if (not_trans.Contains(node)) return null;

        StructureTransition trans_node = null;
        if (nodes.TryGetValue(node, out trans_node)) return trans_node;

        trans_node = node.GetComponent<StructureTransition>();
        if (trans_node != null) nodes[node] = trans_node;
        else not_trans.Add(node);
        return trans_node;
    }

    public MapStructures trans_to;
}
