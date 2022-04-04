using System.Collections;
using System.Collections.Generic;
using System.Linq; // For Except() and Any()
using UnityEngine;

public class UserControl : MonoBehaviour
{
    public static UserControl main;

    public float arrival_radius; // The radius (in world space units) it needs to be consider arriving at a nearby node.
    // If the distance ratio of the target node and the closest node exceeds this value, a reroute will occur.
    // Note that the comparison is the square distance. Also it is [target node : closest node], so reroute occurs when it's > 1.
    public float reroute_thres; // This is in Unity units, not meters.

    int nodes_layer;
    Collider[] nodes_nearby = new Collider[50]; // 50 should suffice, I hope?
    List<MapNodes> valid_nodes_nearby = new List<MapNodes>();
    Dictionary<Collider, MapNodes> nodes_dict = new Dictionary<Collider, MapNodes>();
    List<Collider> invalid_nodes = new List<Collider>();
    float slow_update_elapsed;
    MapNodes prev_target_node; // We have to stop CheckNodeArrival from spamming the target_node until out of range.

    // To prevent comparing with the same two points again, here is a history of it to cache the result.
    MapNodes[] destination_choices = new MapNodes[0];
    // On first check, that node will be bound to that destination.
    Dictionary<MapNodes, MapNodes> best_dest = new Dictionary<MapNodes, MapNodes>();
    // inferior = This node is inferior to the following nodes, superior = This node is superior to the followin nodes.
    Dictionary<MapNodes, HashSet<MapNodes>> reroute_inferior_history = new Dictionary<MapNodes, HashSet<MapNodes>>();
    Dictionary<MapNodes, HashSet<MapNodes>> reroute_superior_history = new Dictionary<MapNodes, HashSet<MapNodes>>();
    // PathBuilder.GetPath() itself is an expensive process, here's the cached results for the rerouted nodes.
    // These only store the path to the best destination. There is no data for other destionation_choices.
    // Also note that GetBestPath itself will populate these 2 dicts.
    Dictionary<MapNodes, List<MapNodes>> paths = new Dictionary<MapNodes, List<MapNodes>>();
    Dictionary<MapNodes, float> paths_length = new Dictionary<MapNodes, float>();

    void Awake()
    {
        main = this;
        nodes_layer = LayerMask.GetMask("nodes");
    }

    void OnEnable()
    {
        DestinationReset();
    }

    void Update()
    {
        if (Map.main.current_route == null) return; // This can update without a route being set. Better stop it if so.
        // Cleaning up nodes_nearby...
        for (var i = 0; i < nodes_nearby.Length; i++) nodes_nearby[i] = null;
        valid_nodes_nearby.Clear();

        // Map.main.path_target_link.localScale.x is basically the distance between target node and user position. Coincidence.
        // Since the radius is literally the distance between a node and user position, it's guaranteed to pick up stuff every tick.
        Physics.OverlapSphereNonAlloc(transform.position, Map.main.path_target_link.localScale.x, nodes_nearby, nodes_layer);
        foreach (var i in nodes_nearby)
        {   
            if (i == null) continue; // Since I'm using an array, there will be nulls as default values inside.
            
            MapNodes node = nodes_dict.ContainsKey(i) ? nodes_dict[i] : null;
            if (node == null)
            {
                if (invalid_nodes.Contains(i)) continue;

                node = i.GetComponent<MapNodes>();
                if (node == null) invalid_nodes.Add(i); // If not, GetComponent will run on that invalid node every tick.
                else
                {
                    if (node.links.Count < 1)
                    {
                        invalid_nodes.Add(i); // Nodes with no link can be eliminated.
                        continue;
                    } 
                    nodes_dict[i] = node;
                }
            }
            valid_nodes_nearby.Add(node);
        }

        CheckNodeArrival(valid_nodes_nearby);

        slow_update_elapsed += Time.deltaTime;
        if (slow_update_elapsed >= 1f) { slow_update_elapsed = 0; SlowUpdate(); }
    }

