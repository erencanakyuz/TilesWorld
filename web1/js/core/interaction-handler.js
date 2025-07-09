/**
 * Interaction Handler - Manages all interactive behaviors and user interactions
 */

class InteractionHandler {
    constructor() {
        this.activeInteractions = new Map();
        this.eventListeners = new Map();
        this.touchStartTime = 0;
        this.touchStartPosition = { x: 0, y: 0 };
        this.isInitialized = false;

        this.init();
    }

    /**
     * Initialize interaction handler
     */
    init() {
        if (this.isInitialized) return;

        this.setupGlobalListeners();
        this.setupComponentInteractions();
        this.setupAccessibilityFeatures();
        this.setupPerformanceOptimizations();

        this.isInitialized = true;
        console.log('🎮 Interaction Handler initialized');
    }

    /**
     * Setup global event listeners
     */
    setupGlobalListeners() {
        // Click handlers
        document.addEventListener('click', this.handleGlobalClick.bind(this));

        // Touch handlers for mobile
        document.addEventListener('touchstart', this.handleTouchStart.bind(this), { passive: false });
        document.addEventListener('touchend', this.handleTouchEnd.bind(this), { passive: false });
        document.addEventListener('touchmove', this.handleTouchMove.bind(this), { passive: false });

        // Mouse handlers for desktop
        document.addEventListener('mousedown', this.handleMouseDown.bind(this));
        document.addEventListener('mouseup', this.handleMouseUp.bind(this));
        document.addEventListener('mouseover', this.handleMouseOver.bind(this));
        document.addEventListener('mouseout', this.handleMouseOut.bind(this));

        // Keyboard handlers
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
        document.addEventListener('keyup', this.handleKeyUp.bind(this));

        // Focus handlers
        document.addEventListener('focusin', this.handleFocusIn.bind(this));
        document.addEventListener('focusout', this.handleFocusOut.bind(this));

        // Window events
        window.addEventListener('resize', this.handleResize.bind(this));
        window.addEventListener('orientationchange', this.handleOrientationChange.bind(this));
    }

    /**
     * Setup component-specific interactions
     */
    setupComponentInteractions() {
        // Main menu interactions
        this.registerInteraction('.primary-btn', {
            click: this.handlePrimaryButton.bind(this),
            hover: this.handleButtonHover.bind(this)
        });

        this.registerInteraction('.secondary-btn', {
            click: this.handleSecondaryButton.bind(this),
            hover: this.handleButtonHover.bind(this)
        });

        // Quick actions
        this.registerInteraction('.quick-action', {
            click: this.handleQuickAction.bind(this),
            hover: this.handleIconHover.bind(this)
        });

        // Game logo
        this.registerInteraction('.game-logo', {
            hover: this.handleLogoHover.bind(this),
            leave: this.handleLogoLeave.bind(this)
        });

        // World Tour interactions
        this.registerInteraction('.location-pin', {
            click: this.handleLocationPin.bind(this),
            hover: this.handlePinHover.bind(this)
        });

        this.registerInteraction('.globe', {
            mousedown: this.handleGlobeMouseDown.bind(this),
            mousemove: this.handleGlobeMouseMove.bind(this),
            mouseup: this.handleGlobeMouseUp.bind(this)
        });

        this.registerInteraction('.zoom-btn', {
            click: this.handleZoomButton.bind(this)
        });

        // Customization interactions
        this.registerInteraction('.category-tab', {
            click: this.handleCategoryTab.bind(this)
        });

        this.registerInteraction('.feature-card', {
            click: this.handleFeatureCard.bind(this),
            hover: this.handleCardHover.bind(this)
        });

        this.registerInteraction('.nav-arrow', {
            click: this.handleNavigationArrow.bind(this)
        });

        // Shop interactions
        this.registerInteraction('.upgrade-card', {
            click: this.handleUpgradeCard.bind(this),
            hover: this.handleCardHover.bind(this)
        });

        this.registerInteraction('.display-pedestal', {
            hover: this.handlePedestalHover.bind(this),
            leave: this.handlePedestalLeave.bind(this)
        });

        // Leaderboard interactions
        this.registerInteraction('.tab-new', {
            click: this.handleLeaderboardTab.bind(this)
        });

        this.registerInteraction('.action-btn', {
            click: this.handleActionButton.bind(this),
            hover: this.handleButtonHover.bind(this)
        });

        // Settings interactions
        this.registerInteraction('.toggle-switch', {
            click: this.handleToggleSwitch.bind(this)
        });

        // Test buttons
        this.registerInteraction('.btn-test', {
            click: this.handleTestButton.bind(this),
            hover: this.handleButtonHover.bind(this)
        });
    }

