/**
 * plane.js - Plot Plan Mesh Operations
 *
 * Provides modular functions for:
 * - Drawing plot plan meshes
 * - Rotating planes
 * - Scaling planes
 * - Positioning/centering planes
 * - Removing planes
 */

import * as THREE from 'three';

// ============================================================================
// PRIVATE HELPER FUNCTIONS
// ============================================================================

/**
 * Find a plane mesh by its tag in the scene
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} planeTag - The tag to search for
 * @returns {THREE.Mesh|null} The found mesh or null
 */
function findPlaneByTag(scene, planeTag) {
    let foundMesh = null;
    scene.traverse((object) => {
        if (object.Tag === planeTag && object.Type === 'plotplan') {
            foundMesh = object;
        }
    });
    return foundMesh;
}

/**
 * Find all plot plan meshes in the scene
 * @param {THREE.Scene} scene - The Three.js scene
 * @returns {THREE.Mesh[]} Array of plot plan meshes
 */
function findAllPlanes(scene) {
    const planes = [];
    scene.traverse((object) => {
        if (object.Type === 'plotplan') {
            planes.push(object);
        }
    });
    return planes;
}

// ============================================================================
// MAIN FUNCTIONS
// ============================================================================

/**
 * Create and draw a new plane mesh (initial render, no transforms applied)
 *
 * @param {number} rendererWidth - Renderer width for geometry sizing
 * @param {number} rendererHeight - Renderer height for geometry sizing
 * @param {string} planeTag - Unique identifier for the plane
 * @param {string} planeTagDescription - Description of the plane
 * @param {string} imageString - Base64 image data string
 * @param {number} elevation - Z-axis elevation (default 0)
 * @param {number} opacity - Opacity 0-1 (default 1)
 * @returns {THREE.Mesh|null} The created mesh or null on error
 */
function drawPlaneMesh(rendererWidth, rendererHeight, planeTag, planeTagDescription,
                       imageString, elevation = 0, opacity = 1) {
    try {
        // Create texture from image
        const image = new Image();
        image.src = imageString;

        const texture = new THREE.Texture(image);

        // Material with texture
        const planeMaterial = new THREE.MeshPhongMaterial({
            map: texture,
            color: 0xffffff,
            transparent: true,
            opacity: opacity,
            side: THREE.DoubleSide,
            flatShading: true,
            polygonOffset: true,
            polygonOffsetFactor: -1
        });

        // Update texture when image loads
        image.onload = function() {
            texture.needsUpdate = true;
        };

        // Create plane geometry and mesh
        const planeGeometry = new THREE.PlaneGeometry(rendererWidth, rendererHeight);
        const planeMesh = new THREE.Mesh(planeGeometry, planeMaterial);

        // Set initial position at elevation
        planeMesh.position.set(0, 0, elevation);

        // Set metadata
        planeMesh.Tag = planeTag;
        planeMesh.TagDescription = planeTagDescription || '';
        planeMesh.Type = 'plotplan';

        // Store original values for reference
        planeMesh.userData = {
            originalWidth: rendererWidth,
            originalHeight: rendererHeight,
            elevation: elevation,
            scaleX: 1,
            scaleY: 1,
            centreX: 0,
            centreY: 0,
            rotation: 0  // in radians
        };

        planeMesh.updateMatrixWorld();

        console.log(`[plane.js] Created plane: ${planeTag} at elevation ${elevation}`);
        return planeMesh;

    } catch (e) {
        console.error(`[plane.js] Error creating plane ${planeTag}:`, e);
        return null;
    }
}

/**
 * Rotate a plane mesh by a specified angle (incremental rotation)
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} planeTag - Tag of the plane to rotate
 * @param {number} angleRadians - Rotation angle in radians (positive = counter-clockwise)
 * @returns {boolean} Success status
 */
function rotatePlane(scene, planeTag, angleRadians) {
    try {
        const planeMesh = findPlaneByTag(scene, planeTag);

        if (!planeMesh) {
            console.warn(`[plane.js] rotatePlane: Plane '${planeTag}' not found`);
            return false;
        }

        // Apply incremental rotation around Z-axis
        planeMesh.rotateZ(angleRadians);

        // Update stored rotation value
        if (planeMesh.userData) {
            planeMesh.userData.rotation = (planeMesh.userData.rotation || 0) + angleRadians;
        }

        planeMesh.updateMatrixWorld();

        console.log(`[plane.js] Rotated plane '${planeTag}' by ${(angleRadians * 180 / Math.PI).toFixed(1)}°`);
        return true;

    } catch (e) {
        console.error(`[plane.js] Error rotating plane ${planeTag}:`, e);
        return false;
    }
}

/**
 * Set absolute rotation of a plane mesh
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} planeTag - Tag of the plane to rotate
 * @param {number} angleRadians - Absolute rotation angle in radians
 * @returns {boolean} Success status
 */
