/**
 * Preview Manager - Handles component preview display and interactions
 */

class PreviewManager {
    constructor() {
        this.currentComponent = null;
        this.isModalOpen = false;
        this.stylesInjected = false;
        
        this.init();
    }

    /**
     * Initialize preview manager
     */
    init() {
        this.injectStyles();
        this.setupEventListeners();
        console.log('🖼️ Preview Manager initialized');
    }

    /**
     * Inject component styles dynamically
     */
    injectStyles() {
        if (this.stylesInjected) return;

        const styleSheet = document.createElement('style');
        styleSheet.textContent = `
            /* Component Preview Styles */
            ${window.MockupData?.phoneStyles || ''}

            /* World Tour Styles */
            .world-tour-bg {
                width: 100%;
                height: 100%;
                background: linear-gradient(135deg, #0a0a2e 0%, #16213e 25%, #1a1a3a 50%, #0f3460 75%, #0e4b99 100%);
                border-radius: 20px;
                position: relative;
                overflow: hidden;
            }

            .world-header {
                padding: 20px;
                background: linear-gradient(135deg, #1e3c72, #2a5298, #3498db);
                color: white;
            }

            .nav-btn-world {
                width: 40px;
                height: 40px;
                background: rgba(255,255,255,0.15);
                backdrop-filter: blur(10px);
                border-radius: 12px;
                display: flex;
                align-items: center;
                justify-content: center;
                color: white;
                font-weight: bold;
                cursor: pointer;
                border: 1px solid rgba(255,255,255,0.2);
                transition: all 0.3s ease;
            }

            .world-title {
                color: white;
                font-size: 1.4rem;
                font-weight: bold;
                text-align: center;
                flex: 1;
            }

            .header-controls {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 15px;
            }

            .tour-progress {
                text-align: center;
            }

            .progress-info {
                display: flex;
                justify-content: space-between;
                color: rgba(255,255,255,0.9);
                font-size: 0.85rem;
                margin-bottom: 8px;
            }

            .progress-bar-world {
                width: 100%;
                height: 6px;
                background: rgba(255,255,255,0.2);
                border-radius: 3px;
                overflow: hidden;
            }

            .progress-fill-world {
                height: 100%;
                background: linear-gradient(90deg, #3498db, #2ecc71);
                border-radius: 3px;
                transition: width 0.3s ease;
            }

            .globe-container {
                padding: 20px;
                display: flex;
                flex-direction: column;
                align-items: center;
                flex: 1;
            }

            .globe-wrapper {
                width: 280px;
                height: 280px;
                position: relative;
                perspective: 1000px;
            }

            .globe {
                width: 100%;
                height: 100%;
                border-radius: 50%;
                background: linear-gradient(135deg, #1e40af, #3b82f6, #60a5fa);
                position: relative;
                transform-style: preserve-3d;
                animation: globeRotate 30s linear infinite;
                box-shadow: inset 20px 20px 40px rgba(0,0,0,0.3), 0 20px 40px rgba(0,0,0,0.3);
                cursor: grab;
            }

            .globe-surface {
                position: absolute;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                border-radius: 50%;
                overflow: hidden;
            }

            .location-pin {
                position: absolute;
                width: 35px;
                height: 35px;
                cursor: pointer;
                transition: all 0.3s ease;
                z-index: 10;
            }

            .pin-icon {
                width: 100%;
                height: 100%;
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 1.2rem;
                border: 3px solid white;
                box-shadow: 0 4px 15px rgba(0,0,0,0.3);
                transition: all 0.3s ease;
            }

            .location-pin.unlocked .pin-icon {
                background: linear-gradient(135deg, #3498db, #2980b9);
                color: white;
            }

            .location-pin.completed .pin-icon {
                background: linear-gradient(135deg, #2ecc71, #27ae60);
                color: white;
            }

            .location-pin.locked .pin-icon {
                background: linear-gradient(135deg, #95a5a6, #7f8c8d);
                color: white;
            }

            .location-pin.special .pin-icon {
                background: linear-gradient(135deg, #f39c12, #e67e22);
                color: white;
            }

            .location-pin.active {
                transform: scale(1.3);
            }

            .pin-pulse {
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                width: 50px;
                height: 50px;
                border: 2px solid rgba(52,152,219,0.6);
                border-radius: 50%;
                animation: pinPulse 2s ease-in-out infinite;
            }

            .completion-star {
                position: absolute;
                top: -8px;
                right: -8px;
                font-size: 1rem;
                animation: starTwinkle 1.5s ease-in-out infinite;
            }

            .lock-overlay {
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                font-size: 0.9rem;
                background: rgba(0,0,0,0.7);
                border-radius: 50%;
                width: 20px;
                height: 20px;
                display: flex;
                align-items: center;
                justify-content: center;
            }

            .special-glow {
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                width: 60px;
                height: 60px;
                border-radius: 50%;
                background: radial-gradient(circle, rgba(243,156,18,0.3) 0%, transparent 70%);
                animation: specialGlow 3s ease-in-out infinite;
            }

            .floating-compass {
                position: absolute;
                top: 20px;
                left: 20px;
                width: 60px;
                height: 60px;
                z-index: 10;
            }

            .compass-ring {
                width: 100%;
                height: 100%;
                border: 3px solid rgba(255,255,255,0.3);
                border-radius: 50%;
                background: rgba(255,255,255,0.1);
                backdrop-filter: blur(10px);
                position: relative;
                display: flex;
                align-items: center;
                justify-content: center;
            }

            .compass-needle {
                width: 20px;
                height: 2px;
                background: linear-gradient(90deg, #e74c3c, #c0392b);
                position: absolute;
                transform-origin: center;
                animation: compassNeedle 8s ease-in-out infinite;
            }

            .compass-directions {
                position: absolute;
                width: 100%;
                height: 100%;
            }

            .direction {
                position: absolute;
                color: rgba(255,255,255,0.8);
                font-size: 0.7rem;
                font-weight: bold;
            }

            .direction.n { top: 2px; left: 50%; transform: translateX(-50%); }
            .direction.s { bottom: 2px; left: 50%; transform: translateX(-50%); }
            .direction.e { right: 2px; top: 50%; transform: translateY(-50%); }
            .direction.w { left: 2px; top: 50%; transform: translateY(-50%); }

            /* Add more component styles as needed */
            .main-menu-bg {
                width: 100%;
                height: 100%;
                background: linear-gradient(135deg, #0F0F23 0%, #1a1a3a 25%, #2d1b69 50%, #d7009e 75%, #ff6b6b 100%);
                border-radius: 20px;
                position: relative;
                display: flex;
                flex-direction: column;
                align-items: center;
                overflow: hidden;
            }

            /* Animation keyframes */
            @keyframes globeRotate {
                0% { transform: rotateY(0deg); }
                100% { transform: rotateY(360deg); }
            }

            @keyframes pinPulse {
                0%, 100% { opacity: 0.5; transform: translate(-50%, -50%) scale(1); }
                50% { opacity: 1; transform: translate(-50%, -50%) scale(1.2); }
            }

            @keyframes starTwinkle {
                0%, 100% { transform: scale(1) rotate(0deg); }
                50% { transform: scale(1.2) rotate(180deg); }
            }

            @keyframes specialGlow {
                0%, 100% { opacity: 0.3; transform: translate(-50%, -50%) scale(1); }
                50% { opacity: 0.7; transform: translate(-50%, -50%) scale(1.3); }
            }

            @keyframes compassNeedle {
                0%, 100% { transform: rotate(0deg); }
                25% { transform: rotate(90deg); }
                50% { transform: rotate(180deg); }
                75% { transform: rotate(270deg); }
            }
        `;

        document.head.appendChild(styleSheet);
        this.stylesInjected = true;
        console.log('🎨 Preview styles injected');
    }

