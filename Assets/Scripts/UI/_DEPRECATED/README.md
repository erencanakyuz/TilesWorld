# Deprecated UI Files

These files have been deprecated as of 2026-02-02.

## Reason for Deprecation

### SongSelectionManager.cs → ModernSongSelectionUI.cs
- **Old System**: Dropdown-based song selection (421 lines)
- **New System**: Card-based scrollable list (307 lines)
- **Reason**: The new ModernSongSelectionUI provides a better UX with:
  - Scrollable card list
  - Visual song cards with difficulty indicators
  - Better mobile touch support
  - Theme integration

### SongSelectionPanel.prefab → ModernSongSelectionPanel.prefab
- **Old**: 47KB prefab with TMP_Dropdown
- **New**: 31KB prefab with ScrollRect + SongCards
- **UIConfig Reference**: Already points to ModernSongSelectionPanel.prefab

## Migration Notes

### Data Classes
The nested classes `SongSelectionManager.SongData` and `SongSelectionManager.GameplaySongData` have been extracted to:
- `Assets/Scripts/Core/SongData.cs` containing:
  - `SongSelectionData` (renamed from SongData to avoid conflict with ScriptableObject)
  - `GameplaySongData`

### If You Need to Restore
1. Move files back from `_DEPRECATED` folder
2. Remove `.deprecated` suffix from filenames
3. Update UIConfig.songSelectionPanelPrefab reference

---
**Deprecated Date**: 2026-02-02
