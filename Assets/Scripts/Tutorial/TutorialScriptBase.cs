using System.Collections.Generic;
using UnityEngine;

public abstract class TutorialScriptBase : MonoBehaviour
{
    public List<System.Action> content{get; protected set;} // This will store the order and the content of the tutorial.
    public TutorialChunk chunk_obj{get; set;} // Check TutorialChunk.Awake() for details.
    public int part_index{get; private set;} // This is the index of content. Auto clamps with NextPart() or PrevPart().

    // The returned boolean indicates whether there's a next part or not.
    public virtual bool NextPart()
    {
        if (part_index + 1 >= content.Count) return false;
        part_index++;
        content[part_index]();
        return part_index >= content.Count;
    }

    public virtual bool PrevPart()
    {
        if (part_index <= 0) return false;
        part_index--;
        content[part_index]();
        return part_index + 1 <= 0;
    }



    // These are lazy methods for changing the tutorial chunk. Makes the script more readable.
    protected virtual void MoveMessageBox(Vector2 pos)
    {
        chunk_obj.message_box_rect.anchoredPosition = pos;
    }

    protected virtual void SetMessage(string txt, bool keep_format_spaces = false)
    {
        if (!keep_format_spaces) txt = txt.Replace("    ","").Replace("\r","").Replace("\n","");
        chunk_obj.message_box_txt.text = txt;
    }

    protected virtual void Highlight(Vector2 pos, float radius)
    {
        if (TutorialChunk.highlight_hole == null) return;
        TutorialChunk.highlight_hole.SetHole(pos, radius);
    }
}
