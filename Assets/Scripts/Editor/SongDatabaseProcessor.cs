using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

/// <summary>
/// SongDatabaseProcessor - MUSIC.csv dosyasını işleyip merkezi bir JSON veritabanı oluşturur.
/// Bu araç, "Tools > TilesWorld > Process Song Database" menüsünden çalıştırılır.
/// </summary>
public class SongDatabaseProcessor : EditorWindow
{
    private const string CsvPath = "Assets/Resources/Database csv/MUSIC.csv";
    private const string OutputPath = "Assets/Resources/songs_database.json";

    [MenuItem("Tools/TilesWorld/Process Song Database")]
    public static void ShowWindow()
    {
        GetWindow<SongDatabaseProcessor>("Song Database Processor");
    }

    void OnGUI()
    {
        GUILayout.Label("Song Database Processor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox($"Bu araç, '{CsvPath}' dosyasını okuyacak ve '{OutputPath}' dosyasını oluşturacak/güncelleyecektir.", MessageType.Info);

        if (GUILayout.Button("Process CSV to JSON", GUILayout.Height(40)))
        {
            ProcessDatabase();
        }
    }

    private void ProcessDatabase()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogError($"Veritabanı dosyası bulunamadı: {CsvPath}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(CsvPath);
            var songList = new List<SongDatabaseInfo>();

            // Başlık satırını atla (i=1)
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] values = ParseCsvLine(line);
                if (values.Length < 5)
                {
                    Debug.LogWarning($"Satır {i + 1} atlanıyor: Yetersiz sütun sayısı.");
                    continue;
                }

                int musicId = int.Parse(values[0]);
                string title = values[2];
                string artist = values[3];
                int tempo = int.Parse(values[4]);

                DifficultyLevel difficulty = GetDifficultyFromTitle(title);
                string cleanTitle = title.Replace("★", "").Replace("☆", "").Trim();

                songList.Add(new SongDatabaseInfo
                {
                    musicId = musicId,
                    title = cleanTitle,
                    artist = artist,
                    tempo = tempo,
                    difficulty = difficulty,
                    songKey = $"{cleanTitle.ToLower().Replace(" ", "_")}_{artist.ToLower().Replace(" ", "_")}"
                });
            }

            var wrapper = new SongDatabaseListWrapper { Songs = songList };
            string json = JsonUtility.ToJson(wrapper, true);

            File.WriteAllText(OutputPath, json);
            AssetDatabase.Refresh();

            Debug.Log($"✅ BAŞARILI! {songList.Count} şarkı işlendi ve '{OutputPath}' dosyasına kaydedildi.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Veritabanı işlenirken bir hata oluştu: {e.Message}\n{e.StackTrace}");
        }
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

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
        result.Add(currentField.ToString());
        return result.Select(s => s.Trim('"')).ToArray();
    }

    private DifficultyLevel GetDifficultyFromTitle(string title)
    {
        int starCount = title.Count(c => c == '★');
        return starCount switch
        {
            0 => DifficultyLevel.Easy,
            1 => DifficultyLevel.Easy,
            2 => DifficultyLevel.Medium,
            3 => DifficultyLevel.Hard,
            4 => DifficultyLevel.Expert,
            _ => DifficultyLevel.Master,
        };
    }
}

// Bu yapılar artık DataStructures.cs dosyasında tanımlanıyor. 