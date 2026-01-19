// NpmJS/src/threejs/objects/ladder.js

import * as THREE from 'three';

// ============================================================
// REUSABLE OBJECTS - Avoid creating these in loops!
// ============================================================
const _tempMatrix = new THREE.Matrix4();
const _tempVec3 = new THREE.Vector3();
const _tempColor = new THREE.Color();
const _tempBox = new THREE.Box3();
const _minVec = new THREE.Vector3();
const _maxVec = new THREE.Vector3();

/**
 * Vertex order for creating 6 triangles (18 vertices) from 8 corner points
 *
 * ORIGINAL PUSH_ORDER - DO NOT CHANGE (matches server-side data format)
 *
 * Creates 3 rectangular faces of a cable tray:
 *   - Left plane:   vertices 0, 1, 4, 5  → triangles [0,1,5], [0,5,4]
 *   - Bottom plane: vertices 1, 2, 5, 6  → triangles [1,2,6], [1,6,5]
 *   - Right plane:  vertices 2, 3, 6, 7  → triangles [2,3,7], [2,7,6]
 *
 * Vertex layout (looking at cross-section):
 *       4 ------- 5 ------- 6 ------- 7
 *       |  LEFT   |  BOTTOM |  RIGHT  |
 *       0 ------- 1 ------- 2 ------- 3
 */
const PUSH_ORDER = [0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5, 2, 3, 7, 2, 7, 6];

/**
 * Shadow configuration options
 */
const SHADOW_CONFIG = {
    enabled: true,           // Master toggle for shadows
    castShadow: true,        // Segments cast shadows
    receiveShadow: true,     // Segments receive shadows from other objects
    materialType: 'lambert'  // 'lambert' (faster) or 'standard' (better quality)
};


/**
 * Draw a single ladder mesh (unchanged from original)
 */
function drawLadderMesh(tag, jsonPoints, material, color, opacity, enableShadows = true) {
    try {
        let clr;
        try {
            clr = JSON.parse(color);
        } catch (colorParseError) {
            console.error(`Error parsing color for tag ${tag}: ${colorParseError}`);
            return null;
        }

        const setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);

        // Use shadow-capable material if not provided
        let meshMaterial = material;
        if (!meshMaterial || meshMaterial.type === 'MeshBasicMaterial') {
            meshMaterial = new THREE.MeshLambertMaterial({
                color: setColor,
                opacity: opacity,
                transparent: opacity < 1,
                side: THREE.DoubleSide
            });
        } else {
            meshMaterial.color = setColor;
        }

        let points;
        try {
            points = JSON.parse(jsonPoints);
        } catch (pointsParseError) {
            console.error(`Error parsing points for tag ${tag}: ${pointsParseError}`);
            return null;
        }

        const vertices1 = [];
        for (let i = 0; i < PUSH_ORDER.length; i++) {
            const pointIndex = PUSH_ORDER[i];
            vertices1[i] = new THREE.Vector3(points[pointIndex].X, points[pointIndex].Y, points[pointIndex].Z);
        }

        const pointsGeometry = new THREE.BufferGeometry().setFromPoints(vertices1);
        const ladderMesh = new THREE.Mesh(pointsGeometry, material);

        ladderMesh.Tag = tag;
        ladderMesh.Type = "ladder";
        ladderMesh.Clicked = false;
        ladderMesh.OriginalColor = ladderMesh.material.color.clone();

        const center = new THREE.Vector3();
        pointsGeometry.computeBoundingBox();
        pointsGeometry.boundingBox.getCenter(center);
        ladderMesh.geometry.center();
        ladderMesh.position.copy(center);
        ladderMesh.material.opacity = opacity;

        // Enable shadows
        if (enableShadows && SHADOW_CONFIG.enabled) {
            ladderMesh.castShadow = SHADOW_CONFIG.castShadow;
            ladderMesh.receiveShadow = SHADOW_CONFIG.receiveShadow;
        }

        return ladderMesh;
    } catch (e) {
        console.error(`${tag} - Unexpected error: ${e}`);
        return null;
    }
}


