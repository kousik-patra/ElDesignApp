// NpmJS/src/threejs/events/mouseEvents.js

import * as THREE from 'three';

/**
 * Configuration for mouse events
 */
const MOUSE_CONFIG = {
    clickThreshold: 5,        // pixels - max movement to count as click
    doubleClickDelay: 300,    // ms - max time between clicks for double-click
    hoverDebounce: 50         // ms - debounce time for hover events
};

/**
 * MouseEventHandler class - handles all mouse interactions with the scene
 */
export class MouseEventHandler {
    constructor(sceneManager) {
        this.sceneManager = sceneManager;
        this.renderer = null;
        this.camera = null;
        this.scene = null;
        this.dotNetRef = null;

        // Raycaster for mouse picking
        this.raycaster = new THREE.Raycaster();
        this.mouse = new THREE.Vector2();

        // Click detection state
        this.mouseDownPosition = { x: 0, y: 0 };
        this.mouseDownTime = 0;
        this.clickTimeout = null;
        this.pendingClick = null;
        this.isMouseDown = false;

        // Hover state
        this.hoveredObject = null;
        this.hoverTimeout = null;

        // Bound event handlers (for removal)
        this.boundHandlers = {};

        // Callbacks for different events (can be set by sceneManager)
        this.callbacks = {
            onSingleClick: null,
            onDoubleClick: null,
            onShiftClick: null,
            onCtrlClick: null,
            onAltClick: null,
            onHover: null,
            onHoverEnd: null,
            onDragStart: null,
            onDrag: null,
            onDragEnd: null,
            onRightClick: null
        };
    }

    /**
     * Initialize the event handler
     * @param {THREE.WebGLRenderer} renderer
     * @param {THREE.Camera} camera
     * @param {THREE.Scene} scene
     * @param {object} dotNetRef - Blazor .NET reference for callbacks
     */
    initialize(renderer, camera, scene, dotNetRef) {
        this.renderer = renderer;
        this.camera = camera;
        this.scene = scene;
        this.dotNetRef = dotNetRef;

        this.attachEventListeners();
        console.log('MouseEventHandler: Initialized');
    }

    /**
     * Update references (useful when camera changes)
     */
    updateReferences(camera, scene) {
        this.camera = camera;
        this.scene = scene;
    }

    /**
     * Attach all event listeners to the canvas
     */
    attachEventListeners() {
        const canvas = this.renderer.domElement;

        // Create bound handlers
        this.boundHandlers = {
            mouseDown: (e) => this.onMouseDown(e),
            mouseUp: (e) => this.onMouseUp(e),
            mouseMove: (e) => this.onMouseMove(e),
            doubleClick: (e) => this.onDoubleClick(e),
            contextMenu: (e) => this.onContextMenu(e),
            mouseLeave: (e) => this.onMouseLeave(e)
        };

        // Attach handlers
        canvas.addEventListener('mousedown', this.boundHandlers.mouseDown);
        canvas.addEventListener('mouseup', this.boundHandlers.mouseUp);
        canvas.addEventListener('mousemove', this.boundHandlers.mouseMove);
        canvas.addEventListener('dblclick', this.boundHandlers.doubleClick);
        canvas.addEventListener('contextmenu', this.boundHandlers.contextMenu);
        canvas.addEventListener('mouseleave', this.boundHandlers.mouseLeave);

        console.log('MouseEventHandler: Event listeners attached');
    }

    /**
     * Remove all event listeners
     */
    removeEventListeners() {
        const canvas = this.renderer?.domElement;
        if (!canvas) return;

        for (const [event, handler] of Object.entries(this.boundHandlers)) {
            const eventName = event.replace(/([A-Z])/g, (m) => m.toLowerCase());
            canvas.removeEventListener(eventName, handler);
        }

        // Clear timeouts
        if (this.clickTimeout) clearTimeout(this.clickTimeout);
        if (this.hoverTimeout) clearTimeout(this.hoverTimeout);

        console.log('MouseEventHandler: Event listeners removed');
    }

    /**
     * Convert mouse event to normalized device coordinates (-1 to +1)
     */
    getMouseNDC(event) {
        const canvas = this.renderer.domElement;
        const rect = canvas.getBoundingClientRect();

        return {
            x: ((event.clientX - rect.left) / rect.width) * 2 - 1,
            y: -((event.clientY - rect.top) / rect.height) * 2 + 1
        };
    }