    /**
     * Register interaction for a selector
     */
    registerInteraction(selector, handlers) {
        this.eventListeners.set(selector, handlers);
    }

    /**
     * Handle global click events
     */
    handleGlobalClick(event) {
        const target = event.target;

        // Find matching selectors
        for (const [selector, handlers] of this.eventListeners) {
            if (target.matches(selector) || target.closest(selector)) {
                const element = target.matches(selector) ? target : target.closest(selector);

                if (handlers.click) {
                    event.preventDefault();
                    handlers.click(event, element);
                }
                break;
            }
        }

        // Handle modal backdrop clicks
        if (target.id === 'previewModal') {
            this.closeModal();
        }
    }

    /**
     * Handle touch start
     */
    handleTouchStart(event) {
        this.touchStartTime = Date.now();
        this.touchStartPosition = {
            x: event.touches[0].clientX,
            y: event.touches[0].clientY
        };

        // Add visual feedback for touch
        const target = event.target.closest('[data-touch-feedback]') || event.target;
        if (this.isTouchableElement(target)) {
            this.addTouchFeedback(target);
        }
    }

    /**
     * Handle touch end
     */
    handleTouchEnd(event) {
        const touchDuration = Date.now() - this.touchStartTime;
        const target = event.target;

        // Remove touch feedback
        this.removeTouchFeedback(target);

        // Handle tap vs long press
        if (touchDuration < 500) {
            this.handleTap(event, target);
        } else {
            this.handleLongPress(event, target);
        }
    }

    /**
     * Handle touch move
     */
    handleTouchMove(event) {
        // Handle globe rotation on touch devices
        if (event.target.closest('.globe')) {
            this.handleGlobeTouchMove(event);
        }
    }

    /**
     * Handle mouse down
     */
    handleMouseDown(event) {
        const target = event.target;

        // Add active state
        if (this.isInteractiveElement(target)) {
            target.classList.add('active');
        }
    }

    /**
     * Handle mouse up
     */
    handleMouseUp(event) {
        const target = event.target;

        // Remove active state
        if (this.isInteractiveElement(target)) {
            target.classList.remove('active');
        }
    }

    /**
     * Handle mouse over (hover)
     */
    handleMouseOver(event) {
        const target = event.target;

        // Find matching hover handlers
        for (const [selector, handlers] of this.eventListeners) {
            if (target.matches(selector) || target.closest(selector)) {
                const element = target.matches(selector) ? target : target.closest(selector);

                if (handlers.hover) {
                    handlers.hover(event, element);
                }
                break;
            }
        }
    }

    /**
     * Handle mouse out (leave)
     */
    handleMouseOut(event) {
        const target = event.target;

        // Find matching leave handlers
        for (const [selector, handlers] of this.eventListeners) {
            if (target.matches(selector) || target.closest(selector)) {
                const element = target.matches(selector) ? target : target.closest(selector);

                if (handlers.leave) {
                    handlers.leave(event, element);
                }
                break;
            }
        }
    }

    /**
     * Component-specific interaction handlers
     */

    handlePrimaryButton(event, element) {
        this.animateButton(element, 'primary');

        // Emit analytics event
        this.emitAnalyticsEvent('button_click', {
            type: 'primary',
            text: element.textContent.trim()
        });
    }

    handleSecondaryButton(event, element) {
        this.animateButton(element, 'secondary');

        this.emitAnalyticsEvent('button_click', {
            type: 'secondary',
            text: element.textContent.trim()
        });
    }

