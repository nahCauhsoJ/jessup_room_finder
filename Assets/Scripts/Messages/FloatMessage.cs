using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatMessage : MonoBehaviour
{
    public static FloatMessage main;

    public TMPro.TMP_Text txt_obj;
    public Animator txt_animator;

    Coroutine text_stay_coroutine; // This is here to interrupt a running coroutine.

    void Awake() {main = this;}
    public static void Send(string txt, float show_time = 3f)
    {
        main.txt_obj.text = txt;
        if (main.text_stay_coroutine != null) main.StopCoroutine(main.text_stay_coroutine);
        main.text_stay_coroutine = main.StartCoroutine(main.msg_show(show_time));
    }
    IEnumerator msg_show(float show_time)
    {
        txt_animator.SetTrigger("interrupt"); // New messages can pop up mid-fade.
        txt_obj.gameObject.SetActive(true);
        txt_animator.enabled = true;
        yield return new WaitForSeconds(show_time);
        txt_animator.SetTrigger("fade");
    }

    public void EndAnimation()
    {
        // Apparently disabling animator here makes this trigger left active if interrupted, so it needs manual resetting.
        txt_animator.ResetTrigger("fade");
        txt_animator.enabled = false;
        txt_obj.text = "";
        txt_obj.alpha = 1f;
        txt_obj.gameObject.SetActive(false);
        txt_obj.rectTransform.anchoredPosition = Vector2.zero;
    }
}