/**
 * OPTIMIZED: Draw ladder mesh batch using merged geometry
 *  
 *  Supports both single color/opacity OR arrays for backward compatibility.
 *
 * This creates a SINGLE mesh containing all segments.
 * Each segment's vertices are directly stored in the geometry buffer.
 *
 * Key features:
 * - Single draw call for all segments
 * - Per-segment colors via vertex colors
 * - Segment identification via face index (for raycasting)
 * - No instancing multiplication bug
 *
 * @param {string[]} tags - Array of segment tags
 * @param {string[]} jsonPointsArray - Array of JSON strings, each containing 8 vertices
 * @param {THREE.Material} material - Base material (optional, will use vertex colors)
 * @param {string[]|number[][]} colors - Array of colors per segment
 * @param {number[]} opacities - Array of opacities per segment
 * @returns {THREE.Mesh} - Single mesh containing all segments
 */
function drawLadderMeshBatch(tags, jsonPointsArray, material, colors, opacities, enableShadows = true) {
    console.log(`[ladder.js] drawLadderMeshBatch: ${tags.length} segments`);
    console.time('drawLadderMeshBatch');

    // ============================================================
    // INPUT VALIDATION
    // ============================================================
    if (!Array.isArray(jsonPointsArray) || !Array.isArray(tags)) {
        console.error('[ladder.js] Invalid input arrays');
        return null;
    }

    // ============================================================
    // DETERMINE COLOR/OPACITY MODE (single vs array)
    // ============================================================
    const isUniformColor = !Array.isArray(colors) || colors.length === 1;
    const isUniformOpacity = !Array.isArray(opacities) || opacities.length === 1;

    // Parse uniform color once (if single value provided)
    let uniformR = 0.5, uniformG = 0.5, uniformB = 0.8; // Default
    let uniformOpacity = 1.0;

    if (isUniformColor) {
        try {
            const colorValue = Array.isArray(colors) ? colors[0] : colors;
            const colorArray = typeof colorValue === 'string'
                ? JSON.parse(colorValue)
                : colorValue;
            if (Array.isArray(colorArray) && colorArray.length >= 3) {
                uniformR = colorArray[0] / 255;
                uniformG = colorArray[1] / 255;
                uniformB = colorArray[2] / 255;
            }
        } catch (e) {
            console.warn('[ladder.js] Using default color');
        }
        console.log(`[ladder.js] Using uniform color: RGB(${(uniformR*255).toFixed(0)}, ${(uniformG*255).toFixed(0)}, ${(uniformB*255).toFixed(0)})`);
    }

    if (isUniformOpacity) {
        uniformOpacity = Array.isArray(opacities) ? opacities[0] : opacities;
        console.log(`[ladder.js] Using uniform opacity: ${uniformOpacity}`);
    }

    // ============================================================
    // PHASE 1: Parse all points (single pass)
    // ============================================================
    console.time('parsePoints');
    const parsedSegments = [];

    for (let i = 0; i < jsonPointsArray.length; i++) {
        try {
            const parsed = JSON.parse(jsonPointsArray[i]);
            if (Array.isArray(parsed) && parsed.length >= 8) {
                parsedSegments.push({
                    points: parsed,
                    originalIndex: i,
                    tag: tags[i],
                });
            }
        } catch (e) {
            //console.warn(`[ladder.js] Failed to parse segment ${i}:`, e.message);
        }
    }
    console.timeEnd('parsePoints');

    const segmentCount = parsedSegments.length;
    if (segmentCount === 0) {
        console.error('[ladder.js] No valid segments to render');
        return null;
    }

    console.log(`[ladder.js] Valid segments: ${segmentCount}/${tags.length}`);

    // ============================================================
    // PHASE 2: Build merged geometry with vertex colors
    // ============================================================
    console.time('buildGeometry');

    const verticesPerSegment = PUSH_ORDER.length; // 18 vertices per segment
    const totalVertices = segmentCount * verticesPerSegment;

    // Pre-allocate typed arrays
    const positions = new Float32Array(totalVertices * 3);
    const vertexColors = new Float32Array(totalVertices * 3);

    let posIndex = 0;
    let colorIndex = 0;

    // Maps face index to segment info (for raycasting)
    // Each segment has 6 triangles (faces)
    const faceToSegmentMap = [];
    const segmentTags = [];
    const segmentIndices = [];

    for (let segIdx = 0; segIdx < segmentCount; segIdx++) {
        const segment = parsedSegments[segIdx];
        const pts = segment.points;

        // Get color for this segment
        let r, g, b;
        if (isUniformColor) {
            // Use pre-parsed uniform color (FAST PATH)
            r = uniformR;
            g = uniformG;
            b = uniformB;
        } else {
            // Parse per-segment color (slower, but supports varied colors)
            r = 0.5; g = 0.5; b = 0.8;
            try {
                const colorValue = colors[segment.originalIndex];
                const colorArray = typeof colorValue === 'string'
                    ? JSON.parse(colorValue)
                    : colorValue;
                if (Array.isArray(colorArray) && colorArray.length >= 3) {
                    r = colorArray[0] / 255;
                    g = colorArray[1] / 255;
                    b = colorArray[2] / 255;
                }
            } catch (e) { }
        }

        // Store mapping info (6 faces/triangles per segment)
        const startFaceIndex = segIdx * 6;
        for (let f = 0; f < 6; f++) {
            faceToSegmentMap[startFaceIndex + f] = segIdx;
        }

        segmentTags.push(segment.tag);
        segmentIndices.push(segment.originalIndex);

        // Add vertices for this segment using ORIGINAL PUSH_ORDER
        for (let j = 0; j < PUSH_ORDER.length; j++) {
            const ptIdx = PUSH_ORDER[j];
            const pt = pts[ptIdx];

            // Position
            positions[posIndex++] = pt.X;
            positions[posIndex++] = pt.Y;
            positions[posIndex++] = pt.Z;

            // Color (same for all vertices in segment)
            vertexColors[colorIndex++] = r;
            vertexColors[colorIndex++] = g;
            vertexColors[colorIndex++] = b;
        }
    }

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    geometry.setAttribute('color', new THREE.BufferAttribute(vertexColors, 3));
    geometry.computeVertexNormals();
    geometry.computeBoundingSphere();

    console.timeEnd('buildGeometry');

    // ============================================================
    // PHASE 3: Create shadow-capable material with vertex colors
    // ============================================================
    let meshMaterial;

    if (SHADOW_CONFIG.materialType === 'standard') {
        // MeshStandardMaterial - Better quality shadows, PBR lighting
        meshMaterial = new THREE.MeshStandardMaterial({
            vertexColors: true,
            opacity: uniformOpacity,
            transparent: uniformOpacity < 1,
            side: THREE.DoubleSide,
            roughness: 0.7,
            metalness: 0.1
        });
    } else {
        // MeshLambertMaterial - Faster, good enough for most cases
        meshMaterial = new THREE.MeshLambertMaterial({
            vertexColors: true,
            opacity: uniformOpacity,
            transparent: uniformOpacity < 1,
            side: THREE.DoubleSide
        });
    }


    // ============================================================
    // PHASE 4: Create mesh with shadow support
    // ============================================================
    console.time('createMesh');

    const mesh = new THREE.Mesh(geometry, meshMaterial);

    // Enable shadows
    if (enableShadows && SHADOW_CONFIG.enabled) {
        mesh.castShadow = SHADOW_CONFIG.castShadow;
        mesh.receiveShadow = SHADOW_CONFIG.receiveShadow;
        console.log(`[ladder.js] Shadows enabled: cast=${mesh.castShadow}, receive=${mesh.receiveShadow}`);
    } else {
        mesh.castShadow = false;
        mesh.receiveShadow = false;
    }

    mesh.frustumCulled = true;

    // ============================================================
    // PHASE 5: Attach metadata for raycasting/picking
    // ============================================================

    // Type identifier
    mesh.Type = "ladder";
    mesh.isMergedSegmentMesh = true;

    // Segment lookup data
    mesh.segmentData = {
        count: segmentCount,
        verticesPerSegment: verticesPerSegment,
        facesPerSegment: 6,
        tags: segmentTags,
        originalIndices: segmentIndices,
        faceToSegmentMap: faceToSegmentMap,
        //originalColors: colors  // Store for reset functionality
    };

    // Store original color for reset (private, won't be serialized to server)
    mesh._uniformColor = isUniformColor ? [uniformR * 255, uniformG * 255, uniformB * 255] : null;
    mesh._originalColors = isUniformColor ? null : colors; // Only store if varied


    // Helper method to get segment info from face index
    mesh.getSegmentFromFace = function(faceIndex) {
        const segIdx = this.segmentData.faceToSegmentMap[faceIndex];
        if (segIdx === undefined) return null;

        return {
            segmentIndex: segIdx,
            tag: this.segmentData.tags[segIdx],
            originalIndex: this.segmentData.originalIndices[segIdx]
        };
    };

    // Helper method to get segment info from intersection
    mesh.getSegmentFromIntersection = function(intersection) {
        if (!intersection || intersection.faceIndex === undefined) return null;
        return this.getSegmentFromFace(intersection.faceIndex);
    };

    console.timeEnd('createMesh');
    console.timeEnd('drawLadderMeshBatch');
    console.log(`[ladder.js] ✓ Created merged mesh with ${segmentCount} segments (${totalVertices} vertices)`);

    return mesh;
}


