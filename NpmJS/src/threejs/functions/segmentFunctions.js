// NpmJS/src/functions/segmentFunctions.js
// Segment rendering functions for cable trays/ladders
// Follows the same pattern as planeFunctions.js and pinFunctions.js

import * as THREE from 'three';
import { LAYERS } from '../sceneManager.js';
import { drawLadderMeshBatch } from '../objects/ladder.js';

// Module-level state
let sceneManager = null;
let segmentGroup = null;
let segmentMeshes = [];
let selectedTags = new Set();
let highlightedTags = new Set();

// Colors
const SELECTION_COLOR = new THREE.Color(0xffff00);  // Yellow for selection
const HIGHLIGHT_COLOR = new THREE.Color(0x00ffff); // Cyan for hover highlight

/**
 * Initialize segment functions with scene manager reference
 * Called from sceneManager.js during initialization
 */
export function initSegmentFunctions(manager) {
    sceneManager = manager;

    // Create segment group if it doesn't exist
    if (!segmentGroup) {
        segmentGroup = new THREE.Group();
        segmentGroup.name = "SegmentGroup";
        segmentGroup.userData.type = "segments";
        segmentGroup.layers.set(LAYERS.LADDERS);
    }

    // Register window functions for Blazor JSInterop
    window.drawSegmentsBatch = drawSegmentsBatch;
    window.clearSegments = clearSegments;
    window.setSegmentVisibility = setSegmentVisibility;
    window.setSegmentsSelected = setSegmentsSelected;
    window.highlightSegments = highlightSegments;
    window.focusOnSegment = focusOnSegment;
    window.focusOnSegments = focusOnSegments;
    window.updateSegment = updateSegment;
    window.getSegmentInfo = getSegmentInfo;
    window.getSelectedSegmentTags = getSelectedSegmentTags;

    console.log('[SegmentFunctions] Initialized');
}

/**
 * Draw segments in batch using instanced mesh for performance
 * @param {string[]} tags - Array of segment tags
 * @param {string[]} jsonPointsArray - Array of JSON point strings
 * @param {string[]} colors - Array of color JSON strings
 * @param {number[]} opacities - Array of opacity values
 */
function drawSegmentsBatch(tags, jsonPointsArray, colors, opacities) {
    //console.log(`[SegmentFunctions] drawSegmentsBatch: Drawing ${tags.length} segments...`);
    //console.trace("WHO CALLED ME?"); 

    if (!sceneManager || !sceneManager.scene) {
        console.error("[SegmentFunctions] Scene not initialized!");
        return false;
    }

    try {
        // Clear existing segments
        clearSegmentsInternal();

        // Add group to scene if not already added
        if (!segmentGroup.parent) {
            sceneManager.scene.add(segmentGroup);
        }

        // Create material (shared for batch rendering)
        const material = new THREE.MeshBasicMaterial({
            side: THREE.DoubleSide,
            transparent: true,
            vertexColors: true
        });

        // Use the batch function from ladder.js
        const instancedMesh = drawLadderMeshBatch(tags, jsonPointsArray, material, colors, opacities);

        if (instancedMesh) {
            instancedMesh.name = "SegmentInstancedMesh";
            instancedMesh.userData.type = "segment";
            instancedMesh.layers.set(LAYERS.LADDERS);

            // Store original colors for selection/highlight reset
            storeOriginalColors(instancedMesh, colors);

            segmentGroup.add(instancedMesh);
            segmentMeshes.push(instancedMesh);

            // Register with scene manager's object registry
            sceneManager.objectRegistry.set(LAYERS.LADDERS, new Map());
            tags.forEach((tag, index) => {
                sceneManager.objectRegistry.get(LAYERS.LADDERS).set(tag, {
                    mesh: instancedMesh,
                    instanceIndex: index
                });
            });

            //console.log(`[SegmentFunctions] Successfully added ${tags.length} segments`);
            return true;
        }

        return false;
    } catch (error) {
        console.error("[SegmentFunctions] drawSegmentsBatch error:", error);
        return false;
    }
}

/**
 * Store original colors for reset after selection/highlight
 */
function storeOriginalColors(mesh, colors) {
    mesh.userData.originalColors = [];
    for (let i = 0; i < colors.length; i++) {
        try {
            const colorArray = JSON.parse(colors[i]);
            mesh.userData.originalColors.push(
                new THREE.Color(colorArray[0] / 255, colorArray[1] / 255, colorArray[2] / 255)
            );
        } catch (e) {
            mesh.userData.originalColors.push(new THREE.Color(0.5, 0.5, 0.5));
        }
    }
}

/**
 * Clear all segments from the scene
 */
