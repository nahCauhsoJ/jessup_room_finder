using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapMenu : MonoBehaviour
{
    public static MapMenu main;
    void Awake () {main = this;}

    public Button dropdown_button;
    public GameObject dropdown_content;

    // The custom states of dropdown_button
    //  1: Hiding Content (Default)
    //  2: Showing Content
    //  -1: Will close GUI of whatever's opened
    public int dd_btn_state{get;set;} = 1;



    public void OnDropdownClick()
    {
        switch (dd_btn_state)
        {
            case 1:
                dd_btn_state = 2;
                dropdown_content.SetActive(true);
                break;
            case 2:
                dd_btn_state = 1;
                dropdown_content.SetActive(false);
                break;
            case -1:
                dd_btn_state = 1;
                SearchRoom.main.gameObject.SetActive(false);
                break;
        }
    }

    public void OnSearchClick()
    {
        dd_btn_state = 2;
        OnDropdownClick(); // I'm just lazy.
        dd_btn_state = -1;
        SearchRoom.main.gameObject.SetActive(true);
    }
}