function setPlaneRotation(scene, planeTag, angleRadians) {
    try {
        const planeMesh = findPlaneByTag(scene, planeTag);

        if (!planeMesh) {
            console.warn(`[plane.js] setPlaneRotation: Plane '${planeTag}' not found`);
            return false;
        }

        // Set absolute rotation (reset to 0 first, then apply)
        planeMesh.rotation.z = angleRadians;

        // Update stored rotation value
        if (planeMesh.userData) {
            planeMesh.userData.rotation = angleRadians;
        }

        planeMesh.updateMatrixWorld();

        console.log(`[plane.js] Set plane '${planeTag}' rotation to ${(angleRadians * 180 / Math.PI).toFixed(1)}°`);
        return true;

    } catch (e) {
        console.error(`[plane.js] Error setting rotation for plane ${planeTag}:`, e);
        return false;
    }
}

/**
 * Scale a plane mesh
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} planeTag - Tag of the plane to scale
 * @param {number} scaleX - X-axis scale factor
 * @param {number} scaleY - Y-axis scale factor
 * @returns {boolean} Success status
 */
function scalePlane(scene, planeTag, scaleX, scaleY) {
    try {
        const planeMesh = findPlaneByTag(scene, planeTag);

        if (!planeMesh) {
            console.warn(`[plane.js] scalePlane: Plane '${planeTag}' not found`);
            return false;
        }

        if (scaleX === 0 || scaleY === 0) {
            console.warn(`[plane.js] scalePlane: Invalid scale values (${scaleX}, ${scaleY})`);
            return false;
        }

        // Apply scale
        planeMesh.scale.set(scaleX, scaleY, 1);

        // Update stored values
        if (planeMesh.userData) {
            planeMesh.userData.scaleX = scaleX;
            planeMesh.userData.scaleY = scaleY;
        }

        planeMesh.updateMatrixWorld();

        console.log(`[plane.js] Scaled plane '${planeTag}' to (${scaleX.toFixed(4)}, ${scaleY.toFixed(4)})`);
        return true;

    } catch (e) {
        console.error(`[plane.js] Error scaling plane ${planeTag}:`, e);
        return false;
    }
}

/**
 * Position/Centre a plane mesh (moves the plane so that the specified point becomes the origin)
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} planeTag - Tag of the plane to position
 * @param {number} centreX - X offset to apply (plane moves by -centreX)
 * @param {number} centreY - Y offset to apply (plane moves by -centreY)
 * @param {number} [elevation] - Optional new elevation (Z position)
 * @returns {boolean} Success status
 */
function centrePlane(scene, planeTag, centreX, centreY, elevation = undefined) {
    try {
        const planeMesh = findPlaneByTag(scene, planeTag);

        if (!planeMesh) {
            console.warn(`[plane.js] centrePlane: Plane '${planeTag}' not found`);
            return false;
        }

        // Determine Z position
        const z = elevation !== undefined ? elevation :
            (planeMesh.userData?.elevation || planeMesh.position.z);

        // Position the plane (negative offset to centre the specified point at origin)
        planeMesh.position.set(-centreX, -centreY, z);

        // Update stored values
        if (planeMesh.userData) {
            planeMesh.userData.centreX = centreX;
            planeMesh.userData.centreY = centreY;
            if (elevation !== undefined) {
                planeMesh.userData.elevation = elevation;
            }
        }

        planeMesh.updateMatrixWorld();

        console.log(`[plane.js] Centred plane '${planeTag}' at (${centreX.toFixed(2)}, ${centreY.toFixed(2)}, ${z.toFixed(2)})`);
        return true;

    } catch (e) {
        console.error(`[plane.js] Error centering plane ${planeTag}:`, e);
        return false;
    }
}

/**
 * Set the elevation (Z position) of a plane mesh
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} planeTag - Tag of the plane
 * @param {number} elevation - New Z position
 * @returns {boolean} Success status
 */
function setPlaneElevation(scene, planeTag, elevation) {
    try {
        const planeMesh = findPlaneByTag(scene, planeTag);

        if (!planeMesh) {
            console.warn(`[plane.js] setPlaneElevation: Plane '${planeTag}' not found`);
            return false;
        }

        planeMesh.position.z = elevation;

        if (planeMesh.userData) {
            planeMesh.userData.elevation = elevation;
        }

        planeMesh.updateMatrixWorld();

        console.log(`[plane.js] Set plane '${planeTag}' elevation to ${elevation.toFixed(2)}`);
        return true;

    } catch (e) {
        console.error(`[plane.js] Error setting elevation for plane ${planeTag}:`, e);
        return false;
    }
}

/**
 * Set the opacity of a plane mesh
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} planeTag - Tag of the plane
 * @param {number} opacity - Opacity value 0-1
 * @returns {boolean} Success status
 */
function setPlaneOpacity(scene, planeTag, opacity) {
    try {
        const planeMesh = findPlaneByTag(scene, planeTag);

        if (!planeMesh) {
            console.warn(`[plane.js] setPlaneOpacity: Plane '${planeTag}' not found`);
            return false;
        }

        if (planeMesh.material) {
            planeMesh.material.opacity = Math.max(0, Math.min(1, opacity));
            planeMesh.material.needsUpdate = true;
        }

        console.log(`[plane.js] Set plane '${planeTag}' opacity to ${opacity.toFixed(2)}`);
        return true;

    } catch (e) {
        console.error(`[plane.js] Error setting opacity for plane ${planeTag}:`, e);
        return false;
    }
}

