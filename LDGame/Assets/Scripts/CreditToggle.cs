using UnityEngine;
using UnityEngine.EventSystems;

public class CreditButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Credit Panel")]
    [SerializeField] private GameObject creditPanel;

    [Header("Scale Settings")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.12f;
    [SerializeField] private float scaleSpeed = 10f;

    private RectTransform rectTransform;
    private Vector3 targetScale;
    private bool isOpen = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogError("Need RectTransform!");
            enabled = false;
            return;
        }

        targetScale = Vector3.one * normalScale;
        rectTransform.localScale = targetScale;
    }

    private void Start()
    {
        if (creditPanel != null)
        {
            creditPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // 缩放动画
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            Time.unscaledDeltaTime * scaleSpeed
        );

        //核心逻辑：只要打开状态，任意点击关闭
        if (isOpen && Input.GetMouseButtonDown(0))
        {
            CloseCredit();
        }
    }

    // 鼠标进入
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = Vector3.one * hoverScale;
    }

    // 鼠标离开
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one * normalScale;
    }

    // 点击按钮
    public void OnPointerClick(PointerEventData eventData)
    {
        if (creditPanel == null) return;

        if (!isOpen)
        {
            OpenCredit();
        }
        else
        {
            CloseCredit();
        }
    }

    private void OpenCredit()
    {
        isOpen = true;
        creditPanel.SetActive(true);
    }

    private void CloseCredit()
    {
        isOpen = false;
        creditPanel.SetActive(false);
    }
}