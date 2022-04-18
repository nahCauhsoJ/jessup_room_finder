using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
    public static Tutorial main;

    public TintWithHole tint_with_hole; // Due to limitations, all tutorial chunks share 1 hole on the screen.
    public UnityEngine.UI.Image next_tut_image;
    public TMPro.TMP_Text next_tut_text;
    public TutorialPlaylist playlist_selection; // This is the screen to select a tutorial.
    public Transform playlist_parent; // This is where the tutorial is displayed. playlist_selection is inactive if this one is active.

    [HideInInspector] public List<TutorialChunk> tutorials; // This is the list of tutorials that will show.
    // This is the current page of tutorial (Not to be confused with each chunk's page number)
    public int current_tutorial_ix{get; private set;}

    void Awake()
    {
        main = this;
        TutorialChunk.highlight_hole = tint_with_hole;
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        playlist_selection.gameObject.SetActive(true);
        tint_with_hole.SetHole(Vector2.zero, 0);
    }

    public void AddTutorial(GameObject tutorial_chunk_prefab)
    {
        GameObject tutorial_chunk_obj = Instantiate(tutorial_chunk_prefab, playlist_parent);
        TutorialChunk chunk = tutorial_chunk_obj.GetComponent<TutorialChunk>();
        if (chunk == null) { Destroy(tutorial_chunk_obj); return; }
        tutorials.Add(chunk);
        tutorial_chunk_obj.gameObject.SetActive(false); // They should be hidden until needed.
    }



    public void StartTutorial()
    {
        next_tut_text.text = tutorials.Count <= 1 ? "End Tutorial" : string.Format("Next Tutorial (1/{0})", tutorials.Count);
        if (tutorials.Count == 0) return;
        tutorials[0].gameObject.SetActive(true);

        playlist_selection.gameObject.SetActive(false);
        playlist_parent.gameObject.SetActive(true);
    }
    public void EndTutorial()
    {
        foreach (var i in tutorials) Destroy(i.gameObject);
        tutorials.Clear();

        playlist_parent.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }



    // If it's the last one, it'll also trigger EndTutorial()
    public void OnNextTutorial()
    {
        if (current_tutorial_ix + 1 >= tutorials.Count) { EndTutorial(); return; }

        next_tut_text.text = current_tutorial_ix + 2 >= tutorials.Count ? 
            "End Tutorial" : string.Format("Next Tutorial ({0}/{1})", current_tutorial_ix + 1, tutorials.Count);
        tutorials[current_tutorial_ix].gameObject.SetActive(false);
        current_tutorial_ix++;
        tutorials[current_tutorial_ix].gameObject.SetActive(true);
    }
}
