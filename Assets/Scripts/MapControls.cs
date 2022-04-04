using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapControls : MonoBehaviour
{
    public static MapControls main;

    Vector2 screen; // Screen resolution, obtained on Awake().
    Camera cam; // Active camera (Eh, there's only one anyways), obtained on Awake();
    public float hold_time_thres = 1f; // Temp variable to set the time needed for a tap to be a hold instead.
    public int move_dist_thres = 10; // Temp variable to set distance needed (in pixels) to be considered moving finger.
    public int swipe_dist_thres = 10; // Temp variable to set drag distance needed (in pixels) to be considered a swipe.

    [HideInInspector] public Touch fing1;
    [HideInInspector] public Touch fing2;

    public Vector2 fing1_down_pos{get; private set;}
    public Vector2 fing2_down_pos{get; private set;}

    public Vector3 fing1_down_world_pos{get; private set;}
    public Vector3 fing2_down_world_pos{get; private set;}
    bool fing1_down;
    bool fing2_down;
    
    float fing1_hold_time;
    bool fing1_moved;
    bool fing1_holding; // If true, user is holding the finger istead of tapping.

    Vector2 screen_world_ratio;
    // Since it requires the camera's position, and it only moves to the correct distance after 1 LateUpdate() in Map,
    //      it is best to initialize it on the first map dragging.
    bool screen_world_ratio_init; 

    public static bool isTapped{get; private set;}
    public static bool isHeldDown{get; private set;}
    public static bool isHeld{get; private set;}
    public static bool isHeldUp{get; private set;}
    public static bool isTapDragged{get; private set;}
    public static bool isHoldDragged{get; private set;}

    void Awake()
    {
        main = this;
        screen = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
        cam = Camera.main;
        fing1.phase = TouchPhase.Canceled; // This is to make sure TouchPhase doesn't start with Begin.
        fing2.phase = TouchPhase.Canceled;
    }

    void Update()
    {
        if (Input.touchCount == 0) {}
        if (Input.touchCount >= 1) fing1 = Input.GetTouch(0);
        if (Input.touchCount >= 2) fing2 = Input.GetTouch(1);
        if (Input.touchCount < 2) fing2.phase = TouchPhase.Ended; // Since GetTouch(2) stops updating when it's lifted, we have to end it manually.

        // Checks tap down for both fingers
        if (fing1.phase == TouchPhase.Began)
        {
            OnFingerDown(1);
            fing1_down = true;
            fing1_down_pos = fing1.position;
            fing1_down_world_pos = ScreenToMapPoint(fing1_down_pos);
        }
        if (fing2.phase == TouchPhase.Began)
        {
            OnFingerDown(2);
            OnTap(2); // There's no need to check 2nd finger holding, so let's leave it here.
            fing2_down = true;
            fing2_down_pos = fing2.position;
            fing2_down_world_pos = ScreenToMapPoint(fing2_down_pos);
        }

        // Checks if finger 1 is holding
        if (fing1_down)
        {
            fing1_hold_time += Time.deltaTime;
            if (!fing1_holding && !fing1_moved && fing1_hold_time >= hold_time_thres)
            {
                OnHold();
                fing1_holding = true;
            }
        }

        // Process action when finger1 is dragging
        if (fing1.phase == TouchPhase.Moved)
        {
            if ((fing1_down_pos - fing1.position).sqrMagnitude >= move_dist_thres * move_dist_thres) 
            {
                fing1_moved = true;
                if (!fing2_down) // If finger 2 is down, all dragging for finger 1 is ignored.
                    if (fing1_holding) OnHoldDrag(1); else OnTapDrag(1);
            }
        }

        // No need to check if finger 2 is holding.
        if (fing2.phase == TouchPhase.Moved) OnTapDrag(2);

        // Checks top/hold up for both fingers
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
            if (fing2_down) { OnFingerUp(2); fing2_down = false; }
        }
    }

    void LateUpdate()
    {
        isTapped = false;
        isHeldDown = false;
        isHeld = false;
        isTapDragged = false;
        isHoldDragged = false;
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
                if (!fing2_down && fing1_holding) Map.main.ZoomMapEnd();
                else Map.main.DragMapEnd();
                if (fing1_holding) isHeldUp = true;
                break;
            case 2://print("up 2");
                Map.main.ZoomMapEnd();
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
                isTapped = true;
                break;
            case 2://print("tap 2");
                break;
        }
    }

    void OnHold()
    {//print("hold");
        if (!fing1_holding) isHeldDown = true;

        fing1_holding = true;
        isHeld = true;
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
                if (!screen_world_ratio_init) { screen_world_ratio_init = true; screen_world_ratio = GetScreenToWorldRatio(); }
                Map.main.DragMap((fing1_down_pos - fing1.position) * screen_world_ratio);
                isTapDragged = true;
                break;
            case 2://print("tap drag 2");
                Map.main.ZoomMap(
                    (Vector2.Distance(fing1.position,fing2.position) - 
                        Vector2.Distance(fing1_down_pos,fing2_down_pos))
                    / screen.y );
                screen_world_ratio = GetScreenToWorldRatio();
                break;
        }
    }

    void OnHoldDrag(int finger)
    {
        switch(finger)
        {
            case 1://print("hold drag 1");
                // Users can put 2nd finger down to do 2-finger zoom while doing 1-finger zoom, hence this condition.
                if (!fing2_down)
                {
                    Map.main.ZoomMap( (fing1_down_pos.y - fing1.position.y) / screen.y );
                    screen_world_ratio = GetScreenToWorldRatio();
                }
                isHoldDragged = true;
                break;
            case 2:
                break;
        }
    }



    Vector3 ScreenToMapPoint(Vector2 pos)
    {
        Vector3 screen_pos = new Vector3(pos.x, pos.y, Map.main.transform.position.z - cam.transform.position.z);
        return cam.ScreenToWorldPoint(screen_pos);
    }

    // Ratio changes upon zooming. Hence this runs every tick the camera moved back or forth.
    Vector2 GetScreenToWorldRatio()
    {
        Vector3 btm_lft_world_pos = ScreenToMapPoint(Vector2.zero);
        Vector3 top_rgt_world_pos = ScreenToMapPoint(screen);
        return new Vector2((top_rgt_world_pos.x - btm_lft_world_pos.x) / screen.x, 
            (top_rgt_world_pos.y - btm_lft_world_pos.y) / screen.y);
    }
}