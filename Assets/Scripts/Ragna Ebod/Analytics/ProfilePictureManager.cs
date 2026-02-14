using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class ProfilePictureManager : MonoBehaviour
{
    public static ProfilePictureManager Instance;

    [Header("UI References")]
    // [CHANGED] Now an Array to support multiple locations (Menu, HUD, etc.)
    public Image[] profileImages; 
    public Button changePfpButton;   

    [Header("Default Avatars")]
    public Sprite[] defaultAvatars;  

    // TRACKING
    public int CurrentAvatarIndex { get; private set; } = 0;
    private string savePath;
    
    // Cache the custom texture
    private Sprite _cachedCustomSprite;

    private void Awake()
    {
        // [FIX] SINGLETON UI HANDOFF
        if (Instance != null && Instance != this) 
        { 
            // 1. Pass the NEW UI connections to the OLD Singleton
            Instance.profileImages = this.profileImages; // Update Array
            Instance.changePfpButton = this.changePfpButton;

            // 2. Re-attach the button listener
            if (Instance.changePfpButton != null) 
                Instance.changePfpButton.onClick.AddListener(Instance.OnProfileClicked);

            // 3. Force update
            Instance.RefreshCurrentProfileUI();

            // 4. Destroy duplicate
            Destroy(gameObject); 
            return; 
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        savePath = Path.Combine(Application.persistentDataPath, "user_pfp.png");
    }

    private void Start()
    {
        if (Instance == this)
        {
            LoadInitialData();
            if (changePfpButton != null) changePfpButton.onClick.AddListener(OnProfileClicked);
        }
    }

    private void LoadInitialData()
    {
        if (PlayerPrefs.HasKey("SavedAvatarIndex"))
        {
            CurrentAvatarIndex = PlayerPrefs.GetInt("SavedAvatarIndex");
            RefreshCurrentProfileUI(); 
        }
        else
        {
            // First time launch: Pick Random
            SetRandomDefaultPFP();
        }
    }

    public void RefreshCurrentProfileUI()
    {
        Sprite spriteToUse = null;

        // Case 1: Default Avatar (0-7)
        if (CurrentAvatarIndex >= 0 && CurrentAvatarIndex < defaultAvatars.Length)
        {
            spriteToUse = defaultAvatars[CurrentAvatarIndex];
        }
        // Case 2: Custom Avatar (-1)
        else if (CurrentAvatarIndex == -1)
        {
            if (_cachedCustomSprite != null)
            {
                spriteToUse = _cachedCustomSprite;
            }
            else if (File.Exists(savePath))
            {
                StartCoroutine(LoadSavedPFP());
                return; // Wait for coroutine
            }
        }

        // Apply to ALL linked images
        if (spriteToUse != null)
        {
            UpdateAllImages(spriteToUse);
        }
    }

    private void UpdateAllImages(Sprite sprite)
    {
        if (profileImages == null) return;

        foreach (var img in profileImages)
        {
            if (img != null) img.sprite = sprite;
        }
    }

    public void OnProfileClicked()
    {
        Debug.Log("[PFP] Opening File Picker...");

        #if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel("Select Profile Picture", "", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(path)) StartCoroutine(LoadImageFromDisk(path));
        #elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            Debug.LogWarning("Install StandaloneFileBrowser!");
        #elif UNITY_ANDROID || UNITY_IOS
            Debug.LogWarning("Install NativeGallery!");
        #endif
    }

    private void SetRandomDefaultPFP()
    {
        if (defaultAvatars.Length > 0)
        {
            int randomIndex = Random.Range(0, defaultAvatars.Length);
            SetAvatarByIndex(randomIndex);
        }
    }

    public void SetAvatarByIndex(int index)
    {
        if (index >= 0 && index < defaultAvatars.Length)
        {
            CurrentAvatarIndex = index;
            PlayerPrefs.SetInt("SavedAvatarIndex", index);
            PlayerPrefs.Save();
            
            _cachedCustomSprite = null; 

            UpdateAllImages(defaultAvatars[index]);
        }
    }

    public void SetCustomAvatar(Texture2D newTexture)
    {
        CurrentAvatarIndex = -1;
        PlayerPrefs.SetInt("SavedAvatarIndex", -1);
        PlayerPrefs.Save();

        Sprite newSprite = Sprite.Create(newTexture, new Rect(0,0,newTexture.width,newTexture.height), new Vector2(0.5f,0.5f));
        _cachedCustomSprite = newSprite;

        UpdateAllImages(newSprite);
        SaveTextureToDisk(newTexture);
    }

    private void SaveTextureToDisk(Texture2D texture)
    {
        try { File.WriteAllBytes(savePath, texture.EncodeToPNG()); } catch {}
    }

    private IEnumerator LoadImageFromDisk(string path)
    {
        string url = "file://" + path;
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                SetCustomAvatar(tex);
            }
        }
    }

    private IEnumerator LoadSavedPFP()
    {
        string url = "file://" + savePath;
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                Sprite savedSprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
                
                _cachedCustomSprite = savedSprite;
                UpdateAllImages(savedSprite);
            }
        }
    }

    // [NEW] Helper function for Leaderboard to grab the current sprite safely
    public Sprite GetCurrentSprite()
    {
        // 1. If Custom (-1), return the cached custom sprite
        if (CurrentAvatarIndex == -1)
        {
            return _cachedCustomSprite;
        }
        
        // 2. If Default (0-7), return from array
        if (CurrentAvatarIndex >= 0 && CurrentAvatarIndex < defaultAvatars.Length)
        {
            return defaultAvatars[CurrentAvatarIndex];
        }

        // 3. Fallback: Return whatever is on the first UI Image
        if (profileImages != null && profileImages.Length > 0 && profileImages[0] != null)
        {
            return profileImages[0].sprite;
        }

        return null;
    }
}