/**
 * Remove a plane mesh from the scene
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} planeTag - Tag of the plane to remove
 * @returns {boolean} Success status
 */
function removePlane(scene, planeTag) {
    try {
        const planeMesh = findPlaneByTag(scene, planeTag);

        if (!planeMesh) {
            console.warn(`[plane.js] removePlane: Plane '${planeTag}' not found`);
            return false;
        }

        // Dispose of geometry and material
        if (planeMesh.geometry) {
            planeMesh.geometry.dispose();
        }
        if (planeMesh.material) {
            if (planeMesh.material.map) {
                planeMesh.material.map.dispose();
            }
            planeMesh.material.dispose();
        }

        // Remove from scene
        scene.remove(planeMesh);

        console.log(`[plane.js] Removed plane '${planeTag}'`);
        return true;

    } catch (e) {
        console.error(`[plane.js] Error removing plane ${planeTag}:`, e);
        return false;
    }
}

/**
 * Reposition all planes by a delta offset (used when key plan changes)
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {number} deltaX - X offset to apply
 * @param {number} deltaY - Y offset to apply
 * @returns {number} Number of planes repositioned
 */
function repositionAllPlanes(scene, deltaX, deltaY) {
    try {
        const planes = findAllPlanes(scene);

        planes.forEach(planeMesh => {
            planeMesh.position.x += deltaX;
            planeMesh.position.y += deltaY;

            if (planeMesh.userData) {
                planeMesh.userData.centreX = (planeMesh.userData.centreX || 0) - deltaX;
                planeMesh.userData.centreY = (planeMesh.userData.centreY || 0) - deltaY;
            }

            planeMesh.updateMatrixWorld();
        });

        console.log(`[plane.js] Repositioned ${planes.length} planes by (${deltaX.toFixed(2)}, ${deltaY.toFixed(2)})`);
        return planes.length;

    } catch (e) {
        console.error(`[plane.js] Error repositioning planes:`, e);
        return 0;
    }
}

/**
 * Get plane info/metadata
 *
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} planeTag - Tag of the plane
 * @returns {Object|null} Plane info or null if not found
 */
function getPlaneInfo(scene, planeTag) {
    const planeMesh = findPlaneByTag(scene, planeTag);

    if (!planeMesh) {
        return null;
    }

    return {
        tag: planeMesh.Tag,
        description: planeMesh.TagDescription,
        position: {
            x: planeMesh.position.x,
            y: planeMesh.position.y,
            z: planeMesh.position.z
        },
        scale: {
            x: planeMesh.scale.x,
            y: planeMesh.scale.y
        },
        rotation: planeMesh.rotation.z,
        opacity: planeMesh.material?.opacity || 1,
        userData: planeMesh.userData
    };
}

// ============================================================================
// LEGACY FUNCTION (for backwards compatibility)
// ============================================================================

/**
 * Legacy function that combines draw, scale, and centre in one call
 * @deprecated Use separate drawPlaneMesh, scalePlane, and centrePlane functions
 */
function drawPlaneMeshLegacy(rendererWidth, rendererHeight, planeTag, planeTagDescription,
                             imageString, scaleX, scaleY, centreX, centreY, elevation, opacity) {
    try {
        const image = new Image();
        image.src = imageString;

        const texture = new THREE.Texture(image);
        const planeMaterial = new THREE.MeshPhongMaterial({
            map: texture,
            color: 0xffffff,
            transparent: true,
            opacity: opacity,
            side: THREE.DoubleSide,
            flatShading: true
        });

        image.onload = function() {
            texture.needsUpdate = true;
        };

        const planeGeometry = new THREE.PlaneGeometry(rendererWidth, rendererHeight);
        const planeMesh = new THREE.Mesh(planeGeometry, planeMaterial);

        planeMesh.translateOnAxis(new THREE.Vector3(0, 0, 1), elevation);
        planeMesh.Tag = planeTag;
        planeMesh.TagDescription = planeTagDescription || '';
        planeMesh.Type = 'plotplan';
        planeMesh.material.opacity = opacity;

        // Apply scale
        if (scaleX !== undefined && scaleY !== undefined && scaleX !== 0 && scaleY !== 0) {
            planeMesh.scale.set(scaleX, scaleY, 1);
            planeMesh.updateMatrixWorld();
        }

        // Apply centre
        if (centreX !== undefined && centreY !== undefined) {
            planeMesh.position.set(-centreX, -centreY, elevation);
            planeMesh.updateMatrixWorld();
        }

        return planeMesh;

    } catch (e) {
        console.error(`[plane.js] Legacy draw error for ${planeTag}:`, e);
        return null;
    }
}

// ============================================================================
// EXPORTS
// ============================================================================

export {
    // Main functions
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

    // Helper functions
    findPlaneByTag,
    findAllPlanes,

    // Legacy (deprecated)
    drawPlaneMeshLegacy
};