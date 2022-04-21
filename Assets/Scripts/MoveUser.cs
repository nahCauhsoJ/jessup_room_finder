using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveUser : MonoBehaviour
{
    public static MoveUser main;

    Camera main_cam;
    public Image pinpoint_img;
    public Button ask_gps_btn;
    public Button confirm_btn;

    public Color pinpoint_allow_color;
    public Color pinpoint_disallow_color;

    public bool needs_confirm{get; set;}

    Vector3 pinpoint_world_pos;

    void Awake()
    {
        main = this;
        main_cam = Camera.main;

        gameObject.SetActive(false); // The object is disabled by default, but needs to populate main if it wants to be activated.
    }

    void OnEnable()
    { 
        pinpoint_img.gameObject.SetActive(true);
        UpdatePinpointState();
    }
    void OnDisable()
    {
        needs_confirm = false;
        if (pinpoint_img != null) pinpoint_img.gameObject.SetActive(false);
    }

    void Update()
    {
        pinpoint_img.transform.position = pinpoint_world_pos;
        ask_gps_btn.interactable = Locations.main.use_gps;
        confirm_btn.interactable = needs_confirm;
    }

    // This moves that cursor to a target spot via world coordinates
    public void MovePinpointToWorld(Vector3 pos)
    {
        pinpoint_world_pos = pos;
    }
    // This moves that cursor to a target spot via screen position
    public void MovePinpointToScreen(Vector2 pos)
    {
        Vector3 true_pos = new Vector3(pos.x, pos.y, transform.position.z - main_cam.transform.position.z);
        pinpoint_world_pos = main_cam.ScreenToWorldPoint(true_pos);
        pinpoint_world_pos = new Vector3(pinpoint_world_pos.x, pinpoint_world_pos.y, 0);
    }

    // Checks if the pinpoint position is within borders. It returns the result for
    //      processing, but the color change is automatic.
    bool UpdatePinpointState()
    {
        if (!CampusBorder.OnCampus(pinpoint_world_pos))
        {
            pinpoint_img.color = pinpoint_disallow_color;
            return false;
        }
        pinpoint_img.color = pinpoint_allow_color;
        return true;
    }



    public static void FindUser()
    {
        MapScroller.main.DragMapReset();
        main.MovePinpointToWorld(Map.main.user_pin.transform.position);
        main.UpdatePinpointState();
    }

    public static void Recenter()
    {
        main.MovePinpointToScreen(new Vector2(Screen.width / 2, Screen.height / 2));
        main.UpdatePinpointState();
    }

    

    public void OnMovePinpoint()
    {
        MovePinpointToScreen(MapControls.main.fing1.position);
        needs_confirm = UpdatePinpointState();
    }

    public void OnAskGps()
    {
        Vector3 gps_pos = Locations.main.GetPosByGps();

        if (!CampusBorder.OnCampus(gps_pos))
        {
            BoxMessage.Send(@"You seem to be off-campus, so it's best to not ask your GPS for now. 
                (If you're sure you're on-campus, it might be your GPS being slightly off the borders.)");
            return;
        }

        // Running here already assumes that the pinpoint is within borders.
        MovePinpointToWorld(gps_pos);
        MapScroller.main.DragMapReset();
        MapScroller.main.map_offset = pinpoint_world_pos;
        needs_confirm = true;
    }

    public void OnCancel()
    {
        gameObject.SetActive(false);
    }
    public void OnConfirm()
    {
        if (!needs_confirm) return; // Just in case...

        Map.main.user_pin.transform.position = new Vector3(
            pinpoint_world_pos.x, pinpoint_world_pos.y, Map.main.user_pin.transform.position.z);
        if (Map.main.user_pin.gameObject.activeInHierarchy) Map.main.CheckInbetweenNodes(); // OnConfirm() can run without a route.
        needs_confirm = false;
        MapScroller.main.DragMapReset();
        UserControl.main.CompareRoutes();

        gameObject.SetActive(false);
    }
}
