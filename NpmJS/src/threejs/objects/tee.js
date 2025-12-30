import * as THREE from 'three';

function drawTeeMesh(tag, jsonFacePoints, material, color, opacity) {
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

        var pointsGeometry = new THREE.BufferGeometry().setFromPoints(vertices1, 3);
        var teeMesh = new THREE.Mesh(pointsGeometry, material);

        teeMesh.Tag = tag;
        teeMesh.Type = "tee";
        teeMesh.Clicked = false;
        teeMesh.OriginalColor = teeMesh.material.color;
        teeMesh.material.opacity = opacity;

        var center = new THREE.Vector3();
        teeMesh.geometry.computeBoundingBox();
        teeMesh.geometry.boundingBox.getCenter(center);
        teeMesh.geometry.center();
        teeMesh.position.copy(center);

        pointsGeometry.dispose();
    } catch (e) {
        console.log(tag + e);
    }
    return teeMesh;
}

export {drawTeeMesh}