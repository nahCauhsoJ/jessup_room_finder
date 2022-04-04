using System.Collections;
using System.Linq; // For .Except()
using System.Collections.Generic;
using UnityEngine;

using System;
[Serializable]
public class MapNodes : MonoBehaviour
{
    public string id; // It'll be the identifier of the node when stored in text file.
    public List<MapNodes> links;
    public string derived_obj{get; protected set;} // This way, GetComponent() only runs on the correct type and I'm not using built-in tags for now.

    public static List<MapNodes> nodes = new List<MapNodes>();
    public static List<MainNode> main_nodes = new List<MainNode>();

    protected virtual void Awake() {
        nodes.Add(this);
    }

/*

    Notes:
        1. If an alternate entrace exists, make an empty Game Object as the child of the node, then position it.
        2. Mark the main spot with the blue marker and alternate entrace yellow. Otherwise it'll default to a white marker.
        3. PLEASE make sure that all search_keys and id are in lower case. I'm not converting them.

*/
    // To prevent an infinite loop, the OnValidate() on Node A will force update links_old for Node B.
    // Note: links_old is not serialized so that Undo will not revert its value. This is important when
    //      trying to Undo a wrong link.
    [NonSerialized] public List<MapNodes> links_old = new List<MapNodes>();
    // To auto-link the nodes for the other side when 1 is linked, also remove link on both sides if 1 is removed.
    protected virtual void OnValidate()
    {
        if (Application.isPlaying) return;
        
        if (links_old.Count > links.Count)
        {
            foreach (var i in links_old.Except(links))
            {
                if (i == null) continue;
                if (i.links.Contains(this))
                {
                    i.links.Remove(this);
                    i.links_old = new List<MapNodes>(i.links);
                }
            }
            links_old = new List<MapNodes>(links); // Hence, links_old is public.
        } else if (links_old.Count < links.Count) {
            foreach (var i in links.Except(links_old))
            {
                if (i == null) continue;
                if (!i.links.Contains(this))
                {
                    i.links.Add(this);
                    i.links_old = new List<MapNodes>(i.links);
                }
            }
            links_old = new List<MapNodes>(links);
        }
    }

    // To draw the nodes
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        foreach (var i in links)
        {
            if (i == null) continue;
            Gizmos.DrawLine(transform.position, i.transform.position);
        }
    }
}
