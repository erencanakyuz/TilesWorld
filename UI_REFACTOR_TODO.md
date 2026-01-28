# UI Refactor TODO

- [x] Create folder structure under `Assets/Scripts/UI` and `Assets/Resources/UI`
- [x] Add `UIConfig.cs` ScriptableObject definition
- [ ] Create `UIConfig.asset` in Unity Editor (Resources/UI/UIConfig)
- [x] Add `CanvasLocator.cs`
- [x] Add `HUDController.cs`
- [x] Add `PanelManager.cs`
- [x] Add `PanelButtonWirer.cs`
- [x] Add `UIEffectPool.cs`
- [x] Add `CountdownController.cs`
- [x] Add `MobileFinder.cs`
- [x] Refactor `UIManager.cs` to facade
- [x] Prevent double UIManager creation in Bootstrap.cs
- [ ] Verify wiring in Bootstrap scene (ensure only one UIManager instance exists)
- [ ] Play mode sanity check (HUD visible in Playing, panels switch, effects render)