    // For performance reasons, calculation-intensive functions will run only once per second.
    void SlowUpdate()
    {   // 2 being the start node and end node. So if it's 2 or less nodes then there's no point finding a shorter route.
        // Also != null means user is inside target node, which means we won't reroute until user's out.
        if (Map.main.current_route.Count > 2) CompareRoutes();
    }

    // Just a lazy function to refresh the arrays. But note that these arrays are parallel, so that
    //      a for loop with numbers matches the nodes for all these arrays.
    public void InitiateDestinations(MapNodes[] dests)
    {
        destination_choices = dests;
        /*
        best_dest = new Dictionary<MapNodes, MapNodes>();
        reroute_inferior_history = new Dictionary<MapNodes, HashSet<MapNodes>>();
        reroute_superior_history = new Dictionary<MapNodes, HashSet<MapNodes>>();
        paths = new Dictionary<MapNodes, List<MapNodes>>();
        paths_length = new Dictionary<MapNodes, float>();*/
        best_dest.Clear();
        reroute_inferior_history.Clear();
        reroute_superior_history.Clear();
        paths.Clear();
        slow_update_elapsed = 0f; // Cuz why not.
        // invalid_nodes need no cleaning. Its data will be useful for the whole runtime.
        // node_dict also needs no cleaning, we need those collider -> MapNodes references.
        prev_target_node = null;
    }

    // This updates paths and paths_length for this node.
    // Since this only runs once per node, it should be fine to run that expensive GetPath().
    void UpdatePath(MapNodes node)
    {
        List<MapNodes> closest_dest_path = new List<MapNodes>();
        float closest_dest_dist = 6969f;
        foreach (var i in destination_choices)
        {
            List<MapNodes> current_dest_path = PathBuilder.GetPath(node, i, MapNodes.nodes);
            float current_dest_dist = PathBuilder.GetPathLength(current_dest_path);
            if (current_dest_dist < closest_dest_dist)
            {
                closest_dest_path = current_dest_path;
                closest_dest_dist = current_dest_dist;
            }
        }
        if (closest_dest_path.Count == 0) return;
        paths[node] = closest_dest_path;
        paths_length[node] = closest_dest_dist;
    }

    // Even lazier, this converts the destination node into the index of destination_choices.
    int GetDestIndex(MapNodes node)
    {
        for (int i = 0; i < destination_choices.Length; i++) if (destination_choices[i] == node) return i;
        return -1; // Unlikely, but just to shut the compiler up.
    }

    void CheckNodeArrival(List<MapNodes> nodes)
    {//   DebugLog.List(invalid_nodes); DebugLog.List(valid_nodes_nearby);
        MapNodes closest_node_found = null;
        float closest_node_found_sqr_dist = 6969f; // 6969 should suffice, I hope?
        //bool node_links_target = false;
        foreach (var i in nodes)
        {
            // If not null, it means the user is within target node. There I'm ignoring all nodes except the target,
            //   until out of range.
            //if (prev_target_node != null && prev_target_node != i) continue;

            float sqr_dist = (Map.main.user_pos.position - i.transform.position).sqrMagnitude;
            if ( sqr_dist <= arrival_radius * arrival_radius )
            {
                //if (prev_target_node != null) return; // Rmb that when != null, only prev_target_node does the checking.

                if (closest_node_found == null) { closest_node_found = i; closest_node_found_sqr_dist = sqr_dist; }
                else if (sqr_dist >= closest_node_found_sqr_dist) continue; // Means that there's a closer node than this.
                else { closest_node_found = i; closest_node_found_sqr_dist = sqr_dist; }
/*
                if (Map.main.current_route[Map.main.current_route_target_ix].links.Contains(i))
                { node_links_target = true; break; } // break cuz node that links to target takes priority.

                if (closest_node_found == Map.main.current_route[Map.main.current_route_target_ix])
                {
                    Map.main.TrimRoute(1);
                    prev_target_node = i;
                    return;
                }*/
            }/* else if (prev_target_node != null) {
                // Yes this gives a 1-tick free time. Not like it matters.
                prev_target_node = null;
                return; // This return statement is why it needs a condition.
            }*/
        }

        if (closest_node_found != null)
        {
            if (closest_node_found == prev_target_node) return;
            prev_target_node = closest_node_found;
            bool need_reroute = true;

            for (var i = 0; i < Map.main.current_route_remaining.Count; i++)
            {
                if (closest_node_found == Map.main.current_route[Map.main.current_route_target_ix + i])
                {
                    Map.main.TrimRoute(i+1);
                    need_reroute = false;
                }
            }

            // The current route is guaranteed to contain a link to target, hence this prevents the link inside the route to
            //      be checked twice. Plus, this does prepending, not trimming.
            if (need_reroute && Map.main.current_route[Map.main.current_route_target_ix].links.Contains(closest_node_found))
            {
                // Note that the loop above deals with the case when the link is the next target.
                Map.main.PrependRoute(closest_node_found);
                need_reroute = false;
            }
            
            if (need_reroute) Map.main.SetupRoute(Map.main.current_route[Map.main.current_route.Count - 1]);

            StructureTransition trans_node = StructureTransition.GetObj(closest_node_found);
            if (trans_node != null)
            {
                int anim_style = 0;
                if (StructureTransition.current_layer > trans_node.trans_to.map_layer) anim_style = 2;
                else if (StructureTransition.current_layer < trans_node.trans_to.map_layer) anim_style = 1;
                MapStructureTransition.main.TransToStructure(trans_node.trans_to, anim_style);
            }
        } else prev_target_node = null;
    }

