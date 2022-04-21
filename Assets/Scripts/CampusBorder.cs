using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampusBorder : MonoBehaviour
{
    public static CampusBorder main;
    public EdgeCollider2D border_col;
    public List<Vector2> border_vertices{get; private set;} = new List<Vector2>();

    void Awake() {
        main = this;
        border_col.GetPoints(border_vertices);
    }

    // This checks if the position is within the border specified by the edge collider above.
    // This is a world position. Use ScreenToWorldPoint() if the position comes from the screen.
    // This uses Crossing Number Algorithm if not sure what this is.
    // This also assumes that the edge collider has at least 2 points.
    public static bool OnCampus(Vector2 pos)
    {
        int edges_collided = 0;
        for (int i = 0; i < main.border_vertices.Count-1; i++)
            // 1000 units should suffice to cover the map, I hope?
            if (SegIntersect(pos, pos + Vector2.up * 1000f, main.border_vertices[i], main.border_vertices[i+1]))
                edges_collided++;
        return edges_collided % 2 != 0;
    }

    // Note that if this runs on campus, it'll only return the first edge it finds.
    // Also note that Vector2.zero is returned only if on-campus or right at 0,0 (which means on-campus).
    public static Vector2 NearestPointOnCampus(Vector2 pos)
    {
        // direction being -pos means aiming at 0,0.
        RaycastHit2D hit = Physics2D.Raycast(pos, -pos, Mathf.Infinity, LayerMask.GetMask("borders"));
        if (hit.collider != null) return hit.point;
        return Vector2.zero;
    }

    // Reference: https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/
    static bool SegIntersect(Vector2 l1_start, Vector2 l1_end, Vector2 l2_start, Vector2 l2_end)
    {
        static bool? orientation(Vector2 p, Vector2 q, Vector2 r)
        {   // true is clockwise. null is collinear
            float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
            if (val == 0) return null;
            return (val > 0)? true : false;
        }
        static bool onSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
                q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
                    return true;
            return false;
        }

        bool? o1 = orientation(l1_start, l1_end, l2_start);
        bool? o2 = orientation(l1_start, l1_end, l2_end);
        bool? o3 = orientation(l2_start, l2_end, l1_start);
        bool? o4 = orientation(l2_start, l2_end, l1_end);
    
        if (o1 != o2 && o3 != o4) return true;
        if (o1 == null && onSegment(l1_start, l2_start, l1_end)) return true;
        if (o2 == null && onSegment(l1_start, l2_end, l1_end)) return true;
        if (o3 == null && onSegment(l2_start, l1_start, l2_end)) return true;
        if (o4 == null && onSegment(l2_start, l1_end, l2_end)) return true;
    
        return false;
    }
}
