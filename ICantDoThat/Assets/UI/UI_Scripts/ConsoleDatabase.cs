using UnityEngine;
using System.Collections.Generic;

public class ConsoleDatabase : MonoBehaviour
{
    public static ConsoleDatabase Instance { get; private set; }

    [Header("Spreadsheet File")]
    public TextAsset databaseFile;

    private Dictionary<string, ConsoleMessageData> textDatabase = new();

    private struct ConsoleMessageData
    {
        public ConsoleLogType logType;
        public string portraitID;
        public string messageText;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;

        LoadDatabase();
    }

    private void LoadDatabase()
    {
        if (databaseFile == null) return;

        string[] rows = databaseFile.text.Split('\n');
        string lastAddedID = "";

        for (int i = 1; i < rows.Length; i++) // Skip row 0 (Header)
        {
            // Trim trailing carriage returns from Windows formatting
            string row = rows[i].TrimEnd('\r', '\n');

            if (string.IsNullOrWhiteSpace(row)) continue;

            string[] columns = row.Split('\t');

            if (columns.Length >= 3)
            {
                string id = columns[0].Trim();
                string portID = columns[1].Trim();

                // Re-join the text just in case there were tabs inside the dialogue itself
                string text = string.Join("\t", columns, 2, columns.Length - 2).Trim();

                // Spreadsheets often wrap text in quotes if it has punctuation or commas. Strip them!
                if (text.StartsWith("\"")) text = text.Substring(1);
                if (text.EndsWith("\"")) text = text.Substring(0, text.Length - 1);

                textDatabase[id] = new ConsoleMessageData
                {
                    logType = ConsoleLogType.VoiceLine, // Defaulting to blue voice lines
                    portraitID = portID,
                    messageText = text
                };

                lastAddedID = id; // Remember this ID in case the next line is a continuation!
            }
            else if (columns.Length < 3 && !string.IsNullOrEmpty(lastAddedID))
            {
                // STITCHING LOGIC: Catch sentences chopped in half by spreadsheet line-breaks
                if (textDatabase.TryGetValue(lastAddedID, out ConsoleMessageData existingData))
                {
                    string appendedText = row.Trim();

                    // Strip trailing quotes if the spreadsheet left them hanging on the second line
                    if (appendedText.EndsWith("\"")) appendedText = appendedText.Substring(0, appendedText.Length - 1);

                    existingData.messageText += "\n" + appendedText;
                    textDatabase[lastAddedID] = existingData; // Save the stitched message back into memory
                }
            }
        }

        // Helpful debug log so you can easily verify it read everything!
        Debug.Log($"[ConsoleDatabase] Successfully loaded {textDatabase.Count} voice lines.");
    }

    public void TriggerMessage(string messageID)
    {
        // 1. Clean the incoming ID just in case the Inspector has an accidental space!
        string cleanID = messageID.Trim();

        if (textDatabase.TryGetValue(cleanID, out ConsoleMessageData data))
        {
            RetroConsole.Instance.Log(data.messageText, data.logType, data.portraitID);
        }
        else
        {
            // 2. Put the dynamic variable back into the error log!
            RetroConsole.Instance.LogError($"[DB ERROR] Voice line ID not found: '{cleanID}'");
        }
    }
}