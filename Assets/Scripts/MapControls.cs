using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapControls : MonoBehaviour
{
    public static MapControls main;

    Vector2 screen; // Screen resolution, obtained on Awake().
    public float hold_time_thres = 1f; // Temp variable to set the time needed for a tap to be a hold instead.
    public int move_dist_thres = 10; // Temp variable to set distance needed (in pixels) to be considered moving finger.
    public int swipe_dist_thres = 10; // Temp variable to set drag distance needed (in pixels) to be considered a swipe.

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
        screen = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
        fing1.phase = TouchPhase.Canceled;
        fing2.phase = TouchPhase.Canceled;
    }

    void Update()
    {
        if (Input.touchCount == 0) {}
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
            if ((fing1_down_pos - fing1.position).sqrMagnitude >= move_dist_thres * move_dist_thres) 
            {
                fing1_moved = true;
                if (fing1_holding) OnHoldDrag(1); else OnTapDrag(1);
            }
        }

        if (fing2.phase == TouchPhase.Moved) OnHoldDrag(2);

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
                break;
            case 2://print("tap 2");
                break;
        }
    }

    void OnHold()
    {//print("hold");
        fing1_holding = true;
/*
        // Debugging use. Remove after use.
        MapNodes n = PathBuilder.GetClosestPoint(user_pos.transform.position, MapNodes.nodes);
        print(n.id);
        MapNodes e = null;
        foreach (var i in MapNodes.main_nodes)
        {
            if (i.id == "cafeteria") e = i;
        }
        DebugLog.List(PathBuilder.GetPath(n,e,MapNodes.nodes));*/
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
                Map.main.DragMap((fing1.position - fing1_down_pos) / screen);
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
                if (!fing2_down) Map.main.ZoomMap( (fing1_down_pos.y - fing1.position.y) / screen.y );
                break;
            case 2://print("hold drag 2");
                Map.main.ZoomMap(
                    (Vector2.Distance(fing1_down_pos,fing2_down_pos) - 
                        Vector2.Distance(fing1.position,fing2.position))
                    / screen.y );
                break;
        }
    }
}