using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class BindingImporter : EditorWindow
{
    private string _sheetUrl = "https://docs.google.com/spreadsheets/d/1fxdsfb5c-fsNA_464f_SQK5uAzztmi_CekzrW8lMcHQ/edit?gid=620223261#gid=620223261";

    private string _outputPath = "Assets/Resources/BindingGraph.json";

    [MenuItem("Tools/Rope Dart/Binding Importer")]
    public static void ShowWindow()
    {
        GetWindow<BindingImporter>("Binding Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Rope Dart Binding Importer (Google Sheets)", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        _sheetUrl = EditorGUILayout.TextField("Google Sheet URL", _sheetUrl);
        _outputPath = EditorGUILayout.TextField("Output JSON Path", _outputPath);

        EditorGUILayout.Space();

        if (GUILayout.Button("Fetch & Convert to JSON"))
        {
            SyncFromGoogleSheets();
        }
    }

    private async void SyncFromGoogleSheets()
    {
        try
        {
            string sheetId = "";
            string gid = "0";

            Match idMatch = Regex.Match(_sheetUrl, @"/d/([a-zA-Z0-9-_]+)");
            if (idMatch.Success) sheetId = idMatch.Groups[1].Value;

            Match gidMatch = Regex.Match(_sheetUrl, @"[#&]gid=([0-9]+)");
            if (gidMatch.Success) gid = gidMatch.Groups[1].Value;

            if (string.IsNullOrEmpty(sheetId))
            {
                EditorUtility.DisplayDialog("Error", "Could not find a valid Spreadsheet ID in the URL.", "OK");
                return;
            }

            string exportUrl = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={gid}";

            EditorUtility.DisplayProgressBar("Downloading", "Fetching data from Google Sheets...", 0.5f);

            using (HttpClient client = new HttpClient())
            {
                string csvText = await client.GetStringAsync(exportUrl);
                EditorUtility.ClearProgressBar();
                ConvertCSVData(csvText);
            }
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Download Error", $"Failed to fetch data from Google Sheets.\n\nMake sure the sheet is set to 'Anyone with the link can view'.\n\nError: {ex.Message}", "OK");
            Debug.LogError(ex);
        }
    }

    private void ConvertCSVData(string csvText)
    {
        try
        {
            string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<BindingGraphNode> nodes = new List<BindingGraphNode>();
            BindingGraphNode currentNode = null;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] columns = SplitCsvLine(line);
                if (columns.Length < 12) continue;

                string idString = columns[0].Trim();
                string nodeName = columns[1].Trim();

                if (!string.IsNullOrEmpty(idString) && int.TryParse(idString, out _))
                {
                    currentNode = new BindingGraphNode
                    {
                        NodeId = nodeName,
                        DoesDecay = ParseBool(columns[2]),
                        Connections = new List<BindingGraphConnection>()
                    };

                    AddConnectionIfValid(currentNode, columns);
                    nodes.Add(currentNode);
                }
                else if (nodeName == ".")
                {
                    if (currentNode != null)
                    {
                        AddConnectionIfValid(currentNode, columns);
                    }
                }
            }

            GraphWrapper wrapper = new GraphWrapper { Nodes = nodes };
            string jsonOutput = JsonUtility.ToJson(wrapper, true);

            File.WriteAllText(_outputPath, jsonOutput);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", $"Downloaded and converted successfully!\nSaved at: {_outputPath}", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Parse Error", $"Failed to parse CSV data: {ex.Message}", "OK");
            Debug.LogError(ex);
        }
    }

    private void AddConnectionIfValid(BindingGraphNode node, string[] columns)
    {
        BindingGraphConnection currentConnection = new BindingGraphConnection
        {
            Nickname = columns[3].Trim(),
            Input = columns[4].Trim(),
            UnitCost = int.TryParse(columns[5], out int totalCost) ? totalCost : 0,
            IsLeadSideValid = ParseBool(columns[6]),
            IsAnchorSideValid = ParseBool(columns[7]),
            IsDownSpinValid = ParseBool(columns[8]),
            IsUpSpinValid = ParseBool(columns[9]),
            IsWallPlaneValid = ParseBool(columns[10]),
            IsDarkPlaneValid = ParseBool(columns[11]),
            IsCoilingNeeded = ParseBool(columns[12]),
            IsStalledNeeded = ParseBool(columns[13]),
            FlipsLeadAnchor = ParseBool(columns[14]),
            FlipsDownUp = ParseBool(columns[15]),
            FlipsWallDark = ParseBool(columns[16]),
            SetsCoiling = ParseBool(columns[17]),
            NodeSequence = new List<BindingStackElement>(),
            Animation = columns.Length > 22 ? columns[22].Trim() : ""
        };

        currentConnection.NodeSequence = new List<BindingStackElement>();
        for (int i = 18; i <= 21; i += 2)
        {
            if (columns.Length > i + 1 && !string.IsNullOrEmpty(columns[i].Trim()))
            {
                string nodeId = columns[i].Trim();
                int unitCost = int.TryParse(columns[i + 1].Trim(), out int nodeCost) ? nodeCost : 0;

                currentConnection.NodeSequence.Add(new BindingStackElement(nodeId, unitCost));
            }
        }

        node.Connections.Add(currentConnection);
    }

    private bool ParseBool(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return value.Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase);
    }

    private string[] SplitCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentToken = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentToken);
                currentToken = "";
            }
            else
            {
                currentToken += c;
            }
        }
        result.Add(currentToken);
        return result.ToArray();
    }

    [Serializable]
    private class GraphWrapper
    {
        public List<BindingGraphNode> Nodes;
    }
}
