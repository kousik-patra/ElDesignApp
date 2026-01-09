// NpmJS/src/sceneManager.js

import * as THREE from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import { createInfiniteAxes, createAxisIndicator, createGridHelper } from './threejs/objects/axisHelpers.js';
import { MouseEventHandler } from './threejs/events/mouseEvents.js';

// Layer constants for object categorization
export const LAYERS = {
    DEFAULT: 0,
    PLOT_PLAN: 1,
    EQUIPMENT: 2,
    LADDERS: 3,
    TRENCHES: 4,
    BENDS: 5,
    TEES: 6,
    CROSSES: 7,
    NODES: 8,
    SLEEVES: 9,
    CABLES: 10
};

// Minimum dimensions for the scene
const MIN_WIDTH = 800;
const MIN_HEIGHT = 600;

class SceneManager {
    constructor() {
        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.controls = null;
        this.canvas = null;
        this.dotNetRef = null;
        this.isInitialized = false;
        this.animationId = null;
        this.currentPage = null;
        this.resizeObserver = null;
        this.resizeTimeout = null;
        this.boundWindowResize = null;
        this.saveInterval = null;

        // Mouse event handler
        this.mouseHandler = new MouseEventHandler(this);

        // Axis helpers
        this.infiniteAxes = null;
        this.axisIndicator = null;
        this.gridHelper = null;

        // Track objects by tag for efficient lookup
        this.objectRegistry = new Map();

        // Scene state for persistence
        this.sceneState = {
            cameraPosition: { x: 0, y: 0, z: 5 },
            cameraRotation: { x: 0, y: 0, z: 0 },
            controlsTarget: { x: 0, y: 0, z: 0 }
        };
    }

    /**
     * Get dimensions with minimum constraints
     */
    getConstrainedDimensions(container) {
        const canvas = this.renderer?.domElement;
        let originalDisplay = '';

        if (canvas) {
            originalDisplay = canvas.style.display;
            canvas.style.display = 'none';
        }

        const computedStyle = window.getComputedStyle(container);
        const paddingLeft = parseFloat(computedStyle.paddingLeft) || 0;
        const paddingRight = parseFloat(computedStyle.paddingRight) || 0;
        const paddingTop = parseFloat(computedStyle.paddingTop) || 0;
        const paddingBottom = parseFloat(computedStyle.paddingBottom) || 0;

        const rect = container.getBoundingClientRect();
        let width = rect.width - paddingLeft - paddingRight;
        let height = rect.height - paddingTop - paddingBottom;

        if (canvas) {
            canvas.style.display = originalDisplay;
        }

        width = Math.max(width || MIN_WIDTH, MIN_WIDTH);
        height = Math.max(height || MIN_HEIGHT, MIN_HEIGHT);

        return { width, height };
    }

    /**
     * Initialize or reattach the scene to a canvas element
     */
    initialize(canvasContainerId, dotNetObjRef, savedStateJson = '') {
        this.dotNetRef = dotNetObjRef;

        const container = document.getElementById(canvasContainerId);
        if (!container) {
            console.error(`Container ${canvasContainerId} not found`);
            return false;
        }

        if (this.isInitialized && this.renderer) {
            this.reattachToContainer(container);
            return true;
        }

        this.createScene();
        this.createCamera(container);
        this.createRenderer(container);
        this.createControls();
        this.createLights();
        this.createHelpers();

        if (savedStateJson) {
            this.restoreState(savedStateJson);
        }

        this.setupEventListeners(container);

        // Initialize mouse event handler
        this.mouseHandler.initialize(
            this.renderer,
            this.camera,
            this.scene,
            this.dotNetRef
        );

        // Setup optional local callbacks
        this.setupMouseCallbacks();

        this.startAnimation();

        this.isInitialized = true;
        console.log('SceneManager: Initialized with axis helpers and mouse events');
        return true;
    }

