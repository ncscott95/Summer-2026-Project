using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class LevelImporter : EditorWindow
{
    private const string TempSheetUrl = "https://docs.google.com/spreadsheets/d/1OwenMD1B8AkDwHoDRsbulK0GfOEeZNMCWk1HSKSYCRk/edit?gid=799660637#gid=799660637";
    private const string WoodSheetUrl = "https://docs.google.com/spreadsheets/d/1OwenMD1B8AkDwHoDRsbulK0GfOEeZNMCWk1HSKSYCRk/edit?gid=1622219906#gid=1622219906";
    private const string FireSheetUrl = "https://docs.google.com/spreadsheets/d/1OwenMD1B8AkDwHoDRsbulK0GfOEeZNMCWk1HSKSYCRk/edit?gid=1880908761#gid=1880908761";
    private const string MetalSheetUrl = "https://docs.google.com/spreadsheets/d/1OwenMD1B8AkDwHoDRsbulK0GfOEeZNMCWk1HSKSYCRk/edit?gid=1610882434#gid=1610882434";
    private const string WaterSheetUrl = "https://docs.google.com/spreadsheets/d/1OwenMD1B8AkDwHoDRsbulK0GfOEeZNMCWk1HSKSYCRk/edit?gid=578282844#gid=578282844";
    private const string EarthSheetUrl = "https://docs.google.com/spreadsheets/d/1OwenMD1B8AkDwHoDRsbulK0GfOEeZNMCWk1HSKSYCRk/edit?gid=1810814273#gid=1810814273";

    private LevelRegion _selectedLevelType;

    [MenuItem("Tools/Rope Dart/Level Importer")]
    public static void ShowWindow()
    {
        GetWindow<LevelImporter>("Level Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Level Data Importer (Google Sheets)", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        _selectedLevelType = (LevelRegion)EditorGUILayout.EnumPopup("Select Level Type", LevelRegion.Temp);

        EditorGUILayout.Space();

        if (GUILayout.Button("Fetch & Convert to JSON"))
        {
            SyncFromGoogleSheets();
        }
    }

    private async void SyncFromGoogleSheets()
    {
        string sheetUrl = _selectedLevelType switch
        {
            LevelRegion.Temp => TempSheetUrl,
            LevelRegion.Wood => WoodSheetUrl,
            LevelRegion.Fire => FireSheetUrl,
            LevelRegion.Metal => MetalSheetUrl,
            LevelRegion.Water => WaterSheetUrl,
            LevelRegion.Earth => EarthSheetUrl,
            _ => throw new ArgumentOutOfRangeException()
        };

        try
        {
            string sheetId = "";
            string gid = "0";

            Match idMatch = Regex.Match(sheetUrl, @"/d/([a-zA-Z0-9-_]+)");
            if (idMatch.Success) sheetId = idMatch.Groups[1].Value;

            Match gidMatch = Regex.Match(sheetUrl, @"[#&]gid=([0-9]+)");
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
            List<LevelData> levels = new List<LevelData>();
            LevelData currentLevel = null;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] columns = SplitCsvLine(line);

                string idString = columns[0].Trim();
                string levelName = columns[1].Trim();
                // ignore column 2, "purpose" column is only for developer reference
                // ignore column 3, "difficulty" column is only for developer reference

                if (!string.IsNullOrEmpty(idString) && idString != ".")
                {
                    currentLevel = new LevelData
                    {
                        LevelId = idString,
                        LevelName = levelName,
                        LevelTargets = new List<LevelTargetItem>()
                    };

                    AddTargetIfValid(currentLevel, columns);
                    if (currentLevel.LevelTargets.Count > 0)
                    {
                        levels.Add(currentLevel);
                    }
                }
                else if (idString == ".")
                {
                    if (currentLevel != null)
                    {
                        AddTargetIfValid(currentLevel, columns);
                    }
                }
            }

            ListWrapper wrapper = new ListWrapper() { Levels = levels };
            string jsonOutput = JsonUtility.ToJson(wrapper, true);
            string outputPath = $"Assets/Resources/{_selectedLevelType}.json";

            File.WriteAllText(outputPath, jsonOutput);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", $"Downloaded and converted successfully!\nSaved at: {outputPath}", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Parse Error", $"Failed to parse CSV data: {ex.Message}", "OK");
            Debug.LogError(ex);
        }
    }

    private void AddTargetIfValid(LevelData level, string[] columns)
    {
        if (!TryParseLevelTargetType(columns[4], out LevelTargetType targetType)) { Debug.LogError($"Failed to parse target type from column 5: {columns[4]}"); return; }
        if (!TryParseLevelTargetSpawnType(columns[5], out LevelTargetSpawnType spawnType)) { Debug.LogError($"Failed to parse spawn type from column 6: {columns[5]}"); return; }
        if (!TryParseSpawnPosition(columns[6], out int spawnPos)) { Debug.LogError($"Failed to parse spawn position from column 7: {columns[6]}"); return; }
        if (!float.TryParse(columns[7], out float pointVal)) { Debug.LogError($"Failed to parse point value from column 8: {columns[7]}"); return; }
        if (!float.TryParse(columns[8], out float modVal)) { Debug.LogError($"Failed to parse mod value from column 9: {columns[8]}"); return; }

        LevelTargetItem currentTarget = new LevelTargetItem
        {
            Id = level.LevelTargets.Count + 1,
            TargetType = targetType,
            SpawnType = spawnType,
            SpawnPosition = spawnPos,
            PointValue = pointVal,
            ModValue = modVal
        };

        level.LevelTargets.Add(currentTarget);
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

    private bool TryParseLevelTargetType(string value, out LevelTargetType targetType)
    {
        switch (value.ToLower())
        {
            case "generic":
                targetType = LevelTargetType.Generic;
                return true;
            case "timer":
                targetType = LevelTargetType.Timer;
                return true;
            case "unknown":
                targetType = LevelTargetType.unknown;
                return true;
            case "points":
                targetType = LevelTargetType.Points;
                return true;
            default:
                targetType = LevelTargetType.Generic;
                return false;
        }
    }

    private bool TryParseLevelTargetSpawnType(string value, out LevelTargetSpawnType spawnType)
    {
        switch (value.ToLower())
        {
            case "with":
                spawnType = LevelTargetSpawnType.WithPrevious;
                return true;
            case "hit":
                spawnType = LevelTargetSpawnType.OnPreviousHit;
                return true;
            case "allhit":
                spawnType = LevelTargetSpawnType.OnAllPreviousHit;
                return true;
            default:
                spawnType = LevelTargetSpawnType.OnPreviousHit;
                return false;
        }
    }

    private bool TryParseSpawnPosition(string value, out int spawnPos)
    {
        switch (value.ToLower())
        {
            case "west":
                spawnPos = -1;
                return true;
            case "center":
                spawnPos = 0;
                return true;
            case "east":
                spawnPos = 1;
                return true;
            default:
                spawnPos = 0;
                return false;
        }
    }

    [Serializable]
    private class ListWrapper
    {
        public List<LevelData> Levels;
    }
}
