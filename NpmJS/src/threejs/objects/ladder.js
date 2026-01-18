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

// Push order for ladder geometry (constant)
const PUSH_ORDER = [0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5, 2, 3, 7, 2, 7, 6];


/**
 * Draw a single ladder mesh (unchanged from original)
 */
function drawLadderMesh(tag, jsonPoints, material, color, opacity) {
    try {
        let clr;
        try {
            clr = JSON.parse(color);
        } catch (colorParseError) {
            console.error(`Error parsing color for tag ${tag}: ${colorParseError}`);
            return null;
        }

        const setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
        material.color = setColor;

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

        return ladderMesh;
    } catch (e) {
        console.error(`${tag} - Unexpected error: ${e}`);
        return null;
    }
}


/**
 * OPTIMIZED: Draw ladder mesh batch for 20,000+ segments
 * Key optimizations:
 * 1. No temporary geometry creation in loop
 * 2. Reusable math objects
 * 3. Single JSON parse per segment
 * 4. Shadows disabled (huge performance gain)
 * 5. Direct bounding box calculation
 */
function drawLadderMeshBatch(tags, jsonPointsArray, material, colors, opacities) {

    console.log(`[ladder.js] drawLadderMeshBatch: ${tags.length} segments`);
    console.trace("WHO CALLED ME?");
    
    console.log(`[ladder.js] drawLadderMeshBatch: ${tags.length} segments`);
    console.time('drawLadderMeshBatch');

    // Validate inputs
    if (!Array.isArray(jsonPointsArray) || !Array.isArray(tags) ||
        !Array.isArray(colors) || !Array.isArray(opacities)) {
        console.error('Invalid input arrays');
        return null;
    }

    if (jsonPointsArray.length !== tags.length ||
        jsonPointsArray.length !== colors.length ||
        jsonPointsArray.length !== opacities.length) {
        console.error('Array lengths mismatch');
        return null;
    }

    // ============================================================
    // PHASE 1: Parse all points (single pass)
    // ============================================================
    console.time('parsePoints');
    const parsedPoints = [];
    const validIndices = [];

    for (let i = 0; i < jsonPointsArray.length; i++) {
        try {
            const parsed = JSON.parse(jsonPointsArray[i]);
            if (Array.isArray(parsed) && parsed.length >= 8) {
                parsedPoints.push(parsed);
                validIndices.push(i);
            }
        } catch (e) {
            // Skip invalid entries silently for performance
        }
    }
    console.timeEnd('parsePoints');

    const count = parsedPoints.length;
    if (count === 0) {
        console.error('No valid points to render');
        return null;
    }

    console.log(`[ladder.js] Valid segments: ${count}/${tags.length}`);

    // ============================================================
    // PHASE 2: Build merged geometry
    // ============================================================
    console.time('buildGeometry');

    // Pre-allocate vertex array (18 vertices per segment * 3 components)
    const vertexCount = count * PUSH_ORDER.length * 3;
    const vertices = new Float32Array(vertexCount);

    let vertexIndex = 0;
    for (let i = 0; i < count; i++) {
        const pts = parsedPoints[i];
        for (let j = 0; j < PUSH_ORDER.length; j++) {
            const idx = PUSH_ORDER[j];
            vertices[vertexIndex++] = pts[idx].X;
            vertices[vertexIndex++] = pts[idx].Y;
            vertices[vertexIndex++] = pts[idx].Z;
        }
    }

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(vertices, 3));
    geometry.computeBoundingSphere();  // Important for frustum culling
    console.timeEnd('buildGeometry');

    // ============================================================
    // PHASE 3: Create material (single material for all)
    // ============================================================
    let baseMaterial;
    try {
        const colorArray = typeof colors[0] === 'string' ? JSON.parse(colors[0]) : colors[0];
        baseMaterial = new THREE.MeshBasicMaterial({
            color: new THREE.Color(colorArray[0] / 255, colorArray[1] / 255, colorArray[2] / 255),
            opacity: opacities[0] || 0.7,
            transparent: true,
            side: THREE.DoubleSide
        });
    } catch (e) {
        baseMaterial = new THREE.MeshBasicMaterial({
            color: 0x8080cc,
            opacity: 0.7,
            transparent: true,
            side: THREE.DoubleSide
        });
    }

    // ============================================================
    // PHASE 4: Create InstancedMesh
    // ============================================================
    console.time('createInstances');
    const instancedMesh = new THREE.InstancedMesh(geometry, baseMaterial, count);

    instancedMesh.instanceTags = [];
    instancedMesh.Type = "ladder";

    // IMPORTANT: Disable shadows for performance!
    instancedMesh.castShadow = false;
    instancedMesh.receiveShadow = false;

    // Enable frustum culling
    instancedMesh.frustumCulled = true;

    // ============================================================
    // PHASE 5: Set matrices and colors (optimized loop)
    // ============================================================
    for (let i = 0; i < count; i++) {
        const pts = parsedPoints[i];
        const originalIndex = validIndices[i];

        // Calculate center directly (no temporary geometry!)
        _minVec.set(Infinity, Infinity, Infinity);
        _maxVec.set(-Infinity, -Infinity, -Infinity);

        for (let j = 0; j < 8; j++) {
            const px = pts[j].X, py = pts[j].Y, pz = pts[j].Z;
            if (px < _minVec.x) _minVec.x = px;
            if (py < _minVec.y) _minVec.y = py;
            if (pz < _minVec.z) _minVec.z = pz;
            if (px > _maxVec.x) _maxVec.x = px;
            if (py > _maxVec.y) _maxVec.y = py;
            if (pz > _maxVec.z) _maxVec.z = pz;
        }

        // Center position
        _tempVec3.set(
            (_minVec.x + _maxVec.x) / 2,
            (_minVec.y + _maxVec.y) / 2,
            (_minVec.z + _maxVec.z) / 2
        );

        // Set matrix (position only, no rotation/scale)
        _tempMatrix.makeTranslation(_tempVec3.x, _tempVec3.y, _tempVec3.z);
        instancedMesh.setMatrixAt(i, _tempMatrix);

        // Set color
        try {
            const colorArray = typeof colors[originalIndex] === 'string'
                ? JSON.parse(colors[originalIndex])
                : colors[originalIndex];
            _tempColor.setRGB(colorArray[0] / 255, colorArray[1] / 255, colorArray[2] / 255);
        } catch (e) {
            _tempColor.setRGB(0.5, 0.5, 0.8);
        }
        instancedMesh.setColorAt(i, _tempColor);

        // Store tag
        instancedMesh.instanceTags.push(tags[originalIndex]);
    }

    // Single update at the end (not inside loop!)
    instancedMesh.instanceMatrix.needsUpdate = true;
    if (instancedMesh.instanceColor) {
        instancedMesh.instanceColor.needsUpdate = true;
    }

    console.timeEnd('createInstances');
    console.timeEnd('drawLadderMeshBatch');
    console.log(`[ladder.js] ✓ Created InstancedMesh with ${count} instances`);

    return instancedMesh;
}


export { drawLadderMesh, drawLadderMeshBatch };