import * as THREE from 'three';

function drawNodeMesh(tag, jsonPoint, material, color, opacity) {
    try {
        var clr = JSON.parse(color);
        var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
        material.color = setColor;

        var nodePoint = JSON.parse(jsonPoint);
        const geometry = new THREE.SphereGeometry(0.03, 5, 5); // radious, widthsegment, heightsegments
        const sphereMesh = new THREE.Mesh(geometry, material);
        sphereMesh.position.set(nodePoint.X, nodePoint.Y, nodePoint.Z);

        sphereMesh.Tag = tag;
        sphereMesh.Type = "node";
        sphereMesh.Clicked = false;
        sphereMesh.OriginalColor = sphereMesh.material.color;
        sphereMesh.material.opacity = opacity;

        geometry.dispose();
    } catch (e) {
        console.log(tag + e);
    }
    return sphereMesh;
}

export {drawNodeMesh}