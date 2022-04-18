using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialChunk : MonoBehaviour
{
    public static TintWithHole highlight_hole; // Supplied by Tutorial.main.
    public Button prev_btn;
    public Button next_btn;
    public RectTransform message_box_rect;
    public TMPro.TMP_Text message_box_txt;

    [Header("Insert the tutorial's script here")]
    public TutorialScriptBase script;

    void Awake()
    {
        if (script == null) return;
        script.chunk_obj = this; // This is for the script's functions to reference the components of this chunk.
    }

    void Start()
    {
        next_btn.interactable = script.content.Count > 1;
        prev_btn.interactable = false;
        if (script.content.Count > 0) script.content[0]();
    }

    public void NextPart()
    {
        bool has_next = script.NextPart();
        next_btn.interactable = has_next;
        prev_btn.interactable = true; // If the user is able to click this, it's assumed that there's 2+ pages.
    }

    public void PrevPart()
    {
        bool has_prev = script.PrevPart();
        prev_btn.interactable = has_prev;
        next_btn.interactable = true;
    }
}
