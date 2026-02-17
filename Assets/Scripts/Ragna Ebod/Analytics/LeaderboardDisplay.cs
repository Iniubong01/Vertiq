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

    [Header("Assets")]
    public Sprite[] avatarIcons; 

    public async void RefreshLeaderboard()
    {
        Debug.Log("[Leaderboard] RefreshLeaderboard called");
        
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[Leaderboard] User is NOT signed in - cannot fetch leaderboard");
            return;
        }
        
        Debug.Log("[Leaderboard] User is signed in - proceeding to fetch");

        // Clear existing rows
        int childCount = contentContainer.childCount;
        foreach (Transform child in contentContainer) Destroy(child.gameObject);
        Debug.Log($"[Leaderboard] Cleared {childCount} existing rows");

        try
        {
            Debug.Log($"[Leaderboard] Fetching leaderboard: {leaderboardId}");
            
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(
                leaderboardId, 
                new GetScoresOptions { Limit = 50, IncludeMetadata = true }
            );

            if (scoresResponse == null)
            {
                Debug.LogError("[Leaderboard] scoresResponse is NULL!");
                return;
            }
            
            if (scoresResponse.Results == null)
            {
                Debug.LogError("[Leaderboard] scoresResponse.Results is NULL!");
                return;
            }
            
            Debug.Log($"[Leaderboard] Fetched {scoresResponse.Results.Count} entries");

            string myPlayerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[Leaderboard] My Player ID: {myPlayerId}");

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
                    catch {}
                }

                bool isMe = (entry.PlayerId == myPlayerId);
                Debug.Log($"[Leaderboard] Processing entry {processedCount + 1}: Rank={entry.Rank + 1}, Name={entry.PlayerName}, Score={entry.Score}, IsMe={isMe}");
                
                CreateLeaderboardRow(entry.Rank + 1, entry.PlayerName, entry.Score, avatarIndex, isMe);
                processedCount++;
            }
            
            Debug.Log($"[Leaderboard] ✅ Successfully processed {processedCount} leaderboard entries");
        }
        catch (System.Exception e) 
        { 
            Debug.LogError($"[Leaderboard] ❌ Fetch Failed: {e.Message}");
            Debug.LogError($"[Leaderboard] Stack trace: {e.StackTrace}");
        }
    }

    private void CreateLeaderboardRow(int rank, string playerName, double score, int avatarIndex, bool isMe)
    {
        Debug.Log($"[Leaderboard] CreateLeaderboardRow called - Rank: {rank}, Name: {playerName}, Score: {score}, IsMe: {isMe}");
        
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
            Debug.LogError($"[Leaderboard] ❌ No prefab available for rank {rank}! Check Inspector assignments.");
            return;
        }
        
        if (contentContainer == null)
        {
            Debug.LogError("[Leaderboard] ❌ contentContainer is NULL! Cannot instantiate row.");
            return;
        }

        GameObject newRow = Instantiate(prefabToUse, contentContainer);
        
        if (newRow == null)
        {
            Debug.LogError($"[Leaderboard] ❌ Failed to instantiate prefab for rank {rank}!");
            return;
        }
        
        Debug.Log($"[Leaderboard] ✅ Successfully instantiated row for rank {rank}");
        
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
                Sprite mySprite = ProfilePictureManager.Instance.GetCurrentSprite();
                if (mySprite != null) avatarImg.sprite = mySprite;
            }
            // Remote Default for OTHERS
            else
            {
                if (avatarIcons != null && avatarIndex >= 0 && avatarIndex < avatarIcons.Length)
                {
                    avatarImg.sprite = avatarIcons[avatarIndex];
                }
            }
        }
    }
}