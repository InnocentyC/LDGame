using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepByMovement : MonoBehaviour
{
    [Header("Movement Check")]
    [Tooltip("小于这个距离变化会被视为没移动，避免抖动误触发")]
    public float moveThreshold = 0.001f;

    [Header("Audio")]
    [Tooltip("脚步声音频，直接拖进来")]
    public AudioClip footstepClip;

    private AudioSource audioSource;
    private Vector3 lastPosition;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        lastPosition = transform.position;

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.clip = footstepClip;
    }

    void Update()
    {
        float movedDistance = Vector3.Distance(transform.position, lastPosition);

        bool isMoving = movedDistance > moveThreshold;

        if (isMoving)
        {
            if (!audioSource.isPlaying && footstepClip != null)
            {
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }

        lastPosition = transform.position;
    }
}