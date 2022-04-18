using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    public static Map main;

    public GameObject map_view; // This is the parent object that contains the map
    public GameObject map_obj; // the 2D map object
    public UserControl user_pin; //The user's pin
    public GameObject user_pin_inactive;

    public MapStructureTransition structure_transition;
    //public MapStructures current_structure; // Supplied by MapStructureTransition.

    public Transform path_links_list; // This is the parent to instantiate the route's links.
    public GameObject path_tobedone_prefab; // This prefab shows where the user should go.
    public Transform path_target_link; // This line shows the current node the user should be going.

    public List<MapNodes> current_route{get; private set;} // Count's always 1 higher than the links.
    public List<GameObject> current_route_links{get; private set;} = new List<GameObject>();
    public int current_route_target_ix{get; private set;} // This is an index of current_route getting the target node.
    // route[range(Count - index)] is being calculated way too frequently. Caching.
    public List<MapNodes> current_route_remaining{get; private set;} = new List<MapNodes>();
    public bool path_link_using_gps; // If true, the target node will try to link to user's GPS location.



    void Awake()
    {
        main = this;
    }
    void Start()
    {
        MapScroller.main.ZoomMap(0);
        MapMenu.main.OnSearchClick();
    }
    void OnEnable() { if (map_view != null && map_obj != null) { map_view.SetActive(true); map_obj.SetActive(true); } }
    void OnDisable() { if (map_view != null && map_obj != null) { map_view.SetActive(false); map_obj.SetActive(false); } }

    void Update()
    {
        UpdateMapView();
        if (path_target_link.gameObject.activeSelf) UpdateTargetNode();
        if (!user_pin.gameObject.activeInHierarchy) user_pin_inactive.transform.position = user_pin.transform.position;
    }



    void UpdateMapView()
    {
        // If using fake gps, might as well let the editor move it instead.
        if (!Locations.main.use_fake_gps && Locations.main.use_real_time_gps_tracking)
            user_pin.transform.position = new Vector3(
                Locations.main.displ.x,
                Locations.main.displ.z,
                -0.1f);

        // If it's not on, it'll use the fake rotation for now.
        //if (Locations.main.loc_service_on) 
        user_pin.transform.rotation = Quaternion.Euler(0,0,-Locations.main.bearing_current);
    }





    // Since multiple functions need it, might as well make a lazy function.
    // If target_link is given, instead of instantiating, it'll only modify that link for a new purpose.
    //      But make sure the target_link is used and later syncs up with current_route_links and the target_ix.
    GameObject CreateLink(MapNodes node1, MapNodes node2, GameObject target_link = null)
    {
        Vector3 rel_pos = node1.transform.InverseTransformPoint(node2.transform.position);
        GameObject new_link = target_link == null ? Instantiate(path_tobedone_prefab, 
            (node1.transform.position + node2.transform.position) / 2,
            Quaternion.Euler(0,0, 90 - Mathf.Atan2(rel_pos.x, rel_pos.y) * Mathf.Rad2Deg),
            path_links_list
            ) : target_link;
        new_link.transform.localScale = new Vector3(
            Vector3.Distance(node1.transform.position, node2.transform.position), 
            new_link.transform.localScale.y,1f);
        if (target_link != null)
        {
            target_link.transform.position = (node1.transform.position + node2.transform.position) / 2;
            target_link.transform.rotation = Quaternion.Euler(0,0, 90 - Mathf.Atan2(rel_pos.x, rel_pos.y) * Mathf.Rad2Deg);
            target_link.SetActive(true); // It's assumed that it's used, aka inactive.
        }

        return new_link;
    }

    // Note that it doesn't define start_node. This script will deal with it.
    // preset_route is here since the path is already calculated during reroute. Feel free to null the end node if using this.
    public void SetupRoute(MapNodes end_node, List<MapNodes> preset_route = null)
    {
        foreach (var i in current_route_links) Destroy(i); // This is to clean up the old links.
        current_route_links.Clear();

        

        if (preset_route != null) current_route = preset_route;
        else
        {
            MapNodes start_node = PathBuilder.GetClosestPoint(user_pin.transform.position, MapNodes.nodes);
            current_route = PathBuilder.GetPath(start_node, end_node, MapNodes.nodes);

            if (current_route != null && MapNodes.main_node_alts.ContainsKey((MainNode) end_node))
            {   
                // This is basically UpdatePath() from UserControl.
                List<MapNodes> closest_dest_path = new List<MapNodes>(current_route);
                float closest_dest_dist = PathBuilder.GetPathLength(closest_dest_path);
                foreach (var i in MapNodes.main_node_alts[(MainNode)end_node])
                {
                    List<MapNodes> current_dest_path = PathBuilder.GetPath(start_node, i, MapNodes.nodes);
                    float current_dest_dist = PathBuilder.GetPathLength(current_dest_path);
                    if (current_dest_dist < closest_dest_dist)
                    {
                        closest_dest_path = current_dest_path;
                        closest_dest_dist = current_dest_dist;
                    }
                }
                current_route = closest_dest_path;
            }
        }

        if (preset_route != null) end_node = preset_route[preset_route.Count - 1];
        if (current_route == null || current_route.Count < 1)
        {   // It means a route cannot be found, which is normally impossible. Hence a bug is inside.
            FloatMessage.Send("Route not found...");
            return;
        }
        for (var i = 0; i < current_route.Count - 1; i++)
            current_route_links.Add(CreateLink(current_route[i],current_route[i+1]));

        path_target_link.gameObject.SetActive(path_link_using_gps);
        user_pin.gameObject.SetActive(true);
        user_pin_inactive.SetActive(false);
        MapScroller.main.DragMapReset(); // I don't see why not.
        current_route_target_ix = 0;
        current_route_remaining = new List<MapNodes>(current_route); // It includes the target node.
        CheckInbetweenNodes();
    }

    // This is much less expensive than SetupRoute(). Use only when it's only just adding an extra node to the front.
    // Running this assumes that current_route has at least 2 items.
    public void PrependRoute(MapNodes node)
    {
        if (current_route_target_ix > 0)
        {
            if (current_route[current_route_target_ix - 1] == node)
            {   // Just in case the user simply moved back to the previous node of the route.
                current_route_target_ix--;
                CreateLink(current_route[current_route_target_ix], 
                    current_route[current_route_target_ix+1],
                    current_route_links[current_route_target_ix]);
                current_route_remaining = current_route.GetRange(current_route_target_ix, 
                    current_route.Count - current_route_target_ix);
                return;
            } else {
                // Running this means that the input node is another node which a link towards the target node.
                //      This means a reroute, but due to the simple operations, this function will handle it as an exception.
                current_route = new List<MapNodes>{node};
                current_route.AddRange(current_route_remaining);
                current_route_target_ix--; // Since it's needed before turning to 0, and needs to retarget to this new node.
                foreach (var i in current_route_links.GetRange(0,current_route_target_ix)) Destroy(i);
                current_route_links = new List<GameObject>(
                    current_route_links.GetRange(current_route_target_ix, current_route_links.Count - current_route_target_ix));
                CreateLink(current_route[0], current_route[1], current_route_links[current_route_target_ix]);
                current_route_target_ix = 0;
            }
        } else {
            // Running this already assumed that current_route_target_ix == 0, hence all the 0s and 1s below.
            current_route.Insert(0, node);
            current_route_links.Insert(0, CreateLink(current_route[0], current_route[1]));
        }
        current_route_remaining = new List<MapNodes>(current_route);
    }

    // This is basically what's used to show the next target node. But also used when the route is cut short somehow.
    // The amount is the number of routes to be trimmed (i.e. links deactivated, target_ix moved), 
    //      assuming to be within range and positive.
    public void TrimRoute(int amount)
    {
        if (current_route_target_ix + amount >= current_route.Count)
        {
            ArrivedDestination();
            return;
        }
        for (var i = 0; i < amount; i++)
        {
            current_route_links[current_route_target_ix + i].SetActive(false);
        }
        current_route_target_ix += amount;
        current_route_remaining = current_route.GetRange(current_route_target_ix, 
            current_route.Count - current_route_target_ix);
    }

    // This moves the link between user position and the target node.
    public void UpdateTargetNode()
    {
        path_target_link.position = (user_pin.transform.position + current_route[current_route_target_ix].transform.position) / 2;
        Vector3 rel_pos = current_route[current_route_target_ix].transform.InverseTransformPoint(user_pin.transform.position);
        path_target_link.rotation = Quaternion.Euler(0,0, 90 - Mathf.Atan2(rel_pos.x, rel_pos.y) * Mathf.Rad2Deg);
        path_target_link.localScale = new Vector3(
            Vector3.Distance(user_pin.transform.position, current_route[current_route_target_ix].transform.position), 
            path_target_link.localScale.y,1f);
    }

    // Sometimes the user might end up inside the path link between two nodes. And sometimes it makes more sense to move to
    //      the next one instead of turning around and reach the spot. This one will Trim the route when that happens.
    // Note that this function assumes that the user is between the target node and the node after that.
    // Use it when the route updates but the user isn't on top of any nodes.
    public void CheckInbetweenNodes()
    {
        // There will be no "inbetweens" if the target is the destination.
        if (current_route_target_ix + 1 >= current_route.Count) return;

        float target_angles = Vector2.Angle(
            (Vector2) (current_route[current_route_target_ix + 1].transform.position - 
                current_route[current_route_target_ix].transform.position),
            (Vector2) (user_pin.transform.position - 
                current_route[current_route_target_ix].transform.position) );

        // The angle threshold can be hard-coded for now.
        if (target_angles < 60) TrimRoute(1);
    }

    public void ArrivedDestination()
    {
        MainNode dest_node = current_route[current_route.Count - 1] as MainNode;
        if (dest_node == null) dest_node = ((AlternateNode) current_route[current_route.Count - 1]).alt_spot_of;

        string dest_name = dest_node.disp_name.Length > 0 ? dest_node.disp_name : dest_node.id;
        FloatMessage.Send(string.Format("You arrived at \n{0}.", dest_name));
        user_pin_inactive.SetActive(true);
        user_pin.gameObject.SetActive(false);
        path_target_link.gameObject.SetActive(false);
        MapMenu.main.OnSearchClick();
    }
}