    // Running this already assumes that there is another node after the target node in the route. Hence + 1 is safe.
    void CompareRoutes()
    {
        MapNodes cur_node = Map.main.current_route[Map.main.current_route_target_ix]; // Too long. I'm shortening it.

        MapNodes closest_node = cur_node;
        float closest_node_dist = PathBuilder.GetPathLength(Map.main.current_route);
        foreach (var i in valid_nodes_nearby)
        {
            if (!best_dest.ContainsKey(i))
            {
                UpdatePath(i); // Remember after running this, both paths and paths_length are modified.
                best_dest[i] = paths[i][paths[i].Count - 1];
            }
            MapNodes dest = best_dest[i];
            int dest_ix = GetDestIndex(dest);

            if (i == cur_node) continue;
            if ((reroute_superior_history.ContainsKey(closest_node) && reroute_superior_history[closest_node].Contains(i)))
            {
                //print("superior "+closest_node.gameObject.name + i.gameObject.name);
                continue;
            }
            if (reroute_inferior_history.ContainsKey(closest_node) && reroute_inferior_history[closest_node].Contains(i))
            {   // This is simply using the cached results from the reroute history dictionaries.
                //print("inferior "+closest_node.gameObject.name + i.gameObject.name);
                closest_node = i;
                closest_node_dist = paths_length[i];
                continue;
            }

            // Having the exact same length is unlikely, but if so, closest_node wins.
            if (closest_node_dist <= paths_length[i]) AddRerouteHistory(i,closest_node);
            else {
                AddRerouteHistory(closest_node,i);
                closest_node = i;
                closest_node_dist = paths_length[i];
            }
        }

        if (closest_node == cur_node || paths_length[cur_node] - closest_node_dist < reroute_thres) return;
        
        // Anything below assumes that a reroute is guaranteed.

        MapNodes user_closest_node = closest_node;
        float user_closest_node_dist = Vector3.Distance(closest_node.transform.position, Map.main.user_pin.transform.position);
        foreach(var i in valid_nodes_nearby)
        {
            if (i == closest_node) continue;
            if (paths[closest_node].Count >= paths[i].Count) continue;

            if (!paths[closest_node].Except(paths[i]).Any())
            {
                float user_current_node_dist = Vector3.Distance(i.transform.position, Map.main.user_pin.transform.position);
                if (user_current_node_dist < user_closest_node_dist)
                {
                    user_closest_node = i;
                    user_closest_node_dist = user_current_node_dist;
                }
            }
        }
        Map.main.SetupRoute(null, paths[user_closest_node]);
        print("rerouting...");




        /*
        // Just in case the current target node does not have data for its route.
        if (!paths[ix].ContainsKey(Map.main.current_route[Map.main.current_route_target_ix]))
            paths[ix][Map.main.current_route[Map.main.current_route_target_ix]] = new List<MapNodes>(Map.main.current_route_remaining);

        float target_dist_to_dest_sqr = ( Map.main.current_route[Map.main.current_route.Count - 1].transform.position - 
                Map.main.current_route[Map.main.current_route_target_ix].transform.position ).sqrMagnitude;
        MapNodes closest_node = Map.main.current_route[Map.main.current_route_target_ix];
        float closest_node_dist_to_dest_sqr = target_dist_to_dest_sqr;
        foreach (var i in valid_nodes_nearby)
        {
            if (i == Map.main.current_route[Map.main.current_route_target_ix]) continue;
            float dist_to_dest_sqr = ( Map.main.current_route[Map.main.current_route.Count - 1].transform.position - 
                i.transform.position ).sqrMagnitude;
            if (reroute_inferior_history.ContainsKey(closest_node) && reroute_inferior_history[closest_node].Contains(i))
            {   // This is simply using the cached results from the reroute history dictionaries.
                //print("inferior"+closest_node.gameObject.name + i.gameObject.name);
                closest_node = i;
                closest_node_dist_to_dest_sqr = dist_to_dest_sqr;
                continue;
            }
            if ((reroute_superior_history.ContainsKey(closest_node) && reroute_superior_history[closest_node].Contains(i)))
            {
                //print("superior"+closest_node.gameObject.name + i.gameObject.name);
                continue;
            }

            if ( target_dist_to_dest_sqr - dist_to_dest_sqr >= reroute_thres)
            {
                if (!paths.ContainsKey(i))
                    paths[i] = PathBuilder.GetPath(i, Map.main.current_route[Map.main.current_route.Count - 1], MapNodes.nodes);
                bool new_path_inferior = false;
                foreach (var j in paths[i])
                {
                    // This means that if the new route contains the current target node, then this route
                    //      is for sure longer than the current one, hence marking this node's route to be inferior
                    //      to the current target node's route in terms of walk distance.
                    if (j == Map.main.current_route[Map.main.current_route_target_ix])
                    {
                        new_path_inferior = true;
                        AddRerouteHistory(i,j);
                        break;
                    }
                }
                if (new_path_inferior) continue;
                AddRerouteHistory(Map.main.current_route[Map.main.current_route_target_ix],i);

                if (dist_to_dest_sqr < closest_node_dist_to_dest_sqr)
                {
                    AddRerouteHistory(closest_node,i);
                    closest_node = i;
                    closest_node_dist_to_dest_sqr = dist_to_dest_sqr;
                } else AddRerouteHistory(i,closest_node);
            }
        }

        if (closest_node != Map.main.current_route[Map.main.current_route_target_ix])
        {
            Map.main.SetupRoute(null, paths[closest_node]);
            print("rerouting...");
            DestinationReset();
        }*/
    }

    void AddRerouteHistory(MapNodes inferior, MapNodes superior)
    {
        if (!reroute_inferior_history.ContainsKey(inferior)) reroute_inferior_history[inferior] = new HashSet<MapNodes>();
        reroute_inferior_history[inferior].Add(superior);
        if (!reroute_superior_history.ContainsKey(superior)) reroute_superior_history[superior] = new HashSet<MapNodes>();
        reroute_superior_history[superior].Add(inferior);
    }

    // All the reroute history and paths have to be wiped when the destination changes.
    public void DestinationReset()
    {
        reroute_inferior_history.Clear();
        reroute_superior_history.Clear();
        paths.Clear();
        slow_update_elapsed = 0f; // Cuz why not.
        // invalid_nodes need no cleaning. Its data will be useful for the whole runtime.
        // node_dict also needs no cleaning, we need those collider -> MapNodes references.
        prev_target_node = null;
    }
}
