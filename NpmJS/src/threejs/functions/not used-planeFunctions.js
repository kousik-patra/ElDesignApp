/**
 * Plot Plan Functions for Three.js (ES Module)
 *
 * Location: NpmJS/src/threejs/functions/not used-planeFunctions.js
 *
 * Handles loading, scaling, positioning, rotating, and removing plot plan images
 */

import * as THREE from 'three';

// ============================================================================
// Module State
// ============================================================================

// Three.js object references (set via initPlotPlanManager)
let _scene = null;
let _renderer = null;
let _camera = null;
let _rendererWidth = 800;
let _rendererHeight = 600;
let _refPointTexts = [];

// Current working plane (the one being configured)
let currentPlaneMesh = null;

// Plot plan configuration
const PlotPlanConfig = {
    defaultOpacity: 1.0,
    defaultElevation: 0,
    namePrefix: 'plotplan',
    maxPlanes: 50
};

// ============================================================================
// Initialization (MUST be called from myThree.js after scene creation)
// ============================================================================

/**
 * Initialize the plot plan manager with Three.js objects
 * Call this once after your scene is created in drawScene3Js
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {THREE.WebGLRenderer} renderer - The renderer
 * @param {THREE.Camera} camera - The main camera
 * @param {number} width - Renderer width
 * @param {number} height - Renderer height
 * @param refPointTexts
 */
export function initPlotPlanManager(scene, renderer, camera, width, height, refPointTexts) {
    _scene = scene;
    _renderer = renderer;
    _camera = camera;
    _rendererWidth = width || 800;
    _rendererHeight = height || 600;
    console.log("PlotPlanManager initialized", { width: _rendererWidth, height: _rendererHeight });
    _refPointTexts = refPointTexts;
}

/**
 * Update renderer dimensions (call on window resize)
 */
export function updateRendererSize(width, height) {
    _rendererWidth = width;
    _rendererHeight = height;
}

// ============================================================================
// Main Plane Functions (exported for use by myThree.js)
// ============================================================================

/**
 * Draw a new plot plan plane in the scene
 * @param {string} name - Display name for the plane
 * @param {string} tag - Unique identifier/tag for the plane
 * @param {string} imageData - Base64 image data or URL
 * @param {number} scaleX - X scale factor
 * @param {number} scaleY - Y scale factor
 * @param {number} centreX - X position
 * @param {number} centreY - Y position
 * @param {number} elevation - Z position (elevation)
 * @param {number} opacity - Transparency (0-1)
 * @returns {THREE.Mesh|null} The created plane mesh or null on error
 */
export function drawPlane(name, tag, imageData, scaleX, scaleY, centreX, centreY, elevation, opacity) {
    try {
        if (!_scene) {
            console.error("drawPlane: Scene not initialized. Call initPlotPlanManager() first.");
            return null;
        }

        if (!imageData) {
            console.error("drawPlane: No image data provided");
            return null;
        }

        // Remove existing plane with same tag if exists
        const existingPlane = _scene.getObjectByName(tag);
        if (existingPlane) {
            console.log(`drawPlane: Removing existing plane with tag '${tag}'`);
            disposePlane(existingPlane);
        }

        // Create texture from image
        const texture = createTextureFromImage(imageData);
        if (!texture) {
            console.error("drawPlane: Failed to create texture");
            return null;
        }

        // Create material
        const material = new THREE.MeshPhongMaterial({
            map: texture,
            color: 0xffffff,
            transparent: true,
            opacity: opacity ?? PlotPlanConfig.defaultOpacity,
            side: THREE.DoubleSide,
            flatShading: true,
            depthWrite: false // Prevents z-fighting with overlapping planes
        });

        // Create geometry
        const geometry = new THREE.PlaneGeometry(_rendererWidth, _rendererHeight);

        // Create mesh
        const planeMesh = new THREE.Mesh(geometry, material);
        planeMesh.name = tag;
        planeMesh.Tag = tag; // For compatibility with existing code
        planeMesh.userData = {
            displayName: name,
            tag: tag,
            type: 'plotplan',
            originalScale: { x: scaleX, y: scaleY },
            originalPosition: { x: centreX, y: centreY, z: elevation }
        };

        // Set initial position
        planeMesh.position.set(-centreX, -centreY, elevation ?? PlotPlanConfig.defaultElevation);

        // Apply scale
        if (scaleX && scaleY) {
            planeMesh.scale.set(scaleX, scaleY, 1);
        }

        // Add to scene
        _scene.add(planeMesh);
        planeMesh.updateMatrixWorld();

        // Store as current working plane
        currentPlaneMesh = planeMesh;

        // Render
        renderScene();

        console.log(`drawPlane: Created plane '${name}' with tag '${tag}'`);
        return planeMesh;

    } catch (error) {
        console.error("drawPlane error:", error);
        return null;
    }
}

