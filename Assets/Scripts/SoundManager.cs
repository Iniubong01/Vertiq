using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private AudioSource audioSource;
    [SerializeField] AudioClip deathSound;
    [SerializeField] AudioClip buttonSound;
    public static SoundManager Instance;

    /// Awake is called when the script instance is being loaded.
    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
        
        DontDestroyOnLoad(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {  
        audioSource = GetComponent<AudioSource>();  
    }

    public void PlayDeathClip()
    {
        audioSource.PlayOneShot(deathSound);
    }

    public void PlayButtonSound()
    {
        audioSource.PlayOneShot(buttonSound);
    }
}
