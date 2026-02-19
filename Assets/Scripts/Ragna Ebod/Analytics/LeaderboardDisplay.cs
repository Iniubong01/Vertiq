using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using System.Collections.Generic;
using Unity.Services.Leaderboards;
using Unity.Services.Authentication; 
using Newtonsoft.Json; 

public class LeaderboardDisplay : MonoBehaviour
{
    [Header("Configuration")]
    public string leaderboardId = "vortiq_leaderboard";
    public Transform contentContainer;
    
    [Header("Row Prefabs")]
    public GameObject firstPlacePrefab;   
    public GameObject secondPlacePrefab;  
    public GameObject thirdPlacePrefab;
    
    [Header("Standard Rows")]
    public GameObject standardPrefab;       // For normal players (Rank 4+)
    public GameObject standardMePrefab;     // For YOU (Rank 4+)

    [Header("My Position Banner")]
    [Tooltip("Container shown below the leaderboard when you are outside the top 5.")]
    public Transform myPositionContainer;
    [Tooltip("Prefab used for the 'my position' banner. Falls back to standardMePrefab if not set.")]
    public GameObject myPositionPrefab;

    [Header("Assets")]
    public Sprite[] avatarIcons;
    
    [Header("UI Feedback")]
    public NotificationPopup notificationPopup;
    
    private void Start()
    {
        // Auto-find NotificationPopup if not assigned
        if (notificationPopup == null)
        {
            notificationPopup = FindFirstObjectByType<NotificationPopup>();
            if (notificationPopup == null)
            {
                Debug.LogWarning("[LeaderboardDisplay] NotificationPopup not found in scene.");
            }
        }
    } 

    public async void RefreshLeaderboard()
    {
        // Check if authentication service is available
        if (AuthenticationService.Instance == null)
        {
            Debug.LogError("[Leaderboard] AuthenticationService not available");
            ShowNotification("Service Error", "Leaderboard service unavailable.", Color.red);
            return;
        }
        
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[Leaderboard] User not signed in, cannot fetch leaderboard");
            return;
        }

        if (contentContainer == null)
        {
            Debug.LogError("[Leaderboard] Content container is null!");
            ShowNotification("UI Error", "Leaderboard UI not properly configured.", Color.red);
            return;
        }

        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        // Clear the "my position" banner container too
        if (myPositionContainer != null)
            foreach (Transform child in myPositionContainer) Destroy(child.gameObject);

