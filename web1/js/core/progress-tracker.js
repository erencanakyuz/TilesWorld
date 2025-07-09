/**
 * Progress Tracker - Manages testing progress and component states
 */

class ProgressTracker {
    constructor(totalComponents = 8) {
        this.totalComponents = totalComponents;
        this.testedComponents = new Set();
        this.componentStates = new Map();
        this.sessionStartTime = Date.now();
        this.testingMetrics = {
            totalTests: 0,
            successfulTests: 0,
            failedTests: 0,
            averageTestTime: 0,
            testTimes: []
        };
        
        this.init();
    }

    /**
     * Initialize progress tracker
     */
    init() {
        this.loadSavedProgress();
        this.setupEventListeners();
        this.initializeComponentStates();
        this.updateProgressDisplay();
        
        console.log('📊 Progress Tracker initialized');
    }

    /**
     * Load saved progress from storage
     */
    loadSavedProgress() {
        try {
            const savedProgress = localStorage.getItem(
                window.UITesterConstants?.STORAGE_KEYS?.TESTED_COMPONENTS || 'ui-tester-tested'
            );
            
            if (savedProgress) {
                const parsed = JSON.parse(savedProgress);
                this.testedComponents = new Set(parsed.components || []);
                this.testingMetrics = { ...this.testingMetrics, ...parsed.metrics };
                
                console.log('📊 Loaded saved progress:', this.testedComponents.size, 'components tested');
            }
        } catch (error) {
            console.warn('⚠️ Failed to load saved progress:', error);
        }
    }

    /**
     * Save progress to storage
     */
    saveProgress() {
        try {
            const progressData = {
                components: Array.from(this.testedComponents),
                metrics: this.testingMetrics,
                lastSaved: Date.now()
            };
            
            localStorage.setItem(
                window.UITesterConstants?.STORAGE_KEYS?.TESTED_COMPONENTS || 'ui-tester-tested',
                JSON.stringify(progressData)
            );
        } catch (error) {
            console.warn('⚠️ Failed to save progress:', error);
        }
    }

    /**
     * Setup event listeners
     */
    setupEventListeners() {
        // Listen for component test events
        document.addEventListener('component:tested', (event) => {
            this.markComponentTested(event.detail.componentId, event.detail.success);
        });

        // Listen for component preview events
        document.addEventListener('component:preview', (event) => {
            this.updateComponentState(event.detail.componentId, 'previewing');
        });

        // Save progress before page unload
        window.addEventListener('beforeunload', () => {
            this.saveProgress();
        });

        // Periodic auto-save
        setInterval(() => {
            this.saveProgress();
        }, 30000); // Save every 30 seconds
    }

    /**
     * Initialize component states
     */
    initializeComponentStates() {
        const componentIds = window.UITesterConstants?.COMPONENT_IDS || [
            'world-tour', 'main-menu', 'hud', 'level-complete',
            'shop', 'settings', 'powerups', 'leaderboard'
        ];

        componentIds.forEach((id, index) => {
            const isAlreadyTested = this.testedComponents.has(id);
            
            this.componentStates.set(id, {
                id,
                index: index + 1,
                status: isAlreadyTested ? 'complete' : 'pending',
                testStartTime: null,
                testEndTime: null,
                testDuration: null,
                attempts: 0,
                lastError: null
            });

            // Update UI status indicator
            this.updateStatusIndicator(id, isAlreadyTested ? 'complete' : 'pending');
        });
    }