function clearSegments() {
    clearSegmentsInternal();
    console.log("[SegmentFunctions] All segments cleared");
}

function clearSegmentsInternal() {
    if (segmentGroup) {
        // Dispose of geometries and materials
        segmentGroup.traverse((child) => {
            if (child.geometry) {
                child.geometry.dispose();
            }
            if (child.material) {
                if (Array.isArray(child.material)) {
                    child.material.forEach(m => m.dispose());
                } else {
                    child.material.dispose();
                }
            }
        });

        // Clear children
        while (segmentGroup.children.length > 0) {
            segmentGroup.remove(segmentGroup.children[0]);
        }
    }

    // Clear registry
    if (sceneManager && sceneManager.objectRegistry.has(LAYERS.LADDERS)) {
        sceneManager.objectRegistry.get(LAYERS.LADDERS).clear();
    }

    segmentMeshes = [];
    selectedTags.clear();
    highlightedTags.clear();
}

/**
 * Toggle visibility of all segments
 */
function setSegmentVisibility(visible) {
    if (segmentGroup) {
        segmentGroup.visible = visible;
        console.log(`[SegmentFunctions] Visibility set to ${visible}`);
    }
}

/**
 * Set selection state for multiple segments
 * @param {string[]} tags - Tags to select/deselect
 * @param {boolean} selected - Selection state
 */
function setSegmentsSelected(tags, selected) {
    if (!tags || tags.length === 0) return;

    console.log(`[SegmentFunctions] setSegmentsSelected: ${tags.length} tags, selected=${selected}`);

    segmentMeshes.forEach(mesh => {
        if (mesh.isInstancedMesh && mesh.instanceTags) {
            let needsUpdate = false;

            tags.forEach(tag => {
                const index = mesh.instanceTags.indexOf(tag);
                if (index !== -1) {
                    if (selected) {
                        selectedTags.add(tag);
                        mesh.setColorAt(index, SELECTION_COLOR);
                    } else {
                        selectedTags.delete(tag);
                        // Reset to original color (or highlighted if hovering)
                        const color = highlightedTags.has(tag)
                            ? HIGHLIGHT_COLOR
                            : (mesh.userData.originalColors?.[index] || new THREE.Color(0.5, 0.5, 0.8));
                        mesh.setColorAt(index, color);
                    }
                    needsUpdate = true;
                }
            });

            if (needsUpdate && mesh.instanceColor) {
                mesh.instanceColor.needsUpdate = true;
            }
        }
    });
}

/**
 * Highlight segments (for hover effect)
 * @param {string[]} tags - Tags to highlight/unhighlight
 * @param {boolean} highlight - Highlight state
 */
function highlightSegments(tags, highlight = true) {
    if (!tags || tags.length === 0) return;

    segmentMeshes.forEach(mesh => {
        if (mesh.isInstancedMesh && mesh.instanceTags) {
            let needsUpdate = false;

            tags.forEach(tag => {
                const index = mesh.instanceTags.indexOf(tag);
                if (index !== -1) {
                    // Don't change color if selected (selection takes precedence)
                    if (!selectedTags.has(tag)) {
                        if (highlight) {
                            highlightedTags.add(tag);
                            mesh.setColorAt(index, HIGHLIGHT_COLOR);
                        } else {
                            highlightedTags.delete(tag);
                            const color = mesh.userData.originalColors?.[index] || new THREE.Color(0.5, 0.5, 0.8);
                            mesh.setColorAt(index, color);
                        }
                        needsUpdate = true;
                    }

                    if (highlight) {
                        highlightedTags.add(tag);
                    } else {
                        highlightedTags.delete(tag);
                    }
                }
            });

            if (needsUpdate && mesh.instanceColor) {
                mesh.instanceColor.needsUpdate = true;
            }
        }
    });
}

/**
 * Focus camera on a single segment
 */
function focusOnSegment(tag) {
    const position = getSegmentPosition(tag);
    if (position) {
        animateCameraTo(position);
        console.log(`[SegmentFunctions] Focused on ${tag}`);
    } else {
        console.warn(`[SegmentFunctions] Segment ${tag} not found`);
    }
}

/**
 * Focus camera to show multiple segments
 */
function focusOnSegments(tags) {
    if (!tags || tags.length === 0) return;

    // Calculate bounding box containing all segments
    const box = new THREE.Box3();
    let foundAny = false;

    tags.forEach(tag => {
        const position = getSegmentPosition(tag);
        if (position) {
            box.expandByPoint(position);
            foundAny = true;
        }
    });

    if (foundAny) {
        const center = new THREE.Vector3();
        box.getCenter(center);

        // Calculate distance based on bounding box size
        const size = new THREE.Vector3();
        box.getSize(size);
        const maxDim = Math.max(size.x, size.y, size.z);
        const distance = maxDim * 2 + 50;

        animateCameraTo(center, distance);
        console.log(`[SegmentFunctions] Focused on ${tags.length} segments`);
    }
}

