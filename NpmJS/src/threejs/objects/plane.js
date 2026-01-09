import * as THREE from 'three';

function drawPlaneMesh(rendererWidth, rendererHeight, planeTag, planeTagDescription,
                       imageString, scaleX, scaleY, centreX, centreY, elevation, opacity) {
    try {
        // Create an image
        const image = new Image(); // or document.createElement('img' );
        // Set image source
        image.src = imageString;
        //console.log("Step 4: Drawing planeMesh plane.js", planeName, planeTag)
        var texture = new THREE.Texture(image);
        var planeMaterial = new THREE.MeshPhongMaterial({
            map: texture,
            color: 0xffffff,
            transparent: true,
            opacity: opacity,
            side: THREE.DoubleSide,
            flatShading: true
        });
        image.onload = function () {
            texture.needsUpdate = true;
        };
        //
        // plane geometry
        var planeGeometry = new THREE.PlaneGeometry(rendererWidth, rendererHeight);
        var planeMesh = new THREE.Mesh(planeGeometry, planeMaterial);
        planeMesh.translateOnAxis(new THREE.Vector3(0, 0, 1), elevation);
        //planeMesh.Name = planeName;// "plotplan"; // main background any additional plane to be added to this main background
        //var planeMeshCount = getObjectByNameArray(scene, 'plotplan').length;
        planeMesh.Tag = planeTag;// "plotplan" + planeMeshCount; //toString();
        planeMesh.Type = 'plotplan';
        planeMesh.material.opacity = opacity;
        // scale the plane
        if (scaleX !== undefined && scaleY !== undefined) {
            if (scaleX !== 0 && scaleY !== 0) {
                planeMesh.scale.set(scaleX, scaleY, 1);
                planeMesh.updateMatrixWorld();
            }
        }
        //
        // align to centre
        if (centreX !== undefined && centreY !== undefined) {
            planeMesh.position.set(-centreX, -centreY, elevation);
            planeMesh.updateMatrixWorld();
        }
        //
    } catch (e) {
        console.log(planeTag + e);
    }
    return planeMesh;
}

export {drawPlaneMesh}