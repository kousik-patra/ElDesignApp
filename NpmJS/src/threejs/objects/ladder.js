import * as THREE from 'three';


function drawLadderMesh(tag, jsonPoints, material, color, opacity) {
    try {
        // Parse color only once and handle potential errors
        let clr;
        try {
            clr = JSON.parse(color);
        } catch (colorParseError) {
            console.error(`Error parsing color for tag ${tag}: ${colorParseError}`);
            return null; // Exit if color parsing fails
        }

        const setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
        material.color = setColor;

        // Parse points only once and handle errors
        let points;
        try {
            points = JSON.parse(jsonPoints);
        } catch (pointsParseError) {
            console.error(`Error parsing points for tag ${tag}: ${pointsParseError}`);
            return null; // Exit if points parsing fails
        }

        const vertices1 = [];
        const pushOrder = [0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5, 2, 3, 7, 2, 7, 6];

        // Pre-allocate array for better performance
        vertices1.length = pushOrder.length;

        // Use a for loop for better performance than forEach
        for (let i = 0; i < pushOrder.length; i++) {
            const pointIndex = pushOrder[i];
            vertices1[i] = new THREE.Vector3(points[pointIndex].X, points[pointIndex].Y, points[pointIndex].Z);
        }

        const pointsGeometry = new THREE.BufferGeometry().setFromPoints(vertices1);

        const ladderMesh = new THREE.Mesh(pointsGeometry, material);

        ladderMesh.Tag = tag;
        ladderMesh.Type = "ladder";
        ladderMesh.Clicked = false;
        ladderMesh.OriginalColor = ladderMesh.material.color.clone(); // Clone the color

        const center = new THREE.Vector3();
        pointsGeometry.computeBoundingBox();
        pointsGeometry.boundingBox.getCenter(center);
        ladderMesh.geometry.center();
        ladderMesh.position.copy(center);
        ladderMesh.material.opacity = opacity;

        // Dispose of geometry after use
        //pointsGeometry.dispose(); // Moved to be handled outside of this function.

        return ladderMesh;
    } catch (e) {
        console.error(`${tag} - Unexpected error: ${e}`);
        return null;
    }
}


