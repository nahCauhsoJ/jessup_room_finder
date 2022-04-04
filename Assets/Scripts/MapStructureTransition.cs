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

    // This makes that animation of transitioning between structures.
    // anim_style:
    //     0 - Straight cut
    //     1 - pull in (i.e. outside -> inside)
    //     2 - pull out (i.e. inside -> outside)
    public void TransToStructure(MapStructures s, int anim_style = 1)
    {
        if (Map.main.current_structure == s) return;

        if (anim_style == 0)
        {
            Map.main.current_structure.gameObject.SetActive(false);
            s.gameObject.SetActive(true);
            Map.main.current_structure = s;
            return;
        }

        transition_structure = s;
        transition_anim.enabled = true;
        transition_anim.SetBool("can_reveal",false);
        vcam_orig_fov = vcam.m_Lens.FieldOfView;
        switch (anim_style)
        {
            case 1:
                transition_anim.SetTrigger("pull_in");
                vcam_zoom_mode = 1;
                break;
            case 2:
                transition_anim.SetTrigger("pull_out");
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
                vcam.m_Lens.FieldOfView -= 0.05f;
                break;
        }
    }

    // This is activated by the Animation.
    public void TransProcess()
    {
        Map.main.current_structure.gameObject.SetActive(false);
        transition_structure.gameObject.SetActive(true);
        Map.main.current_structure = transition_structure;
        transition_structure = null;

        vcam_zoom_mode = 0;
        vcam.m_Lens.FieldOfView = vcam_orig_fov;

        transition_anim.SetBool("can_reveal",true);
    }

    // Also activated by the Animation
    public void EndAnimation()
    {
        transition_anim.enabled = false;
    }
}
