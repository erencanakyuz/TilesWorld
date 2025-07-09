/**
 * Modal Handler - Manages modal dialogs and overlays
 */

class ModalHandler {
    constructor() {
        this.activeModals = new Map();
        this.modalStack = [];
        this.focusTraps = new Map();
        this.isInitialized = false;
        this.lastFocusedElement = null;
        
        this.init();
    }

    /**
     * Initialize modal handler
     */
    init() {
        if (this.isInitialized) return;

        this.setupEventListeners();
        this.createModalStyles();
        this.setupFocusManagement();
        this.setupKeyboardNavigation();
        
        this.isInitialized = true;
        console.log('🖼️ Modal Handler initialized');
    }

    /**
     * Setup global event listeners
     */
    setupEventListeners() {
        // ESC key to close modals
        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape') {
                this.closeTopModal();
            }
        });

        // Click outside to close
        document.addEventListener('click', (event) => {
            if (event.target.classList.contains('modal-backdrop')) {
                this.closeTopModal();
            }
        });

        // Prevent body scroll when modal is open
        document.addEventListener('modal:open', () => {
            document.body.style.overflow = 'hidden';
        });

        document.addEventListener('modal:close', () => {
            if (this.modalStack.length === 0) {
                document.body.style.overflow = '';
            }
        });

        // Handle browser back button
        window.addEventListener('popstate', () => {
            if (this.modalStack.length > 0) {
                this.closeTopModal();
            }
        });
    }

    /**
     * Create modal styles dynamically
     */
    createModalStyles() {
        if (document.querySelector('#modal-styles')) return;

        const styles = document.createElement('style');
        styles.id = 'modal-styles';
        styles.textContent = `
            .modal-backdrop {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.8);
                backdrop-filter: blur(5px);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: var(--z-modal, 1000);
                opacity: 0;
                visibility: hidden;
                transition: opacity 0.3s ease, visibility 0.3s ease;
            }

            .modal-backdrop.active {
                opacity: 1;
                visibility: visible;
            }

            .modal-container {
                background: white;
                border-radius: 20px;
                padding: 30px;
                max-width: 90vw;
                max-height: 90vh;
                position: relative;
                transform: scale(0.8) translateY(20px);
                transition: transform 0.3s ease;
                overflow-y: auto;
                box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
            }

            .modal-backdrop.active .modal-container {
                transform: scale(1) translateY(0);
            }

            .modal-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 20px;
                padding-bottom: 15px;
                border-bottom: 1px solid rgba(0, 0, 0, 0.1);
            }

            .modal-title {
                font-size: 1.5rem;
                font-weight: bold;
                color: #2C3E50;
                margin: 0;
            }

            .modal-close {
                background: #E74C3C;
                color: white;
                border: none;
                border-radius: 50%;
                width: 40px;
                height: 40px;
                cursor: pointer;
                font-size: 1.2rem;
                display: flex;
                align-items: center;
                justify-content: center;
                transition: all 0.3s ease;
            }

            .modal-close:hover {
                background: #C0392B;
                transform: scale(1.1);
            }

            .modal-close:focus {
                outline: 2px solid #3498DB;
                outline-offset: 2px;
            }

            .modal-content {
                flex: 1;
                overflow-y: auto;
            }

            .modal-footer {
                margin-top: 20px;
                padding-top: 15px;
                border-top: 1px solid rgba(0, 0, 0, 0.1);
                display: flex;
                gap: 10px;
                justify-content: flex-end;
            }

            .modal-overlay {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                pointer-events: none;
                z-index: var(--z-overlay, 9999);
            }

            /* Animation classes */
            .modal-fade-in {
                animation: modalFadeIn 0.3s ease-out forwards;
            }

            .modal-fade-out {
                animation: modalFadeOut 0.3s ease-out forwards;
            }

            .modal-slide-in {
                animation: modalSlideIn 0.3s ease-out forwards;
            }

            .modal-slide-out {
                animation: modalSlideOut 0.3s ease-out forwards;
            }

            @keyframes modalFadeIn {
                from { opacity: 0; visibility: hidden; }
                to { opacity: 1; visibility: visible; }
            }

            @keyframes modalFadeOut {
                from { opacity: 1; visibility: visible; }
                to { opacity: 0; visibility: hidden; }
            }

            @keyframes modalSlideIn {
                from { 
                    opacity: 0; 
                    transform: scale(0.8) translateY(20px); 
                }
                to { 
                    opacity: 1; 
                    transform: scale(1) translateY(0); 
                }
            }

            @keyframes modalSlideOut {
                from { 
                    opacity: 1; 
                    transform: scale(1) translateY(0); 
                }
                to { 
                    opacity: 0; 
                    transform: scale(0.8) translateY(20px); 
                }
            }

            /* Responsive design */
            @media (max-width: 768px) {
                .modal-container {
                    margin: 20px;
                    padding: 20px;
                    max-width: calc(100vw - 40px);
                    max-height: calc(100vh - 40px);
                }
                
                .modal-header {
                    flex-direction: column;
                    gap: 10px;
                    align-items: flex-start;
                }
                
                .modal-footer {
                    flex-direction: column;
                }
            }

            /* Accessibility */
            .modal-container:focus {
                outline: none;
            }

            .focus-trap {
                position: absolute;
                left: -9999px;
                width: 1px;
                height: 1px;
                overflow: hidden;
            }

            /* High contrast mode */
            @media (prefers-contrast: high) {
                .modal-backdrop {
                    background: rgba(0, 0, 0, 0.9);
                }
                
                .modal-container {
                    border: 2px solid black;
                }
                
                .modal-close {
                    border: 2px solid white;
                }
            }

            /* Reduced motion */
            @media (prefers-reduced-motion: reduce) {
                .modal-backdrop,
                .modal-container {
                    transition: none;
                }
                
                .modal-fade-in,
                .modal-fade-out,
                .modal-slide-in,
                .modal-slide-out {
                    animation: none;
                }
            }
        `;
        
        document.head.appendChild(styles);
    }

    /**
     * Open a modal
     */
    async openModal(options = {}) {
        const modalId = options.id || this.generateModalId();
        const {
            title = 'Modal',
            content = '',
            size = 'medium',
            closeable = true,
            backdrop = true,
            keyboard = true,
            focus = true,
            animation = 'fade',
            className = '',
            onOpen = null,
            onClose = null,
            buttons = null
        } = options;

        // Store the currently focused element
        this.lastFocusedElement = document.activeElement;

        // Create modal structure
        const modal = this.createModalElement({
            id: modalId,
            title,
            content,
            size,
            closeable,
            className,
            buttons
        });

        // Add to DOM
        document.body.appendChild(modal);

        // Store modal reference
        this.activeModals.set(modalId, {
            element: modal,
            options,
            onClose
        });

        // Add to stack
        this.modalStack.push(modalId);

        // Setup focus trap if needed
        if (focus) {
            this.setupFocusTrap(modalId, modal);
        }

        // Show modal with animation
        await this.showModal(modal, animation);

        // Setup event listeners
        this.setupModalEventListeners(modalId, modal, { backdrop, keyboard, closeable });

        // Call onOpen callback
        if (onOpen) {
            onOpen(modalId, modal);
        }

        // Emit open event
        this.emitEvent('modal:open', { modalId, element: modal });

        // Focus management
        if (focus) {
            this.focusModal(modal);
        }

        console.log(`🖼️ Modal opened: ${modalId}`);
        return modalId;
    }

    /**
     * Close a modal
     */
    async closeModal(modalId = null, force = false) {
        // If no ID provided, close the top modal
        if (!modalId && this.modalStack.length > 0) {
            modalId = this.modalStack[this.modalStack.length - 1];
        }

        if (!modalId || !this.activeModals.has(modalId)) {
            return false;
        }

        const modalData = this.activeModals.get(modalId);
        const { element, options, onClose } = modalData;

        // Call onClose callback
        if (onClose) {
            const shouldClose = onClose(modalId, element);
            if (shouldClose === false && !force) {
                return false;
            }
        }

        // Hide modal with animation
        await this.hideModal(element, options.animation || 'fade');

        // Remove from stack
        const stackIndex = this.modalStack.indexOf(modalId);
        if (stackIndex > -1) {
            this.modalStack.splice(stackIndex, 1);
        }

        // Remove focus trap
        this.removeFocusTrap(modalId);

        // Remove from active modals
        this.activeModals.delete(modalId);

        // Remove from DOM
        element.remove();

        // Restore focus
        if (this.modalStack.length === 0 && this.lastFocusedElement) {
            this.lastFocusedElement.focus();
            this.lastFocusedElement = null;
        }

        // Emit close event
        this.emitEvent('modal:close', { modalId });

        console.log(`🖼️ Modal closed: ${modalId}`);
        return true;
    }

    /**
     * Close the top modal
     */
    closeTopModal() {
        if (this.modalStack.length > 0) {
            const topModalId = this.modalStack[this.modalStack.length - 1];
            this.closeModal(topModalId);
        }
    }

    /**
     * Close all modals
     */
    closeAllModals() {
        const modalIds = [...this.modalStack];
        modalIds.forEach(id => this.closeModal(id, true));
    }

    /**
     * Create modal element
     */
    createModalElement(options) {
        const {
            id,
            title,
            content,
            size,
            closeable,
            className,
            buttons
        } = options;

        const modal = document.createElement('div');
        modal.className = `modal-backdrop ${className}`;
        modal.setAttribute('role', 'dialog');
        modal.setAttribute('aria-modal', 'true');
        modal.setAttribute('aria-labelledby', `modal-title-${id}`);
        modal.setAttribute('data-modal-id', id);

        const container = document.createElement('div');
        container.className = `modal-container modal-${size}`;
        container.setAttribute('tabindex', '-1');

        // Header
        const header = document.createElement('div');
        header.className = 'modal-header';
        
        const titleElement = document.createElement('h2');
        titleElement.className = 'modal-title';
        titleElement.id = `modal-title-${id}`;
        titleElement.textContent = title;
        
        header.appendChild(titleElement);
        
        if (closeable) {
            const closeButton = document.createElement('button');
            closeButton.className = 'modal-close';
            closeButton.innerHTML = '×';
            closeButton.setAttribute('aria-label', 'Close modal');
            closeButton.setAttribute('data-action', 'close');
            header.appendChild(closeButton);
        }

        // Content
        const contentElement = document.createElement('div');
        contentElement.className = 'modal-content';
        
        if (typeof content === 'string') {
            contentElement.innerHTML = content;
        } else if (content instanceof HTMLElement) {
            contentElement.appendChild(content);
        }

        // Footer with buttons
        if (buttons && buttons.length > 0) {
            const footer = document.createElement('div');
            footer.className = 'modal-footer';
            
            buttons.forEach(button => {
                const btn = document.createElement('button');
                btn.className = `btn ${button.className || 'btn-secondary'}`;
                btn.textContent = button.text;
                btn.setAttribute('data-action', button.action || 'custom');
                
                if (button.onclick) {
                    btn.addEventListener('click', button.onclick);
                }
                
                footer.appendChild(btn);
            });
            
            container.appendChild(footer);
        }

        container.appendChild(header);
        container.appendChild(contentElement);
        modal.appendChild(container);

        return modal;
    }

    /**
     * Show modal with animation
     */
    async showModal(modal, animation = 'fade') {
        return new Promise((resolve) => {
            modal.classList.add('active');
            
            if (animation !== 'none') {
                modal.classList.add(`modal-${animation}-in`);
                
                const handleAnimationEnd = () => {
                    modal.removeEventListener('animationend', handleAnimationEnd);
                    modal.classList.remove(`modal-${animation}-in`);
                    resolve();
                };
                
                modal.addEventListener('animationend', handleAnimationEnd);
            } else {
                resolve();
            }
        });
    }

    /**
     * Hide modal with animation
     */
    async hideModal(modal, animation = 'fade') {
        return new Promise((resolve) => {
            if (animation !== 'none') {
                modal.classList.add(`modal-${animation}-out`);
                
                const handleAnimationEnd = () => {
                    modal.removeEventListener('animationend', handleAnimationEnd);
                    modal.classList.remove('active', `modal-${animation}-out`);
                    resolve();
                };
                
                modal.addEventListener('animationend', handleAnimationEnd);
            } else {
                modal.classList.remove('active');
                resolve();
            }
        });
    }

    /**
     * Setup modal event listeners
     */
    setupModalEventListeners(modalId, modal, options) {
        const { backdrop, keyboard, closeable } = options;

        // Click handlers
        modal.addEventListener('click', (event) => {
            const action = event.target.getAttribute('data-action');
            
            if (action === 'close' && closeable) {
                this.closeModal(modalId);
            } else if (backdrop && event.target === modal) {
                this.closeModal(modalId);
            }
        });

        // Keyboard handlers
        if (keyboard) {
            modal.addEventListener('keydown', (event) => {
                if (event.key === 'Escape' && closeable) {
                    this.closeModal(modalId);
                }
            });
        }
    }

    /**
     * Setup focus management
     */
    setupFocusManagement() {
        // Create focus trap elements
        this.beforeTrap = document.createElement('div');
        this.beforeTrap.className = 'focus-trap';
        this.beforeTrap.setAttribute('tabindex', '0');
        
        this.afterTrap = document.createElement('div');
        this.afterTrap.className = 'focus-trap';
        this.afterTrap.setAttribute('tabindex', '0');
    }

    /**
     * Setup focus trap for a modal
     */
    setupFocusTrap(modalId, modal) {
        const focusableElements = modal.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        
        if (focusableElements.length === 0) return;

        const firstFocusable = focusableElements[0];
        const lastFocusable = focusableElements[focusableElements.length - 1];

        // Insert trap elements
        modal.insertBefore(this.beforeTrap.cloneNode(), modal.firstChild);
        modal.appendChild(this.afterTrap.cloneNode());

        // Setup trap event listeners
        const handleBeforeTrapFocus = () => lastFocusable.focus();
        const handleAfterTrapFocus = () => firstFocusable.focus();

        const beforeTrap = modal.querySelector('.focus-trap:first-child');
        const afterTrap = modal.querySelector('.focus-trap:last-child');

        beforeTrap.addEventListener('focus', handleBeforeTrapFocus);
        afterTrap.addEventListener('focus', handleAfterTrapFocus);

        // Store cleanup function
        this.focusTraps.set(modalId, () => {
            beforeTrap.removeEventListener('focus', handleBeforeTrapFocus);
            afterTrap.removeEventListener('focus', handleAfterTrapFocus);
        });
    }

    /**
     * Remove focus trap
     */
    removeFocusTrap(modalId) {
        const cleanup = this.focusTraps.get(modalId);
        if (cleanup) {
            cleanup();
            this.focusTraps.delete(modalId);
        }
    }

    /**
     * Focus modal
     */
    focusModal(modal) {
        const focusableElements = modal.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        
        if (focusableElements.length > 0) {
            focusableElements[0].focus();
        } else {
            modal.querySelector('.modal-container').focus();
        }
    }

    /**
     * Setup keyboard navigation
     */
    setupKeyboardNavigation() {
        document.addEventListener('keydown', (event) => {
            if (this.modalStack.length === 0) return;

            const activeModal = this.getActiveModal();
            if (!activeModal) return;

            // Tab navigation within modal
            if (event.key === 'Tab') {
                this.handleTabNavigation(event, activeModal);
            }
        });
    }

    /**
     * Handle tab navigation
     */
    handleTabNavigation(event, modal) {
        const focusableElements = modal.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        
        if (focusableElements.length === 0) return;

        const firstFocusable = focusableElements[0];
        const lastFocusable = focusableElements[focusableElements.length - 1];

        if (event.shiftKey) {
            // Shift + Tab (backwards)
            if (document.activeElement === firstFocusable) {
                event.preventDefault();
                lastFocusable.focus();
            }
        } else {
            // Tab (forwards)
            if (document.activeElement === lastFocusable) {
                event.preventDefault();
                firstFocusable.focus();
            }
        }
    }

    /**
     * Get active modal element
     */
    getActiveModal() {
        if (this.modalStack.length === 0) return null;
        
        const topModalId = this.modalStack[this.modalStack.length - 1];
        const modalData = this.activeModals.get(topModalId);
        
        return modalData ? modalData.element : null;
    }

    /**
     * Generate unique modal ID
     */
    generateModalId() {
        return `modal-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Check if modal is open
     */
    isModalOpen(modalId = null) {
        if (modalId) {
            return this.activeModals.has(modalId);
        }
        return this.modalStack.length > 0;
    }

    /**
     * Get modal data
     */
    getModal(modalId) {
        return this.activeModals.get(modalId);
    }

    /**
     * Update modal content
     */
    updateModalContent(modalId, content) {
        const modalData = this.activeModals.get(modalId);
        if (!modalData) return false;

        const contentElement = modalData.element.querySelector('.modal-content');
        if (contentElement) {
            if (typeof content === 'string') {
                contentElement.innerHTML = content;
            } else if (content instanceof HTMLElement) {
                contentElement.innerHTML = '';
                contentElement.appendChild(content);
            }
            return true;
        }
        return false;
    }

    /**
     * Emit custom events
     */
    emitEvent(eventName, data = {}) {
        const event = new CustomEvent(eventName, { 
            detail: { 
                ...data, 
                timestamp: Date.now(),
                modalHandler: this 
            } 
        });
        document.dispatchEvent(event);
    }

    /**
     * Utility methods for common modal types
     */

    /**
     * Show alert modal
     */
    alert(message, title = 'Alert') {
        return this.openModal({
            title,
            content: `<p>${message}</p>`,
            buttons: [
                { text: 'OK', className: 'btn-primary', action: 'close' }
            ]
        });
    }

    /**
     * Show confirm modal
     */
    confirm(message, title = 'Confirm') {
        return new Promise((resolve) => {
            this.openModal({
                title,
                content: `<p>${message}</p>`,
                buttons: [
                    { 
                        text: 'Cancel', 
                        className: 'btn-secondary', 
                        onclick: () => {
                            this.closeTopModal();
                            resolve(false);
                        }
                    },
                    { 
                        text: 'OK', 
                        className: 'btn-primary',
                        onclick: () => {
                            this.closeTopModal();
                            resolve(true);
                        }
                    }
                ]
            });
        });
    }

    /**
     * Show loading modal
     */
    showLoading(message = 'Loading...') {
        return this.openModal({
            id: 'loading-modal',
            title: 'Please Wait',
            content: `
                <div style="text-align: center; padding: 20px;">
                    <div class="loading-spinner" style="margin: 0 auto 20px;"></div>
                    <p>${message}</p>
                </div>
            `,
            closeable: false,
            backdrop: false,
            keyboard: false
        });
    }

    /**
     * Hide loading modal
     */
    hideLoading() {
        this.closeModal('loading-modal');
    }

    /**
     * Cleanup and destroy
     */
    destroy() {
        this.closeAllModals();
        this.activeModals.clear();
        this.modalStack = [];
        this.focusTraps.clear();
        
        // Remove styles
        const styles = document.querySelector('#modal-styles');
        if (styles) {
            styles.remove();
        }
        
        console.log('🖼️ Modal Handler destroyed');
    }
}

// Export for global use
window.ModalHandler = ModalHandler;

console.log('🖼️ Modal Handler loaded');
