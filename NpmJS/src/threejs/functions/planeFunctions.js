/**
 * planeFunctions.js - Window Function Registration for Plane Operations
 *
 * This module registers all plane-related window functions that can be called
 * from Blazor via JSInterop.
 *
 * Usage in index.js:
 *   import { initPlaneFunctions } from './threejs/objects/planeFunctions';
 *
 *   // After sceneManager is initialized:
 *   initPlaneFunctions(sceneManager);
 *
 * Blazor calls:
 *   await JsRuntime.InvokeVoidAsync("drawPlane", tag, desc, imgString, scaleX, scaleY, centreX, centreY, z, opacity);
 *   await JsRuntime.InvokeVoidAsync("rotatePlane", angleRadians);
 *   await JsRuntime.InvokeVoidAsync("scalePlane", scaleX, scaleY);
 *   await JsRuntime.InvokeVoidAsync("centrePlane", centreX, centreY, elevation);
 *   await JsRuntime.InvokeVoidAsync("removePlane", planeTag);
 *   await JsRuntime.InvokeVoidAsync("repositionAllPlanes", deltaX, deltaY);
 */

import {
    drawPlaneMesh,
    rotatePlane,
    setPlaneRotation,
    scalePlane,
    centrePlane,
    setPlaneElevation,
    setPlaneOpacity,
    removePlane,
    repositionAllPlanes,
    getPlaneInfo,
    findPlaneByTag,
    findAllPlanes
} from '../objects/plane';

import { LAYERS } from '../sceneManager';

// ============================================================================
// MODULE STATE
// ============================================================================

let _sceneManager = null;

// ============================================================================
// INITIALIZATION
// ============================================================================

/**
 * Initialize plane functions with the scene manager reference
 * Call this after your sceneManager is ready
 *
 * @param {Object} sceneManager - Your Three.js scene manager instance
 */