    /**
     * Mark a component as tested
     */
    markComponentTested(componentId, success = true, testDuration = null) {
        const state = this.componentStates.get(componentId);
        if (!state) {
            console.warn(`⚠️ Component ${componentId} not found in states`);
            return;
        }

        // Update test metrics
        this.testingMetrics.totalTests++;
        if (success) {
            this.testingMetrics.successfulTests++;
            this.testedComponents.add(componentId);
            
            // Update component state
            state.status = 'complete';
            state.testEndTime = Date.now();
            
            if (testDuration) {
                state.testDuration = testDuration;
                this.testingMetrics.testTimes.push(testDuration);
                this.updateAverageTestTime();
            }
        } else {
            this.testingMetrics.failedTests++;
            state.status = 'error';
            state.attempts++;
        }

        // Update UI
        this.updateStatusIndicator(componentId, state.status);
        this.updateProgressDisplay();
        this.updateCompletionStats();

        // Save progress
        this.saveProgress();

        // Emit progress update event
        this.emitProgressUpdate();

        console.log(`📊 Component ${componentId} marked as ${success ? 'tested' : 'failed'}`);
    }

    /**
     * Update component state
     */
    updateComponentState(componentId, status, additionalData = {}) {
        const state = this.componentStates.get(componentId);
        if (!state) return;

        // Update state
        const previousStatus = state.status;
        state.status = status;
        Object.assign(state, additionalData);

        // Handle status-specific updates
        switch (status) {
            case 'testing':
                state.testStartTime = Date.now();
                state.attempts++;
                break;
            case 'previewing':
                // Just update the status, no additional metrics needed
                break;
            case 'error':
                state.lastError = additionalData.error || 'Unknown error';
                break;
        }

        // Update UI if status changed
        if (previousStatus !== status) {
            this.updateStatusIndicator(componentId, status);
        }
    }

    /**
     * Update status indicator in UI
     */
    updateStatusIndicator(componentId, status) {
        const state = this.componentStates.get(componentId);
        if (!state) return;

        const indicator = document.getElementById(`status-${state.index}`);
        if (!indicator) return;

        // Remove all status classes
        indicator.className = indicator.className.replace(/status-\w+/g, '');
        
        // Add new status class
        indicator.classList.add(`status-indicator`, `status-${status}`);

        // Add animation for state changes
        if (status === 'testing') {
            indicator.classList.add('animate-pulse');
        } else {
            indicator.classList.remove('animate-pulse');
        }
    }

    /**
     * Update progress display
     */
    updateProgressDisplay() {
        const progress = this.getCompletionPercentage();
        const progressBar = document.getElementById('overallProgress');
        
        if (progressBar) {
            progressBar.style.width = `${progress}%`;
            
            // Add completion milestone effects
            if (progress === 100 && this.testedComponents.size === this.totalComponents) {
                this.celebrateCompletion();
            }
        }

        // Update any progress text elements
        const progressText = document.querySelector('.progress-text');
        if (progressText) {
            progressText.textContent = `${this.testedComponents.size}/${this.totalComponents} components tested`;
        }
    }

    /**
     * Update completion statistics
     */
    updateCompletionStats() {
        const stats = this.getDetailedStats();
        
        // Update stats display if exists
        const statsElement = document.querySelector('.testing-stats');
        if (statsElement) {
            statsElement.innerHTML = `
                <div class="stat-item">
                    <span class="stat-label">Completed:</span>
                    <span class="stat-value">${stats.completed}/${stats.total}</span>
                </div>
                <div class="stat-item">
                    <span class="stat-label">Success Rate:</span>
                    <span class="stat-value">${stats.successRate}%</span>
                </div>
                <div class="stat-item">
                    <span class="stat-label">Avg Test Time:</span>
                    <span class="stat-value">${stats.averageTime}ms</span>
                </div>
            `;
        }
    }

    /**
     * Calculate average test time
     */
    updateAverageTestTime() {
        const times = this.testingMetrics.testTimes;
        if (times.length > 0) {
            this.testingMetrics.averageTestTime = 
                times.reduce((sum, time) => sum + time, 0) / times.length;
        }
    }

    /**
     * Get completion percentage
     */
    getCompletionPercentage() {
        return Math.round((this.testedComponents.size / this.totalComponents) * 100);
    }

