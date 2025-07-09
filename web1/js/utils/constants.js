/**
 * Constants and Configuration - Global constants and configuration settings
 */

window.UITesterConstants = {

    // Application Configuration
    APP: {
        NAME: 'Unity Mobile UI Design Tester',
        VERSION: '2.1.0',
        DESCRIPTION: 'Piano Tiles Rhythm Game - UI Component Testing Suite',
        AUTHOR: 'Claude AI Assistant',
        BUILD_DATE: new Date().toISOString()
    },

    // Component Configuration
    COMPONENTS: {
        TOTAL_COUNT: 8,
        TEST_DELAY: 2000, // milliseconds between tests
        PREVIEW_TRANSITION_TIME: 500, // milliseconds
        ANIMATION_DURATION: 300 // milliseconds
    },

    // Component IDs and Order
    COMPONENT_IDS: [
        'world-tour',
        'main-menu',
        'hud',
        'level-complete',
        'shop',
        'settings',
        'powerups',
        'leaderboard'
    ],

    // Priority Levels
    PRIORITY: {
        CRITICAL: 5,
        HIGH: 4,
        MEDIUM: 3,
        LOW: 2,
        MINIMAL: 1
    },

    // Component Categories
    CATEGORIES: {
        CORE: 'core',
        PROGRESSION: 'progression',
        MONETIZATION: 'monetization',
        UTILITY: 'utility',
        SOCIAL: 'social'
    },

    // Status States
    STATUS: {
        PENDING: 'pending',
        TESTING: 'testing',
        COMPLETE: 'complete',
        ERROR: 'error'
    },

    // UI States
    UI_STATES: {
        LOADING: 'loading',
        READY: 'ready',
        MODAL_OPEN: 'modal-open',
        ERROR: 'error'
    },

    // Breakpoints (matching CSS)
    BREAKPOINTS: {
        SM: 640,
        MD: 768,
        LG: 1024,
        XL: 1280,
        XXL: 1536
    },

    // Touch Targets (for accessibility)
    TOUCH_TARGETS: {
        MINIMUM: 44, // pixels
        RECOMMENDED: 48, // pixels
        COMFORTABLE: 56 // pixels
    },

    // Animation Timings
    ANIMATIONS: {
        FAST: 150,
        NORMAL: 300,
        SLOW: 500,
        VERY_SLOW: 800
    },

    // Z-Index Layers
    Z_INDEX: {
        DROPDOWN: 10,
        TOOLTIP: 50,
        MODAL_BACKDROP: 100,
        MODAL: 200,
        TOAST: 1000,
        OVERLAY: 9999
    },

    // Color Themes
    THEMES: {
        LIGHT: 'light',
        DARK: 'dark',
        AUTO: 'auto'
    },

    // Storage Keys
    STORAGE_KEYS: {
        THEME_PREFERENCE: 'ui-tester-theme',
        TESTED_COMPONENTS: 'ui-tester-tested',
        USER_PREFERENCES: 'ui-tester-prefs',
        LAST_SESSION: 'ui-tester-session'
    },

    // Error Types
    ERROR_TYPES: {
        NETWORK: 'NETWORK_ERROR',
        COMPONENT_NOT_FOUND: 'COMPONENT_NOT_FOUND',
        MOCKUP_MISSING: 'MOCKUP_MISSING',
        DEPENDENCY_MISSING: 'DEPENDENCY_MISSING',
        INITIALIZATION_FAILED: 'INITIALIZATION_FAILED',
        PREVIEW_FAILED: 'PREVIEW_FAILED'
    },

    // Event Names
    EVENTS: {
        COMPONENT_TESTED: 'component:tested',
        COMPONENT_PREVIEW: 'component:preview',
        PROGRESS_UPDATE: 'progress:update',
        ERROR_OCCURRED: 'error:occurred',
        MODAL_OPEN: 'modal:open',
        MODAL_CLOSE: 'modal:close',
        THEME_CHANGE: 'theme:change'
    },

    // Performance Targets
    PERFORMANCE: {
        LOAD_TIME_TARGET: 3000, // milliseconds
        ANIMATION_FPS_TARGET: 60,
        MEMORY_LIMIT_MB: 100,
        BUNDLE_SIZE_LIMIT_KB: 500
    },

    // Unity-Specific Constants
    UNITY: {
        MIN_VERSION: '2021.3',
        RECOMMENDED_VERSION: '2022.3',
        REQUIRED_PACKAGES: [
            'com.unity.textmeshpro',
            'com.unity.ui.toolkit',
            'com.unity.ugui',
            'com.unity.timeline'
        ],
        OPTIONAL_PACKAGES: [
            'com.unity.purchasing',
            'com.unity.ads',
            'com.unity.analytics',
            'com.unity.cloud-build'
        ]
    },

    // Platform Support
    PLATFORMS: {
        IOS: {
            MIN_VERSION: '12.0',
            RECOMMENDED_VERSION: '15.0',
            SPECIAL_CONSIDERATIONS: [
                'Safe Area Support',
                'Haptic Feedback',
                'App Store Guidelines'
            ]
        },
        ANDROID: {
            MIN_API: 21,
            RECOMMENDED_API: 31,
            SPECIAL_CONSIDERATIONS: [
                'Material Design',
                'Adaptive Icons',
                'Play Store Requirements'
            ]
        },
        WEB: {
            MIN_BROWSERS: {
                chrome: '80',
                firefox: '75',
                safari: '13',
                edge: '80'
            }
        }
    },

    // Testing Configuration
    TESTING: {
        MAX_RETRY_ATTEMPTS: 3,
        TIMEOUT_DURATION: 10000, // milliseconds
        BATCH_SIZE: 3, // components to test simultaneously
        SCREENSHOT_QUALITY: 0.8,
        PERFORMANCE_SAMPLE_SIZE: 100
    },

    // Accessibility Standards
    ACCESSIBILITY: {
        MIN_CONTRAST_RATIO: 4.5,
        MIN_TOUCH_TARGET: 44,
        MAX_ANIMATION_DURATION: 5000,
        FOCUS_OUTLINE_WIDTH: 2,
        SCREEN_READER_TIMEOUT: 3000
    },

    // Monetization Constants
    MONETIZATION: {
        CURRENCY_TYPES: ['coins', 'gems', 'premium'],
        IAP_CATEGORIES: ['consumable', 'non_consumable', 'subscription'],
        PRICE_TIERS: [0.99, 2.99, 4.99, 9.99, 19.99, 49.99],
        CONVERSION_TRACKING: true
    },

    // Analytics Events
    ANALYTICS: {
        COMPONENT_VIEWED: 'component_viewed',
        COMPONENT_TESTED: 'component_tested',
        PREVIEW_OPENED: 'preview_opened',
        IMPLEMENTATION_REQUESTED: 'implementation_requested',
        ANALYSIS_VIEWED: 'analysis_viewed',
        REPORT_GENERATED: 'report_generated',
        EXPORT_INITIATED: 'export_initiated'
    },

    // Localization
    LOCALIZATION: {
        DEFAULT_LANGUAGE: 'en',
        SUPPORTED_LANGUAGES: ['en', 'tr', 'de', 'fr', 'es', 'ja', 'ko'],
        RTL_LANGUAGES: ['ar', 'he', 'fa'],
        DATE_FORMAT: 'YYYY-MM-DD',
        TIME_FORMAT: 'HH:mm:ss'
    },

    // Feature Flags
    FEATURES: {
        DARK_MODE: true,
        ANALYTICS: true,
        ERROR_REPORTING: true,
        PERFORMANCE_MONITORING: true,
        A_B_TESTING: false,
        BETA_FEATURES: false,
        OFFLINE_MODE: false
    },

    // API Configuration
    API: {
        BASE_URL: 'https://api.unity-ui-tester.com',
        VERSION: 'v1',
        TIMEOUT: 30000,
        RETRY_ATTEMPTS: 3,
        RATE_LIMIT: 100 // requests per minute
    },

    // Cache Configuration
    CACHE: {
        EXPIRY_TIME: 24 * 60 * 60 * 1000, // 24 hours in milliseconds
        MAX_SIZE_MB: 50,
        CACHE_KEYS: {
            COMPONENT_DATA: 'comp_data',
            MOCKUP_DATA: 'mockup_data',
            ANALYSIS_DATA: 'analysis_data',
            USER_PREFERENCES: 'user_prefs'
        }
    },

    // Debug Configuration
    DEBUG: {
        ENABLED: true,
        LOG_LEVEL: 'info', // 'debug', 'info', 'warn', 'error'
        PERFORMANCE_LOGGING: true,
        ERROR_STACK_TRACES: true,
        VERBOSE_LOGGING: false
    },

    // Security
    SECURITY: {
        CONTENT_SECURITY_POLICY: true,
        XSS_PROTECTION: true,
        SANITIZE_INPUT: true,
        SECURE_STORAGE: true
    },

    // Component-Specific Constants
    WORLD_TOUR: {
        MAX_LOCATIONS: 12,
        DEFAULT_ZOOM: 1.0,
        MIN_ZOOM: 0.5,
        MAX_ZOOM: 3.0,
        ROTATION_SPEED: 30, // seconds per rotation
        LOCATION_UNLOCK_THRESHOLD: 3 // stars needed
    },

    CUSTOMIZATION: {
        MAX_CATEGORIES: 8,
        ITEMS_PER_PAGE: 12,
        PREVIEW_DURATION: 3000, // milliseconds
        MAX_EQUIPPED_ITEMS: 1,
        RARITY_LEVELS: ['common', 'rare', 'epic', 'legendary']
    },

    LEADERBOARD: {
        DEFAULT_PAGE_SIZE: 20,
        MAX_PAGE_SIZE: 100,
        REFRESH_INTERVAL: 30000, // milliseconds
        CACHE_DURATION: 300000, // 5 minutes
        RANK_CHANGE_THRESHOLD: 5
    },

    // Validation Rules
    VALIDATION: {
        COMPONENT_NAME: {
            MIN_LENGTH: 3,
            MAX_LENGTH: 50,
            PATTERN: /^[a-zA-Z0-9-_]+$/
        },
        VERSION: {
            PATTERN: /^\d+\.\d+\.\d+$/
        },
        COLOR: {
            PATTERN: /^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/
        }
    },

    // Default Settings
    DEFAULTS: {
        THEME: 'auto',
        LANGUAGE: 'en',
        SOUND_ENABLED: true,
        HAPTIC_ENABLED: true,
        ANIMATIONS_ENABLED: true,
        NOTIFICATIONS_ENABLED: true,
        ANALYTICS_ENABLED: true
    },

    // File Paths (for modular loading)
    PATHS: {
        CSS: {
            BASE: 'css/base.css',
            LAYOUT: 'css/layout.css',
            COMPONENTS: 'css/components/',
            ANIMATIONS: 'css/animations.css'
        },
        JS: {
            CORE: 'js/core/',
            COMPONENTS: 'js/components/',
            UTILS: 'js/utils/',
            DATA: 'data/'
        },
        ASSETS: {
            ICONS: 'assets/icons/',
            IMAGES: 'assets/images/',
            FONTS: 'assets/fonts/'
        }
    },

    // Regex Patterns
    PATTERNS: {
        EMAIL: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
        URL: /^https?:\/\/.+/,
        SEMANTIC_VERSION: /^\d+\.\d+\.\d+$/,
        HEX_COLOR: /^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/,
        COMPONENT_ID: /^[a-z]+(-[a-z]+)*$/
    },

    // Time Constants
    TIME: {
        SECOND: 1000,
        MINUTE: 60 * 1000,
        HOUR: 60 * 60 * 1000,
        DAY: 24 * 60 * 60 * 1000,
        WEEK: 7 * 24 * 60 * 60 * 1000
    }
};