/**
 * Highlight specific segments in a merged mesh
 * @param {THREE.Mesh} mesh - The merged ladder mesh
 * @param {string[]} tagsToHighlight - Array of tags to highlight
 * @param {number[]} highlightColor - RGB array [0-255, 0-255, 0-255]
 */
function highlightSegments(mesh, tagsToHighlight, highlightColor = [255, 255, 0]) {
    if (!mesh || !mesh.isMergedSegmentMesh) return;

    const colorAttr = mesh.geometry.getAttribute('color');
    const segmentData = mesh.segmentData;

    // Parse highlight color
    const hr = highlightColor[0] / 255;
    const hg = highlightColor[1] / 255;
    const hb = highlightColor[2] / 255;

    const tagsSet = new Set(tagsToHighlight);

    for (let segIdx = 0; segIdx < segmentData.count; segIdx++) {
        const tag = segmentData.tags[segIdx];
        if (tagsSet.has(tag)) {
            // Set highlight color for this segment's vertices
            const startVertex = segIdx * segmentData.verticesPerSegment;
            for (let v = 0; v < segmentData.verticesPerSegment; v++) {
                const idx = startVertex + v;
                colorAttr.setXYZ(idx, hr, hg, hb);
            }
        }
    }

    colorAttr.needsUpdate = true;
}


