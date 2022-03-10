using System.Collections;
using System.Collections.Generic;
using System.Linq; // For sorting.
using TMPro;
using UnityEngine;

public class SearchRoom : MonoBehaviour
{
    public static SearchRoom main;

    public TMP_InputField input_obj;
    public GameObject top_result_list;
    public UnityEngine.UI.Image top_result_more; // Too lazy. Just drag that box into here. It's within the top list.
    public List<UnityEngine.UI.Image> top_result_image{get; private set;}
    public List<TMP_Text> top_result_text{get; private set;} // Note that these 2 top_result components are parallel.

    List<MapNodes> search_result = new List<MapNodes>();

    void Awake()
    {
        main = this;
        top_result_image = top_result_list.GetComponentsInChildren<UnityEngine.UI.Image>().ToList();
        top_result_text = top_result_list.GetComponentsInChildren<TMP_Text>().ToList();

        top_result_image.Remove(top_result_more);
        top_result_text.Remove(top_result_more.GetComponentInChildren<TMP_Text>());

        RefreshTopList();
        
        gameObject.SetActive(false); // The search bar is disabled by default, but needs to populate main if it wants to be activated.
    }

    public List<MapNodes> GetMatches(string pattern)
    {
        // This is to remove whitespace and convert them to lower casing.
        pattern = string.Join("", pattern.Split(default(string[]), System.StringSplitOptions.RemoveEmptyEntries)).ToLower();
        List<MapNodes> result = new List<MapNodes>();
        foreach (var i in MapNodes.main_nodes)
        {
            if (i.id.Contains(pattern)) { result.Add(i); continue; }
            foreach (var j in i.search_keys) if (j.Contains(pattern)) { result.Add(i); break; }
        }
        return result;
    }

    public List<MapNodes> SortAlphabet(List<MapNodes> matches)
    {
        return matches.OrderBy(x => x.id).ToList();
    }



    // This is what processes the user input and gives a search result.
    public void OnSearch()
    {
        if (input_obj.text.Length > 1) search_result = SortAlphabet(GetMatches(input_obj.text));
        else search_result.Clear();
        //DebugLog.List(search_result.Select(x => x.id).ToList());
        RefreshTopList();
    }

    public void RefreshTopList()
    {   // This means that either only a few top_result_text is used, or only select first 5 search_result if there's 5 top_result_text.
        for (var i = 0; i < Mathf.Min(search_result.Count, top_result_text.Count); i++)
        {
            top_result_text[i].text = search_result[i].disp_name.Length > 0 ? search_result[i].disp_name : search_result[i].id;
            top_result_image[i].enabled = true;
        }
        for (var i = top_result_text.Count - 1; i >= search_result.Count; i--)
        {   // This takes out the top result boxes if not needed. If search_result.Count exceeds, this won't run.
            top_result_text[i].text = string.Empty;
            top_result_image[i].enabled = false;
        }
        top_result_more.gameObject.SetActive(search_result.Count > top_result_text.Count);
    }

    public void OnSelectTopResult(TMP_Text txt_obj)
    {
        MapNodes destination = null;
        DebugLog.List(search_result);
        foreach (var i in search_result.GetRange(0,Mathf.Min(search_result.Count, top_result_text.Count)))
        {
            if (i.disp_name == txt_obj.text || i.id == txt_obj.text)
            {
                destination = i;
                break;
            }
        }

        if (destination != null)
        {
            Map.main.SetupRoute(destination);
            MapMenu.main.OnDropdownClick();
        }
    }
}