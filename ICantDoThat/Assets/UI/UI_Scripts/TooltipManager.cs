using UnityEngine;
using System;
using System.Collections.Generic;

// 1. The Inspector layout for each tooltip
[Serializable]
public class TooltipEntry
{
    [Tooltip("The ID your programmer will pass (e.g., 'Airlock', 'CapPoint', 'EndTurn')")]
    public string buttonID;

    [TextArea(2, 5)]
    [Tooltip("The text that will print to the console.")]
    public string tooltipText;
}

public class TooltipManager : MonoBehaviour
{
    // ==========================================
    // 1. THE EVENT
    // ==========================================
    // Programmers just call TooltipManager.OnTooltipRequested?.Invoke("ButtonName");
    public static Action<string> OnTooltipRequested;

    // ==========================================
    // 2. THE INSPECTOR MENU
    // ==========================================
    [Header("Player Aids & Tooltips")]
    public List<TooltipEntry> tooltipDatabase = new List<TooltipEntry>();

    // ==========================================
    // 3. SUBSCRIPTIONS
    // ==========================================
    private void OnEnable()
    {
        OnTooltipRequested += PrintTooltip;
    }

    private void OnDisable()
    {
        OnTooltipRequested -= PrintTooltip;
    }

    // ==========================================
    // 4. THE ROUTING LOGIC
    // ==========================================
    private void PrintTooltip(string passedButtonID)
    {
        if (string.IsNullOrEmpty(passedButtonID)) return;

        // Search our Inspector list for the matching button ID
        TooltipEntry foundTooltip = tooltipDatabase.Find(t => t.buttonID.ToLower() == passedButtonID.ToLower());

        if (foundTooltip != null)
        {
            // Print it to the console! 
            // We use ConsoleLogType.Flavor so it gets that nice yellow/system color, 
            // and "0.0" so it shows the static screen instead of a character portrait.
            RetroConsole.Instance.Log(foundTooltip.tooltipText, ConsoleLogType.Flavor, "0.0");
        }
        else
        {
            Debug.LogWarning($"[TooltipManager] Someone requested a tooltip for '{passedButtonID}', but it isn't in the Inspector list!");
        }
    }
}