/**
 * Scale the current working plane
 * @param {number} scaleX - X scale factor
 * @param {number} scaleY - Y scale factor
 */
export function scalePlane(scaleX, scaleY) {
    try {
        if (!currentPlaneMesh) {
            console.warn("scalePlane: No current plane to scale");
            return;
        }

        if (!scaleX || !scaleY || scaleX === 0 || scaleY === 0) {
            console.warn("scalePlane: Invalid scale values", { scaleX, scaleY });
            return;
        }

        currentPlaneMesh.scale.set(scaleX, scaleY, 1);
        currentPlaneMesh.updateMatrixWorld();

        // Store original scale in userData
        if (currentPlaneMesh.userData) {
            currentPlaneMesh.userData.originalScale = { x: scaleX, y: scaleY };
        }

        // Clear any clicked reference points
        clearClickedPoints();

        renderScene();
        console.log(`scalePlane: Scaled to (${scaleX}, ${scaleY})`);

    } catch (error) {
        console.error("scalePlane error:", error);
    }
}

/**
 * Position/centre the current working plane
 * @param {number} centreX - X position
 * @param {number} centreY - Y position
 * @param {number} elevation - Z position
 */
export function centrePlane(centreX, centreY, elevation) {
    try {
        if (!currentPlaneMesh) {
            console.warn("centrePlane: No current plane to centre");
            return;
        }

        const z = elevation ?? currentPlaneMesh.position.z;
        currentPlaneMesh.position.set(-centreX, -centreY, z);
        currentPlaneMesh.updateMatrixWorld();

        // Store original position in userData
        if (currentPlaneMesh.userData) {
            currentPlaneMesh.userData.originalPosition = { x: centreX, y: centreY, z: z };
        }

        // Clear any clicked reference points
        clearClickedPoints();

        renderScene();
        console.log(`centrePlane: Positioned to (${centreX}, ${centreY}, ${z})`);

    } catch (error) {
        console.error("centrePlane error:", error);
    }
}

/**
 * Rotate the current working plane
 * @param {number} rotation - Rotation angle in radians
 */
export function rotatePlane(rotation) {
    try {
        if (!currentPlaneMesh) {
            console.warn("rotatePlane: No current plane to rotate");
            return;
        }

        currentPlaneMesh.rotation.z = rotation;
        currentPlaneMesh.updateMatrixWorld();

        renderScene();
        console.log(`rotatePlane: Rotated to ${(rotation * 180 / Math.PI).toFixed(1)} degrees`);

    } catch (error) {
        console.error("rotatePlane error:", error);
    }
}

/**
 * Remove a specific plot plan by tag
 * @param {string} tag - The tag/name of the plane to remove
 * @returns {boolean} True if removed successfully
 */
export function removePlane(tag) {
    try {
        if (!tag) {
            console.warn("removePlane: No tag provided");
            return false;
        }

        if (!_scene) {
            console.error("removePlane: Scene not available");
            return false;
        }

        const plane = _scene.getObjectByName(tag);
        if (!plane) {
            console.warn(`removePlane: No plane found with tag '${tag}'`);
            return false;
        }

        disposePlane(plane);

        // Clear current plane reference if it was the one removed
        if (currentPlaneMesh && currentPlaneMesh.name === tag) {
            currentPlaneMesh = null;
        }

        renderScene();
        console.log(`removePlane: Removed plane '${tag}'`);
        return true;

    } catch (error) {
        console.error("removePlane error:", error);
        return false;
    }
}

