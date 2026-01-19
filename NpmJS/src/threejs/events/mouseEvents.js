// NpmJS/src/threejs/events/mouseEvents.js

import * as THREE from 'three';

// Import pin functions (add these imports)
import { addPin, showPinHelpers, hidePinHelpers } from '../objects/refPoint'
import * as PinCursor from './pinCursor.js';

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
        this.hoveredSegmentTag = null;

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
            onRightClick: null,
            onPinPlaced: null,
            onSegmentClick: null,
            onSegmentHover: null
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
        this.raycaster.layers.mask = this.camera.layers.mask;

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
     * Handle both regular meshes and merged segment meshes
     */
    getIntersectedObjects(event, filterFn = null) {
        const ndc = this.getMouseNDC(event);
        this.mouse.set(ndc.x, ndc.y);

        this.raycaster.setFromCamera(this.mouse, this.camera);

        // Get all meshes with Tags OR merged segment meshes
        const meshes = [];
        this.scene.traverse((child) => {
            if (child.isMesh) {
                // Include regular tagged meshes
                if (child.Tag) {
                    if (!filterFn || filterFn(child)) {
                        meshes.push(child);
                    }
                }
                // Include merged segment meshes
                else if (child.isMergedSegmentMesh) {
                    if (!filterFn || filterFn(child)) {
                        meshes.push(child);
                    }
                }
            }
        });

        return this.raycaster.intersectObjects(meshes, false);
    }
    
    /**
     * Extract segment information from intersection with merged mesh
     * @param {Object} intersection - THREE.js intersection result
     * @returns {Object|null} - Segment info or null
     */
    getSegmentFromIntersection(intersection) {
        if (!intersection || !intersection.object) return null;

        const obj = intersection.object;

        // Check if it's a merged segment mesh
        if (obj.isMergedSegmentMesh && obj.getSegmentFromIntersection) {
            const segmentInfo = obj.getSegmentFromIntersection(intersection);
            if (segmentInfo) {
                return {
                    tag: segmentInfo.tag,
                    segmentIndex: segmentInfo.segmentIndex,
                    originalIndex: segmentInfo.originalIndex,
                    meshType: obj.Type || 'ladder',
                    point: intersection.point
                };
            }
        }

        // Regular mesh with Tag
        if (obj.Tag) {
            return {
                tag: obj.Tag,
                segmentIndex: -1,
                originalIndex: -1,
                meshType: obj.Type || 'unknown',
                point: intersection.point
            };
        }

        return null;
    }

    /**
     * Get all intersected segments (handles both single and merged meshes)
     * @param {Object} event - Mouse event
     * @returns {Array} - Array of {tag, point, meshType, ...}
     */
    getAllIntersectedSegments(event) {
        const intersects = this.getIntersectedObjects(event);
        const segments = [];
        const seenTags = new Set();

        for (const intersection of intersects) {
            const segmentInfo = this.getSegmentFromIntersection(intersection);
            if (segmentInfo && !seenTags.has(segmentInfo.tag)) {
                seenTags.add(segmentInfo.tag);
                segments.push({
                    tag: segmentInfo.tag,
                    point: {
                        x: intersection.point.x,
                        y: intersection.point.y,
                        z: intersection.point.z
                    },
                    meshType: segmentInfo.meshType,
                    segmentIndex: segmentInfo.segmentIndex,
                    distance: intersection.distance
                });
            }
        }

        return segments;
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
     * Include segment information for merged meshes
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
        let meshType = null;
        let segmentIndex = -1;

        // Get ALL intersected segments for merged meshes
        const allIntersectedSegments = this.getAllIntersectedSegments(event);

        if (intersects.length > 0) {
            const firstHit = intersects[0];

            // Check for merged segment mesh first
            const segmentInfo = this.getSegmentFromIntersection(firstHit);
            if (segmentInfo) {
                objectTag = segmentInfo.tag;
                meshType = segmentInfo.meshType;
                segmentIndex = segmentInfo.segmentIndex;
            } else {
                objectTag = firstHit.object.Tag || null;
            }

            objectLayer = this.getObjectLayer(firstHit.object);
            intersectPoint = {
                x: firstHit.point.x,
                y: firstHit.point.y,
                z: firstHit.point.z
            };
            //objectData = firstHit.object.userData || null;
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
            //objectData: objectData,
            intersectCount: intersects.length,
            meshType: meshType,
            segmentIndex: segmentIndex,
            // Array of all intersected tags (for multi-select or info)
            allIntersectedTags: allIntersectedSegments.map(s => s.tag),
            // Full segment info for Blazor
            intersectedSegments: allIntersectedSegments,
            shiftKey: event.shiftKey,
            ctrlKey: event.ctrlKey || event.metaKey,
            altKey: event.altKey,
            button: event.button,
            timestamp: Date.now()
        };
    }

    // ============ EVENT HANDLERS ============

    onMouseDown(event) {
        if (event.button !== 0) return;

        this.isMouseDown = true;
        this.mouseDownPosition = { x: event.clientX, y: event.clientY };
        this.mouseDownTime = Date.now();

        if (this.callbacks.onDragStart) {
            const eventData = this.buildEventData(event, 'dragStart');
            this.callbacks.onDragStart(eventData);
        }
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
        let currentHovered = null;
        let currentSegmentTag = null;

        if (intersects.length > 0) {
            const firstHit = intersects[0];
            currentHovered = firstHit.object;

            // Get segment tag for merged meshes
            const segmentInfo = this.getSegmentFromIntersection(firstHit);
            if (segmentInfo) {
                currentSegmentTag = segmentInfo.tag;
            } else {
                currentSegmentTag = currentHovered.Tag;
            }
        }

        // Check if hover target changed (including segment within merged mesh)
        const hoverChanged = currentHovered !== this.hoveredObject ||
            currentSegmentTag !== this.hoveredSegmentTag;

        if (hoverChanged) {
            // End hover on previous
            if (this.hoveredObject && this.callbacks.onHoverEnd) {
                this.callbacks.onHoverEnd({
                    objectTag: this.hoveredSegmentTag || this.hoveredObject.Tag,
                    objectLayer: this.getObjectLayer(this.hoveredObject)
                });
            }

            // Start hover on new
            if (currentHovered) {
                if (this.callbacks.onHover) {
                    const eventData = this.buildEventData(event, 'hover');
                    this.callbacks.onHover(eventData);
                }
                // Segment-specific hover callback
                if (currentSegmentTag && this.callbacks.onSegmentHover) {
                    this.callbacks.onSegmentHover({
                        tag: currentSegmentTag,
                        meshType: currentHovered.Type || 'unknown'
                    });
                }
            }

            this.hoveredObject = currentHovered;
            this.hoveredSegmentTag = currentSegmentTag;
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

        if (this.hoveredObject && this.callbacks.onHoverEnd) {
            this.callbacks.onHoverEnd({
                objectTag: this.hoveredSegmentTag || this.hoveredObject.Tag,
                objectLayer: this.getObjectLayer(this.hoveredObject)
            });
        }
        this.hoveredObject = null;
        this.hoveredSegmentTag = null;
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

        // ============ PIN PLACEMENT MODE CHECK ============
        // Check if we should place a pin (shift+click when pin mode is active)
        if (eventData.eventType === 'shift' && PinCursor.isPinModeActive()) {
            const pinPlaced = this.handlePinPlacement(eventData);
            if (pinPlaced) {
                // Pin was placed, don't process as normal shift+click
                return;
            }
        }

        // ============ SEGMENT CLICK CALLBACK ============
        if (eventData.intersectedSegments && eventData.intersectedSegments.length > 0) {
            if (this.callbacks.onSegmentClick) {
                this.callbacks.onSegmentClick(eventData);
            }
        }

        // ============ NORMAL CLICK HANDLING ============
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
     * Handle pin placement when in pin mode
     * @param {object} eventData - The event data with world coordinates
     * @returns {boolean} - True if pin was placed, false otherwise
     */
    handlePinPlacement(eventData) {
        // Get current tag from pin cursor state
        const tag = PinCursor.getCurrentPinTag();

        if (!tag) {
            console.log('MouseEventHandler: No pin tag available');
            return false;
        }

        const point = {
            x: eventData.worldX,
            y: eventData.worldY,
            z: eventData.worldZ || 0
        };

        console.log(`MouseEventHandler: Placing pin '${tag}' at (${point.x.toFixed(2)}, ${point.y.toFixed(2)})`);

        // Place the pin
        const placedTag = addPin(
            this.scene,
            tag,
            point,
            true,   // useSprite
            true    // showHelpers
        );

        if (placedTag) {
            // Update event data with the pin tag
            eventData.objectTag = placedTag;
            eventData.eventType = 'pinPlaced';

            // Trigger pin placed callback
            if (this.callbacks.onPinPlaced) {
                this.callbacks.onPinPlaced(eventData);
            }

            // Notify Blazor about the pin placement
            this.notifyBlazor(eventData);

            return true;
        }

        return false;
    }



    /**
     * Send event data to Blazor
     */
    notifyBlazor(eventData) {
        if (this.dotNetRef) {

            // Debug: Log what we're sending
            console.log('Sending to Blazor:', JSON.stringify(eventData, null, 2));
            
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
        this.hoveredSegmentTag = null;
        this.pendingClick = null;
    }
}

export { MOUSE_CONFIG };