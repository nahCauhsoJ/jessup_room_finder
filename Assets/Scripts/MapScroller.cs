using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// MapControls is to control mobile inputs. This is where the map drags and zooms.
public class MapScroller : MonoBehaviour
{
    public static MapScroller main;

    public GameObject cam_target; // The actual spot the virtual camera is aiming at. For offsets.

    public UnityEngine.UI.Image touchscreen_area; // This is the region where users can drag and zoom. Also used to set borders.

    // This shrinks the movable area a little more so that the user doesn't see the black background with a quick swipe.
    // Please make sure this one is positive in both ways. It modifies map_world_corners on Awake(). And it's world units.
    public Vector2 map_border_padding;

    // (deprecated)
    // Previously we set an origin for the GPS converter. Now we go to that location irl and mark that spot on the map.
    //      Calibrate map_obj's transform so that the dead center of the device means 0,0 in GPS measurement.
    //      This value below stores the pos.
    //public Vector2 map_origin_offset;
    
    public float map_scale; // Unlike map_scale_base, this is an offset in z-pos instead of its total value.
    public float map_scale_const; // Just a multiplier.
    public Vector2 map_scale_range; // .x: min value, .y: max value. Rmb that it represents the z-pos, not a percentage.
    Vector2 map_scale_range_orig; // Since map_scale_range can be changed, better to give it a point to resume.
    float map_scale_current; // map_scale is the total z-pos offset while this resets when finger is lifted or interrupted.
    float map_scale_base; // This is not transform's scale. It's just the map's z-pos.

    public Vector2 map_offset;
    Vector2 map_offset_current; // map_offset is the total offset from origin while this resets when finger is lifted or interrupted.
    Vector2 map_offset_base;
    Quaternion map_rot_orig; // For now let's just leave it at identity

    // This is what defines where the user can scroll to. Beyond this will push cam_target back.
    Vector3[] map_world_corners = new Vector3[4];




    // To make sure the map isn't moving when user is clicking on something else, all the movement occurs only
    //      when this is true.
    public bool is_touching_map{get; private set;}

    void Awake()
    {
        main = this;
        touchscreen_area.rectTransform.GetWorldCorners(map_world_corners);
        map_world_corners[0] += (Vector3) map_border_padding;
        map_world_corners[1] += new Vector3(map_border_padding.x, -map_border_padding.y, 0);
        map_world_corners[2] -= (Vector3) map_border_padding;
        map_world_corners[3] -= new Vector3(map_border_padding.x, -map_border_padding.y, 0);
        //map_rot_orig = Map.main.user_pin.transform.rotation;
    }
    void Start()
    {
        map_scale_base = Map.main.map_view.transform.position.z;
        map_offset_base = Map.main.map_view.transform.position;
    }






    public void OnPointerDown() { is_touching_map = true; }
    public void OnPointerUp() { is_touching_map = false; }
    public void Update()
    {
        cam_target.transform.position = new Vector3(
            map_offset.x + map_offset_current.x,
            map_offset.y + map_offset_current.y,
            map_scale + map_scale_current + map_scale_base
        );

        map_offset += ClampedCameraPosition();
    }






    public void DragMap(Vector2 drag_dist_ratio)
    {
        if (!is_touching_map) return;
        map_offset_current = drag_dist_ratio;
    }
    public void DragMapEnd() { map_offset += map_offset_current; map_offset_current = Vector2.zero; }
    public void DragMapReset()
    {
        map_offset = (Vector2) Map.main.user_pin.transform.position;
    }

    public void ZoomMap(float zoom_dist_ratio)
    {
        if (!is_touching_map) return;
        map_scale_current = zoom_dist_ratio * map_scale_const;

        if (map_scale + map_scale_current + map_scale_base < map_scale_range.x)
            map_scale_current = map_scale_range.x - map_scale - map_scale_base;
        if (map_scale + map_scale_current + map_scale_base > map_scale_range.y)
            map_scale_current = map_scale_range.y - map_scale - map_scale_base;
    }
    public void ZoomMapEnd() { map_scale += map_scale_current; map_scale_current = 0; }
    // This one instantly cuts to a scale, but it's not too different
    //      from the user zooming the map with their fingers.
    // Note that new_map_scale is still just an offset from map_scale_base
    // Also note that the new scale doesn't get clamped until the user zooms the map manually, so for
    //      visual convenience make sure the new scale is within range.
    public void ZoomForced(float new_map_scale)
    {
        map_scale = new_map_scale - map_scale_current;
        MapControls.UpdateScreenToWorldRatio();
    }



    // ClampedCameraPosition is used for repositioning, this one's just for detection.
    bool IsOutOfMap()
    {
        Vector2 bottom_left = MapControls.ScreenToMapPoint(Vector2.zero);
        if (bottom_left.x < map_world_corners[0].x) return true;
        if (bottom_left.y < map_world_corners[0].y) return true;
        Vector2 top_right = MapControls.ScreenToMapPoint(new Vector2(Screen.currentResolution.width, Screen.currentResolution.height));
        if (top_right.x > map_world_corners[2].x) return true;
        if (top_right.y > map_world_corners[2].y) return true;
        return false;
    }

    // This just gives an offset to subtract from map_offset, so that all 4 corners of the screen are inside the map.
    Vector2 ClampedCameraPosition()
    {
        Vector2 return_offset = Vector2.zero;
        Vector2 bottom_left = MapControls.ScreenToMapPoint(Vector2.zero);
        Vector2 top_right = MapControls.ScreenToMapPoint(new Vector2(Screen.currentResolution.width, Screen.currentResolution.height));

        if (bottom_left.x < map_world_corners[0].x) return_offset.x -= bottom_left.x - map_world_corners[0].x;
        else if (top_right.x > map_world_corners[2].x) return_offset.x -= top_right.x - map_world_corners[2].x;
        if (bottom_left.y < map_world_corners[0].y) return_offset.y -= bottom_left.y - map_world_corners[0].y;
        else if (top_right.y > map_world_corners[2].y) return_offset.y -= top_right.y - map_world_corners[2].y;

        return return_offset;
    }
}