/**
 * Remove all plot plans from the scene
 * @returns {number} Number of planes removed
 */
export function removeAllPlotPlans() {
    try {
        const plotPlans = getAllPlotPlans();
        let removedCount = 0;

        plotPlans.forEach(plane => {
            disposePlane(plane);
            removedCount++;
        });

        currentPlaneMesh = null;

        renderScene();
        console.log(`removeAllPlotPlans: Removed ${removedCount} planes`);
        return removedCount;

    } catch (error) {
        console.error("removeAllPlotPlans error:", error);
        return 0;
    }
}

/**
 * Reposition all plot plans by a delta offset (used when key plan changes)
 * @param {number} deltaX - X offset to apply
 * @param {number} deltaY - Y offset to apply
 */
export function repositionAllPlanes(deltaX, deltaY) {
    try {
        const plotPlans = getAllPlotPlans();

        plotPlans.forEach(plane => {
            plane.position.x -= deltaX;
            plane.position.y -= deltaY;
            plane.updateMatrixWorld();
        });

        renderScene();
        console.log(`repositionAllPlanes: Moved ${plotPlans.length} planes by (${deltaX}, ${deltaY})`);

    } catch (error) {
        console.error("repositionAllPlanes error:", error);
    }
}

/**
 * Set opacity of a specific plane
 * @param {string} tag - The tag of the plane
 * @param {number} opacity - Opacity value (0-1)
 */
export function setPlaneOpacity(tag, opacity) {
    try {
        if (!_scene) return;

        const plane = _scene.getObjectByName(tag);
        if (!plane) {
            console.warn(`setPlaneOpacity: No plane found with tag '${tag}'`);
            return;
        }

        if (plane.material) {
            plane.material.opacity = Math.max(0, Math.min(1, opacity));
            plane.material.needsUpdate = true;
        }

        renderScene();
        console.log(`setPlaneOpacity: Set '${tag}' opacity to ${opacity}`);

    } catch (error) {
        console.error("setPlaneOpacity error:", error);
    }
}

/**
 * Toggle visibility of a specific plane
 * @param {string} tag - The tag of the plane
 * @param {boolean} visible - Whether the plane should be visible
 */
export function setPlaneVisibility(tag, visible) {
    try {
        if (!_scene) return;

        const plane = _scene.getObjectByName(tag);
        if (!plane) {
            console.warn(`setPlaneVisibility: No plane found with tag '${tag}'`);
            return;
        }

        plane.visible = visible;
        renderScene();
        console.log(`setPlaneVisibility: Set '${tag}' visibility to ${visible}`);

    } catch (error) {
        console.error("setPlaneVisibility error:", error);
    }
}

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Create a Three.js texture from image data
 * @param {string} imageData - Base64 image data or URL
 * @returns {THREE.Texture|null}
 */
function createTextureFromImage(imageData) {
    try {
        const image = new Image();
        image.src = imageData;

        const texture = new THREE.Texture(image);

        // Set color space if available (Three.js r152+)
        if (THREE.SRGBColorSpace) {
            texture.colorSpace = THREE.SRGBColorSpace;
        }

        image.onload = function() {
            texture.needsUpdate = true;
            renderScene();
        };

        image.onerror = function() {
            console.error("createTextureFromImage: Failed to load image");
        };

        return texture;

    } catch (error) {
        console.error("createTextureFromImage error:", error);
        return null;
    }
}

/**
 * Properly dispose of a plane mesh and its resources
 * @param {THREE.Mesh} plane - The plane mesh to dispose
 */