// Utility function to get nested constants safely
window.UITesterConstants.get = function (path, defaultValue = null) {
    return path.split('.').reduce((obj, key) => obj?.[key], this) ?? defaultValue;
};

// Utility function to check if a feature is enabled
window.UITesterConstants.isFeatureEnabled = function (featureName) {
    return window.UITesterConstants.FEATURES[featureName] === true;
};

// Utility function to get breakpoint info
window.UITesterConstants.getBreakpoint = function () {
    const width = window.innerWidth;
    const bp = window.UITesterConstants.BREAKPOINTS;
    if (width < bp.SM) return 'xs';
    if (width < bp.MD) return 'sm';
    if (width < bp.LG) return 'md';
    if (width < bp.XL) return 'lg';
    if (width < bp.XXL) return 'xl';
    return '2xl';
};

// Utility function to check if device is mobile
window.UITesterConstants.isMobile = function () {
    return window.innerWidth < window.UITesterConstants.BREAKPOINTS.MD;
};

// Utility function to get timing for animations
window.UITesterConstants.getAnimationDuration = function (type = 'normal') {
    const anims = window.UITesterConstants.ANIMATIONS;
    const durations = {
        fast: anims.FAST,
        normal: anims.NORMAL,
        slow: anims.SLOW,
        'very-slow': anims.VERY_SLOW
    };
    return durations[type] || anims.NORMAL;
};

// Freeze the constants to prevent modification (after adding utility functions)
Object.freeze(window.UITesterConstants);

console.log('⚙️ Constants loaded');
