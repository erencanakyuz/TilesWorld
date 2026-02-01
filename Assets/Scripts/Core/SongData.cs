using UnityEngine;

/// <summary>
/// Song data structures for song selection UI.
/// NOTE: This is different from the ScriptableObject SongData in GameManager.cs
/// This class is used by the song selection UI to display song information.
/// </summary>

[System.Serializable]
public class SongSelectionData
{
    public int musicId;
    public string title;
    public string artist;
    public string duration;
    public DifficultyLevel difficulty;
    public int bpm;
    public string songKey; // For loading the actual song
}

// GameplaySongData is used to pass song info from UI to GameplayManager
// This bridges the gap between song selection and gameplay systems
[System.Serializable]
public class GameplaySongData
{
    public string songName;
    public string artist;
    public float duration;
    public int bpm;
    public string audioFilePath;
    public string chartFilePath;
    public string songKey; // CRITICAL: For JSON loading!
}
