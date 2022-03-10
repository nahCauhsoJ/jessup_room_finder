using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
                if (used_nodes.Contains(i)) continue;
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

    // I'm too lazy. Let's have this function fix everything.
    // Note that it only fix links by making one-sided links two-sided and remove duplicate / empty links.
    // It also overwrites all the affected nodes's links.
    public static void FixNodeLinks(List<MapNodes> nodes)
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

            //i.links = new List<MapNodes>(used_links); // Cuz why not. It's only gonna run once.
            //i.links_old = new List<MapNodes>(used_links);

            if (i.gameObject.name == "cafeteria")
            {
                SerializedObject so_node = new SerializedObject(i);
                SerializedProperty node_links = so_node.FindProperty("links");
                SerializedProperty node_links_old = so_node.FindProperty("links_old");
                node_links.ClearArray();
                node_links_old.ClearArray();
                for (var j = 0; j < used_links.Count; j++)
                {
                    node_links.InsertArrayElementAtIndex(j);
                    node_links_old.InsertArrayElementAtIndex(j);
                    //Debug.Log(node_links.GetArrayElementAtIndex(j).objectReferenceValue);
                    //Debug.Log(used_links[j]);
                    node_links.GetArrayElementAtIndex(j).objectReferenceValue = used_links[j];
                    node_links.GetArrayElementAtIndex(j).objectReferenceValue = used_links[j];
                    //Debug.Log(node_links.GetArrayElementAtIndex(j).objectReferenceValue);
                    //node_links[j] = used_links[j];
                    //node_links_old[j] = used_links[j];
                }
                so_node.ApplyModifiedProperties();
            }
            
            /*
            SerializedObject so_node = new SerializedObject(i);
            if (i.links.Count != i.links_old.Count)
            {
                Debug.Log(i.gameObject.name);
                Debug.Log(so_node.FindProperty("links").arraySize);
                so_node.FindProperty("links").ClearArray();
                Debug.Log(so_node.FindProperty("links").arraySize);
                Debug.Log(((MapNodes)so_node.targetObject).links.Count);
                so_node.ApplyModifiedProperties();
                Debug.Log(so_node.FindProperty("links").arraySize);
                Debug.Log(((MapNodes)so_node.targetObject).links.Count);
            }*/
        }
    }
}