    handleQuickAction(event, element) {
        this.animateIcon(element);

        // Add notification badge animation if present
        const badge = element.querySelector('.notification-badge');
        if (badge) {
            badge.classList.add('animate-bounce');
            setTimeout(() => badge.classList.remove('animate-bounce'), 600);
        }
    }

    handleLocationPin(event, element) {
        if (element.classList.contains('locked')) return;

        // Remove active from all pins
        document.querySelectorAll('.location-pin').forEach(pin => {
            pin.classList.remove('active');
        });

        // Make clicked pin active
        element.classList.add('active');

        // Update location details
        const location = element.getAttribute('data-location');
        this.updateLocationDetails(location);

        this.emitAnalyticsEvent('location_selected', { location });
    }

    handleGlobeMouseDown(event, element) {
        element.classList.add('dragging');
        element.style.animationPlayState = 'paused';
        this.globeDragging = true;
    }

    handleGlobeMouseMove(event) {
        if (!this.globeDragging) return;

        // Handle globe rotation based on mouse movement
        const globe = document.querySelector('.globe');
        if (globe) {
            const deltaX = event.movementX;
            const currentRotation = parseInt(globe.style.transform.replace(/[^\d]/g, '')) || 0;
            globe.style.transform = `rotateY(${currentRotation + deltaX}deg)`;
        }
    }

    handleGlobeMouseUp(event, element) {
        element.classList.remove('dragging');
        this.globeDragging = false;

        // Resume animation after 2 seconds
        setTimeout(() => {
            element.style.animationPlayState = 'running';
        }, 2000);
    }

    handleCategoryTab(event, element) {
        // Remove active from all tabs
        document.querySelectorAll('.category-tab').forEach(tab => {
            tab.classList.remove('active');
        });

        // Make clicked tab active
        element.classList.add('active');

        // Update content based on category
        const category = element.getAttribute('data-category');
        this.updateCategoryContent(category);

        this.emitAnalyticsEvent('category_changed', { category });
    }

    handleFeatureCard(event, element) {
        if (element.classList.contains('locked')) {
            this.showUnlockRequirement(element);
            return;
        }

        this.animateCard(element);

        const featureId = element.getAttribute('data-feature-id');
        this.emitAnalyticsEvent('feature_selected', { featureId });
    }

    handleToggleSwitch(event, element) {
        element.classList.toggle('active');

        const settingName = element.getAttribute('data-setting');
        const isActive = element.classList.contains('active');

        // Save setting
        this.saveSetting(settingName, isActive);

        this.emitAnalyticsEvent('setting_changed', {
            setting: settingName,
            value: isActive
        });
    }

    saveSetting(settingName, value) {
        // Save to localStorage
        const settings = JSON.parse(localStorage.getItem('ui-tester-settings') || '{}');
        settings[settingName] = value;
        localStorage.setItem('ui-tester-settings', JSON.stringify(settings));

        console.log(`💾 Setting saved: ${settingName} = ${value}`);
    }

    updateLocationDetails(location) {
        // Update location information in UI
        const locationInfo = {
            vienna: { name: 'Vienna', level: 'Beginner', stars: 3 },
            paris: { name: 'Paris', level: 'Intermediate', stars: 2 },
            london: { name: 'London', level: 'Advanced', stars: 4 },
            tokyo: { name: 'Tokyo', level: 'Expert', stars: 5 },
            'new-york': { name: 'New York', level: 'Master', stars: 1 },
            sydney: { name: 'Sydney', level: 'Legendary', stars: 3 }
        };

        const info = locationInfo[location] || { name: location, level: 'Unknown', stars: 0 };
        console.log(`🗺️ Selected location: ${info.name} (${info.level} - ${info.stars} stars)`);

        // Could update a location details panel here
        const detailsPanel = document.querySelector('.location-details');
        if (detailsPanel) {
            detailsPanel.innerHTML = `
                <h4>${info.name}</h4>
                <p>Difficulty: ${info.level}</p>
                <p>Stars: ${'⭐'.repeat(info.stars)}</p>
            `;
        }
    }

