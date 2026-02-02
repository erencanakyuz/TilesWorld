using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// HitZoneTrigger
///  - Attached to an invisible BoxCollider (isTrigger) representing the hit-line area of a lane.
///  - Caches all note objects currently inside the trigger so that HitZoneManager can access
///    them in O(1) time without raycasts or per-frame searches.
/// PERFORMANCE: Uses HashSet internally for O(1) Contains checks during heavy note density.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class HitZoneTrigger : MonoBehaviour
{
    [Tooltip("Lane index this trigger belongs to (0-based). Must match InputManager & NoteRenderer lane indexing")]
    public int laneIndex;

    [SerializeField] private bool showDebug = false;
    private int debugCount = 0;
    private const int maxDebugLogs = 10;

    /// <summary>
    /// The collection of active note gameObjects inside this hit zone.
    /// Uses HashSet internally for O(1) Contains check during heavy note density.
    /// </summary>
    private readonly HashSet<GameObject> insideNotesSet = new HashSet<GameObject>();
    
    /// <summary>
    /// Direct HashSet access for iteration - avoids List allocation.
    /// PERFORMANCE: Use this directly when iterating instead of insideNotes property.
    /// </summary>
    public IEnumerable<GameObject> GetNotesEnumerable() => insideNotesSet;
    
    /// <summary>
    /// List accessor for iteration compatibility. Only use when you need to iterate.
    /// PERFORMANCE NOTE: This regenerates the list on each access when dirty - cache locally if iterating multiple times.
    /// </summary>
    private bool insideNotesListDirty = true;
    public List<GameObject> insideNotes 
    { 
        get 
        { 
            if (insideNotesListDirty)
            {
                insideNotesList.Clear();
                insideNotesList.AddRange(insideNotesSet);
                insideNotesListDirty = false;
            }
            return insideNotesList;
        }
    }
    private readonly List<GameObject> insideNotesList = new List<GameObject>(32);

    [SerializeField] private int cleanupFrameInterval = 5;

    void Awake()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (cleanupFrameInterval < 1) cleanupFrameInterval = 1;
        if (Time.frameCount % cleanupFrameInterval != 0) return;

        // Clean up destroyed or inactive (pooled) notes from the set
        int before = insideNotesSet.Count;
        insideNotesSet.RemoveWhere(note => note == null || !note.activeInHierarchy);
        if (insideNotesSet.Count != before) insideNotesListDirty = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Note")) return;
        
        // O(1) Contains check with HashSet
        if (insideNotesSet.Add(other.gameObject))
        {
            insideNotesListDirty = true;
            if (showDebug && debugCount < maxDebugLogs)
            {
                // Debug.Log($"[HitZoneTrigger] Lane {laneIndex} ENTER note '{other.gameObject.name}' at Z={other.transform.position.z:F2}. insideNotes={insideNotesSet.Count}");
                debugCount++;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Note")) return;
        if (insideNotesSet.Remove(other.gameObject))
        {
            insideNotesListDirty = true;
        }
        if (showDebug && debugCount < maxDebugLogs)
        {
            // Debug.Log($"[HitZoneTrigger] Lane {laneIndex} EXIT note '{other.gameObject.name}' at Z={other.transform.position.z:F2}. insideNotes={insideNotesSet.Count}");
            debugCount++;
        }
    }

    /// <summary>
    /// Returns the first note that entered the zone (FIFO). Returns null if none.
    /// NOTE: HashSet doesn't maintain order, so this returns any note.
    /// </summary>
    public GameObject PeekEarliestNote()
    {
        foreach (var note in insideNotesSet)
        {
            return note;
        }
        return null;
    }

    /// <summary>
    /// Removes the specified note if present (to be called by HitZoneManager after successful hit).
    /// </summary>
    public void RemoveNote(GameObject noteObject)
    {
        if (insideNotesSet.Remove(noteObject))
        {
            insideNotesListDirty = true;
        }
    }
    
    /// <summary>
    /// Gets the count of notes currently inside this hit zone.
    /// PERFORMANCE: Use this instead of insideNotes.Count when you only need the count.
    /// </summary>
    public int GetNoteCount()
    {
        return insideNotesSet.Count;
    }
}
