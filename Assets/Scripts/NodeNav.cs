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
        Map.main.DragMapReset();

        StructureTransition s = Map.main.current_route_target_ix + 1 < Map.main.current_route.Count ?
            Map.main.current_route[Map.main.current_route_target_ix + 1].GetComponent<StructureTransition>() : null;
        if (s != null && s.trans_to != Map.main.current_structure)
        {
            switch (s.trans_to.structure_type)
            {
                case MapStructures.StructureType.Upstairs:
                    ChangeTargetIcon("go_up");
                    break;
                case MapStructures.StructureType.Downstairs:
                    ChangeTargetIcon("go_down");
                    break;
                case MapStructures.StructureType.Outside:
                    ChangeTargetIcon("enter");
                    break;
                case MapStructures.StructureType.Inside:
                    ChangeTargetIcon("enter");
                    break;
            }
        } else ChangeTargetIcon("arrived");
        
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