    updateCategoryContent(category) {
        // Update category content display
        const categories = {
            pianos: 'Piano Collection',
            styles: 'Music Styles',
            effects: 'Sound Effects',
            themes: 'Visual Themes'
        };

        const categoryName = categories[category] || category;
        console.log(`🏷️ Switched to category: ${categoryName}`);

        // Could update category content here
        const contentArea = document.querySelector('.category-content');
        if (contentArea) {
            contentArea.innerHTML = `<h4>Showing: ${categoryName}</h4>`;
        }
    }

    showUnlockRequirement(element) {
        const requirement = element.getAttribute('data-unlock-requirement') || 'Complete previous levels';
        console.log(`🔒 Unlock requirement: ${requirement}`);

        // Could show a tooltip or modal here
        if (element.title) {
            element.title = `Unlock requirement: ${requirement}`;
        }
    }

    handleTestButton(event, element) {
        this.animateButton(element, 'test');

        // Prevent double clicks
        element.disabled = true;
        setTimeout(() => {
            element.disabled = false;
        }, 1000);
    }

    handleZoomButton(event, element) {
        const action = element.dataset.action || 'zoom-in';
        const globe = document.querySelector('.globe');

        if (!globe) return;

        let currentScale = parseFloat(globe.style.transform?.match(/scale\(([^)]+)\)/)?.[1] || 1);

        if (action === 'zoom-in') {
            currentScale = Math.min(currentScale * 1.2, 2);
        } else if (action === 'zoom-out') {
            currentScale = Math.max(currentScale * 0.8, 0.5);
        }

        globe.style.transform = `scale(${currentScale})`;

        this.animateButton(element);

