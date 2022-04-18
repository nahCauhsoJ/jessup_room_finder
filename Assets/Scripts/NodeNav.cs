using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeNav : MonoBehaviour
{
    public static NodeNav main;
    public static Camera main_cam;

    public NodeNavBtn target_btn;
    public Sprite arrived_sprite;
    public Sprite enter_sprite;
    public Sprite go_up_sprite;
    public Sprite go_down_sprite;

    void Awake()
    {
        main = this;
        main_cam = Camera.main;
    }

    void Update()
    {
        if (Map.main.user_pin.gameObject.activeInHierarchy)
        {
            if (!target_btn.gameObject.activeInHierarchy) target_btn.gameObject.SetActive(true);
            target_btn.MoveToNode(Map.main.current_route[Map.main.current_route_target_ix]);
        } else {
            if (target_btn.gameObject.activeInHierarchy) target_btn.gameObject.SetActive(false);
        }
    }

    // This is for the target node's button to do simpler tasks
    public void MoveToNextTarget()
    {
        Map.main.user_pin.transform.position = Map.main.current_route[Map.main.current_route_target_ix].transform.position;
        MapScroller.main.DragMapReset();
    }

    public void UpdateTargetIcon()
    {
        MapStructures cur_target = Map.main.current_route[Map.main.current_route_target_ix].structure_belong;
        MapStructures next_target = Map.main.current_route_target_ix + 1 < Map.main.current_route.Count ?
            Map.main.current_route[Map.main.current_route_target_ix + 1].structure_belong : cur_target;
        // If condition is false, I'm just forcing the below statements to result in the "arrived" icon.
        // Make sure either side of the stairs aren't MainNode-s, or the destination's icon can become stairs.

        // Again, as in MapStructureTransition, let's do it hard-coded. Simple and effective.
        // StructureLayer doesn't matter since in both cases the icon will be the same.

        // If next_target == cur_target, there's no need to transition between structures.
        if (next_target == cur_target) ChangeTargetIcon("arrived");
        // If either target is null, one of them is outside. Going outside or inside, the icon is the same for now.
        // If both aren't null, due to prev condition it means user must be transitioning between two inside structures.
        //      If both are on the same floor it still uses the "enter" icon. e.g. Commons and Library 1/F.
        else if (next_target == null || cur_target == null || next_target.structure_floor == cur_target.structure_floor)
            ChangeTargetIcon("enter");
        else {
            // Reaching here means we're either going up or down the stairs/ elevator.
            if (next_target.structure_floor == MapStructures.StructureFloor.Downstairs)
                ChangeTargetIcon("go_down");
            else 
                ChangeTargetIcon("go_up");
        }
    }

    // Available names: "arrived", "enter", "go_up", "go_down"
    public void ChangeTargetIcon(string icon_name)
    {
        switch (icon_name)
        {
            case "arrived":
                target_btn.icon.sprite = arrived_sprite;
                break;
            case "enter":
                target_btn.icon.sprite = enter_sprite;
                break;
            case "go_up":
                target_btn.icon.sprite = go_up_sprite;
                break;
            case "go_down":
                target_btn.icon.sprite = go_down_sprite;
                break;
        }
    }
}
