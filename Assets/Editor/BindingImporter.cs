using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class BindingImporter : EditorWindow
{
    // old Graph tab
    // private string sheetUrl = "https://docs.google.com/spreadsheets/d/1fxdsfb5c-fsNA_464f_SQK5uAzztmi_CekzrW8lMcHQ/edit?gid=1907152520#gid=1907152520";
    
    // new Limited Graph tab
    private string _sheetUrl = "https://docs.google.com/spreadsheets/d/1fxdsfb5c-fsNA_464f_SQK5uAzztmi_CekzrW8lMcHQ/edit?gid=1907152520#gid=1907152520";

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
            List<BindingGraphData.BindingGraphNode> nodes = new List<BindingGraphData.BindingGraphNode>();
            BindingGraphData.BindingGraphNode currentNode = null;

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
                    currentNode = new BindingGraphData.BindingGraphNode();

                    currentNode.NodeId = nodeName;
                    currentNode.IsStable = ParseBool(columns[2]);
                    currentNode.DoesDecay = ParseBool(columns[3]);
                    currentNode.CanCast = ParseBool(columns[4]);
                    currentNode.CanTurn = ParseBool(columns[5]);

                    currentNode.BindPoints = new List<string>();
                    if (!string.IsNullOrEmpty(columns.Length > 6 ? columns[6].Trim() : ""))
                    {
                        currentNode.BindPoints.Add(columns.Length > 6 ? columns[6].Trim() : "");
                    }
                    if (!string.IsNullOrEmpty(columns.Length > 7 ? columns[7].Trim() : ""))
                    {
                        currentNode.BindPoints.Add(columns.Length > 7 ? columns[7].Trim() : "");
                    }
                    if (!string.IsNullOrEmpty(columns.Length > 8 ? columns[8].Trim() : ""))
                    {
                        currentNode.BindPoints.Add(columns.Length > 8 ? columns[8].Trim() : "");
                    }

                    currentNode.Connections = new List<BindingGraphData.BindingGraphConnection>();

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

            GraphWrapper wrapper = new GraphWrapper { nodes = nodes };
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

    private void AddConnectionIfValid(BindingGraphData.BindingGraphNode node, string[] columns)
    {
        if (columns.Length > 9)
        {
            string targetNode = columns[9].Trim();
            if (!string.IsNullOrEmpty(targetNode))
            {
                string binding = columns.Length > 10 ? columns[10].Trim() : "";
                int cost = 1;
                if (columns.Length > 11 && int.TryParse(columns[11], out int parsedCost))
                {
                    cost = parsedCost;
                }
                string animation = columns.Length > 12 ? columns[12].Trim() : "";

                node.Connections.Add(new BindingGraphData.BindingGraphConnection
                {
                    NodeId = targetNode,
                    Input = binding,
                    UnitCost = cost,
                    Animation = animation
                });
            }
        }
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
        public List<BindingGraphData.BindingGraphNode> nodes;
    }
}
