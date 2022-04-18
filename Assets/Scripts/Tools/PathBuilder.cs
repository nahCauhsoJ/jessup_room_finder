using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PathBuilder
{

    public static MapNodes GetClosestPoint(Vector3 pos, List<MapNodes> nodes)
    {
        MapNodes closest_node = null;
        float closest_dist = 9999f; // Note that the distance stored is squared.
        foreach (var i in nodes)
        {
            float sqr_dist = (pos - i.transform.position).sqrMagnitude;
            if (closest_dist > sqr_dist) { closest_dist = sqr_dist; closest_node = i; }
        }
        return closest_node;
    }

    // Make sure start and end node are within nodes
    public static List<MapNodes> GetPath(MapNodes start_node, MapNodes end_node, List<MapNodes> nodes)
    {
        Dictionary<MapNodes, float[]> astar_costs = new Dictionary<MapNodes, float[]>();
        Dictionary<MapNodes, List<MapNodes>> paths = new Dictionary<MapNodes, List<MapNodes>>(); // This is parallel to astar_costs.
        List<MapNodes> used_nodes = new List<MapNodes>(); // To prevent looping in circles.

        paths[start_node] = new List<MapNodes>{start_node};
        MapNodes best_node = start_node;
        
        // If the loop ended without reaching then end, then it's guaranteed to be unreachable.
        for (var cnt = 0; cnt < nodes.Count; cnt++)
        {
            if (best_node == end_node) return paths[best_node];
            // <= 1 means that it only linked to the node it came from, i.e. a dead end. <= to eliminate nodes with no link, i.e. invalid.

            foreach (var i in best_node.links)
            {
                if (i == null) continue; // Sometimes the links can be missing. If not take this away.
                // If used, it's once a best node. If this runs, it means that one of its links is
                //      better than it. Hence, it will not be considered.
                if (used_nodes.Contains(i)) continue;
                if (astar_costs.ContainsKey(i)) continue;

                float g_cost = Vector3.Distance(i.transform.position, start_node.transform.position);
                float h_cost = Vector3.Distance(i.transform.position, end_node.transform.position);
                astar_costs[i] = new float[]{g_cost,h_cost};
                paths[i] = new List<MapNodes>(paths[best_node]);
                paths[i].Add(i);
            }

            astar_costs.Remove(best_node);
            paths.Remove(best_node);
            used_nodes.Add(best_node);
            MapNodes current_best_node = null;
            foreach (var i in astar_costs.Keys)
            {
                if (current_best_node == null) { current_best_node = i; continue; }
                if (astar_costs[i][0] + astar_costs[i][1] < astar_costs[current_best_node][0] + astar_costs[current_best_node][1])
                    current_best_node = i;
                else if (astar_costs[i][0] + astar_costs[i][1] == astar_costs[current_best_node][0] + astar_costs[current_best_node][1])
                {   // This scanerio will rarely happen since the cost is a float, but just in case.
                    if (astar_costs[i][1] < astar_costs[current_best_node][1])
                        current_best_node = i;
                }
            }
            if (current_best_node != null) best_node = current_best_node;
        }

        return null;
    }

    // This is assumed that it is a valid path from GetPath().
    public static float GetPathLength(List<MapNodes> nodes)
    {
        if (nodes.Count <= 1) return 0;

        float total_dist = 0f;
        for (var i = 1; i < nodes.Count; i++)
            total_dist += Vector3.Distance(nodes[i].transform.position, nodes[i-1].transform.position);
        return total_dist;
    }
}