/**
 * Reset segment colors to original
 * @param {THREE.Mesh} mesh - The merged ladder mesh
 */
function resetSegmentColors(mesh) {
    if (!mesh || !mesh.isMergedSegmentMesh) return;

    const colorAttr = mesh.geometry.getAttribute('color');
    const segmentData = mesh.segmentData;

    // Check if uniform color was used
    if (mesh._uniformColor) {
        const r = mesh._uniformColor[0] / 255;
        const g = mesh._uniformColor[1] / 255;
        const b = mesh._uniformColor[2] / 255;

        // Fast path: set all vertices to same color
        for (let i = 0; i < colorAttr.count; i++) {
            colorAttr.setXYZ(i, r, g, b);
        }
    } else if (mesh._originalColors) {
        // Varied colors: restore per-segment
        const originalColors = mesh._originalColors;

        for (let segIdx = 0; segIdx < segmentData.count; segIdx++) {
            const origIdx = segmentData.originalIndices[segIdx];

            let r = 0.5, g = 0.5, b = 0.8;
            try {
                const colorArray = typeof originalColors[origIdx] === 'string'
                    ? JSON.parse(originalColors[origIdx])
                    : originalColors[origIdx];
                if (Array.isArray(colorArray) && colorArray.length >= 3) {
                    r = colorArray[0] / 255;
                    g = colorArray[1] / 255;
                    b = colorArray[2] / 255;
                }
            } catch (e) { }

            const startVertex = segIdx * segmentData.verticesPerSegment;
            for (let v = 0; v < segmentData.verticesPerSegment; v++) {
                colorAttr.setXYZ(startVertex + v, r, g, b);
            }
        }
    }

    colorAttr.needsUpdate = true;
}


