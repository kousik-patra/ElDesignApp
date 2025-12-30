/**
 * Plot Plan Management for Three.js
 * Handles loading, scaling, positioning, rotating, and removing plot plan images
 *
 * Dependencies: Three.js
 *
 * Setup (call once after scene is created):
 *   initPlotPlanManager(scene, renderer, camera, rendererWidth, rendererHeight)
 *
 * Usage:
 *   drawPlane(name, tag, imageData, scaleX, scaleY, centreX, centreY, elevation, opacity)
 *   scalePlane(scaleX, scaleY)
 *   centrePlane(centreX, centreY, elevation)
 *   rotatePlane(rotation)
 *   removePlane(tag)
 *   removeAllPlotPlans()
 *   repositionAllPlanes(deltaX, deltaY)
 */

// ============================================================================
// Global State & References
// ============================================================================

// Three.js object references (set via initPlotPlanManager or auto-detected)
let _scene = null;
let _renderer = null;
let _camera = null;
let _orthoCamera = null;
let _rendererWidth = 800;
let _rendererHeight = 600;

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
// Initialization
// ============================================================================

/**
 * Initialize the plot plan manager with Three.js objects
 * Call this once after your scene is created
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {THREE.WebGLRenderer} renderer - The renderer
 * @param {THREE.Camera} camera - The main camera
 * @param {number} width - Renderer width
 * @param {number} height - Renderer height
 */
function initPlotPlanManager(sceneObj, rendererObj, cameraObj, width, height) {
    _scene = sceneObj;
    _renderer = rendererObj;
    _camera = cameraObj;
    _rendererWidth = width || 800;
    _rendererHeight = height || 600;
    console.log("PlotPlanManager initialized", { width: _rendererWidth, height: _rendererHeight });
}

/**
 * Get the scene object (tries multiple sources)
 * @returns {THREE.Scene|null}
 */
function getScene() {
    // Try our stored reference first
    if (_scene) return _scene;

    // Try global 'scene' variable
    if (typeof scene !== 'undefined' && scene) {
        _scene = scene;
        return _scene;
    }

    // Try window.scene
    if (typeof window !== 'undefined' && window.scene) {
        _scene = window.scene;
        return _scene;
    }

    console.error("getScene: Scene not found. Call initPlotPlanManager() first or ensure 'scene' is global.");
    return null;
}

/**
 * Get the renderer object
 * @returns {THREE.WebGLRenderer|null}
 */
function getRenderer() {
    if (_renderer) return _renderer;
    if (typeof renderer !== 'undefined' && renderer) {
        _renderer = renderer;
        return _renderer;
    }
    if (typeof window !== 'undefined' && window.renderer) {
        _renderer = window.renderer;
        return _renderer;
    }
    console.warn("getRenderer: Renderer not found");
    return null;
}

/**
 * Get the camera object
 * @returns {THREE.Camera|null}
 */
function getCamera() {
    if (_camera) return _camera;
    if (typeof camera !== 'undefined' && camera) {
        _camera = camera;
        return _camera;
    }
    if (typeof window !== 'undefined' && window.camera) {
        _camera = window.camera;
        return _camera;
    }
    console.warn("getCamera: Camera not found");
    return null;
}

/**
 * Get renderer dimensions
 */
function getRendererWidth() {
    if (_rendererWidth) return _rendererWidth;
    if (typeof rendererWidth !== 'undefined') return rendererWidth;
    if (typeof window !== 'undefined' && window.rendererWidth) return window.rendererWidth;
    return 800;
}

function getRendererHeight() {
    if (_rendererHeight) return _rendererHeight;
    if (typeof rendererHeight !== 'undefined') return rendererHeight;
    if (typeof window !== 'undefined' && window.rendererHeight) return window.rendererHeight;
    return 600;
}

/**
 * Render the scene
 */
function renderScene() {
    const r = getRenderer();
    const s = getScene();
    const c = getCamera();
    if (r && s && c) {
        r.render(s, c);
    }
}

