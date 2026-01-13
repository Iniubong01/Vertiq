using UnityEngine;
using System.Collections.Generic;

public class MobileDebugger : MonoBehaviour
{
    private Queue<string> logQueue = new Queue<string>();
    private string output = "";
    private Vector2 scrollPosition;
    private bool isVisible = true; // Set to true by default to see errors immediately

    void OnEnable() { Application.logMessageReceived += HandleLog; }
    void OnDisable() { Application.logMessageReceived -= HandleLog; }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string color = "white";
        if (type == LogType.Error || type == LogType.Exception) color = "red";
        else if (type == LogType.Warning) color = "yellow";

        // Simplify log for readability
        if (logString.Length > 200) logString = logString.Substring(0, 200) + "...";

        string newLog = $"<color={color}>[{type}] {logString}</color>";
        
        logQueue.Enqueue(newLog);
        if (logQueue.Count > 20) logQueue.Dequeue(); // Keep last 20 logs

        output = string.Join("\n\n", logQueue.ToArray());
        
        // Auto-scroll logic could go here, but usually just updating output is enough
    }

    void OnGUI()
    {
        if (!isVisible) return;

        // Scale GUI for high DPI mobile screens
        float scale = Screen.dpi / 160f; 
        if (scale < 1) scale = 1;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

        // Background Box
        GUI.Box(new Rect(10, 10, 450, 600), "Debug Console");

        // Scroll View
        GUILayout.BeginArea(new Rect(20, 40, 430, 560));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.Label(output); // Rich text tags work in GUI.Label
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    
    void Update()
    {
        // Toggle with 3-finger tap
        if (Input.touchCount == 3 && Input.GetTouch(0).phase == TouchPhase.Began)
            isVisible = !isVisible;
    }
}