    /**
     * Get detailed statistics
     */
    getDetailedStats() {
        const completed = this.testedComponents.size;
        const total = this.totalComponents;
        const successRate = this.testingMetrics.totalTests > 0 
            ? Math.round((this.testingMetrics.successfulTests / this.testingMetrics.totalTests) * 100)
            : 0;
        
        return {
            completed,
            total,
            percentage: this.getCompletionPercentage(),
            successRate,
            failureRate: 100 - successRate,
            averageTime: Math.round(this.testingMetrics.averageTestTime),
            totalTests: this.testingMetrics.totalTests,
            sessionDuration: Date.now() - this.sessionStartTime,
            componentStates: Object.fromEntries(this.componentStates)
        };
    }

    /**
     * Get components by status
     */
    getComponentsByStatus(status) {
        const components = [];
        for (const [id, state] of this.componentStates) {
            if (state.status === status) {
                components.push({ id, ...state });
            }
        }
        return components;
    }

    /**
     * Get next component to test
     */
    getNextComponentToTest() {
        const pendingComponents = this.getComponentsByStatus('pending');
        if (pendingComponents.length === 0) return null;
        
        // Return the first pending component (or implement priority logic)
        return pendingComponents[0];
    }

    /**
     * Reset progress
     */
    reset() {
        this.testedComponents.clear();
        this.componentStates.clear();
        this.testingMetrics = {
            totalTests: 0,
            successfulTests: 0,
            failedTests: 0,
            averageTestTime: 0,
            testTimes: []
        };
        
        // Reinitialize
        this.initializeComponentStates();
        this.updateProgressDisplay();
        this.saveProgress();
        
        // Emit reset event
        this.emitEvent('progress:reset');
        
        console.log('📊 Progress reset');
    }

    /**
     * Generate progress report
     */
    generateReport() {
        const stats = this.getDetailedStats();
        const sessionTime = this.formatDuration(stats.sessionDuration);
        
        const report = {
            summary: {
                totalComponents: this.totalComponents,
                completedComponents: stats.completed,
                completionPercentage: stats.percentage,
                successRate: stats.successRate,
                sessionDuration: sessionTime
            },
            componentDetails: [],
            recommendations: [],
            metrics: this.testingMetrics
        };

        // Add component details
        for (const [id, state] of this.componentStates) {
            report.componentDetails.push({
                id,
                title: window.ComponentData?.titles?.[id] || id,
                status: state.status,
                attempts: state.attempts,
                testDuration: state.testDuration,
                priority: window.ComponentData?.priority?.[id] || 3
            });
        }

        // Generate recommendations
        report.recommendations = this.generateRecommendations(stats);

        return report;
    }

    /**
     * Generate recommendations based on progress
     */
    generateRecommendations(stats) {
        const recommendations = [];
        
        if (stats.completed < stats.total) {
            const remaining = stats.total - stats.completed;
            recommendations.push(`Continue testing ${remaining} remaining component${remaining > 1 ? 's' : ''}`);
        }
        
        if (stats.successRate < 100 && this.testingMetrics.failedTests > 0) {
            recommendations.push('Review failed tests and retry components that encountered errors');
        }
        
        if (stats.averageTime > 5000) {
            recommendations.push('Consider optimizing test performance - average test time is high');
        }
        
        if (stats.completed === stats.total) {
            recommendations.push('All components tested! Ready to proceed with Unity implementation');
        }
        
        return recommendations;
    }

    /**
     * Celebrate completion
     */
    celebrateCompletion() {
        // Add completion animation to progress bar
        const progressBar = document.getElementById('overallProgress');
        if (progressBar) {
            progressBar.classList.add('animate-glow');
            progressBar.style.background = 'linear-gradient(90deg, #2ECC71, #F1C40F)';
        }

        // Show completion notification
        this.showCompletionNotification();

        // Emit completion event
        this.emitEvent('progress:complete', this.generateReport());
        
        console.log('🎉 All components tested successfully!');
    }

