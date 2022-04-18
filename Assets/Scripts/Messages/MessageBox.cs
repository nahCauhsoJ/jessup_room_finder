using UnityEngine;

public class MessageBox : MonoBehaviour
{
    public bool closed{get; private set;}

    public TMPro.TMP_Text msg_obj;
    public TMPro.TMP_Text btn_txt_obj;

    // This is the extra bottom bar plus the toggle button. If unused, its Game Object will not be active.
    public UnityEngine.UI.Image no_repeat_bar;
    public UnityEngine.UI.Toggle no_repeat_btn; // This is the actual toggle button.

    // If not empty, a "No repeat" button will appear, and will be recorded if the user ticks the box.
    public string message_id;

    string orig_btn_txt; // This is for the timer to append to text instead of overwriting.

    // The message box's game object is disabled by default. So the message is only shown after using Display()
    // If the message box will close itself after some time, set close_timer.
    public void Display(string txt, float close_timer = 0)
    {
        msg_obj.text = txt;
        gameObject.SetActive(true);

        if (close_timer > 0)
        {
            orig_btn_txt = btn_txt_obj.text;
            StartCoroutine(DisplayTimeLimit(close_timer));
        }
    }
    // Please make sure it's at most 1 decimal place.
    System.Collections.IEnumerator DisplayTimeLimit(float sec)
    {
        if (sec % 1 == 0)
            while (sec >= 1f)
            {
                btn_txt_obj.text = string.Format("{0} ({1}s)", orig_btn_txt, sec);
                sec--;
                yield return new WaitForSeconds(1f);
            }
        else
            while (sec >= 0)
            {
                btn_txt_obj.text = string.Format("{0} ({1}s)", orig_btn_txt, Mathf.Round(sec*10)*.1);
                sec-=.1f;
                yield return new WaitForSeconds(.1f);
            }
        btn_txt_obj.text = orig_btn_txt;
        Close();
    }

    // This is a lazy function to make a no-repeat option available.
    public void NoRepeat(string message_id)
    {
        this.message_id = message_id;
        msg_obj.rectTransform.anchoredPosition = new Vector2(
            msg_obj.rectTransform.anchoredPosition.x,
            msg_obj.rectTransform.anchoredPosition.y + no_repeat_bar.rectTransform.sizeDelta.y / 2
        );
        msg_obj.rectTransform.sizeDelta = new Vector2(
            msg_obj.rectTransform.sizeDelta.x, 
            msg_obj.rectTransform.sizeDelta.y - no_repeat_bar.rectTransform.sizeDelta.y
        );
        no_repeat_bar.gameObject.SetActive(true);
    }

    public void Close()
    {   // BoxMessage will handle the destroying.
        closed = true;
    }
}