    /**
     * Setup event listeners for preview interactions
     */
    setupEventListeners() {
        // Handle dynamic interactions in previews
        document.addEventListener('click', (e) => {
            if (!this.isModalOpen) return;

            // Location pin interactions
            if (e.target.closest('.location-pin') && !e.target.closest('.location-pin').classList.contains('locked')) {
                this.handleLocationPinClick(e.target.closest('.location-pin'));
            }

            // Globe interaction
            if (e.target.closest('.globe')) {
                this.handleGlobeClick(e.target.closest('.globe'));
            }

            // Category tab interactions
            if (e.target.classList.contains('category-tab')) {
                this.handleCategoryTabClick(e.target);
            }
        });
    }

    /**
     * Show preview for a component
     */
    async showPreview(componentId) {
        try {
            const component = window.ComponentData?.titles?.[componentId];
            const mockupHtml = window.MockupData?.components?.[componentId];

            if (!mockupHtml) {
                throw new Error(`Mockup not found for component: ${componentId}`);
            }

            const modal = document.getElementById('previewModal');
            const title = document.getElementById('previewTitle');
            const content = document.getElementById('previewContent');

            if (!modal || !title || !content) {
                throw new Error('Preview modal elements not found');
            }

            // Set content
            title.textContent = component || componentId;
            content.innerHTML = mockupHtml;

            // Show modal
            modal.style.display = 'flex';
            this.currentComponent = componentId;
            this.isModalOpen = true;

            // Add component-specific interactions
            this.setupComponentInteractions(componentId);

            console.log(`🖼️ Showing preview for: ${componentId}`);

        } catch (error) {
            console.error('❌ Error showing preview:', error);
            throw error;
        }
    }

