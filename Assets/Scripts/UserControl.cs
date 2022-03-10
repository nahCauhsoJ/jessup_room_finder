using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserControl : MonoBehaviour
{
    public static UserControl main;

    public float arrival_radius; // The radius (in world space units) it needs to be consider arriving at a nearby node.
    // If the distance ratio of the target node and the closest node exceeds this value, a reroute will occur.
    // Note that the comparison is the square distance. Also it is [target node : closest node], so reroute occurs when it's > 1.
    public float reroute_ratio;

    int nodes_layer;
    Collider[] nodes_nearby = new Collider[10]; // 10 should suffice, I hope?
    List<MapNodes> valid_nodes_nearby = new List<MapNodes>();
    Dictionary<Collider, MapNodes> nodes_dict = new Dictionary<Collider, MapNodes>();
    List<Collider> invalid_nodes = new List<Collider>();
    float slow_update_elapsed;
    MapNodes prev_target_node; // We have to stop CheckNodeArrival from spamming the target_node until out of range.

    // To prevent comparing with the same two points again, here is a history of it to cache the result.
    // inferior = This node is inferior to the following nodes, superior = This node is superior to the followin nodes.
    Dictionary<MapNodes, HashSet<MapNodes>> reroute_inferior_history = new Dictionary<MapNodes, HashSet<MapNodes>>();
    Dictionary<MapNodes, HashSet<MapNodes>> reroute_superior_history = new Dictionary<MapNodes, HashSet<MapNodes>>();
    // PathBuilder.GetPath() itself is an expensive process, here's the cached results for the rerouted nodes.
    Dictionary<MapNodes, List<MapNodes>> paths = new Dictionary<MapNodes, List<MapNodes>>();

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
            if (node == null && !invalid_nodes.Contains(i))
            {
                node = nodes_dict.ContainsKey(i) ? nodes_dict[i] : i.GetComponent<MapNodes>();
                if (node != null) nodes_dict[i] = node;
                else invalid_nodes.Add(i); // If not, GetComponent will run on that invalid node every tick.
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
        if (prev_target_node == null && Map.main.current_route.Count > 2) CompareRoutes();
    }

    void CheckNodeArrival(List<MapNodes> nodes)
    {
        MapNodes closest_node_found = null;
        float closest_node_found_sqr_dist = 6969f; // 6969 should suffice, I hope?
        bool node_links_target = false;
        foreach (var i in nodes)
        {
            // If not null, it means the user is within target node. There I'm ignoring all nodes except the target,
            //   until out of range.
            if (prev_target_node != null && prev_target_node != i) continue;

            float sqr_dist = (Map.main.user_pos.position - i.transform.position).sqrMagnitude;
            if ( sqr_dist <= arrival_radius * arrival_radius )
            {
                if (prev_target_node != null) return; // Rmb that when != null, only prev_target_node does the checking.

                if (closest_node_found == null) { closest_node_found = i; closest_node_found_sqr_dist = sqr_dist; }
                else if (sqr_dist >= closest_node_found_sqr_dist) continue; // Means that there's a closer node than this.
                else { closest_node_found = i; closest_node_found_sqr_dist = sqr_dist; }

                if (Map.main.current_route[Map.main.current_route_target_ix].links.Contains(i))
                { node_links_target = true; break; } // break cuz node that links to target takes priority.

                if (closest_node_found == Map.main.current_route[Map.main.current_route_target_ix])
                {
                    Map.main.TrimRoute(1);
                    prev_target_node = i;
                    return;
                }
            } else if (prev_target_node != null) {
                // Yes this gives a 1-tick free time. Not like it matters.
                prev_target_node = null;
                return; // This return statement is why it needs a condition.
            }
        }

        if (closest_node_found != null)
        {
            if (node_links_target)
            {   // i = 1 since 0 refers to the target itself, which is checked in above loop.
                for (var i = 1; i < Map.main.current_route_remaining.Count; i++)
                {
                    if (closest_node_found == Map.main.current_route[Map.main.current_route_target_ix + i])
                    {
                        Map.main.TrimRoute(i+1);
                        return;
                    }
                }
                
                if (Map.main.current_route[Map.main.current_route_target_ix].links.Contains(closest_node_found))
                    // Note that the loop above deals with the case when the link is the next target.
                    Map.main.PrependRoute(closest_node_found);
            } else {
                Map.main.SetupRoute(Map.main.current_route[Map.main.current_route.Count - 1]);
            }
        }
    }

    // Running this already assumes that there is another node after the target node in the route. Hence + 1 is safe.
    void CompareRoutes()
    {
        // Just in case the current target node does not have data for its route.
        if (!paths.ContainsKey(Map.main.current_route[Map.main.current_route_target_ix]))
            paths[Map.main.current_route[Map.main.current_route_target_ix]] = new List<MapNodes>(Map.main.current_route_remaining);

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
                closest_node = i;
                closest_node_dist_to_dest_sqr = dist_to_dest_sqr;
                continue;
            }

            if ( target_dist_to_dest_sqr / dist_to_dest_sqr < reroute_ratio * reroute_ratio)
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
        }
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
