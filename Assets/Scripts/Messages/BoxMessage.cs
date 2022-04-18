using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxMessage : MonoBehaviour
{
    public static BoxMessage main;
    public GameObject message_box_prefab;
    static int max_msg_count = 5; // This doesn't need to be exposed. Just change the value here if needed.
    public static List<MessageBox> message_boxes = new List<MessageBox>();

    // Note that every message instantiates a new MessageBox GameObject. To prevent bugs overloading the screen,
    //      messages will be ignored when there's too much, tracked by max_msg_count.
    // message_id: A "No repeat" button will appear if not empty. Message will not send if the user ticked the box.
    public static void Send(string txt, string message_id = "", float close_timer = 0)
    {
        if (message_boxes.Count >= max_msg_count) return;

        if (message_id.Length > 0 && BoxMessageNoRepeatData.Contains(message_id)) return;

        GameObject instance = Instantiate(main.message_box_prefab, main.transform);
        instance.name = string.Format("Message ({0}/{1})", message_boxes.Count + 1, max_msg_count);
        MessageBox msg_box = instance.GetComponent<MessageBox>();
        if (message_id.Length > 0) msg_box.NoRepeat(message_id);
        msg_box.Display(txt, close_timer); // The prefab should have the script by default.
        message_boxes.Add(msg_box);
    }

    // SO means scriptable object.
    public static void Send_SO(MessageScriptableObject mso)
    {
        Send(mso.message_txt, mso.prompt_no_repeat ? mso.name : "", mso.display_time);
    }

    void Awake()
    {
        main = this;


    }

    void Update()
    {
        for (var i = message_boxes.Count - 1; i >= 0; i--)
        {
            if (!message_boxes[i].closed) continue;
            
            // For this to be true, it's guaranteed that 1: User clicked on the checkbox, 2: message has an id.
            if (message_boxes[i].no_repeat_btn.isOn)
            {
                BoxMessageNoRepeatData.Add(message_boxes[i].message_id);
            }

            Destroy(message_boxes[i].gameObject);
            message_boxes.RemoveAt(i);
        }
    }
}
