using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapStructureTransition : MonoBehaviour
{
    public static MapStructureTransition main;
    void Awake() { main = this; }

    public Animator transition_anim;
    public UnityEngine.UI.Image transition_screen; // This is what fades the screen to nothing.

    public Cinemachine.CinemachineVirtualCamera vcam;
    float vcam_orig_fov;
    int vcam_zoom_mode; // 1: zoom in (linear), 2: zoom out (linear)

    MapStructures transition_structure;

    // This is the handler to check if it needs a transition, and executes if so. This runs every time the user arrives at a new node.
    // If it needs to both go up and in, it'll do the out->in fade instead. Layer has higher priority than floor.
    // Also, if there's no "master", it throws errors instead of ignoring. Knowing where's the master structure is important.
    public bool CompareStructures(MapStructures next_structure, MapStructures current_structure)
    {
        if (next_structure == null) next_structure = MapStructures.master;
        if (current_structure == null) current_structure = MapStructures.master;

        // Let's just hard-code the scenarios for now. Simple and effective.
        if (next_structure.structure_layer != current_structure.structure_layer)
        {
            if (current_structure.structure_layer == MapStructures.StructureLayer.Inside)
                TransToStructure(next_structure, 2);
            else
                TransToStructure(next_structure, 1);
            return true; // Again, Layer takes priority.
        }
        
        if (next_structure.structure_floor != current_structure.structure_floor)
        {   // Will deal with upstairs and downstairs later.
            TransToStructure(next_structure, 0);
            return true;
        }

        return false;
    }
    public bool CompareStructures(MapNodes next_node, MapNodes current_node)
    {
        if (CompareStructures(next_node.structure_belong, current_node.structure_belong)) return true;

        MapStructures next_structure = next_node.structure_belong;
        MapStructures current_structure = current_node.structure_belong;
        if (next_structure == null) next_structure = MapStructures.master;
        if (current_structure == null) current_structure = MapStructures.master;

        // Even if the layer and floor is an exact match, there should at least have a fade animation.
        if (next_structure != current_structure)
        {
            TransToStructure(next_structure, 0);
            return true;
        }

        return false;
    }

    // This makes that animation of transitioning between structures.
    // anim_style:
    //     -1 - Straight cut (i.e. No animation)
    //     0 - Fade in-out (i.e. blackscreen and clear)
    //     1 - pull in (i.e. outside -> inside)
    //     2 - pull out (i.e. inside -> outside)
    public void TransToStructure(MapStructures s, int anim_style = 1)
    {
        if (MapStructures.current == s) return;

        if (anim_style == -1)
        {
            MapStructures.current.gameObject.SetActive(false);
            s.gameObject.SetActive(true);
            MapStructures.current = s;
            return;
        }

        transition_structure = s;
        transition_anim.enabled = true;
        transition_anim.SetBool("can_reveal",false);
        vcam_orig_fov = vcam.m_Lens.FieldOfView;
        switch (anim_style)
        {
            case 0:
                transition_anim.SetTrigger("fade");
                break;
            case 1:
                transition_anim.SetTrigger("fade");
                vcam_zoom_mode = 1;
                break;
            case 2:
                transition_anim.SetTrigger("fade");
                vcam_zoom_mode = 2;
                break;
        }
    }

    void Update()
    {
        switch (vcam_zoom_mode)
        {
            case 1:
                vcam.m_Lens.FieldOfView -= 0.05f;
                break;
            case 2:
                vcam.m_Lens.FieldOfView += 0.05f;
                break;
        }
    }

    // This is activated by the Animation.
    public void TransProcess()
    {
        MapStructures.current.gameObject.SetActive(false);
        transition_structure.gameObject.SetActive(true);
        MapStructures.current = transition_structure;
        transition_structure = null;

        vcam_zoom_mode = 0;
        vcam.m_Lens.FieldOfView = vcam_orig_fov;
        MapScroller.main.ZoomForced(MapStructures.current.map_scale_inside);

        transition_anim.SetBool("can_reveal",true);
    }

    // Also activated by the Animation
    public void EndAnimation()
    {
        transition_anim.enabled = false;
        transition_screen.gameObject.SetActive(false);
    }
}