        try
        {
            Debug.Log($"[Leaderboard] Fetching leaderboard: {leaderboardId}");
            
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(
                leaderboardId, 
                new GetScoresOptions { Limit = 50, IncludeMetadata = true }
            );

            if (scoresResponse == null)
            {
                //Debug.LogError("[Leaderboard] scoresResponse is NULL!");
                ShowNotification("Fetch Failed", "Could not retrieve leaderboard data.", Color.red);
                return;
            }
            
            if (scoresResponse.Results == null)
            {
                //Debug.LogError("[Leaderboard] scoresResponse.Results is NULL!");
                ShowNotification("Fetch Failed", "Leaderboard data is empty.", Color.red);
                return;
            }
            
            Debug.Log($"[Leaderboard] Fetched {scoresResponse.Results.Count} entries");

            string myPlayerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[Leaderboard] My Player ID: {myPlayerId}");

            // Track my entry for the position banner
            int myRank = -1;
            string myName = "";
            double myScore = 0;
            int myAvatarIndex = 0;

            int processedCount = 0;
            foreach (var entry in scoresResponse.Results)
            {
                int avatarIndex = 0; 
                if (!string.IsNullOrEmpty(entry.Metadata))
                {
                    try 
                    {
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(entry.Metadata);
                        if (data.ContainsKey("avatar")) int.TryParse(data["avatar"], out avatarIndex);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[Leaderboard] Failed to parse metadata for entry: {e.Message}");
                    }
                }

                bool isMe = (entry.PlayerId == myPlayerId);
                Debug.Log($"[Leaderboard] Processing entry {processedCount + 1}: Rank={entry.Rank + 1}, Name={entry.PlayerName}, Score={entry.Score}");
                Debug.Log($"[Leaderboard] IsMe Check: entry.PlayerId='{entry.PlayerId}' vs myPlayerId='{myPlayerId}' -> IsMe={isMe}");
                
                CreateLeaderboardRow(entry.Rank + 1, entry.PlayerName, entry.Score, avatarIndex, isMe);

                // Cache my data for the position banner
                if (isMe)
                {
                    myRank        = entry.Rank + 1;
                    myName        = entry.PlayerName;
                    myScore       = entry.Score;
                    myAvatarIndex = avatarIndex;
                }

                processedCount++;
            }

            // --- MY POSITION BANNER ---
            // Show only when I'm outside the top 5 and the container is assigned
            if (myPositionContainer != null)
            {
                if (myRank > 5)
                {
                    GameObject bannerPrefab = myPositionPrefab != null ? myPositionPrefab : standardMePrefab;
                    if (bannerPrefab != null)
                    {
                        CreateLeaderboardRow(myRank, myName, myScore, myAvatarIndex, isMe: true, targetContainer: myPositionContainer);
                        myPositionContainer.gameObject.SetActive(true);
                        Debug.Log($"[Leaderboard] My position banner shown at rank {myRank}");
                    }
                }
                else
                {
                    // I'm in the top 5 — hide the banner
                    myPositionContainer.gameObject.SetActive(false);
                }
            }
            
            Debug.Log($"[Leaderboard] ✅ Successfully processed {processedCount} leaderboard entries");
        }
        catch (System.Exception e) 
        { 
            Debug.LogError($"[Leaderboard] ❌ Fetch Failed: {e.Message}");
            Debug.LogError($"[Leaderboard] Stack trace: {e.StackTrace}");
            
            // Provide user-friendly error messages
            string userMessage = "Could not load leaderboard.";
            if (e.Message.Contains("network") || e.Message.Contains("timeout") || e.Message.Contains("connection"))
            {
                userMessage = "Network error. Check your connection.";
            }
            else if (e.Message.Contains("unauthorized") || e.Message.Contains("authentication"))
            {
                userMessage = "Authentication failed. Please reconnect.";
            }
            
            ShowNotification("Fetch Failed", userMessage, Color.red);
        }
    }

    // targetContainer: if null, uses the default contentContainer.
    // Used by the "my position" banner to write into a separate container.
    private void CreateLeaderboardRow(int rank, string playerName, double score, int avatarIndex, bool isMe, Transform targetContainer = null)
    {
        Debug.Log($"[Leaderboard] CreateLeaderboardRow called - Rank: {rank}, Name: {playerName}, Score: {score}, IsMe: {isMe}");

        Transform container = targetContainer != null ? targetContainer : contentContainer;
        
        // 1. CHOOSE PREFAB
        GameObject prefabToUse = standardPrefab; 

        if (rank == 1 && firstPlacePrefab != null) 
        {
            prefabToUse = firstPlacePrefab;
            Debug.Log("[Leaderboard] Using firstPlacePrefab");
        }
        else if (rank == 2 && secondPlacePrefab != null) 
        {
            prefabToUse = secondPlacePrefab;
            Debug.Log("[Leaderboard] Using secondPlacePrefab");
        }
        else if (rank == 3 && thirdPlacePrefab != null) 
        {
            prefabToUse = thirdPlacePrefab;
            Debug.Log("[Leaderboard] Using thirdPlacePrefab");
        }
        else
        {
            // Rank 4 and below
            if (isMe && standardMePrefab != null) 
            {
                prefabToUse = standardMePrefab;
                Debug.Log("[Leaderboard] Using standardMePrefab");
            }
            else 
            {
                prefabToUse = standardPrefab;
                Debug.Log("[Leaderboard] Using standardPrefab");
            }
        }

        if (prefabToUse == null)
        {
            Debug.LogError($"[Leaderboard] No prefab available for rank {rank}");
            //ShowNotification("UI Error", "Missing leaderboard row prefab.", Color.yellow);
            return;
        }
        
        if (container == null)
        {
            Debug.LogError("[Leaderboard] Content container is null!");
            return;
        }

        GameObject newRow = Instantiate(prefabToUse, container);
        
        if (newRow == null)
        {
            Debug.LogError($"[Leaderboard] Failed to instantiate row for rank {rank}");
            return;
        }

        
        // 2. DEFINE COLORS
        Color rankColor = Color.white; 
        if (rank == 1) rankColor = new Color(1f, 0.84f, 0f);            // GOLD
        else if (rank == 2) rankColor = new Color(0.75f, 0.75f, 0.75f); // SILVER
        else if (rank == 3) rankColor = new Color(0.8f, 0.5f, 0.2f);    // BRONZE

        // 3. SET TEXT
        TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();
        
        if (texts.Length >= 3)
        {
            // --- RANK ---
            texts[0].text = rank.ToString();
            texts[0].color = rankColor; 

            // --- NAME ---
            string displayName = playerName;
            if (displayName.Contains("#")) displayName = displayName.Split('#')[0];
            
            texts[1].color = Color.white; // Keep name white
            
            if (isMe) 
            {
                // Format: Name [YOU] (Pink)
                texts[1].text = $"{displayName} <color=#FF00FF>[YOU]</color>"; 
            }
            else 
            {
                texts[1].text = displayName; 
            }

            // --- SCORE ---
            texts[2].text = score.ToString();
            texts[2].color = rankColor; 
        }

        // 4. SET AVATAR IMAGE
        Image[] images = newRow.GetComponentsInChildren<Image>();
        Image avatarImg = null;

        foreach(var img in images)
        {
            if (img.gameObject != newRow && (img.gameObject.name.Contains("Avatar") || img.gameObject.name.Contains("Icon"))) 
            {
                avatarImg = img;
                break;
            }
        }
        
        if (avatarImg == null && images.Length > 1) avatarImg = images[1];

        if (avatarImg != null)
        {
            // [FIXED LOGIC HERE]
            // Local Override for ME
            if (isMe && ProfilePictureManager.Instance != null)
            {
                try
                {
                    Sprite mySprite = ProfilePictureManager.Instance.GetCurrentSprite();
                    if (mySprite != null) avatarImg.sprite = mySprite;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Leaderboard] Failed to get current sprite: {e.Message}");
                }
            }
            // Remote Default for OTHERS
            else
            {
                if (avatarIcons != null && avatarIndex >= 0 && avatarIndex < avatarIcons.Length)
                {
                    avatarImg.sprite = avatarIcons[avatarIndex];
                }
                else
                {
                    Debug.LogWarning($"[Leaderboard] Avatar index {avatarIndex} out of range (total: {avatarIcons?.Length ?? 0})");
                }
            }
        }
    }
    
    private void ShowNotification(string title, string message, Color color)
    {
        try
        {
            if (notificationPopup != null)
            {
                notificationPopup.Show(title, message, color);
            }
            else
            {
                Debug.Log($"[Notification] {title}: {message}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LeaderboardDisplay] Failed to show notification: {e.Message}");
        }
    }
}