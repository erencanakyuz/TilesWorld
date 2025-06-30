using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// HitZoneTrigger
///  - Attached to an invisible BoxCollider (isTrigger) representing the hit-line area of a lane.
///  - Caches all note objects currently inside the trigger so that HitZoneManager can access
///    them in O(1) time without raycasts or per-frame searches.
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
    /// Newest note is appended to the end; therefore the first element is always
    /// the earliest note (closest to player in time).
    /// </summary>
    public readonly List<GameObject> insideNotes = new List<GameObject>();

    void Awake()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        // Make collider thicker along Z for better note detection (orijinal Java'ya uygun)
        col.size = new Vector3(col.size.x, col.size.y, 2.3f); // 2.3f = total hit window size from Java

        Debug.Log($"[HitZoneTrigger] Lane {laneIndex} initialized: Collider size={col.size}, isTrigger={col.isTrigger}");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Note")) return;
        if (!insideNotes.Contains(other.gameObject))
        {
            insideNotes.Add(other.gameObject);

            if (showDebug && debugCount < maxDebugLogs)
            {
                Debug.Log($"[HitZoneTrigger] Lane {laneIndex} ENTER note '{other.gameObject.name}' at Z={other.transform.position.z:F2}. insideNotes={insideNotes.Count}");
                debugCount++;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Note")) return;
        insideNotes.Remove(other.gameObject);
        if (showDebug && debugCount < maxDebugLogs)
        {
            Debug.Log($"[HitZoneTrigger] Lane {laneIndex} EXIT note '{other.gameObject.name}' at Z={other.transform.position.z:F2}. insideNotes={insideNotes.Count}");
            debugCount++;
        }
    }

    /// <summary>
    /// Returns the first note that entered the zone (FIFO). Returns null if none.
    /// </summary>
    public GameObject PeekEarliestNote()
    {
        return insideNotes.Count > 0 ? insideNotes[0] : null;
    }

    /// <summary>
    /// Removes the specified note if present (to be called by HitZoneManager after successful hit).
    /// </summary>
    public void RemoveNote(GameObject noteObj)
    {
        insideNotes.Remove(noteObj);
    }
}