/**
 * Component Data - Central repository for all UI component metadata
 */

window.ComponentData = {
    
    // Component prompts for each UI element
    prompts: {
        'world-tour': "Design a 3D interactive world map for a piano rhythm game featuring a rotatable globe with musical heritage locations. Include Vienna (Classical), Paris (Impressionist), London (Baroque), New York (Jazz), Tokyo (Modern), and more. Each location should have unlock states, difficulty levels, signature composers, and cultural themes. Add smooth rotation controls, zoom functionality, location detail panels, and progress tracking with a tour completion system.",
        
        'main-menu': "Design a high-fidelity mobile main menu for a piano-tiles rhythm game in a friendly, cartoon-inspired style. Use a vibrant pastel palette (soft purples, pinks, mint greens) with rounded-corner panels, subtle drop shadows, and a faint tile-pattern background. The title sits at top in a bold, modern sans-serif with a light gradient, below it two large pill-shaped buttons ('Play' and 'Shop') in purple and mint. Add a bottom bar with circular icons for Settings, Leaderboard and Profile, each on frosted-glass buttons, with small neon highlights on hover states.",
        
        'hud': "Create an in-game HUD overlay for a piano tile-tapping game: a narrow translucent bar pinned to the top showing score (white text + neon accent), combo counter in a colored pill (mint), and timer on the right. Make the bar slightly blurred (glassmorphism) with soft inner glow. Tiles themselves are dark slate with neon outlines; when tapped they emit a small particle burst. Include a floating circular pause button with subtle drop shadow in the top-right corner.",
        
        'level-complete': "Generate a level complete pop-up modal in a cartoon-flat style: a centered card with frosted-glass background, softly blurred game scene behind it. The card has a big header 'Level Complete!' in gradient text, below three star outlines that fill gold when earned. Underneath, two large buttons: a green 'Next Level' pill with soft shadow and a transparent 'Replay' outline button. Add tiny confetti illustrations around the card in matching pastel colors.",
        
        'shop': "Design a revolutionary shop interface combining 3D world integration with modern upgrade systems. Use a clean parchment background with multiple upgrade cards, unlock panels, and renovation popups. Each element should have glassmorphism effects, smooth transitions, and premium visual hierarchy. Include currency displays, progress bars, and interactive preview systems for piano themes, tile designs, and musical content.",
        
        'settings': "Produce a settings side-panel sliding in from the left: background is a semi-opaque dark overlay, the panel itself is a mint green card with rounded corners. Sections include toggles for Music, SFX, Vibration—each toggle is a pill-shaped switch that slides and glows softly when turned on. Font is a clean geometric sans-serif in white, section headers in bold uppercase. Include a 'Restore Purchases' text button at bottom in subtle purple underline.",
        
        'powerups': "Create a comprehensive customization system for a piano rhythm game: featuring instrument selection (piano, guitar, violin, drums), artist collections (classical composers, modern artists), 3D tile designs with different shapes and materials, background environments, and visual effects. Use category tabs for navigation, interactive preview system with smooth transitions, equipment states (locked/unlocked/equipped), rarity systems, and smooth navigation arrows. Include try-before-buy functionality and achievement-based unlock progression.",
        
        'leaderboard': "Design a premium leaderboard screen with parchment texture background and champions podium for top 3 players. Include tabbed navigation (Friends/Global/Weekly), player cards with avatars and rank changes, social features like challenge and share buttons. Use elegant color scheme with white cards, gold accents for winners, and smooth animations for rank updates. Add floating action buttons and real-time update notifications."
    },

    // Component titles for display
    titles: {
        'world-tour': 'World Tour Map - 3D Globe Experience',
        'main-menu': 'Main Menu Interface - Premium Design',
        'hud': 'In-Game HUD Overlay',
        'level-complete': 'Level Complete Modal',
        'shop': 'Piano Shop & Upgrade System',
        'settings': 'Settings Side Panel',
        'powerups': 'Instrument & Style Customization',
        'leaderboard': 'Leaderboard & Social Features'
    },

    // Component categories for organization
    categories: {
        'core': ['main-menu', 'hud', 'level-complete'],
        'progression': ['world-tour', 'leaderboard'],
        'monetization': ['shop', 'powerups'],
        'utility': ['settings']
    },

    // Implementation priority (1-5, 5 being critical)
    priority: {
        'world-tour': 5,      // Core differentiation feature
        'main-menu': 5,       // First impression critical
        'shop': 5,           // Revenue generation
        'powerups': 5,       // User engagement
        'hud': 5,           // Core gameplay
        'level-complete': 4, // Player retention
        'leaderboard': 4,    // Social engagement
        'settings': 3        // User experience
    },

    // Technical complexity assessment (1-5)
    complexity: {
        'world-tour': 5,     // 3D globe, complex interactions
        'powerups': 5,       // Multiple systems integration
        'shop': 4,          // Multiple upgrade systems
        'leaderboard': 4,    // Network integration
        'main-menu': 3,      // Animations and effects
        'level-complete': 3, // Modal system
        'hud': 3,           // Real-time updates
        'settings': 2        // Simple UI elements
    },

    // Development time estimates (in days)
    timeEstimates: {
        'world-tour': 8,     // Complex 3D implementation
        'powerups': 6,       // Multiple category systems
        'shop': 5,          // Upgrade and purchase flow
        'leaderboard': 4,    // Social features + backend
        'main-menu': 3,      // Premium animations
        'level-complete': 2, // Modal with animations
        'hud': 3,           // Real-time UI updates
        'settings': 2        // Standard settings panel
    },

    // Required Unity packages/assets
    requiredPackages: {
        'world-tour': [
            'DOTween Pro',
            'UI Blur',
            'TextMeshPro',
            'Particle System'
        ],
        'main-menu': [
            'DOTween Pro', 
            'UI Gradient',
            'TextMeshPro',
            'Audio Manager'
        ],
        'shop': [
            'Unity IAP',
            'DOTween Pro',
            'UI Blur',
            'Save System'
        ],
        'powerups': [
            'Audio Preview System',
            'Asset Bundle Manager',
            'DOTween Pro',
            'Save System'
        ],
        'leaderboard': [
            'Unity Netcode',
            'Social Platform SDKs',
            'DOTween Pro',
            'Avatar System'
        ],
        'hud': [
            'TextMeshPro',
            'UI Blur',
            'Particle System',
            'Audio Manager'
        ],
        'level-complete': [
            'DOTween Pro',
            'Particle System',
            'Audio Manager',
            'Achievement System'
        ],
        'settings': [
            'Platform Services',
            'Audio Manager',
            'Save System',
            'Haptic Feedback'
        ]
    },

    // Platform considerations
    platformNotes: {
        'world-tour': {
            'iOS': 'Test 3D performance on older devices, consider LOD system',
            'Android': 'Fragment-based implementation, memory optimization',
            'Universal': 'Safe area handling for notched devices'
        },
        'main-menu': {
            'iOS': 'Haptic feedback integration, App Store guidelines',
            'Android': 'Material Design considerations, adaptive icons',
            'Universal': 'Responsive layout for tablets'
        },
        'shop': {
            'iOS': 'App Store IAP guidelines, receipt validation',
            'Android': 'Play Store billing, subscription handling',
            'Universal': 'GDPR compliance, parental controls'
        }
    },

    // Accessibility considerations
    accessibility: {
        'world-tour': [
            'Screen reader support for location names',
            'High contrast mode for pins',
            'Alternative text for visual elements',
            'Keyboard navigation support'
        ],
        'main-menu': [
            'Minimum touch target size (44pt)',
            'Color contrast WCAG AA compliance',
            'VoiceOver/TalkBack support',
            'Reduced motion preferences'
        ],
        'shop': [
            'Price announcement for screen readers',
            'Clear purchase confirmation',
            'Error message accessibility',
            'Currency format localization'
        ]
    }
};

console.log('📊 Component Data loaded');
