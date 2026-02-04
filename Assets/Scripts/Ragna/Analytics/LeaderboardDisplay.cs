using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Leaderboards;
using System.Threading.Tasks;

public class LeaderboardDisplay : MonoBehaviour
{
    [Header("Configuration")]
    public string leaderboardId = "vortiq_leaderboard";
    public GameObject rowPrefab;
    public Transform contentContainer;
    //public GameObject loadingSpinner; // Optional: A spinning icon

    [Header("Pagination")]
    public int maxScoresToFetch = 50;

    // Call this when you open the Leaderboard UI
    public async void RefreshLeaderboard()
    {
        // [FIX] Safety Check
        if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("Cannot fetch leaderboard: Not signed in to Unity Services yet.");
            return; // Stop here to prevent the error
        }
        
        // 1. Clear old data
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        //if (loadingSpinner != null) loadingSpinner.SetActive(true);

        try
        {
            // 2. Fetch scores from Unity
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(
                leaderboardId, 
                new GetScoresOptions { Limit = maxScoresToFetch }
            );

            // 3. Loop through and create rows
            foreach (var entry in scoresResponse.Results)
            {
                CreateLeaderboardRow(entry.Rank + 1, entry.PlayerName, entry.Score);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to fetch leaderboard: {e.Message}");
        }
        finally
        {
            //if (loadingSpinner != null) loadingSpinner.SetActive(false);
        }
    }

    private void CreateLeaderboardRow(int rank, string playerName, double score)
    {
        GameObject newRow = Instantiate(rowPrefab, contentContainer);
        
        // Find Text Components (Assuming you named them correctly or they are in order)
        // Better way: Create a small script for the Row, but this works for simple setups
        TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length >= 3)
        {
            texts[0].text = rank.ToString(); // Rank
            
            // Clean up the name (remove the #1234 tag if it exists)
            string displayName = playerName;
            if (displayName.Contains("#")) 
                displayName = displayName.Split('#')[0];
            
            texts[1].text = displayName;     // Name
            texts[2].text = score.ToString(); // Score
        }
        else
        {
            Debug.LogWarning("Row Prefab needs 3 TextMeshProUGUI components (Rank, Name, Score)");
        }
    }
}