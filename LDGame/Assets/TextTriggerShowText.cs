using TMPro;
using UnityEngine;
using UnityEngine.UI;

//这个脚本控制游戏中文本trigger，将trigger物体摆放在对应位置然后设置碰撞区域。玩家经过这个区域时就在一个textUI元件上显示文字

public class TriggerShowUIText : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] public TextMeshProUGUI targetText;
    [SerializeField] public Image textWindow;

    [Header("Content")]
    [SerializeField] public string message = "some words";


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return; // 可选：给玩家加 Tag=Player
        if (targetText != null)
            textWindow.IsActive();
            targetText.text = message;
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (targetText != null)
            targetText.text = "Go there......";
    }
}