// ============================================================================
// Main Functions (called from Blazor)
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
function drawPlane(name, tag, imageData, scaleX, scaleY, centreX, centreY, elevation, opacity) {
    try {
        const sceneObj = getScene();
        if (!sceneObj) {
            console.error("drawPlane: Scene not initialized. Call initPlotPlanManager() first.");
            return null;
        }

        if (!imageData) {
            console.error("drawPlane: No image data provided");
            return null;
        }

        // Remove existing plane with same tag if exists
        const existingPlane = sceneObj.getObjectByName(tag);
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
        const rWidth = getRendererWidth();
        const rHeight = getRendererHeight();
        const geometry = new THREE.PlaneGeometry(rWidth, rHeight);

        // Create mesh
        const planeMesh = new THREE.Mesh(geometry, material);
        planeMesh.name = tag;
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
        sceneObj.add(planeMesh);
        planeMesh.updateMatrixWorld();

        // Store as current working plane
        currentPlaneMesh = planeMesh;

        // Add shadow ring if this is the first/key plan
        const plotPlanCount = getPlotPlanCount();
        if (plotPlanCount === 1 && typeof shadowRing === 'function') {
            shadowRing(0, 0, (elevation || 0) + 0.01);
        }

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
function scalePlane(scaleX, scaleY) {
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
        currentPlaneMesh.userData.originalScale = { x: scaleX, y: scaleY };

        // Clear any clicked reference points
        clearClickedPoints();

        // Update orthographic camera if exists
        const rWidth = getRendererWidth();
        const rHeight = getRendererHeight();
        if (_orthoCamera || (typeof orthoCamera !== 'undefined')) {
            _orthoCamera = new THREE.OrthographicCamera(
                rWidth / -2, rWidth / 2,
                rHeight / 2, rHeight / -2,
                1, 1000
            );
        }

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
function centrePlane(centreX, centreY, elevation) {
    try {
        if (!currentPlaneMesh) {
            console.warn("centrePlane: No current plane to centre");
            return;
        }

        const z = elevation ?? currentPlaneMesh.position.z;
        currentPlaneMesh.position.set(-centreX, -centreY, z);
        currentPlaneMesh.updateMatrixWorld();

        // Store original position in userData
        currentPlaneMesh.userData.originalPosition = { x: centreX, y: centreY, z: z };

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
function rotatePlane(rotation) {
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
function removePlane(tag) {
    try {
        if (!tag) {
            console.warn("removePlane: No tag provided");
            return false;
        }

        const sceneObj = getScene();
        if (!sceneObj) {
            console.error("removePlane: Scene not available");
            return false;
        }

        const plane = sceneObj.getObjectByName(tag);
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
function removeAllPlotPlans() {
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
function repositionAllPlanes(deltaX, deltaY) {
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
function setPlaneOpacity(tag, opacity) {
    try {
        const sceneObj = getScene();
        if (!sceneObj) return;

        const plane = sceneObj.getObjectByName(tag);
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
function setPlaneVisibility(tag, visible) {
    try {
        const sceneObj = getScene();
        if (!sceneObj) return;

        const plane = sceneObj.getObjectByName(tag);
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
// Legacy Function Aliases (for backward compatibility)
// ============================================================================

/**
 * @deprecated Use drawPlane instead
 */
function loadPlotPlan(userImageFile, scaleX, scaleY, centreX, centreY, elevation, opacity) {
    console.warn("loadPlotPlan is deprecated, use drawPlane instead");
    const tag = PlotPlanConfig.namePrefix + (getPlotPlanCount() + 1).toString().padStart(2, '0');
    return drawPlane("Plot Plan", tag, userImageFile, scaleX, scaleY, centreX, centreY, elevation, opacity);
}

/**
 * @deprecated Use scalePlane instead
 */
function scalePlotPlan(scaleX, scaleY) {
    console.warn("scalePlotPlan is deprecated, use scalePlane instead");
    scalePlane(scaleX, scaleY);
}

/**
 * @deprecated Use centrePlane instead
 */
function centrePlotPlan(centreX, centreY, elevation) {
    console.warn("centrePlotPlan is deprecated, use centrePlane instead");
    centrePlane(centreX, centreY, elevation);
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

    const sceneObj = getScene();
    if (sceneObj) {
        sceneObj.remove(plane);
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
    const sceneObj = getScene();
    if (!sceneObj) return [];

    const plotPlans = [];

    sceneObj.traverse(function(object) {
        if (object.isMesh && object.userData && object.userData.type === 'plotplan') {
            plotPlans.push(object);
        }
    });

    // Fallback: also check by name prefix
    if (plotPlans.length === 0) {
        sceneObj.traverse(function(object) {
            if (object.isMesh && object.name && object.name.startsWith(PlotPlanConfig.namePrefix)) {
                plotPlans.push(object);
            }
        });
    }

    return plotPlans;
}

/**
 * Get count of plot plans in the scene
 * @returns {number}
 */
function getPlotPlanCount() {
    return getAllPlotPlans().length;
}

/**
 * Clear all clicked reference points from the scene
 */
function clearClickedPoints() {
    const sceneObj = getScene();
    if (!sceneObj) return;

    let point = sceneObj.getObjectByName('clickedPoints');
    while (point) {
        sceneObj.remove(point);
        if (point.geometry) point.geometry.dispose();
        if (point.material) point.material.dispose();
        point = sceneObj.getObjectByName('clickedPoints');
    }
}

/**
 * Get objects by name (returns array of all matching objects)
 * @param {THREE.Scene} sceneObj - The scene to search
 * @param {string} name - Name to search for
 * @returns {THREE.Object3D[]}
 */
function getObjectByNameArray(sceneObj, name) {
    const objects = [];
    if (!sceneObj) return objects;

    sceneObj.traverse(function(object) {
        if (object.name === name || (object.name && object.name.startsWith(name))) {
            objects.push(object);
        }
    });
    return objects;
}

/**
 * Get information about all plot plans (for debugging)
 * @returns {Object[]}
 */
function getPlotPlanInfo() {
    return getAllPlotPlans().map(plane => ({
        name: plane.name,
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

// ============================================================================
// Debug/Development Helpers
// ============================================================================

/**
 * Log all plot plans to console (for debugging)
 */
function debugPlotPlans() {
    const info = getPlotPlanInfo();
    console.log("=== Plot Plans ===");
    console.log(`Total: ${info.length}`);
    info.forEach((p, i) => {
        console.log(`[${i}] ${p.name} (${p.displayName})`);
        console.log(`    Position: (${p.position.x.toFixed(2)}, ${p.position.y.toFixed(2)}, ${p.position.z.toFixed(2)})`);
        console.log(`    Scale: (${p.scale.x.toFixed(4)}, ${p.scale.y.toFixed(4)})`);
        console.log(`    Rotation: ${(p.rotation * 180 / Math.PI).toFixed(1)}°`);
        console.log(`    Visible: ${p.visible}, Opacity: ${p.opacity}`);
    });
    console.log("==================");
    return info;
}

// Export for module usage (if using ES modules)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        initPlotPlanManager,
        drawPlane,
        scalePlane,
        centrePlane,
        rotatePlane,
        removePlane,
        removeAllPlotPlans,
        repositionAllPlanes,
        setPlaneOpacity,
        setPlaneVisibility,
        getPlotPlanInfo,
        debugPlotPlans
    };
}

// Also expose to window for global access
if (typeof window !== 'undefined') {
    window.initPlotPlanManager = initPlotPlanManager;
    window.drawPlane = drawPlane;
    window.scalePlane = scalePlane;
    window.centrePlane = centrePlane;
    window.rotatePlane = rotatePlane;
    window.removePlane = removePlane;
    window.removeAllPlotPlans = removeAllPlotPlans;
    window.repositionAllPlanes = repositionAllPlanes;
    window.setPlaneOpacity = setPlaneOpacity;
    window.setPlaneVisibility = setPlaneVisibility;
    window.getPlotPlanInfo = getPlotPlanInfo;
    window.debugPlotPlans = debugPlotPlans;

    // Legacy aliases
    window.loadPlotPlan = loadPlotPlan;
    window.scalePlotPlan = scalePlotPlan;
    window.centrePlotPlan = centrePlotPlan;
}