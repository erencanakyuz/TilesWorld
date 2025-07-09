using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace GameUI
{
    /// <summary>
    /// Main UI Controller for converted CSS-to-Unity UI
    /// This demonstrates how to use the converted USS files
    /// </summary>
    public class UIControllerTemplate : MonoBehaviour
    {
        [Header("UI Documents")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Style Sheets (Converted from CSS)")]
        [SerializeField] private StyleSheet baseStyles;      // base.uss (converted from base.css)
        [SerializeField] private StyleSheet layoutStyles;   // layout.uss (converted from layout.css)
        [SerializeField] private StyleSheet animationStyles; // animations.uss (converted from animations.css)

        private VisualElement root;

        // Component references
        private Button testAllButton;
        private Button resetButton;
        private VisualElement mainMenu;
        private VisualElement worldTour;
        private VisualElement shopPanel;

        void Start()
        {
            InitializeUI();
            SetupEventHandlers();
        }

        void InitializeUI()
        {
            // Get root visual element
            root = uiDocument.rootVisualElement;

            // Add converted style sheets
            root.styleSheets.Add(baseStyles);
            root.styleSheets.Add(layoutStyles);
            root.styleSheets.Add(animationStyles);

            // Find UI elements (converted from your HTML structure)
            testAllButton = root.Q<Button>("test-all-btn");
            resetButton = root.Q<Button>("reset-btn");
            mainMenu = root.Q<VisualElement>("main-menu");
            worldTour = root.Q<VisualElement>("world-tour");
            shopPanel = root.Q<VisualElement>("shop");

            Debug.Log("✅ Unity UI initialized with converted CSS styles!");
        }

        void SetupEventHandlers()
        {
            // Convert your JavaScript event handlers to C#

            // Original JS: onclick="UITester.testAll()"
            testAllButton?.RegisterCallback<ClickEvent>(OnTestAllClicked);

            // Original JS: onclick="UITester.resetTests()"
            resetButton?.RegisterCallback<ClickEvent>(OnResetClicked);

            // Example: Component navigation (from your bottom navigation)
            SetupComponentNavigation();
        }

        void OnTestAllClicked(ClickEvent evt)
        {
            Debug.Log("🚀 Testing all components...");
            StartCoroutine(TestAllComponents());
        }

        void OnResetClicked(ClickEvent evt)
        {
            Debug.Log("🔄 Resetting tests...");
            ResetAllComponents();
        }

        IEnumerator TestAllComponents()
        {
            // Convert your JavaScript component testing logic
            string[] components = { "world-tour", "main-menu", "hud", "level-complete", "shop", "settings", "powerups", "leaderboard" };

            foreach (string componentName in components)
            {
                Debug.Log($"🔍 Previewing component: {componentName}");
                ShowComponent(componentName);
                yield return new WaitForSeconds(0.5f); // Animation time
            }
        }

        void ShowComponent(string componentName)
        {
            // Hide all components
            HideAllComponents();

            // Show target component with CSS class from your original design
            VisualElement component = root.Q<VisualElement>(componentName);
            if (component != null)
            {
                component.RemoveFromClassList("hidden");
                component.AddToClassList("active");

                // Apply converted animations (from animations.uss)
                AnimateComponentIn(component);
            }
        }

        void HideAllComponents()
        {
            string[] components = { "world-tour", "main-menu", "hud", "level-complete", "shop", "settings", "powerups", "leaderboard" };

            foreach (string componentName in components)
            {
                VisualElement component = root.Q<VisualElement>(componentName);
                if (component != null)
                {
                    component.RemoveFromClassList("active");
                    component.AddToClassList("hidden");
                }
            }
        }

        void AnimateComponentIn(VisualElement element)
        {
            // Use Unity's animation system for effects that couldn't be directly converted
            // This replaces complex CSS animations with C# code

            element.style.opacity = 0;
            element.style.scale = new Scale(Vector3.one * 0.8f);

            // Fade in animation (replaces CSS fadeIn animation)
            element.experimental.animation.Start(
                new StyleValues { opacity = 1, scale = new Scale(Vector3.one) },
                300 // 0.3s duration, matching your CSS
            ).Ease(Easing.OutBack); // Similar to your CSS ease-out-back
        }

        void SetupComponentNavigation()
        {
            // Convert your bottom navigation JavaScript to C#
            var navButtons = root.Query<Button>(className: "nav-btn").ToList();

            foreach (var button in navButtons)
            {
                button.RegisterCallback<ClickEvent>(OnNavButtonClicked);
            }
        }

        void OnNavButtonClicked(ClickEvent evt)
        {
            Button clickedButton = evt.target as Button;
            string componentName = clickedButton.name; // Assuming button name matches component

            // Apply your button selection styling (from CSS)
            ClearNavSelection();
            clickedButton.AddToClassList("selected");

            // Show the component
            ShowComponent(componentName);

            // Analytics tracking (replacing your JavaScript analytics)
            Debug.Log($"📊 Navigation: {componentName} selected");
        }

        void ClearNavSelection()
        {
            var navButtons = root.Query<Button>(className: "nav-btn").ToList();
            foreach (var button in navButtons)
            {
                button.RemoveFromClassList("selected");
            }
        }

        void ResetAllComponents()
        {
            HideAllComponents();
            ClearNavSelection();

            // Reset any component states
            // Add your specific reset logic here
        }

        // Example: Convert complex JavaScript interactions
        public void HandleSettingsToggle(bool isEnabled, string settingName)
        {
            // Replaces your JavaScript saveSetting function
            PlayerPrefs.SetInt($"ui_setting_{settingName}", isEnabled ? 1 : 0);
            Debug.Log($"💾 Setting saved: {settingName} = {isEnabled}");
        }

        public void HandleLocationSelection(string locationName)
        {
            // Replaces your JavaScript updateLocationDetails function
            Debug.Log($"📍 Selected location: {locationName}");

            // Update UI based on location
            var locationInfo = root.Q<VisualElement>("location-info");
            if (locationInfo != null)
            {
                var locationLabel = locationInfo.Q<Label>("location-name");
                if (locationLabel != null)
                {
                    locationLabel.text = GetLocationDisplayName(locationName);
                }
            }
        }

        string GetLocationDisplayName(string locationName)
        {
            // Convert your JavaScript location data
            return locationName switch
            {
                "vienna" => "Vienna - Beginner",
                "paris" => "Paris - Intermediate",
                "london" => "London - Advanced",
                "tokyo" => "Tokyo - Expert",
                _ => locationName
            };
        }
    }
}