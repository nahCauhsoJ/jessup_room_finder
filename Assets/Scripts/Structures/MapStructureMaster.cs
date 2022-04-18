using System.Collections.Generic;
using UnityEngine;

// This is just a lazy way to indicate that this is the outermost layer of the map. Attach this and it'll be stored in MapStructures.
public class MapStructureMaster : MapStructures
{
    // Note that structure_layer, structure_floor and nodes_inside need no configuration. But since hiding them
    //      from inspector requires a custom editor, for simplicity let's just leave them there. Just don't touch it.

    void Awake() {
        master = this; current = this;

        // Since there cannot be multiple seralization, I'm assigning them here.
        structure_layer = StructureLayer.Outside;
        structure_floor = StructureFloor.Downstairs;
    }

    void Start() { map_scale_inside = MapScroller.main.map_scale; }
}
