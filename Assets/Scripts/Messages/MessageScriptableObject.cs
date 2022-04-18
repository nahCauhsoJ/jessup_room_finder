using UnityEngine;

[CreateAssetMenu(fileName = "BoxMessage", menuName = "Messages/BoxMessage", order = 1)]
public class MessageScriptableObject : ScriptableObject
{
    [TextArea] public string message_txt;
    public bool prompt_no_repeat;
    public float display_time;
}