function initPlaneFunctions(sceneManager) {
    if (!sceneManager) {
        console.error('[planeFunctions] initPlaneFunctions: sceneManager is required');
        return;
    }

    _sceneManager = sceneManager;
    registerWindowFunctions();

    console.log('[planeFunctions] Plane functions initialized');
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Get the scene from scene manager
 */
function getScene() {
    return _sceneManager?.scene || null;
}

/**
 * Get renderer dimensions
 */
function getRendererDimensions() {
    return {
        width: _sceneManager?.renderer?.domElement?.width || 800,
        height: _sceneManager?.renderer?.domElement?.height || 600
    };
}

/**
 * Resolve plane tag - uses provided tag or falls back to last added plane
 */
function resolveTag(planeTag) {
    if (planeTag) return planeTag;

    // Use sceneManager's tracked last added plane
    if (_sceneManager?.lastAddedPlaneTag) return _sceneManager.lastAddedPlaneTag;

    // Fallback: Try to find the last plane in the scene
    const scene = getScene();
    if (scene) {
        const planes = findAllPlanes(scene);
        if (planes.length > 0) {
            return planes[planes.length - 1].Tag;
        }
    }

    return null;
}

/**
 * Validate scene manager is initialized
 */
function validateSceneManager(functionName) {
    if (!_sceneManager || !_sceneManager.scene) {
        console.error(`[planeFunctions] ${functionName}: sceneManager not initialized. Call initPlaneFunctions() first.`);
        return false;
    }
    return true;
}

// ============================================================================
// WINDOW FUNCTION REGISTRATION
// ============================================================================

function registerWindowFunctions() {

    /**
     * Draw a new plot plan
     * @param {string} planeTag - Unique identifier
     * @param {string} planeTagDescription - Description
     * @param {string} imageString - Base64 image data
     * @param angleOfRotationRad
     * @param {number} scaleX - X scale factor
     * @param {number} scaleY - Y scale factor
     * @param {number} centreX - X centre offset
     * @param {number} centreY - Y centre offset
     * @param {number} elevation - Z position
     * @param {number} opacity - Opacity 0-1
     */
    window.drawPlane = function(planeTag, planeTagDescription, imageString, angleOfRotationRad, scaleX, scaleY, centreX, centreY, elevation, opacity) {
        if (!validateSceneManager('drawPlane')) return false;

        const { width, height } = getRendererDimensions();
        const scene = getScene();

        // Create the mesh
        const mesh = drawPlaneMesh(
            width,
            height,
            planeTag,
            planeTagDescription,
            imageString,
            elevation,
            opacity
        );

        if (!mesh) {
            console.error(`[planeFunctions] drawPlane: Failed to create mesh for '${planeTag}'`);
            return false;
        }

        // Add to scene via sceneManager (this also tracks lastAddedPlaneTag)
        _sceneManager.addObject(mesh, planeTag, LAYERS.PLOT_PLAN);

        // Apply rotation if provided and not default
        if (angleOfRotationRad && angleOfRotationRad !== 0) {
            rotatePlane(scene, planeTag, angleOfRotationRad);
        }

        // Apply scale if provided and not default
        if (scaleX && scaleY && (scaleX !== 1 || scaleY !== 1)) {
            scalePlane(scene, planeTag, scaleX, scaleY);
        }

        // Apply centre if provided and not zero
        if (centreX || centreY) {
            centrePlane(scene, planeTag, centreX, centreY, elevation);
        }

        console.log(`[planeFunctions] drawPlane: '${planeTag}' added`);
        return true;
    };

    /**
     * Rotate plane by angle (incremental)
     * @param {number} angleRadians - Rotation in radians
     * @param {string} [planeTag] - Optional, uses last added if not specified
     */
    window.rotatePlane = function(angleRadians, planeTag = null) {
        if (!validateSceneManager('rotatePlane')) return false;

        const tag = resolveTag(planeTag);
        if (!tag) {
            console.error('[planeFunctions] rotatePlane: No plane tag available');
            return false;
        }

        return rotatePlane(getScene(), tag, angleRadians);
    };

    /**
     * Set absolute rotation of plane
     * @param {number} angleRadians - Absolute rotation in radians
     * @param {string} [planeTag] - Optional
     */
    window.setPlaneRotation = function(angleRadians, planeTag = null) {
        if (!validateSceneManager('setPlaneRotation')) return false;

        const tag = resolveTag(planeTag);
        if (!tag) {
            console.error('[planeFunctions] setPlaneRotation: No plane tag available');
            return false;
        }

        return setPlaneRotation(getScene(), tag, angleRadians);
    };

    /**
     * Scale plane
     * @param {number} scaleX - X scale factor
     * @param {number} scaleY - Y scale factor
     * @param {string} [planeTag] - Optional
     */
    window.scalePlane = function(scaleX, scaleY, planeTag = null) {
        if (!validateSceneManager('scalePlane')) return false;

        const tag = resolveTag(planeTag);
        if (!tag) {
            console.error('[planeFunctions] scalePlane: No plane tag available');
            return false;
        }

        return scalePlane(getScene(), tag, scaleX, scaleY);
    };

    /**
     * Centre/position plane
     * @param {number} centreX - X offset
     * @param {number} centreY - Y offset
     * @param {number} [elevation] - Optional Z position
     * @param {string} [planeTag] - Optional
     */
    window.centrePlane = function(centreX, centreY, elevation = undefined, planeTag = null) {
        if (!validateSceneManager('centrePlane')) return false;

        const tag = resolveTag(planeTag);
        if (!tag) {
            console.error('[planeFunctions] centrePlane: No plane tag available');
            return false;
        }

        return centrePlane(getScene(), tag, centreX, centreY, elevation);
    };

    /**
     * Set plane elevation
     * @param {number} elevation - Z position
     * @param {string} [planeTag] - Optional
     */
    window.setPlaneElevation = function(elevation, planeTag = null) {
        if (!validateSceneManager('setPlaneElevation')) return false;

        const tag = resolveTag(planeTag);
        if (!tag) {
            console.error('[planeFunctions] setPlaneElevation: No plane tag available');
            return false;
        }

        return setPlaneElevation(getScene(), tag, elevation);
    };

    /**
     * Set plane opacity
     * @param {number} opacity - Opacity 0-1
     * @param {string} [planeTag] - Optional
     */
    window.setPlaneOpacity = function(opacity, planeTag = null) {
        if (!validateSceneManager('setPlaneOpacity')) return false;

        const tag = resolveTag(planeTag);
        if (!tag) {
            console.error('[planeFunctions] setPlaneOpacity: No plane tag available');
            return false;
        }

        return setPlaneOpacity(getScene(), tag, opacity);
    };

    /**
     * Remove plane from scene
     * @param {string} planeTag - Tag of plane to remove
     */
    window.removePlane = function(planeTag) {
        if (!validateSceneManager('removePlane')) return false;

        if (!planeTag) {
            console.error('[planeFunctions] removePlane: planeTag is required');
            return false;
        }

        // Remove via sceneManager (handles disposal and lastAddedPlaneTag tracking)
        _sceneManager.removeObject(planeTag);

        // Also call removePlane for additional cleanup if needed
        return removePlane(getScene(), planeTag);
    };

    /**
     * Reposition all planes by delta
     * @param {number} deltaX - X offset
     * @param {number} deltaY - Y offset
     */
    window.repositionAllPlanes = function(deltaX, deltaY) {
        if (!validateSceneManager('repositionAllPlanes')) return 0;

        return repositionAllPlanes(getScene(), deltaX, deltaY);
    };

    /**
     * Get plane information
     * @param {string} planeTag - Tag of plane
     * @returns {Object|null} Plane info
     */
    window.getPlaneInfo = function(planeTag) {
        if (!validateSceneManager('getPlaneInfo')) return null;

        const tag = resolveTag(planeTag);
        if (!tag) return null;

        return getPlaneInfo(getScene(), tag);
    };

    /**
     * Get all plane tags in the scene
     * @returns {string[]} Array of plane tags
     */
    window.getAllPlaneTags = function() {
        if (!validateSceneManager('getAllPlaneTags')) return [];

        const planes = findAllPlanes(getScene());
        return planes.map(p => p.Tag).filter(Boolean);
    };

    /**
     * Get the last added plane tag
     * @returns {string|null}
     */
    window.getLastAddedPlaneTag = function() {
        return _sceneManager?.lastAddedPlaneTag || null;
    };
}

// ============================================================================
// EXPORTS
// ============================================================================

export { initPlaneFunctions };