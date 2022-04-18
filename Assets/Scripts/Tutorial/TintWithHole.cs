using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TintWithHole : MonoBehaviour
{
    public ReverseUIMask tint; // This is the whole screen, not the hole.
    public UnityEngine.UI.Image hole;

    void Awake()
    {
        tint.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
    }

    // This is what moves the hole. Due to how the hole has to be the parent of the tint, please only use this function
    //      to move the hole. The tint is also forced to be full screen.
    // pos: The delta position from screen's center
    // radius: Hole's radius in screen pixels.
    public void SetHole(Vector2 pos, float radius)
    {
        hole.rectTransform.anchoredPosition = pos;
        hole.rectTransform.sizeDelta = Vector2.one * radius;
        tint.rectTransform.anchoredPosition = -pos;
    }
}
