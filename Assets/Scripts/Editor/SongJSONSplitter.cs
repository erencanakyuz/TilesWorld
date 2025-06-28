using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🎵 Song JSON Splitter - Creates individual JSON files for each song
/// Splits all_songs_notes.json into separate files based on music_id
/// Uses CSV database for proper naming
/// </summary>
public class SongJSONSplitter : EditorWindow
{
    [MenuItem("Tools/TilesWorld/Split Song JSONs")]
    public static void ShowWindow()
    {
        GetWindow<SongJSONSplitter>("Song JSON Splitter");
    }

    private string musicColumnsPath = "Assets/Resources/Database csv/MUSIC_COLUMNS.csv";
    private string musicDatabasePath = "Assets/Resources/Database csv/MUSIC.csv";
    private string outputFolder = "Assets/Resources/Song_Note_Jsons/Individual/";

    void OnGUI()
    {
        GUILayout.Label("🎵 Song JSON Splitter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        musicColumnsPath = EditorGUILayout.TextField("MUSIC_COLUMNS CSV:", musicColumnsPath);
        musicDatabasePath = EditorGUILayout.TextField("MUSIC Database:", musicDatabasePath);
        outputFolder = EditorGUILayout.TextField("Output Folder:", outputFolder);

        EditorGUILayout.Space();

        if (GUILayout.Button("🎯 Split JSONs by Song", GUILayout.Height(40)))
        {
            SplitSongJSONs();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will create individual JSON files for each song based on music_id from MUSIC_COLUMNS.csv.", MessageType.Info);
    }

    void SplitSongJSONs()
    {
        try
        {
            Debug.Log("🎵 Starting Song JSON splitting process from MUSIC_COLUMNS.csv...");

            // 1. Load song database for names
            var songDatabase = LoadSongDatabase();
            Debug.Log($"📊 Loaded {songDatabase.Count} songs from MUSIC database");

            // 2. Load and parse MUSIC_COLUMNS.csv
            var allSequences = LoadMusicColumnsData();
            Debug.Log($"🎼 Loaded {allSequences.Count} total sequences from MUSIC_COLUMNS.csv");

            // 3. Create output directory
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                AssetDatabase.Refresh();
            }

            // 4. Group sequences by music_id and create individual files
            var groupedSequences = allSequences.GroupBy(s => s.music_id).ToList();
            Debug.Log($"🎯 Found {groupedSequences.Count} unique music_ids");

            int createdFiles = 0;
            foreach (var group in groupedSequences)
            {
                int musicId = group.Key;
                var sequences = group.OrderBy(s => s.seq).ToArray();

                // Get song info from database
                if (songDatabase.TryGetValue(musicId, out SongInfo songInfo))
                {
                    string fileName = CreateFileName(songInfo);
                    string filePath = Path.Combine(outputFolder, fileName + ".json");

                    // Create JSON content (array format for compatibility)
                    string jsonContent = JsonUtility.ToJson(new JsonSequenceArray { sequences = sequences }, true);

                    // Write file
                    File.WriteAllText(filePath, jsonContent);
                    createdFiles++;

                    Debug.Log($"✅ Created: {fileName}.json ({sequences.Length} sequences)");
                }
                else
                {
                    Debug.LogWarning($"⚠️ No database entry found for music_id {musicId}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"🎉 Successfully created {createdFiles} individual song JSON files!");

            // 5. Update GameNoteCreator mapping
            UpdateGameNoteCreatorMapping(songDatabase);

        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error splitting JSONs: {e.Message}");
        }
    }

    Dictionary<int, SongInfo> LoadSongDatabase()
    {
        var database = new Dictionary<int, SongInfo>();

        if (!File.Exists(musicDatabasePath))
        {
            Debug.LogError($"❌ MUSIC database not found: {musicDatabasePath}");
            return database;
        }

        string[] lines = File.ReadAllLines(musicDatabasePath);

        // Skip header line
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line)) continue;

            // Parse CSV properly with quoted fields
            var parts = ParseCSVLine(line);
            if (parts.Length >= 5)
            {
                // CSV Format: "music_id","net_music_seq","title","artist","tempo",...
                string musicIdStr = parts[0].Trim('"');
                string title = parts[2].Trim('"').Replace("★", "").Replace("☆", "").Trim();
                string artist = parts[3].Trim('"');
                string tempoStr = parts[4].Trim('"');

                if (int.TryParse(musicIdStr, out int musicId))
                {
                    var songInfo = new SongInfo
                    {
                        musicId = musicId,
                        songName = title,
                        artist = artist
                    };
                    database[musicId] = songInfo;
                    Debug.Log($"📀 Loaded: {title} by {artist} (ID:{musicId})");
                }
            }
        }

        return database;
    }

    /// <summary>
    /// Parse CSV line with quoted fields properly
    /// Handles: "value1","value2","value3"
    /// </summary>
    string[] ParseCSVLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var currentField = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        // Add the last field
        result.Add(currentField.ToString());

        return result.ToArray();
    }

