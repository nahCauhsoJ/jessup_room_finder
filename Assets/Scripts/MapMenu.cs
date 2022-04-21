using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapMenu : MonoBehaviour
{
    public static MapMenu main;
    void Awake () {main = this;}

    public Button dropdown_button;
    public Image dropdown_image;
    public GameObject dropdown_content;

    public Sprite more_actions_icon;
    public Sprite close_icon;



    public Button menu_toggle;
    public Image menu_tint;
    public GameObject menu_content;

    public bool menu_opened{get; private set;}


    // The custom states of dropdown_button
    //  1: Hiding Content (Default)
    //  2: Showing Content
    //  -1: Will close GUI of whatever's opened
    public int dd_btn_state{get;set;} = 1;




    // If tf is specified, it'll force an open/close.
    public void OnMenuToggle()
    {
        if (menu_opened)
        {
            menu_content.SetActive(false);
            menu_tint.gameObject.SetActive(false);
            menu_opened = false;
        } else {
            menu_content.SetActive(true);
            menu_tint.gameObject.SetActive(true);
            menu_opened = true;
        }
    }
    public void OpenMenu()
    {
        menu_content.SetActive(true);
        menu_tint.gameObject.SetActive(true);
        menu_opened = true;
    }
    public void CloseMenu()
    {
        menu_content.SetActive(false);
        menu_tint.gameObject.SetActive(false);
        menu_opened = false;
    }



    public void OnSearchClick()
    {
        SearchRoom.main.gameObject.SetActive(true);
    }

    public void OnMoveClick()
    {
        MoveUser.main.gameObject.SetActive(true);
    }

    public void OnTutorialClick()
    {
        Tutorial.main.gameObject.SetActive(true);
    }

    // Does nothing until implemented
    public void OnSettingsClick()
    {
        BoxMessage.Send(@"Settings are not ready. Apologies.");
    }
}
