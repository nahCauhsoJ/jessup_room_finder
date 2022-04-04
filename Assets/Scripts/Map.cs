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
    public GameObject cam_target; // The actual spot the virtual camera is aiming at. For offsets.

    public MapStructureTransition structure_transition;
    public MapStructures current_structure;

    public Transform path_links_list; // This is the parent to instantiate the route's links.
    public GameObject path_tobedone_prefab; // This prefab shows where the user should go.
    public Transform path_target_link; // This line shows the current node the user should be going.
    


    // Previously we set an origin for the GPS converter. Now we go to that location irl and mark that spot on the map.
    //      Calibrate map_obj's transform so that the dead center of the device means 0,0 in GPS measurement.
    //      This value below stores the pos.
    public Vector2 map_origin_offset;
    
    public float map_scale; // Unlike map_scale_orig, this is an offset in z-pos instead of its actual value.
    public float map_scale_const; // Just a multiplier.
    public Vector2 map_scale_range; // .x: min value, .y: max value. Rmb that it represents the z-pos, not a percentage.
    float map_scale_current; // map_scale is the total z-pos offset while this resets when finger is lifted or interrupted.
    float map_scale_orig; // This is not transform's scale. It's just the map's z-pos.

    public Vector2 map_offset;
    public float map_offset_const;
    public Vector2 map_displ_scale; // Assuming the scale is 0, this is the displacement (from actual GPS) over Unity's coords.
    Vector2 map_offset_current; // map_offset is the total offset from origin while this resets when finger is lifted or interrupted.
    Vector2 map_offset_orig;
    Vector2 map_offset_scale; // This is calculated by a manual input times the scale. Used to scale the displacement from Locations.
    Quaternion map_rot_orig; // For now let's just leave it at identity

    public Transform user_pos{get; private set;} // The user's pin location. Too lazy to put an extra .transform every time.



    public List<MapNodes> current_route{get; private set;} // Count's always 1 higher than the links.
    public List<GameObject> current_route_links{get; private set;} = new List<GameObject>();
    public int current_route_target_ix{get; private set;} // This is an index of current_route getting the target node.
    // route[range(Count - index)] is being calculated way too frequently. Caching.
    public List<MapNodes> current_route_remaining{get; private set;} = new List<MapNodes>();
    public bool path_link_using_gps; // If true, the target node will try to link to user's GPS location.



    void Awake()
    {
        main = this;
        //map_rot_orig = user_pos.transform.rotation;

        map_scale_orig = map_view.transform.position.z;
        map_offset_orig = map_view.transform.position;
        map_offset_scale = map_displ_scale;

        user_pos = user_pin.transform;
    }
    void Start()
    {
        ZoomMap(0);
        MapMenu.main.OnSearchClick();
    }
    void OnEnable() { if (map_view != null && map_obj != null) { map_view.SetActive(true); map_obj.SetActive(true); } }
    void OnDisable() { if (map_view != null && map_obj != null) { map_view.SetActive(false); map_obj.SetActive(false); } }

    void Update()
    {
        UpdateMapView();
        if (path_target_link.gameObject.activeSelf && path_link_using_gps) UpdateTargetNode();
        if (!user_pin.gameObject.activeInHierarchy) user_pin_inactive.transform.position = user_pin.transform.position;
    }






    public void DragMap(Vector2 drag_dist_ratio)
    {
        //was_dragging = true;
        //map_offset_current = drag_dist_ratio * map_offset_const * Mathf.Max(map_scale_orig - map_scale, 1f);
        map_offset_current = drag_dist_ratio;
    }
    public void DragMapEnd() { map_offset += map_offset_current; map_offset_current = Vector2.zero; }
        //was_dragging = false; //was_zooming = false; }
    public void DragMapReset()
    {
        map_offset = (Vector2) user_pin.transform.position;
    }

    public void ZoomMap(float zoom_dist_ratio)
    {
        //was_zooming = true;
        map_scale_current = zoom_dist_ratio * map_scale_const;

        if (map_scale + map_scale_current + map_scale_orig < map_scale_range.x)
            map_scale_current = map_scale_range.x - map_scale - map_scale_orig;
        if (map_scale + map_scale_current + map_scale_orig > map_scale_range.y)
            map_scale_current = map_scale_range.y - map_scale - map_scale_orig;

        float total_map_scale = map_scale + map_scale_current + map_scale_orig;
        //map_offset_scale = map_displ_scale * total_map_scale;
    }
    public void ZoomMapEnd() { map_scale += map_scale_current; map_scale_current = 0; }
    //was_zooming = false; }

    void UpdateMapView()
    {
        /* cam_target.transform.position = new Vector3(
            user_pin.transform.position.x + map_offset.x + map_offset_current.x, 
            user_pin.transform.position.y + map_offset.y + map_offset_current.y, 
            map_scale + map_scale_current + map_scale_orig
        ); */
        cam_target.transform.position = new Vector3(
            map_offset.x + map_offset_current.x, 
            map_offset.y + map_offset_current.y,
            map_scale + map_scale_current + map_scale_orig
        );

        // If using fake gps, might as well let the editor move it instead.
        if (!Locations.main.use_fake_gps && Locations.main.use_real_time_gps_tracking)
            user_pos.transform.position = new Vector3(
                map_origin_offset.x + Locations.main.displ.x / map_offset_scale.x,
                map_origin_offset.y + Locations.main.displ.z / map_offset_scale.y,
                -0.1f);

        // If it's not on, it'll use the fake rotation for now.
        //if (Locations.main.loc_service_on) 
        user_pos.transform.rotation = Quaternion.Euler(0,0,-Locations.main.bearing_current);
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

        current_route = preset_route == null ?
            PathBuilder.GetPath(
            PathBuilder.GetClosestPoint(user_pos.transform.position, MapNodes.nodes),
            end_node,
            MapNodes.nodes) : preset_route;
        if (preset_route != null) end_node = preset_route[preset_route.Count - 1];
        if (current_route == null)
        {   // It means a route cannot be found, which is normally impossible. Hence a bug is inside.
            print("Route not found");
            return;
        }
        for (var i = 0; i < current_route.Count - 1; i++)
            current_route_links.Add(CreateLink(current_route[i],current_route[i+1]));

        path_target_link.gameObject.SetActive(path_link_using_gps);
        user_pin.gameObject.SetActive(true);
        user_pin_inactive.SetActive(false);
        DragMapReset(); // I don't see why not.
        UserControl.main.InitiateDestinations(new MapNodes[]{end_node});
        current_route_target_ix = 0;
        current_route_remaining = new List<MapNodes>(current_route); // It includes the target node.
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
        Vector3 rel_pos = current_route[current_route_target_ix].transform.InverseTransformPoint(user_pos.transform.position);
        path_target_link.rotation = Quaternion.Euler(0,0, 90 - Mathf.Atan2(rel_pos.x, rel_pos.y) * Mathf.Rad2Deg);
        path_target_link.localScale = new Vector3(
            Vector3.Distance(user_pos.transform.position, current_route[current_route_target_ix].transform.position), 
            path_target_link.localScale.y,1f);
    }

    public void ArrivedDestination()
    {
        MainNode dest_node = (MainNode) current_route[current_route.Count - 1];
        string dest_name = dest_node.disp_name.Length > 0 ? dest_node.disp_name : dest_node.id;
        FloatMessage.Send(string.Format("You arrived at \n{0}.", dest_name));
        user_pin_inactive.SetActive(true);
        user_pin.gameObject.SetActive(false);
        path_target_link.gameObject.SetActive(false);
        MapMenu.main.OnSearchClick();
    }
}