    List<JsonSongSequence> LoadMusicColumnsData()
    {
        var sequences = new List<JsonSongSequence>();

        if (!File.Exists(musicColumnsPath))
        {
            Debug.LogError($"❌ MUSIC_COLUMNS not found: {musicColumnsPath}");
            return sequences;
        }

        string[] lines = File.ReadAllLines(musicColumnsPath);

        // Skip header line
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line)) continue;

            // Parse CSV with quoted fields: "music_id","seq","line1","line2",...
            var parts = ParseCSVLine(line);
            if (parts.Length >= 8) // music_id, seq, line1-line6
            {
                string musicIdStr = parts[0].Trim('"');
                string seqStr = parts[1].Trim('"');

                if (int.TryParse(musicIdStr, out int musicId) && int.TryParse(seqStr, out int seq))
                {
                    var sequence = new JsonSongSequence
                    {
                        music_id = musicId,
                        seq = seq,
                        line1 = parts[2].Trim('"'),
                        line2 = parts[3].Trim('"'),
                        line3 = parts[4].Trim('"'),
                        line4 = parts[5].Trim('"'),
                        line5 = parts[6].Trim('"'),
                        line6 = parts[7].Trim('"')
                    };

                    sequences.Add(sequence);
                }
            }
        }

        return sequences;
    }

    string CreateFileName(SongInfo songInfo)
    {
        // Create clean file name: song_composer format
        string songName = CleanFileName(songInfo.songName);
        string artist = CleanFileName(songInfo.artist);

        return $"{songName}_{artist}".ToLower();
    }

    string CleanFileName(string input)
    {
        // Remove invalid characters and replace spaces with underscores
        string cleaned = input.Replace(" ", "_")
                             .Replace(".", "")
                             .Replace(",", "")
                             .Replace("'", "")
                             .Replace("\"", "")
                             .Replace("(", "")
                             .Replace(")", "");

        // Remove any remaining invalid characters
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            cleaned = cleaned.Replace(c.ToString(), "");
        }

        return cleaned;
    }

    void UpdateGameNoteCreatorMapping(Dictionary<int, SongInfo> songDatabase)
    {
        Debug.Log("🔧 Updating GameNoteCreator mapping...");

        // Generate the mapping code
        var mappingCode = new System.Text.StringBuilder();
        mappingCode.AppendLine("        // 🎵 AUTO-GENERATED SONG MAPPING");
        mappingCode.AppendLine("        switch (song.songName.ToLower())");
        mappingCode.AppendLine("        {");

        foreach (var kvp in songDatabase)
        {
            var songInfo = kvp.Value;
            string fileName = CreateFileName(songInfo);

            mappingCode.AppendLine($"            case \"{songInfo.songName.ToLower()}\":");
            mappingCode.AppendLine($"                return \"Song_Note_Jsons/Individual/{fileName}\";");
        }

        mappingCode.AppendLine("            default:");
        mappingCode.AppendLine("                Debug.LogWarning($\"⚠️ Unknown song: {song.songName}, using demo\");");
        mappingCode.AppendLine("                return \"Song_Note_Jsons/Individual/cannon_pachelbel\"; // Fallback");
        mappingCode.AppendLine("        }");

        Debug.Log("📝 Generated mapping code:\n" + mappingCode.ToString());
    }

    [System.Serializable]
    public class SongInfo
    {
        public int musicId;
        public string songName;
        public string artist;
    }

    [System.Serializable]
    public class JsonSequenceArray
    {
        public JsonSongSequence[] sequences;
    }

    [System.Serializable]
    public class JsonSongSequence
    {
        public int music_id;
        public int seq;
        public string line1;
        public string line2;
        public string line3;
        public string line4;
        public string line5;
        public string line6;
    }
}