        this.emitAnalyticsEvent('globe_zoom', {
            action: action,
            scale: currentScale
        });
    }

    handleNavigationArrow(event, element) {
        const direction = element.dataset.direction || 'next';
        const container = element.closest('.feature-container') || element.closest('.shop-container');

        if (!container) return;

        const scrollContainer = container.querySelector('.feature-grid') || container.querySelector('.upgrade-grid');
        if (!scrollContainer) return;

        const scrollAmount = 300;
        const currentScroll = scrollContainer.scrollLeft;

        if (direction === 'next') {
            scrollContainer.scrollTo({
                left: currentScroll + scrollAmount,
                behavior: 'smooth'
            });
        } else {
            scrollContainer.scrollTo({
                left: currentScroll - scrollAmount,
                behavior: 'smooth'
            });
        }

        this.animateButton(element);
    }

    handleUpgradeCard(event, element) {
        // Toggle selected state
        const isSelected = element.classList.contains('selected');

        // Remove selection from other cards in the same group
        const group = element.closest('.upgrade-grid');
        if (group) {
            group.querySelectorAll('.upgrade-card.selected').forEach(card => {
                if (card !== element) card.classList.remove('selected');
            });
        }

        // Toggle current card
        element.classList.toggle('selected', !isSelected);

        this.animateCard(element);

        this.emitAnalyticsEvent('upgrade_selected', {
            upgrade: element.dataset.upgrade || 'unknown',
            selected: !isSelected
        });
    }

    handleLeaderboardTab(event, element) {
        const tabType = element.dataset.tab || 'global';

        // Remove active from all tabs
        const tabContainer = element.closest('.leaderboard-tabs');
        if (tabContainer) {
            tabContainer.querySelectorAll('.tab-new.active').forEach(tab => {
                tab.classList.remove('active');
            });
        }

        // Activate current tab
        element.classList.add('active');

        // Switch content (this would normally load different leaderboard data)
        this.switchLeaderboardContent(tabType);

        this.emitAnalyticsEvent('leaderboard_tab', {
            tab: tabType
        });
    }

    handleActionButton(event, element) {
        const action = element.dataset.action || 'default';

        this.animateButton(element, 'action');

        // Handle different action types
        switch (action) {
            case 'challenge':
                this.handleChallengeAction(element);
                break;
            case 'share':
                this.handleShareAction(element);
                break;
            case 'follow':
                this.handleFollowAction(element);
                break;
            case 'default':
                this.handleDefaultAction(element);
                break;
            default:
                console.log('🔄 Generic action triggered:', action);
        }
    }

    handleDefaultAction(element) {
        // Handle default action
        console.log('🎯 Default action triggered');

        // Add visual feedback
        element.style.transform = 'scale(0.95)';
        setTimeout(() => {
            element.style.transform = '';
        }, 150);
    }

    switchLeaderboardContent(tabType) {
        // This would switch the leaderboard content
        console.log('Switching to leaderboard tab:', tabType);
    }

    handleChallengeAction(element) {
        console.log('Challenge action triggered');
    }

    handleShareAction(element) {
        console.log('Share action triggered');
    }

    handleFollowAction(element) {
        const isFollowing = element.classList.contains('following');
        element.classList.toggle('following', !isFollowing);
        element.textContent = isFollowing ? 'Follow' : 'Following';
    }

    /**
     * Animation helpers
     */
    animateButton(element, type = 'default') {
        const animations = {
            primary: ['animate-scale-in', 'animate-glow'],
            secondary: ['animate-fade-in-up'],
            test: ['animate-bounce-in'],
            default: ['animate-pulse']
        };

        const animationClasses = animations[type] || animations.default;

        // Add animation classes
        animationClasses.forEach(cls => element.classList.add(cls));

        // Remove after animation
        setTimeout(() => {
            animationClasses.forEach(cls => element.classList.remove(cls));
        }, 600);
    }

    animateIcon(element) {
        element.style.transform = 'scale(0.9)';
        element.style.background = 'rgba(255,255,255,0.3)';

        setTimeout(() => {
            element.style.transform = 'scale(1.05)';
            element.style.background = '';
        }, 150);

        setTimeout(() => {
            element.style.transform = '';
        }, 300);
    }

    animateCard(element) {
        element.style.transform = 'translateY(-2px) scale(0.98)';

        setTimeout(() => {
            element.style.transform = 'translateY(-5px) scale(1)';
        }, 150);

        setTimeout(() => {
            element.style.transform = '';
        }, 300);
    }

    /**
     * Hover effect handlers
     */
    handleButtonHover(event, element) {
        if (element.disabled) return;

        element.style.transform = 'translateY(-2px)';
        element.style.boxShadow = '0 6px 20px rgba(0,0,0,0.2)';
    }

    handleIconHover(event, element) {
        element.style.transform = 'scale(1.1)';
        element.style.background = 'rgba(255,255,255,0.25)';
    }

    handleLogoHover(event, element) {
        element.style.transform = 'scale(1.1) rotate(5deg)';
        element.style.boxShadow = '0 0 60px rgba(255,107,107,0.8), 0 0 100px rgba(215,0,158,0.5)';
    }

    handleLogoLeave(event, element) {
        element.style.transform = 'scale(1) rotate(0deg)';
        element.style.boxShadow = '0 0 40px rgba(255,107,107,0.5), 0 0 80px rgba(215,0,158,0.3)';
    }

    handleCardHover(event, element) {
        if (element.classList.contains('locked')) return;

        element.style.transform = 'translateY(-3px)';
        element.style.boxShadow = '0 12px 30px rgba(0,0,0,0.2)';
    }

    handlePinHover(event, element) {
        if (element.classList.contains('locked')) return;

        element.style.transform = 'scale(1.2)';
        element.style.zIndex = '20';
    }

    handlePedestalHover(event, element) {
        element.style.transform = 'translateY(-5px) scale(1.05)';
        element.style.boxShadow = '0 8px 25px rgba(0,0,0,0.2)';
    }

    handlePedestalLeave(event, element) {
        element.style.transform = 'translateY(0) scale(1)';
        element.style.boxShadow = '0 4px 15px rgba(0,0,0,0.1)';
    }

    /**
     * Utility functions
     */
    isTouchableElement(element) {
        const touchableSelectors = [
            'button', '.btn', '.card', '.icon', '.toggle', '.tab'
        ];

        return touchableSelectors.some(selector =>
            element.matches(selector) || element.closest(selector)
        );
    }

    isInteractiveElement(element) {
        const interactiveSelectors = [
            'button', 'a', '[role="button"]', '.clickable'
        ];

        return interactiveSelectors.some(selector =>
            element.matches(selector)
        );
    }

    addTouchFeedback(element) {
        element.classList.add('touch-active');
        element.style.transform = 'scale(0.95)';
        element.style.opacity = '0.8';
    }

    removeTouchFeedback(element) {
        element.classList.remove('touch-active');
        element.style.transform = '';
        element.style.opacity = '';
    }

    /**
     * Accessibility features
     */
    setupAccessibilityFeatures() {
        // Keyboard navigation
        this.setupKeyboardNavigation();

        // Screen reader support
        this.setupScreenReaderSupport();

        // Focus management
        this.setupFocusManagement();
    }

    setupScreenReaderSupport() {
        // Add ARIA labels and descriptions
        document.querySelectorAll('.btn, .card, .icon').forEach(element => {
            if (!element.getAttribute('aria-label') && !element.getAttribute('aria-labelledby')) {
                const text = element.textContent.trim() || element.dataset.action || 'Interactive element';
                element.setAttribute('aria-label', text);
            }
        });
    }

    setupFocusManagement() {
        // Ensure proper focus indicators
        document.querySelectorAll('button, [role="button"], .clickable').forEach(element => {
            element.setAttribute('tabindex', '0');
        });
    }

    handleTabNavigation(event) {
        // Custom tab navigation logic if needed
        console.log('Tab navigation');
    }

    handleKeyboardActivation(event) {
        const target = event.target;
        if (target.matches('button, [role="button"], .clickable')) {
            target.click();
        }
    }

    handleEscapeKey(event) {
        // Close modals or cancel operations
        const modal = document.getElementById('previewModal');
        if (modal && modal.style.display !== 'none') {
            this.closeModal();
        }
    }

    handleArrowNavigation(event) {
        // Custom arrow key navigation for components
        console.log('Arrow navigation:', event.key);
    }

    openCommandPalette() {
        console.log('Command palette opened');
    }

    printReport() {
        window.print();
    }

    closeModal() {
        const modal = document.getElementById('previewModal');
        if (modal) {
            modal.style.display = 'none';
        }
    }

    setupKeyboardNavigation() {
        document.addEventListener('keydown', (event) => {
            // Tab navigation
            if (event.key === 'Tab') {
                this.handleTabNavigation(event);
            }

            // Enter/Space activation
            if (event.key === 'Enter' || event.key === ' ') {
                this.handleKeyboardActivation(event);
            }

            // Escape key
            if (event.key === 'Escape') {
                this.handleEscapeKey(event);
            }

            // Arrow key navigation
            if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(event.key)) {
                this.handleArrowNavigation(event);
            }
        });
    }

    handleKeyDown(event) {
        // Global keyboard shortcuts
        if (event.ctrlKey || event.metaKey) {
            switch (event.key) {
                case 'k':
                    event.preventDefault();
                    this.openCommandPalette();
                    break;
                case 'p':
                    event.preventDefault();
                    this.printReport();
                    break;
            }
        }
    }

    handleKeyUp(event) {
        // Handle key release events
    }

    handleFocusIn(event) {
        const element = event.target;

        // Add focus ring for keyboard navigation
        if (this.wasKeyboardNavigation) {
            element.classList.add('keyboard-focus');
        }
    }

    handleFocusOut(event) {
        const element = event.target;
        element.classList.remove('keyboard-focus');
    }

    /**
     * Performance optimizations
     */
    setupPerformanceOptimizations() {
        // Throttle resize events
        this.throttledResize = this.throttle(this.handleResize.bind(this), 250);

        // Debounce touch events on some elements
        this.debouncedTouch = this.debounce(this.handleTouch.bind(this), 100);

        // Use passive listeners where possible
        this.usePassiveListeners();
    }

    usePassiveListeners() {
        // Use passive listeners for better performance on touch events
        const passiveOptions = { passive: true };

        document.addEventListener('touchstart', this.handleTouchStart.bind(this), passiveOptions);
        document.addEventListener('touchmove', this.handleTouchMove.bind(this), passiveOptions);
        document.addEventListener('wheel', this.handleWheel.bind(this), passiveOptions);
    }

    handleTouch(event) {
        // Handle touch events with debouncing
        console.log('Touch event handled');
    }

    handleWheel(event) {
        // Handle wheel events (for zooming, scrolling, etc.)
        const target = event.target.closest('.globe');
        if (target) {
            const delta = event.deltaY > 0 ? 'zoom-out' : 'zoom-in';
            this.handleZoomButton(event, { dataset: { action: delta } });
        }
    }

    handleTap(event, target) {
        // Handle tap events on touch devices
        const tapEvent = new CustomEvent('tap', {
            detail: { target, originalEvent: event }
        });
        target.dispatchEvent(tapEvent);
    }

    handleLongPress(event, target) {
        // Handle long press events on touch devices
        const longPressEvent = new CustomEvent('longpress', {
            detail: { target, originalEvent: event }
        });
        target.dispatchEvent(longPressEvent);
    }

    handleGlobeTouchMove(event) {
        // Handle globe rotation on touch devices
        if (event.touches.length === 1) {
            const touch = event.touches[0];
            const deltaX = touch.clientX - (this.lastTouchX || touch.clientX);
            const deltaY = touch.clientY - (this.lastTouchY || touch.clientY);

            const globe = event.target.closest('.globe');
            if (globe) {
                const currentRotateY = parseInt(globe.style.transform?.match(/rotateY\(([^)]+)deg\)/)?.[1] || 0);
                const currentRotateX = parseInt(globe.style.transform?.match(/rotateX\(([^)]+)deg\)/)?.[1] || 0);

                globe.style.transform = `rotateY(${currentRotateY + deltaX}deg) rotateX(${currentRotateX - deltaY}deg)`;
            }

            this.lastTouchX = touch.clientX;
            this.lastTouchY = touch.clientY;
        }
    }

    throttle(func, limit) {
        let inThrottle;
        return function () {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    handleResize() {
        // Update layout calculations
        this.updateResponsiveElements();

        // Emit resize event
        this.emitEvent('window:resize', {
            width: window.innerWidth,
            height: window.innerHeight,
            breakpoint: window.UITesterConstants?.getBreakpoint()
        });
    }

    updateResponsiveElements() {
        // Update responsive UI elements based on screen size
        const isMobile = window.innerWidth <= 768;
        const isTablet = window.innerWidth > 768 && window.innerWidth <= 1024;

        document.body.classList.toggle('mobile', isMobile);
        document.body.classList.toggle('tablet', isTablet);
        document.body.classList.toggle('desktop', !isMobile && !isTablet);

        // Update globe size for responsive design
        const globe = document.querySelector('.globe');
        if (globe) {
            const size = isMobile ? '200px' : isTablet ? '250px' : '300px';
            globe.style.width = size;
            globe.style.height = size;
        }
    }

    handleOrientationChange() {
        // Handle mobile orientation changes
        setTimeout(() => {
            this.updateResponsiveElements();
        }, 100);
    }

    /**
     * Event emission and analytics
     */
    emitEvent(eventName, data = {}) {
        const event = new CustomEvent(eventName, { detail: data });
        document.dispatchEvent(event);
    }

    emitAnalyticsEvent(eventName, data = {}) {
        if (window.UITesterConstants?.isFeatureEnabled('ANALYTICS')) {
            this.emitEvent('analytics:track', {
                event: eventName,
                data: {
                    ...data,
                    timestamp: Date.now(),
                    url: window.location.href
                }
            });
        }
    }

    /**
     * Cleanup
     */
    destroy() {
        // Remove all event listeners
        this.eventListeners.clear();
        this.activeInteractions.clear();

        console.log('🎮 Interaction Handler destroyed');
    }
}

// Initialize global interaction handler
window.InteractionHandler = InteractionHandler;

console.log('🎮 Interaction Handler loaded');