    /**
     * Get screen coordinates relative to canvas
     */
    getScreenCoords(event) {
        const canvas = this.renderer.domElement;
        const rect = canvas.getBoundingClientRect();

        return {
            x: event.clientX - rect.left,
            y: event.clientY - rect.top
        };
    }

    /**
     * Get world coordinates from mouse position (on Z=0 plane)
     */
    getWorldPosition(event, planeZ = 0) {
        const ndc = this.getMouseNDC(event);
        this.mouse.set(ndc.x, ndc.y);

        this.raycaster.setFromCamera(this.mouse, this.camera);

        const plane = new THREE.Plane(new THREE.Vector3(0, 0, 1), -planeZ);
        const worldPosition = new THREE.Vector3();
        this.raycaster.ray.intersectPlane(plane, worldPosition);

        return worldPosition;
    }

    /**
     * Perform raycasting to find intersected objects
     */
    getIntersectedObjects(event, filterFn = null) {
        const ndc = this.getMouseNDC(event);
        this.mouse.set(ndc.x, ndc.y);

        this.raycaster.setFromCamera(this.mouse, this.camera);

        // Get all meshes with Tags
        const meshes = [];
        this.scene.traverse((child) => {
            if (child.isMesh && child.Tag) {
                if (!filterFn || filterFn(child)) {
                    meshes.push(child);
                }
            }
        });

        return this.raycaster.intersectObjects(meshes, false);
    }

    /**
     * Get the layer of an object
     */
    getObjectLayer(object) {
        // Check which layer the object belongs to
        for (let i = 0; i <= 10; i++) {
            if (object.layers.isEnabled(i)) {
                return i;
            }
        }
        return 0;
    }

    /**
     * Build comprehensive click/event data object
     */
    buildEventData(event, eventType) {
        const worldPos = this.getWorldPosition(event);
        const screenCoords = this.getScreenCoords(event);
        const intersects = this.getIntersectedObjects(event);

        // Get first intersected object info
        let objectTag = null;
        let objectLayer = null;
        let intersectPoint = null;
        let objectData = null;

        if (intersects.length > 0) {
            const firstHit = intersects[0];
            objectTag = firstHit.object.Tag || null;
            objectLayer = this.getObjectLayer(firstHit.object);
            intersectPoint = {
                x: firstHit.point.x,
                y: firstHit.point.y,
                z: firstHit.point.z
            };
            // Include any custom data attached to the object
            objectData = firstHit.object.userData || null;
        }

        return {
            eventType: eventType,
            screenX: screenCoords.x,
            screenY: screenCoords.y,
            worldX: worldPos ? worldPos.x : 0,
            worldY: worldPos ? worldPos.y : 0,
            worldZ: worldPos ? worldPos.z : 0,
            objectTag: objectTag,
            objectLayer: objectLayer,
            intersectPoint: intersectPoint,
            objectData: objectData,
            intersectCount: intersects.length,
            allIntersectedTags: intersects.map(i => i.object.Tag).filter(Boolean),
            shiftKey: event.shiftKey,
            ctrlKey: event.ctrlKey || event.metaKey,
            altKey: event.altKey,
            button: event.button,
            timestamp: Date.now()
        };
    }

    // ============ EVENT HANDLERS ============

    onMouseDown(event) {
        this.isMouseDown = true;
        this.mouseDownTime = Date.now();
        this.mouseDownPosition = {
            x: event.clientX,
            y: event.clientY
        };
    }

    onMouseUp(event) {
        if (!this.isMouseDown) return;
        this.isMouseDown = false;

        // Only handle left clicks here (right click handled by contextmenu)
        if (event.button !== 0) return;

        // Calculate distance moved
        const dx = event.clientX - this.mouseDownPosition.x;
        const dy = event.clientY - this.mouseDownPosition.y;
        const distance = Math.sqrt(dx * dx + dy * dy);

        // If moved too much, it's a drag, not a click
        if (distance > MOUSE_CONFIG.clickThreshold) {
            // Trigger drag end if we were dragging
            if (this.callbacks.onDragEnd) {
                const eventData = this.buildEventData(event, 'dragEnd');
                this.callbacks.onDragEnd(eventData);
            }
            return;
        }

        // Determine click type
        const clickType = this.getClickType(event);
        const eventData = this.buildEventData(event, clickType);

        // Handle modified clicks immediately
        if (clickType !== 'single') {
            this.executeClick(eventData);
            return;
        }

        // For single click, wait to see if it becomes a double click
        if (this.clickTimeout) {
            clearTimeout(this.clickTimeout);
            this.clickTimeout = null;
        }

        this.pendingClick = eventData;
        this.clickTimeout = setTimeout(() => {
            if (this.pendingClick) {
                this.executeClick(this.pendingClick);
                this.pendingClick = null;
            }
        }, MOUSE_CONFIG.doubleClickDelay);
    }

