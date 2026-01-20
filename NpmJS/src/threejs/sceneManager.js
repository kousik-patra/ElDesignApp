// NpmJS/src/sceneManager.js

import * as THREE from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';

import { createInfiniteAxes, createAxisIndicator, createGridHelper } from './objects/axisHelpers.js';
import { MouseEventHandler } from './events/mouseEvents.js';

import {initPinManager, updatePinScales} from "./objects/refPoint";
import { initPlaneFunctions } from "./functions/planeFunctions";
import { initPinFunctions } from "./functions/pinFunctions";

import { initSegmentFunctions, disposeSegmentFunctions } from "./functions/segmentFunctions";
import Stats from 'stats.js';

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
        this.stats = null;
        this.renderInfo = {
            fps: 0,
            drawCalls: 0,
            triangles: 0,
            geometries: 0,
            textures: 0
        };

        // Mouse event handler
        this.mouseHandler = new MouseEventHandler(this);

        // Axis helpers
        this.infiniteAxes = null;
        this.axisIndicator = null;
        this.gridHelper = null;

        // Track objects by tag for efficient lookup
        this.objectRegistry = new Map();
        this.lastAddedPlaneTag = null;

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
        this.createStats(container);
        this.createControls();
        this.createLights();
        this.createHelpers();
        this.setupShadows(this.renderer, this.scene);

        if (savedStateJson) {
            this.restoreState(savedStateJson);
        }

        this.setupEventListeners(container);

        // Initialize mouse event handler
        this.mouseHandler.initialize(this.renderer, this.camera, this.scene, this.dotNetRef);

        // After scene is created, initialize pin manager
        initPinManager(this.scene);

        // Initialize pin cursor with the same Blazor reference.
        // Initialize pin cursor via WINDOW functions (ensures same module instance)
        if (window.initPinPlacementMode) {
            window.initPinPlacementMode(dotNetObjRef);
            //console.log('[SceneManager] initPinPlacementMode called via window');
        } else {
            console.warn('[SceneManager] window.initPinPlacementMode not available!');
        }

        if (window.setSceneReferences) {
            window.setSceneReferences(this.scene, this.camera, this.renderer);
            //console.log('[SceneManager] setSceneReferences called via window');
        } else {
            console.warn('[SceneManager] window.setSceneReferences not available!');
        }

        // Initialize plane functions (drawPlane, rotatePlane, scalePlane, etc.)
        // This registers all window.* functions for Blazor JSInterop
        initPlaneFunctions(this);

        // Initialize pin functions (addPin, removePin, clearAllPins, etc.)
        // This registers all pin-related window.* functions for Blazor JSInterop
        initPinFunctions(this);

        initSegmentFunctions(this);

        // Setup optional local callbacks
        this.setupMouseCallbacks();

        this.startAnimation();

        this.isInitialized = true;
        this.onContainerResize();
        //console.log('SceneManager: Initialized with axis helpers and mouse events');
        
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

        //console.log(`SceneManager: Renderer created with size ${width}x${height}`);
    }

    /**
     * Create stats panel for performance monitoring
     * Shows FPS, MS (frame time), MB (memory - Chrome only)
     */
    createStats(container) {
        this.stats = new Stats();

        // Panel options: 0 = FPS, 1 = MS, 2 = MB (Chrome only)
        this.stats.showPanel(0);

        // Position: bottom-left corner
        const dom = this.stats.dom;
        dom.style.position = 'absolute';
        dom.style.left = '10px';
        dom.style.bottom = '10px';
        dom.style.top = 'auto';       // Override default top
        dom.style.zIndex = '10000';
        dom.style.cursor = 'pointer';  // Click to cycle panels

        // Ensure container has position for absolute positioning
        if (getComputedStyle(container).position === 'static') {
            container.style.position = 'relative';
        }

        container.appendChild(dom);

        // Add custom info panel
        this.createInfoPanel(container);
    }

    /**
     * Create custom info panel showing render statistics
     */
    createInfoPanel(container) {
        const panel = document.createElement('div');
        panel.id = 'render-info-panel';
        panel.style.cssText = `
        position: absolute;
        left: 90px;
        bottom: 10px;
        background: rgba(0, 0, 0, 0.7);
        color: #0ff;
        font-family: 'Consolas', 'Monaco', monospace;
        font-size: 11px;
        padding: 8px 12px;
        border-radius: 4px;
        z-index: 10000;
        pointer-events: none;
        line-height: 1.4;
    `;
        panel.innerHTML = `
        <div>Draw Calls: <span id="info-drawcalls">0</span></div>
        <div>Triangles: <span id="info-triangles">0</span></div>
        <div>Geometries: <span id="info-geometries">0</span></div>
    `;
        container.appendChild(panel);
        this.infoPanel = panel;
    }

    /**
     * Update the info panel with current render stats
     */
    updateInfoPanel() {
        if (!this.renderer || !this.infoPanel) return;

        const info = this.renderer.info;

        document.getElementById('info-drawcalls').textContent = info.render.calls.toString();
        document.getElementById('info-triangles').textContent = info.render.triangles.toLocaleString();
        document.getElementById('info-geometries').textContent = info.memory.geometries.toString();

        // Store for external access
        this.renderInfo = {
            drawCalls: info.render.calls,
            triangles: info.render.triangles,
            geometries: info.memory.geometries,
            textures: info.memory.textures
        };
    }

    /**
     * Get current render statistics (can be called from Blazor)
     */
    getRenderStats() {
        if (!this.renderer) return null;

        const info = this.renderer.info;
        return {
            fps: this.stats ? Math.round(1000 / this.stats.domElement.textContent) : 0,
            drawCalls: info.render.calls,
            triangles: info.render.triangles,
            geometries: info.memory.geometries,
            textures: info.memory.textures,
            programs: info.programs?.length || 0
        };
    }

    /**
     * Show/hide stats panel
     */
    setStatsVisible(visible) {
        if (this.stats && this.stats.dom) {
            this.stats.dom.style.display = visible ? 'block' : 'none';
        }
        if (this.infoPanel) {
            this.infoPanel.style.display = visible ? 'block' : 'none';
        }
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

        //console.log('SceneManager: Axis helpers created');
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

        // Re-add stats to new container
        if (this.stats && this.stats.dom) {
            container.appendChild(this.stats.dom);
        }
        if (this.infoPanel) {
            container.appendChild(this.infoPanel);
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

        // Re-initialize pin cursor after reattachment
        if (window.initPinPlacementMode) {
            window.initPinPlacementMode(this.dotNetRef);
        }
        if (window.setSceneReferences) {
            window.setSceneReferences(this.scene, this.camera, this.renderer);
        }

        //console.log('SceneManager: Reattached to container');
    }

    /**
     * Add an object to the scene with layer assignment
     */
    addObject(mesh, tag, layer = LAYERS.DEFAULT) {
        if (!mesh) return;

        mesh.layers.set(layer);
        mesh.Tag = tag;

        if (!this.objectRegistry.has(layer)) {
            this.objectRegistry.set(layer, new Map());
        }
        this.objectRegistry.get(layer).set(tag, mesh);

        this.scene.add(mesh);

        // Track last added plane for auto-resolution
        if (layer === LAYERS.PLOT_PLAN && mesh.Type === 'plotplan') {
            this.lastAddedPlaneTag = tag;
        }

        //console.log(`SceneManager: Added object '${tag}' to layer ${layer}`);
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

                // Clear last added if it was this one
                if (this.lastAddedPlaneTag === tag) {
                    this.lastAddedPlaneTag = null;
                }

                //console.log(`SceneManager: Removed object '${tag}'`);
                return true;
            }
        }
        return false;
    }

    /**
     * Get an object by tag
     */
    getObject(tag) {
        for (const [layer, objects] of this.objectRegistry) {
            if (objects.has(tag)) {
                return objects.get(tag);
            }
        }
        return null;
    }


    /**
     * Clear objects from a specific layer
     */
    clearLayer(layer) {
        if (!this.objectRegistry.has(layer)) return;

        const objects = this.objectRegistry.get(layer);
        for (const [tag, mesh] of objects) {
            this.disposeObject(mesh);
            this.scene.remove(mesh);
        }
        objects.clear();

        //console.log(`SceneManager: Cleared layer ${layer}`);
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
                obj.material.forEach(m => {
                    if (m.map) m.map.dispose();
                    m.dispose();
                });
            } else {
                if (obj.material.map) obj.material.map.dispose();
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

        // this.saveInterval = setInterval(() => {
        //     if (this.dotNetRef && this.isInitialized) {
        //         this.dotNetRef.invokeMethodAsync('SaveSceneInfo', this.getState());
        //     }
        // }, 10000);
    }

    /**
     * Handle container resize with min dimensions
     */
    onContainerResize() {
        if (!this.camera || !this.renderer || !this.canvas) return;

        const { width, height } = this.getConstrainedDimensions(this.canvas);

        // Update camera aspect to match actual canvas dimensions
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();

        this.renderer.setSize(width, height, false);

        const canvas = this.renderer.domElement;
        canvas.style.width = width + 'px';
        canvas.style.height = height + 'px';

        // Ensure canvas is aligned to top-left
        canvas.style.position = 'absolute';
        canvas.style.top = '0';
        canvas.style.left = '0';
        
        //console.log(`SceneManager: Resized to ${width}x${height}`);
        if (this.dotNetRef && this.isInitialized) {
            this.dotNetRef.invokeMethodAsync('OnWindowResize', width, height);
        }
    }

    onWindowResize() {
        this.onContainerResize();
    }

    startAnimation() {

        const stats = this.stats;
        const self = this;

        // Throttle info panel updates (every 30 frames)
        let frameCount = 0;
        const INFO_UPDATE_INTERVAL = 30;

        // Store render info before axis indicator overwrites it
        let capturedRenderInfo = { calls: 0, triangles: 0 };
        
        const animate = () => {
            this.animationId = requestAnimationFrame(animate);

            // Start stats measurement
            if (stats) stats.begin();

            // Update controls
            if (this.controls) {
                this.controls.update();
            }

            // Update axis indicator to match camera orientation
            if (this.axisIndicator) {
                this.axisIndicator.update(this.camera);
            }

            // Update pin scales to maintain constant screen size
            updatePinScales(this.camera, this.renderer);

            // Render main scene
            const { width, height } = this.getConstrainedDimensions(this.canvas);
            
            // // Use renderer's actual size instead of recalculating
            // const width = this.renderer.domElement.width;
            // const height = this.renderer.domElement.height;
            //
            // if (this.camera.aspect !== width / height) {
            //     this.camera.aspect = width / height;
            //     this.camera.updateProjectionMatrix();
            // }

            this.renderer.setViewport(0, 0, width, height);
            this.renderer.render(this.scene, this.camera);

            // ⭐ CAPTURE RENDER INFO IMMEDIATELY AFTER MAIN RENDER ⭐
            // (Before axis indicator resets it!)
            capturedRenderInfo = {
                calls: this.renderer.info.render.calls,
                triangles: this.renderer.info.render.triangles,
                geometries: this.renderer.info.memory.geometries,
                textures: this.renderer.info.memory.textures
            };

            // Render axis indicator overlay (this will reset renderer.info)
            if (this.axisIndicator) {
                this.axisIndicator.render(this.renderer, width, height);
            }

            // Update info panel using CAPTURED values (throttled)
            frameCount++;
            if (frameCount >= INFO_UPDATE_INTERVAL) {
                self.updateInfoPanelWithData(capturedRenderInfo);
                frameCount = 0;
            }

            // End stats measurement
            if (stats) stats.end();
            
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
     * Update the info panel with provided render stats
     * (Used to show main scene stats, not axis indicator stats)
     */
    updateInfoPanelWithData(renderInfo) {
        if (!this.infoPanel) return;

        const drawCallsEl = document.getElementById('info-drawcalls');
        const trianglesEl = document.getElementById('info-triangles');
        const geometriesEl = document.getElementById('info-geometries');

        if (drawCallsEl) drawCallsEl.textContent = renderInfo.calls.toString();
        if (trianglesEl) trianglesEl.textContent = renderInfo.triangles.toLocaleString();
        if (geometriesEl) geometriesEl.textContent = renderInfo.geometries.toString();

        // Store for external access
        this.renderInfo = {
            drawCalls: renderInfo.calls,
            triangles: renderInfo.triangles,
            geometries: renderInfo.geometries,
            textures: renderInfo.textures || 0
        };
    }

    /**
     * Complete disposal - only call when app is closing
     */
    dispose() {
        this.stopAnimation();

        // Dispose stats
        if (this.stats && this.stats.dom && this.stats.dom.parentNode) {
            this.stats.dom.parentNode.removeChild(this.stats.dom);
            this.stats = null;
        }

        // Remove info panel
        if (this.infoPanel && this.infoPanel.parentNode) {
            this.infoPanel.parentNode.removeChild(this.infoPanel);
            this.infoPanel = null;
        }

        // Dispose mouse handler
        this.mouseHandler.dispose();
        
        // Dispose pin cursor
        if (window.disposePinPlacementMode) {
            window.disposePinPlacementMode();
        }

        // Dispose Segments
        if (window.disposeSegmentFunctions) {
            window.disposeSegmentFunctions();
        }

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
        //console.log('SceneManager: Clearing all layers');

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

        //console.log('SceneManager: All layers cleared');
    }

    /**
     * Get mouse event handler for external access
     */
    getMouseHandler() {
        return this.mouseHandler;
    }

    setupShadows(renderer, scene) {
        // ============================================================
        // 1. RENDERER - Enable shadow maps
        // ============================================================
        renderer.shadowMap.enabled = true;
        renderer.shadowMap.type = THREE.PCFSoftShadowMap; // Softer shadows
        // Other options:
        // THREE.BasicShadowMap      - Fast, low quality
        // THREE.PCFShadowMap        - Default, medium quality
        // THREE.PCFSoftShadowMap    - Soft edges, better quality
        // THREE.VSMShadowMap        - Very soft, can have artifacts

        // ============================================================
        // 2. DIRECTIONAL LIGHT - Main shadow-casting light
        // ============================================================
        const directionalLight = new THREE.DirectionalLight(0xffffff, 1.0);
        directionalLight.position.set(100, 100, 100); // Adjust based on your scene scale
        directionalLight.castShadow = true;

        // Shadow camera (orthographic) - adjust based on your scene size
        const shadowSize = 500; // Increase for larger scenes
        directionalLight.shadow.camera.left = -shadowSize;
        directionalLight.shadow.camera.right = shadowSize;
        directionalLight.shadow.camera.top = shadowSize;
        directionalLight.shadow.camera.bottom = -shadowSize;
        directionalLight.shadow.camera.near = 0.5;
        directionalLight.shadow.camera.far = 1000;

        // Shadow map resolution (higher = sharper shadows, more GPU)
        directionalLight.shadow.mapSize.width = 2048;  // Default: 512
        directionalLight.shadow.mapSize.height = 2048;

        // Shadow bias (prevents shadow acne)
        directionalLight.shadow.bias = -0.0001;

        scene.add(directionalLight);

        // ============================================================
        // 3. AMBIENT LIGHT - Fill light so shadows aren't pure black
        // ============================================================
        const ambientLight = new THREE.AmbientLight(0x404040, 0.5); // Soft ambient
        scene.add(ambientLight);

        // ============================================================
        // 4. OPTIONAL: Hemisphere light for more natural lighting
        // ============================================================
        const hemiLight = new THREE.HemisphereLight(0xffffff, 0x444444, 0.4);
        hemiLight.position.set(0, 100, 0);
        scene.add(hemiLight);

        //console.log('Shadows configured');

        return { directionalLight, ambientLight, hemiLight };
    }
    
}

// Singleton instance
const sceneManager = new SceneManager();
window.getRenderStats = () => sceneManager.getRenderStats();
window.setStatsVisible = (visible) => sceneManager.setStatsVisible(visible);
window.sceneManager = sceneManager;
export default sceneManager;