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

    // The custom states of dropdown_button
    //  1: Hiding Content (Default)
    //  2: Showing Content
    //  -1: Will close GUI of whatever's opened
    public int dd_btn_state{get;set;} = 1;



    public void OnDropdownClick()
    {
        // Some functions disabled dropdown, but in all cases running this functions means it needs to come back.
        ToggleDropdown(true);

        switch (dd_btn_state)
        {
            case 1:
                dd_btn_state = 2;
                dropdown_image.sprite = close_icon;
                dropdown_content.SetActive(true);
                break;
            case 2:
                dd_btn_state = 1;
                dropdown_image.sprite = more_actions_icon;
                dropdown_content.SetActive(false);
                break;
            case -1:
                dd_btn_state = 1;
                dropdown_image.sprite = more_actions_icon;
                CloseUI();
                break;
        }
    }

    public void OnSearchClick()
    {
        OpenUI();
        SearchRoom.main.gameObject.SetActive(true);
    }

    public void OnMoveClick()
    {
        OpenUI();
        MoveUser.main.gameObject.SetActive(true);
        ToggleDropdown(false);
    }

    public void OnSettingsClick()
    {
        OpenUI();
        OnDropdownClick(); // Does nothing until implemented
    }

    // true: show back the dropdown menu, false: hide the dropdown menu
    public void ToggleDropdown(bool tf)
    {
        dropdown_button.gameObject.SetActive(tf);
    }



    void OpenUI()
    {
        dd_btn_state = 2;
        OnDropdownClick(); // I'm just lazy.
        dd_btn_state = -1;
        dropdown_image.sprite = close_icon;
    }

    void CloseUI()
    {
        SearchRoom.main.gameObject.SetActive(false);
        MoveUser.main.gameObject.SetActive(false);
    }
}