    /**
     * Show completion notification
     */
    showCompletionNotification() {
        // Create notification element if it doesn't exist
        let notification = document.querySelector('.completion-notification');
        if (!notification) {
            notification = document.createElement('div');
            notification.className = 'completion-notification';
            notification.innerHTML = `
                <div class="notification-content">
                    <div class="notification-icon">🎉</div>
                    <div class="notification-text">
                        <h3>Testing Complete!</h3>
                        <p>All ${this.totalComponents} components have been tested successfully.</p>
                    </div>
                    <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
                </div>
            `;
            
            // Add styles
            notification.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                background: linear-gradient(135deg, #2ECC71, #27AE60);
                color: white;
                padding: 20px;
                border-radius: 15px;
                box-shadow: 0 10px 30px rgba(46, 204, 113, 0.3);
                z-index: 1000;
                animation: slideInRight 0.5s ease-out;
            `;
            
            document.body.appendChild(notification);
            
            // Auto-remove after 5 seconds
            setTimeout(() => {
                if (notification.parentElement) {
                    notification.remove();
                }
            }, 5000);
        }
    }

    /**
     * Format duration in milliseconds to human readable
     */
    formatDuration(ms) {
        const seconds = Math.floor(ms / 1000);
        const minutes = Math.floor(seconds / 60);
        const hours = Math.floor(minutes / 60);
        
        if (hours > 0) {
            return `${hours}h ${minutes % 60}m ${seconds % 60}s`;
        } else if (minutes > 0) {
            return `${minutes}m ${seconds % 60}s`;
        } else {
            return `${seconds}s`;
        }
    }

    /**
     * Emit custom events
     */
    emitEvent(eventName, data = {}) {
        const event = new CustomEvent(eventName, { 
            detail: { 
                ...data, 
                timestamp: Date.now(),
                progressTracker: this 
            } 
        });
        document.dispatchEvent(event);
    }

    /**
     * Emit progress update event
     */
    emitProgressUpdate() {
        this.emitEvent('progress:update', {
            completed: this.testedComponents.size,
            total: this.totalComponents,
            percentage: this.getCompletionPercentage(),
            stats: this.getDetailedStats()
        });
    }

    /**
     * Export progress data
     */
    exportProgress() {
        const report = this.generateReport();
        const dataStr = JSON.stringify(report, null, 2);
        const dataBlob = new Blob([dataStr], { type: 'application/json' });
        
        const link = document.createElement('a');
        link.href = URL.createObjectURL(dataBlob);
        link.download = `ui-testing-progress-${new Date().toISOString().split('T')[0]}.json`;
        link.click();
        
        console.log('📊 Progress data exported');
    }

    /**
     * Import progress data
     */
    importProgress(jsonData) {
        try {
            const data = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;
            
            if (data.summary && data.componentDetails) {
                // Import component states
                data.componentDetails.forEach(component => {
                    if (component.status === 'complete') {
                        this.testedComponents.add(component.id);
                    }
                    
                    const state = this.componentStates.get(component.id);
                    if (state) {
                        Object.assign(state, {
                            status: component.status,
                            attempts: component.attempts || 0,
                            testDuration: component.testDuration
                        });
                    }
                });
                
                // Import metrics if available
                if (data.metrics) {
                    this.testingMetrics = { ...this.testingMetrics, ...data.metrics };
                }
                
                // Update UI
                this.updateProgressDisplay();
                this.initializeComponentStates();
                this.saveProgress();
                
                console.log('📊 Progress data imported successfully');
                return true;
            }
        } catch (error) {
            console.error('❌ Failed to import progress data:', error);
            return false;
        }
    }

    /**
     * Get component testing time estimate
     */
    getEstimatedTimeRemaining() {
        const remaining = this.totalComponents - this.testedComponents.size;
        if (remaining === 0) return 0;
        
        const avgTime = this.testingMetrics.averageTestTime || 3000; // Default 3 seconds
        return remaining * avgTime;
    }

    /**
     * Cleanup and destroy
     */
    destroy() {
        this.saveProgress();
        console.log('📊 Progress Tracker destroyed');
    }
}

// Export for global use
window.ProgressTracker = ProgressTracker;

console.log('📊 Progress Tracker loaded');
