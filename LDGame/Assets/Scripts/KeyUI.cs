using UnityEngine;

public class KeyUI : MonoBehaviour
{
    public static KeyUI Instance;

    [Header("UI")]
    [SerializeField] private GameObject root;

    [SerializeField] private GameObject Key01;
    [SerializeField] private GameObject Key02;
    [SerializeField] private GameObject Key03;


    private void Awake()
    {
        Instance = this;
        HideAll();
    }

    public void ShowKey(string keyId)
    {
        HideAll();

        switch (keyId)
        {
            case "Key01":
                Key01.SetActive(true);
                break;
            case "Key02":
                Key02.SetActive(true);
                break;
            case "Key03":
                Key03.SetActive(true);
                break;
        }

        root.SetActive(true);
    }

    public void HideAll()
    {
        Key01.SetActive(false);
        Key02.SetActive(false);
        Key03.SetActive(false);
        root.SetActive(false);
    }
}