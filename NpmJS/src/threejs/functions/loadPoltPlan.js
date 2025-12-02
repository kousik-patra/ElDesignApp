function loadPlotPlan(userImageFile, scaleX, scaleY, centreX, centreY, elevation, opacity) {
    // online pdf to png converter: https://pdf2png.com
    // online image editor: https://www.online-image-editor.com
    // remove if there is any existing plot
    //

    if (userImageFile === undefined) {
        alert("Select the plot plan background image.");
        loadPlotPlanCalled = false;
        return;
    }
    // Create an image
    const image = new Image(); // or document.createElement('img' );
    // Set image source
    image.src = userImageFile;
    image.onload = function () {
        texture.needsUpdate = true;
    };

    var texture = new THREE.Texture(image);
    var plotPlanOpacity = opacity;
    var planeMaterial = new THREE.MeshPhongMaterial({
        map: texture,
        color: 0xa0a0a0,
        transparent: true,
        opacity: plotPlanOpacity,
        side: THREE.DoubleSide,
        flatShading: true,
    });

    // plane geometry
    var planeGeometry = new THREE.PlaneBufferGeometry(rendererWidth, rendererHeight);
    planeMesh = new THREE.Mesh(planeGeometry, planeMaterial);
    planeMesh.translateOnAxis(new THREE.Vector3(0, 0, 1), elevation);
    scene.add(planeMesh);

    planeMesh.name = "plotplan"; // main background any additional plane to be added to this main background
    var planeMeshCount = getObjectByNameArray(scene, 'plotplan').length;
    planeMesh.Tag = "plotplan" + planeMeshCount; //toString();

    scalePlotPlan(scaleX, scaleY);
    centrePlotPlan(centreX, centreY, elevation);
    shadowRing(0, 0, plotElevtn + 0.01); // to shadow mouse movement when ctrl key is pressed

    renderer.render(scene, camera);
    return scene;
}

function scalePlotPlan(scaleX, scaleY) {
    // scale the plane
    if (scaleX !== undefined && scaleY !== undefined) {
        if (scaleX !== 0 && scaleY !== 0) {
            planeMesh.scale.set(scaleX, scaleY, 1);
            planeMesh.updateMatrixWorld();
            scene.getObjectByName('shadowRing');
            while (scene.getObjectByName('clickedPoints')) {
                scene.remove(scene.getObjectByName('clickedPoints'));
            }
            orthoCamera = new THREE.OrthographicCamera(rendererWidth / -2, rendererWidth / 2, rendererHeight / 2, rendererHeight / -2, 1, 1000);
            renderer.render(scene, orthoCamera);
        }
    }
}

function centrePlotPlan(centreX, centreY, elevation) {
    // alighn to centre
    if (centreX !== undefined && centreY !== undefined) {
        planeMesh.position.set(-centreX, -centreY, elevation);
        planeMesh.updateMatrixWorld();
        while (scene.getObjectByName('clickedPoints')) {
            scene.remove(scene.getObjectByName('clickedPoints'));
        }
        renderer.render(scene, camera);
    }
}