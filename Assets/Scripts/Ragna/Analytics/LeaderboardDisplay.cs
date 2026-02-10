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
        if (!AuthenticationService.Instance.IsSignedIn) return;

        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        try
        {
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(
                leaderboardId, 
                new GetScoresOptions { Limit = 50, IncludeMetadata = true }
            );

            string myPlayerId = AuthenticationService.Instance.PlayerId;

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
                CreateLeaderboardRow(entry.Rank + 1, entry.PlayerName, entry.Score, avatarIndex, isMe);
            }
        }
        catch (System.Exception e) { Debug.LogError($"Fetch Failed: {e.Message}"); }
    }

    private void CreateLeaderboardRow(int rank, string playerName, double score, int avatarIndex, bool isMe)
    {
        // 1. CHOOSE PREFAB
        GameObject prefabToUse = standardPrefab; 

        if (rank == 1 && firstPlacePrefab != null) 
            prefabToUse = firstPlacePrefab;
        else if (rank == 2 && secondPlacePrefab != null) 
            prefabToUse = secondPlacePrefab;
        else if (rank == 3 && thirdPlacePrefab != null) 
            prefabToUse = thirdPlacePrefab;
        else
        {
            // Rank 4 and below
            if (isMe && standardMePrefab != null) 
                prefabToUse = standardMePrefab; 
            else 
                prefabToUse = standardPrefab;
        }

        GameObject newRow = Instantiate(prefabToUse, contentContainer);
        
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