import * as THREE from 'three';

function drawSleeveMesh(tag, jsonPoints, radious, segment, radialSegments, material, color, opacity) {
    try {
        var clr = JSON.parse(color);
        var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
        material.color = setColor;

        // var rawPoints = JSON.parse(jsonPoints);
        // var points = [];
        // rawPoints.forEach(p => {
        //     points.push(new THREE.Vector3(p.X, p.Y, p.Z));
        // });
        //
        // //var pp = curve.getPoints(segment);
        // var pps = [];
        // points.forEach(p => { pps.push([p.x, p.y, p.z]); });
        //
        // var geometry = new THREE.BufferGeometry().setFromPoints(vertices1, 3);
        // var curve = new THREE.CatmullRomCurve3(points);
        // var tubeGeometry = new THREE.TubeBufferGeometry(curve, segment, radious, radialSegments, false);
        // var tubeMesh = new THREE.Mesh(tubeGeometry, material);


        // Parse and create points
        var rawPoints = JSON.parse(jsonPoints);
        var points = rawPoints.map(p => new THREE.Vector3(p.X, p.Y, p.Z));

        // Create curve from points
        var curve = new THREE.CatmullRomCurve3(points);

        // Create tube geometry
        var tubeGeometry = new THREE.TubeGeometry(curve, segment, radious, radialSegments, false);

        // Create mesh
        var tubeMesh = new THREE.Mesh(tubeGeometry, material);

        tubeMesh.Tag = tag;
        tubeMesh.Type = "sleeve";
        tubeMesh.Clicked = false;
        tubeMesh.OriginalColor = color;
        tubeMesh.material.opacity = opacity;
        //console.log("drawing sleeve sleeve.js tubeJeometry done for ", tag);
        tubeGeometry.dispose();
    } catch (e) {
        console.log(tag + e);
    }
    return tubeMesh;
}

export {drawSleeveMesh}