    onMouseMove(event) {
        // Handle drag if mouse is down
        if (this.isMouseDown) {
            const dx = event.clientX - this.mouseDownPosition.x;
            const dy = event.clientY - this.mouseDownPosition.y;
            const distance = Math.sqrt(dx * dx + dy * dy);

            if (distance > MOUSE_CONFIG.clickThreshold && this.callbacks.onDrag) {
                const eventData = this.buildEventData(event, 'drag');
                this.callbacks.onDrag(eventData);
            }
            return;
        }

        // Handle hover (debounced)
        if (this.hoverTimeout) {
            clearTimeout(this.hoverTimeout);
        }

        this.hoverTimeout = setTimeout(() => {
            this.handleHover(event);
        }, MOUSE_CONFIG.hoverDebounce);
    }

    handleHover(event) {
        const intersects = this.getIntersectedObjects(event);
        const currentHovered = intersects.length > 0 ? intersects[0].object : null;

        // Check if hover target changed
        if (currentHovered !== this.hoveredObject) {
            // End hover on previous object
            if (this.hoveredObject && this.callbacks.onHoverEnd) {
                this.callbacks.onHoverEnd({
                    objectTag: this.hoveredObject.Tag,
                    objectLayer: this.getObjectLayer(this.hoveredObject)
                });
            }

            // Start hover on new object
            if (currentHovered && this.callbacks.onHover) {
                const eventData = this.buildEventData(event, 'hover');
                this.callbacks.onHover(eventData);
            }

            this.hoveredObject = currentHovered;
        }
    }

    onDoubleClick(event) {
        if (event.button !== 0) return;

        // Cancel pending single click
        if (this.clickTimeout) {
            clearTimeout(this.clickTimeout);
            this.clickTimeout = null;
            this.pendingClick = null;
        }

        const eventData = this.buildEventData(event, 'double');
        this.executeClick(eventData);
    }

    onContextMenu(event) {
        // Prevent default context menu
        event.preventDefault();

        if (this.callbacks.onRightClick) {
            const eventData = this.buildEventData(event, 'rightClick');
            this.callbacks.onRightClick(eventData);
            this.notifyBlazor(eventData);
        }
    }

    onMouseLeave(event) {
        this.isMouseDown = false;

        // Clear hover state
        if (this.hoveredObject && this.callbacks.onHoverEnd) {
            this.callbacks.onHoverEnd({
                objectTag: this.hoveredObject.Tag,
                objectLayer: this.getObjectLayer(this.hoveredObject)
            });
        }
        this.hoveredObject = null;
    }

    /**
     * Determine click type based on modifier keys
     */
    getClickType(event) {
        if (event.altKey) return 'alt';
        if (event.shiftKey) return 'shift';
        if (event.ctrlKey || event.metaKey) return 'ctrl';
        return 'single';
    }

    /**
     * Execute click and trigger callbacks
     */
    executeClick(eventData) {
        console.log(`MouseEventHandler: ${eventData.eventType} click`, eventData);

        // Trigger local callback
        const callbackName = `on${eventData.eventType.charAt(0).toUpperCase()}${eventData.eventType.slice(1)}Click`;
        if (this.callbacks[callbackName]) {
            this.callbacks[callbackName](eventData);
        }

        // Special handling for single click
        if (eventData.eventType === 'single' && this.callbacks.onSingleClick) {
            this.callbacks.onSingleClick(eventData);
        }

        // Notify Blazor
        this.notifyBlazor(eventData);
    }

    /**
     * Send event data to Blazor
     */
    notifyBlazor(eventData) {
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnSceneClick', JSON.stringify(eventData))
                .catch(err => console.error('Error calling Blazor OnSceneClick:', err));
        }
    }

    /**
     * Set a callback for a specific event type
     */
    setCallback(eventType, callback) {
        if (this.callbacks.hasOwnProperty(eventType)) {
            this.callbacks[eventType] = callback;
        } else {
            console.warn(`MouseEventHandler: Unknown callback type '${eventType}'`);
        }
    }

    /**
     * Dispose of all resources
     */
    dispose() {
        this.removeEventListeners();
        this.callbacks = {};
        this.hoveredObject = null;
        this.pendingClick = null;
    }
}

export { MOUSE_CONFIG };