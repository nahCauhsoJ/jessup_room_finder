using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeNavBtn : MonoBehaviour
{
    public RectTransform rectTransform;
    public Image img;
    public Button btn;
    public Image icon;
    public TMPro.TMP_Text txt_obj;

    public void MoveToNode(MapNodes node)
    {
        MoveToPos(node.transform.position);
    }

    public void MoveToPos(Vector3 world_pos)
    {
        transform.position = world_pos;
        //rectTransform.anchoredPosition = NodeNav.main_cam.WorldToScreenPoint(world_pos);
    }

    public void OnClick()
    {
        print("meh");
    }
}
