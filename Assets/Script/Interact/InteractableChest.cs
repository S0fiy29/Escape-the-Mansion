using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class InteractableChest : MonoBehaviour
{
    public bool isRange;
    public KeyCode interactKey = KeyCode.F;
    public UnityEvent interactAction;

    private bool hasInteracted = false;
    public InteractableChestData chestData;
    public Animator animator;
    public Collider2D triggerCollider;

    [Header("Proximity Cue")]
    [Tooltip("Plays a soft chime that gets faster and louder as the player gets close, so hidden chests can be found by ear.")]
    public bool playProximityCue = true;
    [Tooltip("Distance in world units where the chime starts.")]
    public float cueRadius = 5f;
    [Tooltip("Seconds between chimes at the edge of the radius.")]
    public float cueMaxInterval = 2.5f;
    [Tooltip("Seconds between chimes right next to the chest.")]
    public float cueMinInterval = 0.6f;
    [Range(0f, 1f)] public float cueVolume = 0.4f;
    [Tooltip("Optional. Leave empty to use the built-in chime.")]
    public AudioClip cueClip;

    private AudioSource cueSource;
    private Transform player;
    private float nextCueTime;
    private static AudioClip defaultCue;

    private void Awake()
    {
        chestData = GetComponent<InteractableChestData>();
    }

    private void Update()
    {
        if (isRange && !hasInteracted && Input.GetKeyDown(interactKey))
        {
            Interact();
        }

        UpdateProximityCue();
    }

    private void UpdateProximityCue()
    {
        if (!playProximityCue || hasInteracted || (chestData != null && chestData.isOpen)) return;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null) return;
            player = playerObject.transform;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > cueRadius || Time.time < nextCueTime) return;

        // 0 at the edge of the radius, 1 right next to the chest.
        float closeness = 1f - Mathf.Clamp01(distance / cueRadius);

        if (cueSource == null)
        {
            cueSource = gameObject.AddComponent<AudioSource>();
            cueSource.playOnAwake = false;
            cueSource.spatialBlend = 0f;
        }

        cueSource.pitch = Mathf.Lerp(0.9f, 1.25f, closeness);
        cueSource.PlayOneShot(cueClip != null ? cueClip : GetDefaultCue(), cueVolume * Mathf.Lerp(0.2f, 1f, closeness));
        nextCueTime = Time.time + Mathf.Lerp(cueMaxInterval, cueMinInterval, closeness);
    }

    // A short two-note chime made in code, so no audio file is needed.
    private static AudioClip GetDefaultCue()
    {
        if (defaultCue != null) return defaultCue;

        const int sampleRate = 44100;
        const float length = 0.7f;
        float[] samples = new float[(int)(sampleRate * length)];

        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-5f * t) * Mathf.Clamp01(t * 200f);
            float tone = Mathf.Sin(2f * Mathf.PI * 880f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 1318.5f * t);
            samples[i] = tone * envelope * 0.35f;
        }

        defaultCue = AudioClip.Create("ChestCue", samples.Length, 1, sampleRate, false);
        defaultCue.SetData(samples, 0);
        return defaultCue;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !hasInteracted)
        {
            isRange = true;
            PlayerManager manager = collision.gameObject.GetComponent<PlayerManager>();
            if (manager != null)
            {
                manager.NotifyPlayer("Press 'F' to open the chest");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isRange = false;
            PlayerManager manager = collision.gameObject.GetComponent<PlayerManager>();
            if (manager != null)
            {
                manager.DeNotifyPlayer();
            }
        }
    }

    private void Interact()
    {
        if (hasInteracted || chestData == null) return;

        hasInteracted = true;
        chestData.Interact();
        interactAction.Invoke();
        UpdateChestState();

        if (chestData.isOpen)
        {
            PlayerManager manager = FindAnyObjectByType<PlayerManager>();
            if (manager != null)
            {
                switch (chestData.chestKeyType)
                {
                    case InteractableChestData.KeyType.General:
                        manager.PickupKey();
                        break;
                    case InteractableChestData.KeyType.Gate:
                        manager.PickupGateKey();
                        break;
                    case InteractableChestData.KeyType.Store:
                        manager.PickupStoreKey();
                        break;
                    case InteractableChestData.KeyType.Secret:
                        manager.PickupSecretKey();
                        break;
                    case InteractableChestData.KeyType.None:
                        manager.NotifyPlayer("The chest is empty.");
                        break;
                }
            }
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        StartCoroutine(ClearNotificationAfterDelay(1.5f));
    }

    private IEnumerator ClearNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        PlayerManager manager = FindAnyObjectByType<PlayerManager>();
        if (manager != null)
        {
            manager.DeNotifyPlayer();
        }
        else
        {
            Debug.LogWarning("PlayerManager not found when trying to clear notification.");
        }
    }

    public void UpdateChestState()
    {
        if (chestData != null)
        {
            if (animator != null)
            {
                animator.SetBool("IsOpened", chestData.isOpen);
            }

            if (triggerCollider != null)
            {
                triggerCollider.enabled = !chestData.isOpen;
            }
        }
    }

    public SaveData GetChestState()
    {
        SaveData chestState = new SaveData
        {
            uniqueID = chestData.uniqueID,
            isOpen = chestData.isOpenInt, // Save as integer
            isColliderEnabled = triggerCollider.enabled
        };
        return chestState;
    }

    public void RestoreChestState(SaveData chestState)
    {
        if (chestData != null)
        {
            chestData.RestoreState(chestData.IsInteracted, chestState.isOpen); // Restore as integer
            triggerCollider.enabled = chestState.isColliderEnabled;
            UpdateChestState();
        }
    }
}
