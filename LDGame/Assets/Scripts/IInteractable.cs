using UnityEngine;

//接口定义

/// <summary>
/// 可交互物体接口
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 交互提示文本
    /// </summary>
    string InteractionPrompt { get; }

    /// <summary>
    /// 执行交互
    /// </summary>
    void Interact();

    /// <summary>
    /// 是否可以被交互
    /// </summary>
    bool CanInteract();
}
