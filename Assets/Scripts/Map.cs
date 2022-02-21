using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    public static Map main;

    public GameObject map_view; // This is the parent object that contains the map
    public GameObject map_obj; // the 2D map object
    public GameObject user_pos; // The user's pin location
    public GameObject cam_target; // The actual spot the virtual camera is aiming at. For offsets.
    // Previously we set an origin for the GPS converter. Now we go to that location irl and mark that spot on the map.
    //      Calibrate map_obj's transform so that the dead center of the device means 0,0 in GPS measurement.
    //      This value below stores the pos.
    public Vector2 map_origin_offset{get; private set;}
    
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

    Quaternion map_rot_orig;

    Vector2 screen; // Screen resolution, obtained on Awake().
    
    public float hold_time_thres = 1f; // Temp variable to set the time needed for a tap to be a hold instead.
    public int move_dist_thres = 10; // Temp variable to set distance needed (in pixels) to be considered moving finger.
    public int swipe_dist_thres = 10; // Temp variable to set drag distance needed (in pixels) to be considered a swipe.

    bool was_dragging; // Tells if... well, if the player is dragging the screen.
    bool was_zooming; // Tells if the 2 fingers moved and did the zooming.
    Touch fing1;
    Touch fing2;
    bool fing1_down;
    bool fing2_down;
    Vector2 fing1_down_pos; // Touch.rawPosition isn't working. I'm putting this here.
    Vector2 fing2_down_pos;
    float fing1_hold_time;
    bool fing1_moved;
    bool fing1_holding; // If true, user is holding the finger istead of tapping.

    void Awake()
    {
        main = this;
        map_rot_orig = user_pos.transform.rotation;//map_obj.transform.rotation;
        screen = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

        map_origin_offset = (Vector2) user_pos.transform.position;
        map_scale_orig = map_view.transform.position.z;
        map_offset_orig = map_view.transform.position;
        map_offset_scale = map_displ_scale;

        fing1.phase = TouchPhase.Canceled;
        fing2.phase = TouchPhase.Canceled;
    }


    void OnEnable()
    {
        if (map_view != null) map_view.SetActive(true);
    }

    void OnDisable()
    {
        if (map_view != null) map_view.SetActive(false);
    }

    void Update()
    {
        if (Input.touchCount == 0)
        {
            
        }
        if (Input.touchCount >= 1) fing1 = Input.GetTouch(0);
        if (Input.touchCount >= 2) fing2 = Input.GetTouch(1);

        if (fing1.phase == TouchPhase.Began)
        {
            OnFingerDown(1);
            fing1_down = true;
            fing1_down_pos = fing1.position;
        }
        if (fing2.phase == TouchPhase.Began)
        {
            OnFingerDown(2);
            OnTap(2); // There's no need to check 2nd finger holding, so let's leave it here.
            fing2_down = true;
            fing2_down_pos = fing2.position;
        }

        if (fing1_down)
        {
            fing1_hold_time += Time.deltaTime;
            if (!fing1_holding && !fing1_moved && fing1_hold_time >= hold_time_thres)
            {
                OnHold();
                fing1_holding = true;
            }
        }

        if (fing1.phase == TouchPhase.Moved)
        {
            if (Vector2.Distance(fing1_down_pos, fing1.position) >= move_dist_thres) fing1_moved = true;
            if (fing1_holding && fing1_moved) OnHoldDrag(1); else OnTapDrag(1);
        }

        if (fing2.phase == TouchPhase.Moved)
        {
            OnHoldDrag(2);
        }

        if (fing1.phase == TouchPhase.Ended)
        {
            if (fing1_down)
            {
                if (!fing1_holding && fing1_hold_time < hold_time_thres)
                {
                    if (fing1_moved) OnSwipe(); else OnTap(1);
                }
                OnFingerUp(1);
                fing1_moved = false;
                fing1_holding = false;
                fing1_down = false;
                fing1_hold_time = 0;
            }
        }
        if (fing2.phase == TouchPhase.Ended)
        {
            if (fing2_down)
            {
                OnFingerUp(2);
                fing2_down = false;
            }
        }

        cam_target.transform.localPosition = new Vector3(
            map_offset.x + map_offset_current.x, 
            map_offset.y + map_offset_current.y, 
            map_scale + map_scale_current + map_scale_orig
        );
        user_pos.transform.position = new Vector3(
            map_origin_offset.x + Locations.main.displ.x / map_offset_scale.x,
            map_origin_offset.y + Locations.main.displ.z / map_offset_scale.y,
            -0.1f);

        // If it's not on, it'll use the fake rotation for now.
        //if (Locations.main.loc_service_on) 
        user_pos.transform.rotation = map_rot_orig * Quaternion.Euler(0,0,-Locations.main.bearing_current);
    }

    void OnFingerDown(int finger)
    {
        switch(finger)
        {
            case 1://print("down 1");
                break;
            case 2://print("down 2");
                break;
        }
    }

    void OnFingerUp(int finger)
    {
        switch(finger)
        {
            case 1://print("up 1");
                map_offset += map_offset_current;
                map_offset_current = Vector2.zero;
                if (!fing2_down && fing1_holding)
                {
                    //print(new Vector4(fing1_down_pos.x,fing1_down_pos.y,fing1.position.x,fing1.position.y));
                    map_scale += map_scale_current;
                    map_scale_current = 0;
                }
                was_dragging = false;
                was_zooming = false;
                break;
            case 2://print("up 2");
                //print(new Vector4(fing1_down_pos.x,fing1_down_pos.y,fing1.position.x,fing1.position.y));
                map_scale += map_scale_current;
                map_scale_current = 0;
                was_zooming = false;
                break;
        }
    }

    // finger = 1: first finger
    // finger = 2: second finger
    void OnTap(int finger)
    {
        switch(finger)
        {
            case 1://print("tap 1");
                break;
            case 2://print("tap 2");
                break;
        }
    }

    void OnHold()
    {//print("hold");
        fing1_holding = true;
    }

    void OnSwipe()
    {
        //print("swipe");
    }

    void OnTapDrag(int finger)
    {
        switch(finger)
        {
            case 1://print("tap drag 1");
                DragMap();
                break;
            case 2://print("tap drag 2");
                break;
        }
    }

    void OnHoldDrag(int finger)
    {
        switch(finger)
        {
            case 1://print("hold drag 1");
                // Users can put 2nd finger down to do 2-finger zoom while doing 1-finger zoom, hence this condition.
                if (!fing2_down) ZoomMap();
                break;
            case 2://print("hold drag 2");
                ZoomMap();
                break;
        }
    }



    void DragMap()
    {
        was_dragging = true;
        map_offset_current = (fing1.position - fing1_down_pos) / screen * map_offset_const;
    }

    void ZoomMap()
    {
        was_zooming = true;
        map_scale_current = map_scale_const *
        (
            fing2_down ?
                Vector2.Distance(fing1_down_pos,fing2_down_pos) - Vector2.Distance(fing1.position,fing2.position):
                fing1_down_pos.y - fing1.position.y
        ) / screen.y;

        if (map_scale + map_scale_current + map_scale_orig < map_scale_range.x)
            map_scale_current = map_scale_range.x - map_scale - map_scale_orig;
        if (map_scale + map_scale_current + map_scale_orig > map_scale_range.y)
            map_scale_current = map_scale_range.y - map_scale - map_scale_orig;

        float total_map_scale = map_scale + map_scale_current + map_scale_orig;
        //map_offset_scale = map_displ_scale * total_map_scale;
    }
}