function drawLadderMeshBatch(tags, jsonPointsArray, material, colors, opacities) {
    console.log("Step 4: Drawing from ladder.js : drawLadderMeshBatch for " + tags.length +
        " trays with " + jsonPointsArray.length)
    // Validate inputs
    if (!Array.isArray(jsonPointsArray) || !Array.isArray(tags) || !Array.isArray(colors) || !Array.isArray(opacities)) {
        console.error('Invalid input arrays:', {tags, jsonPointsArray, colors, opacities});
        return null;
    }
    if (jsonPointsArray.length !== tags.length || jsonPointsArray.length !== colors.length || jsonPointsArray.length !== opacities.length) {
        console.error('Array lengths mismatch:', {
            tags: tags.length,
            points: jsonPointsArray.length,
            colors: colors.length,
            opacities: opacities.length
        });
        return null;
    }

    const geometry = new THREE.BufferGeometry();
    const vertices = [];
    const pushOrder = [0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5, 2, 3, 7, 2, 7, 6];

    // Parse and validate points
    const points = [];
    for (let i = 0; i < jsonPointsArray.length; i++) {
        try {
            const parsed = JSON.parse(jsonPointsArray[i]);
            if (!Array.isArray(parsed) || parsed.length < 8) {
                console.error(`Tray ${i} : Invalid points array for tag ${tags[i]}: Expected at least 8 vertices, got`, parsed);
                continue;
            }
            if (!parsed.every(p => p && typeof p.X === 'number' && typeof p.Y === 'number' && typeof p.Z === 'number')) {
                console.error(`Tray ${i} : Invalid point structure for tag ${tags[i]}: Missing or invalid X, Y, Z`, parsed);
                continue;
            }
            points.push(parsed);
        } catch (e) {
            console.error(`Tray ${i} : Error parsing JSON for tag ${tags[i]}:`, e);
        }
    }

    if (points.length === 0) {
        console.error('No valid points to render');
        return null;
    }

    // Create vertices for valid ladders
    for (let i = 0; i < points.length; i++) {
        for (let j = 0; j < pushOrder.length; j++) {
            const idx = pushOrder[j];
            vertices.push(points[i][idx].X, points[i][idx].Y, points[i][idx].Z);
        }
    }
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));

    // Material cache
    const materialCache = {};

    function getMaterial(color, opacity) {
        const key = `${color}_${opacity}`;
        if (!materialCache[key]) {
            let colorArray;
            try {
                // Handle case where color is a JSON string
                if (typeof color === 'string') {
                    colorArray = JSON.parse(color);
                } else if (Array.isArray(color)) {
                    // Handle case where color is already an array
                    colorArray = color;
                } else {
                    throw new Error('Tray ' + i + ' : Invalid color format');
                }
                // Validate color array
                if (!Array.isArray(colorArray) || colorArray.length !== 3 || !colorArray.every(c => typeof c === 'number' && c >= 0 && c <= 255)) {
                    throw new Error('Tray ' + i + ' : Invalid color array');
                }
                materialCache[key] = new THREE.MeshBasicMaterial({
                    color: new THREE.Color(...colorArray.map(c => c / 255)),
                    opacity,
                    transparent: true
                });
            } catch (e) {
                console.error(`Error processing color for key ${key}:`, e, 'Color value:', color);
                // Fallback to default color (white)
                materialCache[key] = new THREE.MeshBasicMaterial({
                    color: 0xffffff,
                    opacity,
                    transparent: true
                });
            }
        }
        return materialCache[key];
    }

    const count = points.length;
    const instancedMesh = new THREE.InstancedMesh(geometry, getMaterial(colors[0], opacities[0]), count);

    // Store tags for valid ladders only
    instancedMesh.instanceTags = points.map((_, i) => tags[i]);
    instancedMesh.Type = "ladder";
    instancedMesh.castShadow = true; // Cast shadows
    instancedMesh.receiveShadow = true; // Receive shadows (optional, for ladders shadowing each other)

    // Set instance matrices and colors
    for (let i = 0; i < count; i++) {
        const matrix = new THREE.Matrix4();
        const center = new THREE.Vector3();
        const tempGeometry = new THREE.BufferGeometry().setFromPoints(
            pushOrder.map(idx => new THREE.Vector3(points[i][idx].X, points[i][idx].Y, points[i][idx].Z))
        );
        tempGeometry.computeBoundingBox();
        tempGeometry.boundingBox.getCenter(center);
        matrix.setPosition(center);
        instancedMesh.setMatrixAt(i, matrix);
        try {
            let colorArray;
            if (typeof colors[i] === 'string') {
                colorArray = JSON.parse(colors[i]);
            } else if (Array.isArray(colors[i])) {
                colorArray = colors[i];
            } else {
                throw new Error('Tray ' + i + ' : Invalid color format');
            }
            if (!Array.isArray(colorArray) || colorArray.length !== 3 || !colorArray.every(c => typeof c === 'number' && c >= 0 && c <= 255)) {
                throw new Error('Tray ' + i + ' : Invalid color array');
            }
            instancedMesh.setColorAt(i, new THREE.Color(...colorArray.map(c => c / 255)));
        } catch (e) {
            console.error(`Tray ${i} : Error setting color for tag ${tags[i]}:`, e, 'Color value:', colors[i]);
            instancedMesh.setColorAt(i, new THREE.Color(1, 11));
        }
        tempGeometry.dispose();
    }

    instancedMesh.instanceMatrix.needsUpdate = true;
    instancedMesh.instanceColor.needsUpdate = true;

    return instancedMesh;
}

export {drawLadderMesh, drawLadderMeshBatch}