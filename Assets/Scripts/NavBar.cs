using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NavBar : MonoBehaviour
{
    public List<GameObject> screens;
    public int cur_screen_ix{get; private set;}

    void Start()
    {
        screens[cur_screen_ix].SetActive(true);
    }

    public void NextScreen()
    {
        screens[cur_screen_ix].SetActive(false);
        cur_screen_ix++;
        if (cur_screen_ix >= screens.Count) cur_screen_ix = 0;
        screens[cur_screen_ix].SetActive(true);
    }
}