    /**
     * Setup local callbacks for mouse events
     */
    setupMouseCallbacks() {
        // Example: Log hover events
        this.mouseHandler.setCallback('onHover', (eventData) => {
            // console.log('Hovering over:', eventData.objectTag);
            // You could change cursor, highlight object, etc.
        });

        this.mouseHandler.setCallback('onHoverEnd', (eventData) => {
            // console.log('Stopped hovering:', eventData.objectTag);
        });

        // You can add more callbacks as needed
    }

    createScene() {
        this.scene = new THREE.Scene();
        this.scene.name = 'MainScene';
    }

    createCamera(container) {
        const { width, height } = this.getConstrainedDimensions(container);

        this.camera = new THREE.PerspectiveCamera(
            60,
            width / height,
            0.1,
            200000  // Increased far plane for infinite axes
        );
        this.camera.position.set(
            this.sceneState.cameraPosition.x,
            this.sceneState.cameraPosition.y,
            this.sceneState.cameraPosition.z
        );

        Object.values(LAYERS).forEach(layer => {
            this.camera.layers.enable(layer);
        });
    }

    createRenderer(container) {
        this.renderer = new THREE.WebGLRenderer({
            antialias: true,
            preserveDrawingBuffer: true
        });

        const { width, height } = this.getConstrainedDimensions(container);

        this.renderer.setSize(width, height);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

        const canvas = this.renderer.domElement;
        canvas.style.display = 'block';
        canvas.style.maxWidth = '100%';
        canvas.style.maxHeight = '100%';
        canvas.style.width = '100%';
        canvas.style.height = '100%';

        container.appendChild(canvas);
        this.canvas = container;

        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();

        console.log(`SceneManager: Renderer created with size ${width}x${height}`);
    }

    createControls() {
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        this.controls.screenSpacePanning = true;
        this.controls.minDistance = 1;
        this.controls.maxDistance = 100000;  // Increased for large scenes
    }

    createLights() {
        this.scene.add(new THREE.AmbientLight(0xffffff, 0.9));
        this.scene.add(new THREE.HemisphereLight(0x9f9f9b, 0x080820, 1));

        const pointLight = new THREE.PointLight(0xffffff, 1, 100);
        pointLight.position.set(0, 0, 100);
        this.scene.add(pointLight);
    }

    createHelpers() {
        // Create infinite axis lines
        this.infiniteAxes = createInfiniteAxes();
        this.infiniteAxes.layers.set(LAYERS.DEFAULT);
        this.scene.add(this.infiniteAxes);

        // Create axis indicator for corner display
        this.axisIndicator = createAxisIndicator();

        // Optional: Add grid helper
        // this.gridHelper = createGridHelper(10000, 100);
        // this.gridHelper.rotation.x = Math.PI / 2;  // Rotate to XY plane
        // this.gridHelper.layers.set(LAYERS.DEFAULT);
        // this.scene.add(this.gridHelper);

        console.log('SceneManager: Axis helpers created');
    }

    /**
     * Reattach existing renderer to a new container
     */
    reattachToContainer(container) {
        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
        }

        // Remove and re-add mouse listeners
        this.mouseHandler.removeEventListeners();

        if (this.renderer && this.renderer.domElement.parentNode) {
            this.renderer.domElement.parentNode.removeChild(this.renderer.domElement);
        }

        const canvas = this.renderer.domElement;
        canvas.style.display = 'block';
        canvas.style.maxWidth = '100%';
        canvas.style.maxHeight = '100%';
        canvas.style.width = '100%';
        canvas.style.height = '100%';

        container.appendChild(canvas);
        this.canvas = container;

        if (this.controls) {
            this.controls.dispose();
            this.controls = new OrbitControls(this.camera, this.renderer.domElement);
            this.controls.enableDamping = true;
            this.controls.screenSpacePanning = true;
        }

