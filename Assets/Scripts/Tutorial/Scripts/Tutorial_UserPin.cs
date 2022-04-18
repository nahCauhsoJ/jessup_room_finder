using System.Collections.Generic;
using UnityEngine;

public class Tutorial_UserPin : TutorialScriptBase
{
    void Awake()
    {
        content = new List<System.Action>(){
            pt1, pt2
        };
    }

    void pt1()
    {
        Highlight(new Vector2(0,75), 175f);
        MoveMessageBox(new Vector2(0, -50f));
        SetMessage(@"
            This pin/arrow indicates where you are on campus. However, due to technical limitations, 
            the system doesn't track where you are.
        ");
    }

    void pt2()
    {
        SetMessage(@"
            To tell the system where you are (so that we can guide you), click on this 3 dots and select Move. 
            If you don't know how to use the Move feature, there is a section in the tutorial list.
        ");
    }
}