    /**
     * Close preview modal
     */
    closePreview() {
        const modal = document.getElementById('previewModal');
        if (modal) {
            modal.style.display = 'none';
            this.currentComponent = null;
            this.isModalOpen = false;
            console.log('🖼️ Preview closed');
        }
    }

    /**
     * Setup component-specific interactions
     */
    setupComponentInteractions(componentId) {
        switch (componentId) {
            case 'world-tour':
                this.setupWorldTourInteractions();
                break;
            case 'powerups':
                this.setupCustomizationInteractions();
                break;
            case 'leaderboard':
                this.setupLeaderboardInteractions();
                break;
            // Add more cases as needed
        }
    }

    /**
     * Setup World Tour specific interactions
     */
    setupWorldTourInteractions() {
        // Globe rotation pause on interaction
        const globe = document.querySelector('.globe');
        if (globe) {
            globe.addEventListener('mousedown', () => {
                globe.style.animationPlayState = 'paused';
            });
            
            globe.addEventListener('mouseup', () => {
                setTimeout(() => {
                    globe.style.animationPlayState = 'running';
                }, 2000);
            });
        }
    }

    /**
     * Setup Customization specific interactions
     */
    setupCustomizationInteractions() {
        // Category switching
        const categoryTabs = document.querySelectorAll('.category-tab');
        categoryTabs.forEach(tab => {
            tab.addEventListener('click', () => {
                categoryTabs.forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
            });
        });
    }

    /**
     * Setup Leaderboard specific interactions
     */
    setupLeaderboardInteractions() {
        // Tab switching
        const tabs = document.querySelectorAll('.tab-new');
        tabs.forEach(tab => {
            tab.addEventListener('click', () => {
                tabs.forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
            });
        });
    }

    /**
     * Handle location pin clicks
     */
    handleLocationPinClick(pin) {
        // Remove active from all pins
        document.querySelectorAll('.location-pin').forEach(p => {
            p.classList.remove('active');
        });
        
        // Make clicked pin active
        pin.classList.add('active');
        
        const location = pin.getAttribute('data-location');
        console.log(`📍 Selected location: ${location}`);
    }

    /**
     * Handle globe clicks
     */
    handleGlobeClick(globe) {
        console.log('🌍 Globe clicked - rotation paused');
        globe.style.animationPlayState = 'paused';
        
        setTimeout(() => {
            globe.style.animationPlayState = 'running';
            console.log('🌍 Globe rotation resumed');
        }, 2000);
    }

    /**
     * Handle category tab clicks
     */
    handleCategoryTabClick(tab) {
        document.querySelectorAll('.category-tab').forEach(t => {
            t.classList.remove('active');
        });
        tab.classList.add('active');
        
        const category = tab.getAttribute('data-category');
        console.log(`🏷️ Switched to category: ${category}`);
    }

    /**
     * Get component preview HTML
     */
    getComponentPreview(componentId) {
        return window.MockupData?.components?.[componentId] || `
            <div class="phone-mockup">
                <div style="padding: 40px; text-align: center; color: #666;">
                    <h3>Preview not available for ${componentId}</h3>
                    <p>Component preview is loading...</p>
                </div>
            </div>
        `;
    }
}

// Initialize Preview Manager
window.PreviewManager = new PreviewManager();

console.log('🖼️ Preview Manager loaded');