        // Re-initialize mouse handler
        this.mouseHandler.initialize(
            this.renderer,
            this.camera,
            this.scene,
            this.dotNetRef
        );
        this.setupMouseCallbacks();

        this.setupResizeObserver(container);
        this.onContainerResize();
    }

    /**
     * Add an object to the scene with layer assignment
     */
    addObject(mesh, tag, layer = LAYERS.DEFAULT) {
        if (!mesh) return;

        mesh.Tag = tag;
        mesh.layers.set(layer);

        if (!this.objectRegistry.has(layer)) {
            this.objectRegistry.set(layer, new Map());
        }
        this.objectRegistry.get(layer).set(tag, mesh);

        this.scene.add(mesh);
    }

    /**
     * Remove an object by tag
     */
    removeObject(tag) {
        for (const [layer, objects] of this.objectRegistry) {
            if (objects.has(tag)) {
                const mesh = objects.get(tag);
                this.disposeObject(mesh);
                this.scene.remove(mesh);
                objects.delete(tag);
                return true;
            }
        }
        return false;
    }

    /**
     * Remove all objects in a specific layer
     */
    clearLayer(layer) {
        const objects = this.objectRegistry.get(layer);
        if (!objects) return;

        for (const [tag, mesh] of objects) {
            this.disposeObject(mesh);
            this.scene.remove(mesh);
        }
        objects.clear();
    }

    /**
     * Show/hide entire layer
     */
    setLayerVisibility(layer, visible) {
        if (!this.camera) {
            console.warn('SceneManager: Camera not initialized, skipping setLayerVisibility');
            return;
        }

        if (visible) {
            this.camera.layers.enable(layer);
        } else {
            this.camera.layers.disable(layer);
        }
    }

    /**
     * Configure visible layers for a specific page
     */
    setPageContext(pageName, visibleLayers) {
        this.currentPage = pageName;

        Object.values(LAYERS).forEach(layer => {
            this.camera.layers.disable(layer);
        });

        this.camera.layers.enable(LAYERS.DEFAULT);
        this.camera.layers.enable(LAYERS.PLOT_PLAN);

        visibleLayers.forEach(layer => {
            this.camera.layers.enable(layer);
        });
    }

    /**
     * Properly dispose of Three.js objects to prevent memory leaks
     */
    disposeObject(obj) {
        if (obj.geometry) {
            obj.geometry.dispose();
        }
        if (obj.material) {
            if (Array.isArray(obj.material)) {
                obj.material.forEach(m => m.dispose());
            } else {
                obj.material.dispose();
            }
        }
        if (obj.texture) {
            obj.texture.dispose();
        }
    }

    /**
     * Save current scene state
     */
    getState() {
        return JSON.stringify({
            cameraPosition: {
                x: this.camera.position.x,
                y: this.camera.position.y,
                z: this.camera.position.z
            },
            cameraRotation: {
                x: this.camera.rotation.x,
                y: this.camera.rotation.y,
                z: this.camera.rotation.z
            },
            controlsTarget: {
                x: this.controls.target.x,
                y: this.controls.target.y,
                z: this.controls.target.z
            },
            rendererWidth: this.renderer.domElement.width,
            rendererHeight: this.renderer.domElement.height
        });
    }

    /**
     * Restore scene state from JSON
     */
    restoreState(stateJson) {
        try {
            const state = JSON.parse(stateJson);
            if (state.cameraPosition) {
                this.camera.position.set(
                    state.cameraPosition.x,
                    state.cameraPosition.y,
                    state.cameraPosition.z
                );
            }
            if (state.controlsTarget && this.controls) {
                this.controls.target.set(
                    state.controlsTarget.x,
                    state.controlsTarget.y,
                    state.controlsTarget.z
                );
            }
        } catch (e) {
            console.error('Failed to restore scene state:', e);
        }
    }

    /**
     * Setup ResizeObserver for container-based resizing
     */
    setupResizeObserver(container) {
        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
        }

        this.resizeObserver = new ResizeObserver((entries) => {
            if (this.resizeTimeout) {
                clearTimeout(this.resizeTimeout);
            }
            this.resizeTimeout = setTimeout(() => {
                this.onContainerResize();
            }, 50);
        });

        this.resizeObserver.observe(container);
    }

    setupEventListeners(container) {
        this.setupResizeObserver(container);

        this.boundWindowResize = () => this.onContainerResize();
        window.addEventListener('resize', this.boundWindowResize);

        this.saveInterval = setInterval(() => {
            if (this.dotNetRef && this.isInitialized) {
                this.dotNetRef.invokeMethodAsync('SaveSceneInfo', this.getState());
            }
        }, 10000);
    }

    /**
     * Handle container resize with min dimensions
     */
    onContainerResize() {
        if (!this.camera || !this.renderer || !this.canvas) return;

        const { width, height } = this.getConstrainedDimensions(this.canvas);

        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();

        this.renderer.setSize(width, height, false);

        const canvas = this.renderer.domElement;
        canvas.style.width = width + 'px';
        canvas.style.height = height + 'px';

        console.log(`SceneManager: Resized to ${width}x${height}`);
    }

    onWindowResize() {
        this.onContainerResize();
    }

    startAnimation() {
        const animate = () => {
            this.animationId = requestAnimationFrame(animate);

            if (this.controls) {
                this.controls.update();
            }

            // Update axis indicator to match camera orientation
            if (this.axisIndicator) {
                this.axisIndicator.update(this.camera);
            }

            // Render main scene
            const { width, height } = this.getConstrainedDimensions(this.canvas);
            this.renderer.setViewport(0, 0, width, height);
            this.renderer.render(this.scene, this.camera);

            // Render axis indicator overlay
            if (this.axisIndicator) {
                this.axisIndicator.render(this.renderer, width, height);
            }
        };
        animate();
    }

    stopAnimation() {
        if (this.animationId) {
            cancelAnimationFrame(this.animationId);
            this.animationId = null;
        }
    }

    /**
     * Complete disposal - only call when app is closing
     */
    dispose() {
        this.stopAnimation();

        // Dispose mouse handler
        this.mouseHandler.dispose();

        // Dispose axis indicator
        if (this.axisIndicator) {
            this.axisIndicator.dispose();
        }

        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
            this.resizeObserver = null;
        }

        if (this.boundWindowResize) {
            window.removeEventListener('resize', this.boundWindowResize);
        }

        if (this.saveInterval) {
            clearInterval(this.saveInterval);
        }

        if (this.resizeTimeout) {
            clearTimeout(this.resizeTimeout);
        }

        for (const [layer, objects] of this.objectRegistry) {
            for (const [tag, mesh] of objects) {
                this.disposeObject(mesh);
            }
        }
        this.objectRegistry.clear();

        if (this.controls) {
            this.controls.dispose();
        }
        if (this.renderer) {
            this.renderer.dispose();
        }

        this.isInitialized = false;
    }

    /**
     * Clear all objects from all layers except infrastructure
     */
    clearAllLayers() {
        console.log('SceneManager: Clearing all layers');

        for (const [layer, objects] of this.objectRegistry) {
            for (const [tag, mesh] of objects) {
                this.disposeObject(mesh);
                this.scene.remove(mesh);
            }
            objects.clear();
        }

        const objectsToRemove = [];
        this.scene.traverse((child) => {
            if (child.Tag && child.isMesh) {
                objectsToRemove.push(child);
            }
        });

        objectsToRemove.forEach(obj => {
            this.disposeObject(obj);
            this.scene.remove(obj);
        });

        console.log('SceneManager: All layers cleared');
    }

    /**
     * Get mouse event handler for external access
     */
    getMouseHandler() {
        return this.mouseHandler;
    }
}

// Singleton instance
const sceneManager = new SceneManager();
window.sceneManager = sceneManager;
export default sceneManager;