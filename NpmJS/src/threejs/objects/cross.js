import * as THREE from 'three';

function drawCrossMesh(tag, jsonFacePoints, material, color, opacity) {
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
        var geometry = new THREE.BufferGeometry().setFromPoints(vertices1, 3);
        var crossMesh = new THREE.Mesh(geometry, material);

        crossMesh.Tag = tag;
        crossMesh.Type = "cross";
        crossMesh.Clicked = false;
        crossMesh.OriginalColor = crossMesh.material.color;
        crossMesh.material.opacity = opacity;

        var center = new THREE.Vector3();
        crossMesh.geometry.computeBoundingBox();
        crossMesh.geometry.boundingBox.getCenter(center);
        crossMesh.geometry.center();
        crossMesh.position.copy(center);

        geometry.dispose();
    } catch (e) {
        console.log(tag + e);
    }
    return crossMesh;
}

export {drawCrossMesh}