/**
 * Reset specific segments to their original colors
 * @param {THREE.Mesh} mesh - The merged ladder mesh
 * @param {string[]} tagsToReset - Array of tags to reset
 */
function resetSpecificSegments(mesh, tagsToReset) {
    if (!mesh || !mesh.isMergedSegmentMesh) return;

    const colorAttr = mesh.geometry.getAttribute('color');
    const segmentData = mesh.segmentData;
    const tagsSet = new Set(tagsToReset);

    // Determine original color
    let defaultR, defaultG, defaultB;
    if (mesh._uniformColor) {
        defaultR = mesh._uniformColor[0] / 255;
        defaultG = mesh._uniformColor[1] / 255;
        defaultB = mesh._uniformColor[2] / 255;
    } else {
        defaultR = 0.5; defaultG = 0.5; defaultB = 0.8;
    }

    for (let segIdx = 0; segIdx < segmentData.count; segIdx++) {
        const tag = segmentData.tags[segIdx];
        if (!tagsSet.has(tag)) continue;

        let r = defaultR, g = defaultG, b = defaultB;

        // If varied colors, get the specific color
        if (mesh._originalColors) {
            const origIdx = segmentData.originalIndices[segIdx];
            try {
                const colorArray = typeof mesh._originalColors[origIdx] === 'string'
                    ? JSON.parse(mesh._originalColors[origIdx])
                    : mesh._originalColors[origIdx];
                if (Array.isArray(colorArray) && colorArray.length >= 3) {
                    r = colorArray[0] / 255;
                    g = colorArray[1] / 255;
                    b = colorArray[2] / 255;
                }
            } catch (e) { }
        }

        const startVertex = segIdx * segmentData.verticesPerSegment;
        for (let v = 0; v < segmentData.verticesPerSegment; v++) {
            colorAttr.setXYZ(startVertex + v, r, g, b);
        }
    }

    colorAttr.needsUpdate = true;
}

/**
 * Configure shadow settings
 * @param {Object} config - Shadow configuration
 */
function configureShadows(config) {
    if (config.enabled !== undefined) SHADOW_CONFIG.enabled = config.enabled;
    if (config.castShadow !== undefined) SHADOW_CONFIG.castShadow = config.castShadow;
    if (config.receiveShadow !== undefined) SHADOW_CONFIG.receiveShadow = config.receiveShadow;
    if (config.materialType !== undefined) SHADOW_CONFIG.materialType = config.materialType;

    console.log('[ladder.js] Shadow config updated:', SHADOW_CONFIG);
}

export {
    drawLadderMesh,
    drawLadderMeshBatch,
    highlightSegments,
    resetSegmentColors,
    resetSpecificSegments,
    configureShadows,
    SHADOW_CONFIG,
    PUSH_ORDER
};