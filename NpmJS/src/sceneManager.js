// NpmJS/src/sceneManager.js

import * as THREE from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';

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
     * Initialize or reattach the scene to a canvas element
     */
    initialize(canvasContainerId, dotNetObjRef, savedStateJson = '') {
        this.dotNetRef = dotNetObjRef;

        const container = document.getElementById(canvasContainerId);
        if (!container) {
            console.error(`Container ${canvasContainerId} not found`);
            return false;
        }

        // If already initialized, just reattach to new container
        if (this.isInitialized && this.renderer) {
            this.reattachToContainer(container);
            return true;
        }

        // First-time initialization
        this.createScene();
        this.createCamera();
        this.createRenderer(container);
        this.createControls();
        this.createLights();
        this.createHelpers();

        // Restore saved state if provided
        if (savedStateJson) {
            this.restoreState(savedStateJson);
        }

        this.setupEventListeners();
        this.startAnimation();

        this.isInitialized = true;
        return true;
    }

    createScene() {
        this.scene = new THREE.Scene();
        this.scene.name = 'MainScene';
    }

    createCamera() {
        this.camera = new THREE.PerspectiveCamera(
            60,
            window.innerWidth / window.innerHeight,
            0.1,
            20000
        );
        this.camera.position.set(
            this.sceneState.cameraPosition.x,
            this.sceneState.cameraPosition.y,
            this.sceneState.cameraPosition.z
        );

        // Enable all layers by default
        Object.values(LAYERS).forEach(layer => {
            this.camera.layers.enable(layer);
        });
    }


    createRenderer(container) {
        this.renderer = new THREE.WebGLRenderer({
            antialias: true,
            preserveDrawingBuffer: true
        });

        // Use container size, not window size
        const rect = container.getBoundingClientRect();
        const width = rect.width || 800;
        const height = rect.height || 600;

        this.renderer.setSize(width, height);
        this.renderer.setPixelRatio(window.devicePixelRatio);
        container.appendChild(this.renderer.domElement);
        this.canvas = container;

        // Update camera aspect
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
    }
    
    
    

    createControls() {
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        this.controls.screenSpacePanning = true;
        this.controls.minDistance = 1;
        this.controls.maxDistance = 10000;
    }

    createLights() {
        this.scene.add(new THREE.AmbientLight(0xffffff, 0.9));
        this.scene.add(new THREE.HemisphereLight(0x9f9f9b, 0x080820, 1));

        const pointLight = new THREE.PointLight(0xffffff, 1, 100);
        pointLight.position.set(0, 0, 100);
        this.scene.add(pointLight);
    }

    createHelpers() {
        const axesHelper = new THREE.AxesHelper(500);
        axesHelper.layers.set(LAYERS.DEFAULT);
        this.scene.add(axesHelper);
    }

    /**
     * Reattach existing renderer to a new container
     */
    reattachToContainer(container) {
        if (this.renderer && this.renderer.domElement.parentNode) {
            this.renderer.domElement.parentNode.removeChild(this.renderer.domElement);
        }
        container.appendChild(this.renderer.domElement);
        this.canvas = container;

        // Update controls
        if (this.controls) {
            this.controls.dispose();
            this.controls = new OrbitControls(this.camera, this.renderer.domElement);
            this.controls.enableDamping = true;
            this.controls.screenSpacePanning = true;
        }

        this.onWindowResize();
    }

    /**
     * Add an object to the scene with layer assignment
     */
    addObject(mesh, tag, layer = LAYERS.DEFAULT) {
        if (!mesh) return;

        mesh.Tag = tag;
        mesh.layers.set(layer);

        // Store in registry for efficient lookup
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

        // Disable all layers first
        Object.values(LAYERS).forEach(layer => {
            this.camera.layers.disable(layer);
        });

        // Enable only specified layers
        this.camera.layers.enable(LAYERS.DEFAULT); // Always show default
        this.camera.layers.enable(LAYERS.PLOT_PLAN); // Always show plot plan

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

    setupEventListeners() {
        window.addEventListener('resize', () => this.onWindowResize());

        // Periodic state save to server
        setInterval(() => {
            if (this.dotNetRef && this.isInitialized) {
                this.dotNetRef.invokeMethodAsync('SaveSceneInfo', this.getState());
            }
        }, 10000);
    }

    onWindowResize() {
        if (!this.camera || !this.renderer || !this.canvas) return;

        const rect = this.canvas.getBoundingClientRect();
        const width = rect.width || 800;
        const height = rect.height || 600;

        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
    }

    startAnimation() {
        const animate = () => {
            this.animationId = requestAnimationFrame(animate);
            if (this.controls) {
                this.controls.update();
            }
            this.renderer.render(this.scene, this.camera);
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

        // Dispose all objects
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
}


/**
 * Clear all objects from all layers except infrastructure (lights, axes, helpers)
 */
clearAllLayers() 
{
    console.log('SceneManager: Clearing all layers');

    // Clear all registered objects
    for (const [layer, objects] of this.objectRegistry) {
        for (const [tag, mesh] of objects) {
            this.disposeObject(mesh);
            this.scene.remove(mesh);
        }
        objects.clear();
    }

    // Also remove any objects with Tags that aren't in registry
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



// Singleton instance
const sceneManager = new SceneManager();
window.sceneManager = sceneManager;
export default sceneManager;