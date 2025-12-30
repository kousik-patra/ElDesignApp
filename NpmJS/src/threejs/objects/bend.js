import * as THREE from 'three';

function drawBendMesh(tag, jsonFacePoints, material, color, opacity) {
    try {
        var clr = JSON.parse(color);
        var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
        material.color = setColor;

        var faces = JSON.parse(jsonFacePoints);
        var vertices1 = [];
        faces.forEach(face => {
            face.forEach(pt => {
                vertices1.push(new THREE.Vector3(pt.X, pt.Y, pt.Z));
            });
        });
        if (jsonFacePoints.includes("NaN")) {
            console.log("NanN :", tag, jsonFacePoints);
        }
        var pointsGeometry = new THREE.BufferGeometry().setFromPoints(vertices1, 3);
        pointsGeometry.computeBoundingSphere();
        if (isNaN(pointsGeometry.boundingSphere.radius)) {
            geometry.boundingSphere.radius = 1; // Set a default value or any suitable value
        }
        var bendMesh = new THREE.Mesh(pointsGeometry, material);

        bendMesh.Tag = tag;
        bendMesh.Type = "bend";
        bendMesh.Clicked = false;
        bendMesh.OriginalColor = bendMesh.material.color;
        bendMesh.material.opacity = opacity;

        var center = new THREE.Vector3();
        bendMesh.geometry.computeBoundingBox();
        bendMesh.geometry.boundingBox.getCenter(center);
        bendMesh.geometry.center();
        bendMesh.position.copy(center);

        pointsGeometry.dispose();

    } catch (e) {
        console.log(tag + e);
    }
    return bendMesh;
}

export {drawBendMesh}