function disposePlane(plane) {
    if (!plane) return;

    if (_scene) {
        _scene.remove(plane);
    }

    // Dispose geometry
    if (plane.geometry) {
        plane.geometry.dispose();
    }

    // Dispose material and textures
    if (plane.material) {
        if (plane.material.map) {
            plane.material.map.dispose();
        }
        if (plane.material.normalMap) {
            plane.material.normalMap.dispose();
        }
        if (plane.material.specularMap) {
            plane.material.specularMap.dispose();
        }
        plane.material.dispose();
    }
}

/**
 * Get all plot plan meshes in the scene
 * @returns {THREE.Mesh[]} Array of plot plan meshes
 */
function getAllPlotPlans() {
    if (!_scene) return [];

    const plotPlans = [];

    _scene.traverse(function(object) {
        if (object.isMesh) {
            // Check userData.type
            if (object.userData && object.userData.type === 'plotplan') {
                plotPlans.push(object);
            }
            // Also check Tag property (for compatibility)
            else if (object.Tag && object.Tag.includes('plotplan')) {
                plotPlans.push(object);
            }
            // Also check name prefix
            else if (object.name && object.name.startsWith(PlotPlanConfig.namePrefix)) {
                plotPlans.push(object);
            }
        }
    });

    return plotPlans;
}

/**
 * Get count of plot plans in the scene
 * @returns {number}
 */
export function getPlotPlanCount() {
    return getAllPlotPlans().length;
}

/**
 * Clear all clicked reference points from the scene
 */
function clearClickedPoints() {
    if (!_scene) return;

    let point = _scene.getObjectByName('clickedPoints');
    while (point) {
        _scene.remove(point);
        if (point.geometry) point.geometry.dispose();
        if (point.material) point.material.dispose();
        point = _scene.getObjectByName('clickedPoints');
    }
}

/**
 * Render the scene
 */
function renderScene() {
    if (_renderer && _scene && _camera) {
        _renderer.render(_scene, _camera);
    }
}

/**
 * Get information about all plot plans (for debugging)
 * @returns {Object[]}
 */
export function getPlotPlanInfo() {
    return getAllPlotPlans().map(plane => ({
        name: plane.name,
        tag: plane.Tag,
        displayName: plane.userData?.displayName,
        position: {
            x: plane.position.x,
            y: plane.position.y,
            z: plane.position.z
        },
        scale: {
            x: plane.scale.x,
            y: plane.scale.y
        },
        rotation: plane.rotation.z,
        visible: plane.visible,
        opacity: plane.material?.opacity
    }));
}

/**
 * Log all plot plans to console (for debugging)
 */
export function debugPlotPlans() {
    const info = getPlotPlanInfo();
    console.log("=== Plot Plans ===");
    console.log(`Total: ${info.length}`);
    info.forEach((p, i) => {
        console.log(`[${i}] ${p.name} (${p.displayName || p.tag})`);
        console.log(`    Position: (${p.position.x.toFixed(2)}, ${p.position.y.toFixed(2)}, ${p.position.z.toFixed(2)})`);
        console.log(`    Scale: (${p.scale.x.toFixed(4)}, ${p.scale.y.toFixed(4)})`);
        console.log(`    Rotation: ${(p.rotation * 180 / Math.PI).toFixed(1)}°`);
        console.log(`    Visible: ${p.visible}, Opacity: ${p.opacity}`);
    });
    console.log("==================");
    return info;
}

/**
 * Update Ref Clicked Points texts
 */
export function updateRefPointTexts(texts) {
    _refPointTexts = texts;
    return info;
}


// ============================================================================
// Default Export (all functions as object)
// ============================================================================

export default {
    initPlotPlanManager,
    updateRendererSize,
    drawPlane,
    scalePlane,
    centrePlane,
    rotatePlane,
    removePlane,
    removeAllPlotPlans,
    repositionAllPlanes,
    setPlaneOpacity,
    setPlaneVisibility,
    getPlotPlanCount,
    getPlotPlanInfo,
    debugPlotPlans,
    updateRefPointTexts
};