/**
 * Get segment position by tag
 */
function getSegmentPosition(tag) {
    for (const mesh of segmentMeshes) {
        if (mesh.isInstancedMesh && mesh.instanceTags) {
            const index = mesh.instanceTags.indexOf(tag);
            if (index !== -1) {
                const matrix = new THREE.Matrix4();
                mesh.getMatrixAt(index, matrix);
                const position = new THREE.Vector3();
                matrix.decompose(position, new THREE.Quaternion(), new THREE.Vector3());
                return position;
            }
        }
    }
    return null;
}

/**
 * Animate camera to target position
 */
function animateCameraTo(targetPosition, distance = 50) {
    if (!sceneManager || !sceneManager.camera || !sceneManager.controls) return;

    const camera = sceneManager.camera;
    const controls = sceneManager.controls;

    // Calculate new camera position
    const direction = new THREE.Vector3(0, 0, 1);
    const newCameraPosition = targetPosition.clone().add(direction.multiplyScalar(distance));

    // Simple animation using requestAnimationFrame
    const startPosition = camera.position.clone();
    const startTarget = controls.target.clone();
    const duration = 500; // ms
    const startTime = performance.now();

    function animate() {
        const elapsed = performance.now() - startTime;
        const progress = Math.min(elapsed / duration, 1);

        // Ease out cubic
        const eased = 1 - Math.pow(1 - progress, 3);

        camera.position.lerpVectors(startPosition, newCameraPosition, eased);
        controls.target.lerpVectors(startTarget, targetPosition, eased);
        controls.update();

        if (progress < 1) {
            requestAnimationFrame(animate);
        }
    }

    animate();
}

/**
 * Update a single segment's appearance
 */
function updateSegment(tag, jsonPoints, colorJson, opacity) {
    // For instanced meshes, we need to update the specific instance
    // This is more complex - for now, log a warning
    console.warn(`[SegmentFunctions] updateSegment: Single segment update not fully implemented for instanced mesh`);
    // TODO: Implement single segment update by modifying the instance
}

/**
 * Get segment info by tag
 */
function getSegmentInfo(tag) {
    const position = getSegmentPosition(tag);
    if (position) {
        return {
            tag: tag,
            x: position.x,
            y: position.y,
            z: position.z,
            isSelected: selectedTags.has(tag),
            isHighlighted: highlightedTags.has(tag)
        };
    }
    return null;
}

/**
 * Get currently selected segment tags
 */
function getSelectedSegmentTags() {
    return Array.from(selectedTags);
}

/**
 * Handle segment click from mouse events
 * Call this from mouseEvents.js when a segment is clicked
 */
export function handleSegmentClick(tag, shiftKey, ctrlKey) {
    if (!tag) return;

    if (ctrlKey) {
        // Ctrl+click: toggle selection
        if (selectedTags.has(tag)) {
            setSegmentsSelected([tag], false);
        } else {
            setSegmentsSelected([tag], true);
        }
    } else if (shiftKey) {
        // Shift+click: add to selection
        setSegmentsSelected([tag], true);
    } else {
        // Normal click: select only this one
        // Clear previous selection
        const previousTags = Array.from(selectedTags);
        setSegmentsSelected(previousTags, false);
        // Select new one
        setSegmentsSelected([tag], true);
    }

    // Notify Blazor
    if (sceneManager && sceneManager.dotNetRef) {
        sceneManager.dotNetRef.invokeMethodAsync('OnSegmentsSelected',
            Array.from(selectedTags));
    }

    return Array.from(selectedTags);
}

/**
 * Cleanup function
 */
export function disposeSegmentFunctions() {
    clearSegmentsInternal();

    if (segmentGroup && segmentGroup.parent) {
        segmentGroup.parent.remove(segmentGroup);
    }

    segmentGroup = null;
    sceneManager = null;

    // Remove window functions
    delete window.drawSegmentsBatch;
    delete window.clearSegments;
    delete window.setSegmentVisibility;
    delete window.setSegmentsSelected;
    delete window.highlightSegments;
    delete window.focusOnSegment;
    delete window.focusOnSegments;
    delete window.updateSegment;
    delete window.getSegmentInfo;
    delete window.getSelectedSegmentTags;

    console.log('[SegmentFunctions] Disposed');
}

export {
    drawSegmentsBatch,
    clearSegments,
    setSegmentVisibility,
    setSegmentsSelected,
    highlightSegments,
    focusOnSegment,
    focusOnSegments,
    getSelectedSegmentTags
};