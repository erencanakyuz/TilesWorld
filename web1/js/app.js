/**
 * Unity Mobile UI Design Tester - Main Application Coordinator
 * Coordinates all components, manages state, and handles core functionality
 */

class UITester {
    constructor() {
        this.components = new Map();
        this.testedComponents = new Set();
        this.totalComponents = 10;
        this.isInitialized = false;
        this.currentComponentIndex = 0;
        this.componentIds = [];

        // Initialize when DOM is ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.init());
        } else {
            this.init();
        }
    }

    /**
     * Initialize the application
     */
    async init() {
        try {
            console.log('🚀 Initializing Unity UI Tester...');

            // Wait for all required modules to load
            await this.waitForDependencies();

            // Initialize core systems
            this.initializeComponents();
            this.setupEventListeners();
            this.renderComponentsGrid();
            this.renderUnityTips();
            this.renderComponentCarousel();
            this.setupKeyboardNavigation();

            // Initialize progress tracking
            if (window.ProgressTracker) {
                this.progressTracker = new ProgressTracker(this.totalComponents);
            }

            // Initialize interaction handlers
            if (window.InteractionHandler) {
                this.interactionHandler = new InteractionHandler();
            }

            this.isInitialized = true;
            console.log('✅ Unity UI Tester initialized successfully');

        } catch (error) {
            console.error('❌ Failed to initialize Unity UI Tester:', error);
            this.showError('Failed to initialize application. Please refresh the page.');
        }
    }

    /**
     * Wait for all required dependencies to load
     */
    async waitForDependencies() {
        const requiredModules = [
            'ComponentData',
            'MockupData',
            'AnalysisData',
            'PreviewManager'
        ];

        const maxAttempts = 50;
        let attempts = 0;

        while (attempts < maxAttempts) {
            const allLoaded = requiredModules.every(module => window[module]);
            if (allLoaded) return;

            await new Promise(resolve => setTimeout(resolve, 100));
            attempts++;
        }

        throw new Error('Required modules failed to load');
    }

    /**
     * Initialize component data
     */
    initializeComponents() {
        if (!window.ComponentData) {
            throw new Error('ComponentData not loaded');
        }

        const componentList = [
            {
                id: 'world-tour',
                title: 'World Tour Map - 3D Globe Experience',
                icon: 'icon-world',
                priority: 5,
                status: 'pending'
            },
            {
                id: 'main-menu',
                title: 'Main Menu Interface - Premium Design',
                icon: 'icon-menu',
                priority: 5,
                status: 'pending'
            },
            {
                id: 'hud',
                title: 'In-Game HUD Overlay',
                icon: 'icon-hud',
                priority: 5,
                status: 'pending'
            },
            {
                id: 'level-complete',
                title: 'Level Complete Modal',
                icon: 'icon-modal',
                priority: 4,
                status: 'pending'
            },
            {
                id: 'shop',
                title: 'Piano Shop & Upgrade System',
                icon: 'icon-shop',
                priority: 5,
                status: 'pending'
            },
            {
                id: 'settings',
                title: 'Settings Side Panel',
                icon: 'icon-settings',
                priority: 3,
                status: 'pending'
            },
            {
                id: 'powerups',
                title: 'Instrument & Style Customization',
                icon: 'icon-powerup',
                priority: 5,
                status: 'pending'
            },
            {
                id: 'leaderboard',
                title: 'Leaderboard & Social Features',
                icon: 'icon-leaderboard',
                priority: 4,
                status: 'pending'
            },
            {
                id: 'achievement-gallery',
                title: 'Achievement Gallery & Rewards',
                icon: 'icon-achievement',
                priority: 4,
                status: 'pending'
            },
            {
                id: 'music-selection',
                title: 'Music Selection & Library',
                icon: 'icon-music',
                priority: 5,
                status: 'pending'
            }
        ];

        componentList.forEach(comp => {
            this.components.set(comp.id, comp);
        });

        // Store component IDs for navigation
        this.componentIds = componentList.map(comp => comp.id);
    }

    /**
     * Setup global event listeners
     */
    setupEventListeners() {
        // Close modal when clicking outside
        const modal = document.getElementById('previewModal');
        if (modal) {
            modal.addEventListener('click', (e) => {
                if (e.target === modal) {
                    this.closePreview();
                }
            });
        }

        // Handle window resize
        window.addEventListener('resize', this.handleResize.bind(this));
    }

    /**
     * Render the components grid
     */
    renderComponentsGrid() {
        const grid = document.getElementById('componentsGrid');
        if (!grid) return;

        let html = '';
        let index = 1;

        for (const [id, component] of this.components) {
            html += this.createComponentCard(id, component, index);
            index++;
        }

        grid.innerHTML = html;
    }

    /**
     * Create a component card HTML
     */
    createComponentCard(id, component, index) {
        const promptText = window.ComponentData?.prompts?.[id] || 'Component description not available';
        const analysis = window.AnalysisData?.analysis?.[id] || 'Analysis not available';
        const implementation = window.AnalysisData?.implementation?.[id] || 'Implementation guide not available';

        return `
            <div class="ui-card" data-component="${id}">
                <h3>
                    <span class="ui-icon ${component.icon}">${index}</span>
                    ${component.title}
                    <span class="status-indicator status-pending" id="status-${index}"></span>
                </h3>
                
                <div class="prompt-text">
                    "${promptText}"
                </div>

                <div class="analysis">
                    <h4>🧠 Claude's Analysis:</h4>
                    <p>${analysis}</p>
                </div>

                <div class="implementation">
                    <h4>🔧 Implementation Steps:</h4>
                    <p>${implementation}</p>
                </div>

                <div class="test-buttons">
                    <button class="btn-test btn-preview" onclick="UITester.previewComponent('${id}')">👁️ Preview</button>
                    <button class="btn-test btn-implement" onclick="UITester.implementComponent('${id}')">⚙️ Implement</button>
                    <button class="btn-test btn-analyze" onclick="UITester.analyzeComponent('${id}')">📊 Analyze</button>
                </div>
            </div>
        `;
    }

    /**
     * Render Unity implementation tips
     */
    renderUnityTips() {
        const tipsContainer = document.getElementById('unityTips');
        if (!tipsContainer) return;

        const tips = [
            {
                title: 'Glassmorphism Effect',
                css: 'background: rgba(255,255,255,0.2);\nbackdrop-filter: blur(10px);',
                unity: 'Use UI Blur shader or custom material for Unity UI'
            },
            {
                title: 'Rounded Corners & Shadows',
                css: 'border-radius: 16px;\nbox-shadow: 0 4px 12px rgba(0,0,0,0.15);',
                unity: 'Use Rounded Image component or 9-slice sprites'
            },
            {
                title: 'Neon Text Accents',
                css: 'text-shadow: 0 0 6px rgba(142,80,255,0.7);',
                unity: 'TextMeshPro with Glow material or custom shader'
            },
            {
                title: 'Pill-Shaped Buttons',
                css: 'padding: 12px 24px;\nborder-radius: 999px;',
                unity: 'Use Capsule sprites or procedural shapes'
            }
        ];

        let tipsHTML = `
            <h2>🔧 Unity Implementation Tips</h2>
            <div class="tips-grid">
        `;

        tips.forEach(tip => {
            tipsHTML += `
                <div class="tip-card">
                    <h4>${tip.title}</h4>
                    <code>${tip.css}</code>
                    <p style="color: rgba(255,255,255,0.8); margin-top: 10px;">${tip.unity}</p>
                </div>
            `;
        });

        tipsHTML += '</div>';
        tipsContainer.innerHTML = tipsHTML;
        tipsContainer.style.display = 'block';
    }

    /**
     * Preview a component
     */
    async previewComponent(componentId) {
        try {
            console.log(`🔍 Previewing component: ${componentId}`);

            const statusElement = document.getElementById(`status-${this.getComponentNumber(componentId)}`);
            if (statusElement) {
                statusElement.className = 'status-indicator status-testing';
            }

            // Use PreviewManager if available
            if (window.PreviewManager) {
                await window.PreviewManager.showPreview(componentId);
            } else {
                // Fallback to basic preview
                this.showBasicPreview(componentId);
            }

            // Mark as tested
            this.testedComponents.add(componentId);
            this.updateProgress();

            if (statusElement) {
                setTimeout(() => {
                    statusElement.className = 'status-indicator status-complete';
                }, 500);
            }

        } catch (error) {
            console.error(`❌ Error previewing ${componentId}:`, error);
            this.showError(`Failed to preview ${componentId}`);
        }
    }

    /**
     * Show basic preview fallback
     */
    showBasicPreview(componentId) {
        const component = this.components.get(componentId);
        const modal = document.getElementById('previewModal');
        const title = document.getElementById('previewTitle');
        const content = document.getElementById('previewContent');

        if (title) title.textContent = component?.title || componentId;
        if (content) content.innerHTML = `<p>Preview for ${componentId} - Loading modular preview system...</p>`;
        if (modal) modal.style.display = 'flex';
    }

    /**
     * Implement component
     */
    implementComponent(componentId) {
        console.log(`⚙️ Implementing component: ${componentId}`);
        alert(`⚙️ Generating Unity implementation for ${componentId}...\n\nThis would create:\n• Prefab file\n• C# script template\n• Material settings\n• Animation clips\n• Documentation`);
    }

    /**
     * Analyze component
     */
    analyzeComponent(componentId) {
        console.log(`📊 Analyzing component: ${componentId}`);
        const analysis = window.AnalysisData?.detailedAnalysis?.[componentId] ||
            `Analysis for ${componentId} - Loading detailed analysis system...`;
        alert(`📊 Analysis for ${componentId}:\n\n${analysis}`);
    }

    /**
     * Test all components
     */
    async testAllComponents() {
        console.log('🚀 Testing all components...');

        const componentIds = Array.from(this.components.keys());

        for (let i = 0; i < componentIds.length; i++) {
            setTimeout(() => {
                this.previewComponent(componentIds[i]);
            }, i * 2000);
        }
    }



    /**
     * Reset all tests
     */
    resetTests() {
        console.log('🔄 Resetting tests...');

        this.testedComponents.clear();
        this.updateProgress();

        // Reset all status indicators
        for (let i = 1; i <= this.totalComponents; i++) {
            const statusElement = document.getElementById(`status-${i}`);
            if (statusElement) {
                statusElement.className = 'status-indicator status-pending';
            }
        }

        alert('🔄 All tests reset! Ready for fresh testing.');
    }

    /**
     * Close preview modal
     */
    closePreview() {
        const modal = document.getElementById('previewModal');
        if (modal) {
            modal.style.display = 'none';
        }
    }

    /**
     * Update progress bar
     */
    updateProgress() {
        const progress = (this.testedComponents.size / this.totalComponents) * 100;
        const progressBar = document.getElementById('overallProgress');
        if (progressBar) {
            progressBar.style.width = `${progress}%`;
        }
    }

    /**
     * Get component number for status tracking
     */
    getComponentNumber(componentId) {
        const mapping = {
            'world-tour': 1,
            'main-menu': 2,
            'hud': 3,
            'level-complete': 4,
            'shop': 5,
            'settings': 6,
            'powerups': 7,
            'leaderboard': 8,
            'achievement-gallery': 9,
            'music-selection': 10
        };
        return mapping[componentId] || 1;
    }

    /**
     * Setup keyboard navigation
     */
    setupKeyboardNavigation() {
        document.addEventListener('keydown', (event) => {
            // Only handle if no input is focused
            if (document.activeElement.tagName === 'INPUT' || document.activeElement.tagName === 'TEXTAREA') {
                return;
            }

            switch (event.key) {
                case 'ArrowLeft':
                    event.preventDefault();
                    this.navigatePrev();
                    break;
                case 'ArrowRight':
                    event.preventDefault();
                    this.navigateNext();
                    break;
                case 'Escape':
                    if (document.getElementById('previewModal').style.display !== 'none') {
                        this.closePreview();
                    }
                    break;
                case 'Enter':
                case ' ':
                    if (this.componentIds[this.currentComponentIndex]) {
                        event.preventDefault();
                        this.previewComponent(this.componentIds[this.currentComponentIndex]);
                    }
                    break;
            }
        });
    }

    /**
     * Render component carousel
     */
    renderComponentCarousel() {
        const carousel = document.getElementById('componentCarousel');
        if (!carousel) return;

        let html = '';
        let index = 0;

        for (const [id, component] of this.components) {
            const isActive = index === this.currentComponentIndex;
            const testStatus = this.testedComponents.has(id) ? 'tested' : 'pending';

            html += `
                <div class="carousel-item ${isActive ? 'active' : ''} ${testStatus}" 
                     data-component="${id}" 
                     data-index="${index}"
                     onclick="UITester.selectComponent(${index})">
                    <div class="thumbnail">
                        <div class="component-icon">${index + 1}</div>
                        <div class="component-name">${component.title.split(' - ')[0]}</div>
                        <div class="test-status">
                            ${this.testedComponents.has(id) ? '✅' : '⭕'}
                        </div>
                    </div>
                </div>
            `;
            index++;
        }

        carousel.innerHTML = html;
        this.updateNavigationState();
    }

    /**
     * Navigate to previous component
     */
    navigatePrev() {
        this.currentComponentIndex = this.currentComponentIndex > 0
            ? this.currentComponentIndex - 1
            : this.componentIds.length - 1;
        this.updateCarouselView();
        this.updateNavigationState();
    }

    /**
     * Navigate to next component
     */
    navigateNext() {
        this.currentComponentIndex = this.currentComponentIndex < this.componentIds.length - 1
            ? this.currentComponentIndex + 1
            : 0;
        this.updateCarouselView();
        this.updateNavigationState();
    }

    /**
     * Navigate to previous component in modal
     */
    navigatePrevInModal() {
        this.navigatePrev();
        const currentId = this.componentIds[this.currentComponentIndex];
        this.previewComponent(currentId);
    }

    /**
     * Navigate to next component in modal
     */
    navigateNextInModal() {
        this.navigateNext();
        const currentId = this.componentIds[this.currentComponentIndex];
        this.previewComponent(currentId);
    }

    /**
     * Select specific component
     */
    selectComponent(index) {
        this.currentComponentIndex = index;
        this.updateCarouselView();
        this.updateNavigationState();

        const componentId = this.componentIds[index];
        this.previewComponent(componentId);
    }

    /**
     * Update carousel view
     */
    updateCarouselView() {
        const items = document.querySelectorAll('.carousel-item');
        items.forEach((item, index) => {
            item.classList.toggle('active', index === this.currentComponentIndex);
        });

        // Scroll to active item
        const activeItem = document.querySelector('.carousel-item.active');
        if (activeItem) {
            activeItem.scrollIntoView({
                behavior: 'smooth',
                block: 'nearest',
                inline: 'center'
            });
        }
    }

    /**
     * Update navigation state indicators
     */
    updateNavigationState() {
        const currentIndexElement = document.getElementById('currentComponentIndex');
        const totalComponentsElement = document.getElementById('totalComponents');

        if (currentIndexElement) {
            currentIndexElement.textContent = this.currentComponentIndex + 1;
        }
        if (totalComponentsElement) {
            totalComponentsElement.textContent = this.componentIds.length;
        }

        // Update button states
        const prevBtn = document.querySelector('.prev-btn');
        const nextBtn = document.querySelector('.next-btn');

        if (prevBtn && nextBtn) {
            prevBtn.disabled = false; // Always enabled for circular navigation
            nextBtn.disabled = false; // Always enabled for circular navigation
        }
    }

    /**
     * Handle window resize
     */
    handleResize() {
        // Add responsive handling logic here
        console.log('📱 Window resized');
    }

    /**
     * Show error message
     */
    showError(message) {
        const errorElement = document.getElementById('errorMessage');
        if (errorElement) {
            errorElement.textContent = message;
            errorElement.style.display = 'block';

            setTimeout(() => {
                errorElement.style.display = 'none';
            }, 5000);
        }
    }

    /**
     * Public API methods
     */
    static testAll() {
        if (window.uiTesterApp) {
            window.uiTesterApp.testAllComponents();
        }
    }



    static resetTests() {
        if (window.uiTesterApp) {
            window.uiTesterApp.resetTests();
        }
    }

    static closePreview() {
        if (window.uiTesterApp) {
            window.uiTesterApp.closePreview();
        }
    }

    static previewComponent(id) {
        if (window.uiTesterApp) {
            window.uiTesterApp.previewComponent(id);
        }
    }

    static implementComponent(id) {
        if (window.uiTesterApp) {
            window.uiTesterApp.implementComponent(id);
        }
    }

    static analyzeComponent(id) {
        if (window.uiTesterApp) {
            window.uiTesterApp.analyzeComponent(id);
        }
    }

    static navigatePrev() {
        if (window.uiTesterApp) {
            window.uiTesterApp.navigatePrev();
        }
    }

    static navigateNext() {
        if (window.uiTesterApp) {
            window.uiTesterApp.navigateNext();
        }
    }

    static navigatePrevInModal() {
        if (window.uiTesterApp) {
            window.uiTesterApp.navigatePrevInModal();
        }
    }

    static navigateNextInModal() {
        if (window.uiTesterApp) {
            window.uiTesterApp.navigateNextInModal();
        }
    }

    static selectComponent(index) {
        if (window.uiTesterApp) {
            window.uiTesterApp.selectComponent(index);
        }
    }
}

// Initialize the application
window.uiTesterApp = new UITester();

// Expose UITester class globally for HTML onclick handlers
window.UITester = UITester;

console.log('📦 Main App Coordinator loaded');
