using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private AudioSource audioSource;
    [SerializeField] AudioClip deathSound, enemyDeathSound;
    [SerializeField] AudioClip buttonSound, hoverSound;
    public static SoundManager Instance;

    [SerializeField] private AudioSource splashAudioSource;
    [SerializeField] private AudioSource gameAudioSource;

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

    public void PlayEnemyDeathClip()
    {
        audioSource.PlayOneShot(enemyDeathSound);
    }

    public void PlayButtonSound()
    {
        audioSource.PlayOneShot(buttonSound);
    }

    public void PlayHoverSound()
    {
        audioSource.PlayOneShot(hoverSound);
    }

    public void EnableSplashAS()
    {
        if(gameAudioSource.isPlaying)
        {     
            gameAudioSource.Stop();
            gameAudioSource.gameObject.SetActive(false);
            splashAudioSource.gameObject.SetActive(true);
        }
        splashAudioSource.Play();
    }

    public void EnableInGameAS()
    {
        if(splashAudioSource.isPlaying)
        {     
            splashAudioSource.Stop();
            splashAudioSource.gameObject.SetActive(false);
            gameAudioSource.gameObject.SetActive(true);
        }
        gameAudioSource.Play();
    }
}
