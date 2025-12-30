import * as THREE from 'three';
import {OrbitControls} from 'three/examples/jsm/controls/OrbitControls.js';
import {FontLoader} from 'three/examples/jsm/loaders/FontLoader';
import {TextGeometry} from 'three/examples/jsm/geometries/TextGeometry';
import {GLTFExporter} from 'three/examples/jsm/exporters/GLTFExporter';
import {GUI} from 'three/examples/jsm/libs/lil-gui.module.min'; //dat-gui
//import {Stats} from 'three/examples/jsm/libs/stats.module';
import {drawPlaneMesh} from '../src/threejs/objects/plane.js'
import {drawLadderMesh} from '../src/threejs/objects/ladder'
import {drawBendMesh} from '../src/threejs/objects/bend.js'
import {drawTeeMesh} from '../src/threejs/objects/tee.js'
import {drawCrossMesh} from '../src/threejs/objects/cross.js'
import {drawNodeMesh} from './threejs/objects/node'
import {drawSleeveMesh} from '../src/threejs/objects/sleeve.js'
import {drawEquipmentMesh} from '../src/threejs/objects/equipment'
import {drawSleepMesh} from '../src/threejs/objects/sleeve'
import {drawRefPointMesh} from '../src/threejs/objects/refPoint'

import * as PLANE from '../src/threejs/functions/planeFunctions'


var dotNetObj;
let scene, camera, renderer;
let canvas, controls, currentPlane;
let rendererWidth = window.innerWidth;
let rendererHeight = window.innerHeight;
var raycaster;
var [posx, posy, posz, eventclientX, eventclientY,
    eventpageX, eventpageY, eventoffsetX, eventoffsetY, eventlayerX, eventlayerY, eventx, eventy, mousex, mousey,
    linePositionx, linePositiony, linePositionz] = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
//const stats = new Stats();

// materials
var defaultOpacity = 0.9, defaultColor = 0xc8c8c8, defaultColorLadder = 0xc80000, defaultColorEq = 0x7f868a,
    defaultColorTrench = 0x77ff88;
var defaultColorSSFloor = 0x424242, defaultColorSleeve = 0x7f868a, defaultColorMCT = 0xc8c8c8,
    defaultColorConcrete = 0xc8c8c8;
var substationFloorMaterial = new THREE.MeshStandardMaterial({
    color: defaultColorSSFloor,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true
});
var ladderMaterial = new THREE.MeshPhongMaterial({
    color: defaultColorLadder,
    shininess: 100,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true
});
var equipmentMaterial = new THREE.MeshPhongMaterial({
    color: defaultColorEq,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true
});
var sleeveMaterial = new THREE.MeshPhongMaterial({
    color: defaultColorSleeve,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true
});
var trenchMaterial = new THREE.MeshPhongMaterial({
    color: defaultColorTrench,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true
});
var concreteMaterial = new THREE.MeshPhongMaterial({
    color: defaultColorConcrete,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true
});
var mctMaterial = new THREE.MeshPhongMaterial({
    color: defaultColorMCT,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true
});
var pointsMaterial = new THREE.PointsMaterial({color: 0x0080ff, size: 0.02, alphaTest: 0.5});
var redRingMaterial = new THREE.MeshBasicMaterial({color: 0xff0000, side: THREE.DoubleSide});
var yellowRingMaterial = new THREE.MeshBasicMaterial({color: 0xffff00, side: THREE.DoubleSide});
var selectedObjectMaterial = new THREE.MeshPhongMaterial({
    color: 0xffff00,
    transparent: true,
    opacity: 0.8,
    side: THREE.DoubleSide,
    flatShading: true
});
var savedObject, savedObjectMaterial; // storing previously selecyed object and its meterials properties
var selectItemColor = new THREE.MeshPhongMaterial({
    color: 'yellow',
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true
});

var ctrlKeyPressed = false;
var shiftKeyPressed = false;

var shadowRingMaterial = new THREE.MeshBasicMaterial({color: 0xcccccc, side: THREE.DoubleSide});

var intervalId = window.setInterval(function () {
    if (camera != undefined) {
        const sceneInfo = JSON.stringify([rendererWidth, rendererHeight, camera.position.x, camera.position.y, camera.position.z, camera.rotation.x, camera.rotation.y, camera.rotation.z]);
        dotNetObj.invokeMethodAsync("SaveSceneInfo", sceneInfo);
    }
}, 10000);

let eventListenersAdded = false;

//clearInterval(intervalId)
function drawScene3Js(divId, sceneInfoJson = "", dotNetObjRef, retryCount = 0) {
    
    
    
    if (eventListenersAdded) return; // Prevent adding listeners multiple times

    canvas = document.getElementById(divId);

    // If canvas doesn't exist yet, retry a few times
    if (!canvas) {
        if (retryCount < 10) {
            console.warn(`Canvas element '${divId}' not found, retrying... (${retryCount + 1}/10)`);
            setTimeout(() => drawScene3Js(divId, sceneInfoJson, dotNetObjRef, retryCount + 1), 100);
            return;
        } else {
            console.error(`Canvas element '${divId}' not found after 10 retries. Aborting.`);
            return;
        }
    }

    // Check if element is visible (display !== 'none')
    const computedStyle = window.getComputedStyle(canvas);
    if (computedStyle.display === 'none') {
        if (retryCount < 10) {
            console.warn(`Canvas element '${divId}' is hidden, retrying... (${retryCount + 1}/10)`);
            setTimeout(() => drawScene3Js(divId, sceneInfoJson, dotNetObjRef, retryCount + 1), 100);
            return;
        } else {
            console.error(`Canvas element '${divId}' is still hidden after 10 retries. Aborting.`);
            return;
        }
    }

    dotNetObj = dotNetObjRef;

    
    canvas.setAttribute('tabindex', '0'); // Make canvas focusable
    //canvas.focus(); // Optionally set focus programmatically
    scene = new THREE.Scene();
    scene.Name = "name";
    
    // Camera with larger viewing range
    camera = new THREE.PerspectiveCamera(
        60,                                          // FOV (60-75 is typical)
        window.innerWidth / window.innerHeight,      // Aspect ratio
        0.1,                                         // Near plane (keep small for close-up detail)
        20000                                        // Far plane (large enough for your scene)
    );
    
    //light
    scene.add(new THREE.AmbientLight(0xffffff, 0.9));
    scene.add(new THREE.HemisphereLight(0x9f9f9b, 0x080820, 1));
    const pointLight = new THREE.PointLight(0xffffff, 1, 100);
    pointLight.position.set(rendererWidth / 2, rendererHeight / 2, 100);
    scene.add(pointLight);

    const axesHelper = new THREE.AxesHelper(500);
    scene.add(axesHelper);
    //shadowRing(0, 0, plotElevtn + 0.01); // to shadow mouse movement when ctrl key is pressed    
    const shadowRing = new THREE.Mesh(new THREE.RingGeometry(0.2, 0.21, 20), shadowRingMaterial);
    shadowRing.name = 'shadowRing';
    shadowRing.position.set(0, 0, 0.01);
    scene.add(shadowRing);

    raycaster = new THREE.Raycaster();

    renderer = new THREE.WebGLRenderer();
    renderer.setSize(window.innerWidth, window.innerHeight);
    canvas.appendChild(renderer.domElement);

    // Initialize the plot plan manager
    PLANE.initPlotPlanManager(scene, renderer, camera, rendererWidth, rendererHeight);
    
    controls = new OrbitControls(camera, renderer.domElement);
    controls.autoRotate = true;
    controls.autoRotateSpeed = 6;
    controls.screenSpacePanning = true;
    controls.minDistance = 1;
    controls.maxDistance = 10000; // less than camera far
    if (sceneInfoJson != "") {
        var sceneInfo = JSON.parse(sceneInfoJson);
        rendererWidth = sceneInfo[0];
        rendererHeight = sceneInfo[1];
        camera.position.x = sceneInfo[2];
        camera.position.y = sceneInfo[3];
        camera.position.z = sceneInfo[4];
        camera.rotation.x = sceneInfo[5];
        camera.rotation.y = sceneInfo[6];
        camera.rotation.z = sceneInfo[7];
    }
    controls.addEventListener('change', () => {
        const distance = camera.position.distanceTo(shadowRing.position);
        const scale = distance *.1;
        shadowRing.scale.set(scale, scale, scale);

        // Updated scaling logic
        const refObjects = getObjectsByName(scene, 'refPoint');

        if (refObjects && refObjects.length > 0) {
            refObjects.forEach((obj) => {
                const d = camera.position.distanceTo(obj.position); // Use obj's position (group or individual)
                const sc = d * 0.1;

                if (obj instanceof THREE.Group) {
                    // Scale the group itself: this scales all children and their relative positions
                    obj.scale.set(sc, sc, sc);
                } else {
                    // For non-groups, scale the object directly (as before)
                    obj.scale.set(sc, sc, sc);
                }
            });
        }
        
    });
    
    const sceneInfo1 = JSON.stringify([rendererWidth, rendererHeight, camera.position.x, camera.position.y, camera.position.z, camera.rotation.x, camera.rotation.y, camera.rotation.z]);
    dotNetObj.invokeMethodAsync("SaveSceneInfo", sceneInfo1);
    controls.update();

    camera.position.z = 5;

// Add event listeners
    window.addEventListener('resize', onWindowResize, false);
    window.addEventListener('keydown', onKeyDown, false);
    window.addEventListener('keyup', onKeyUp, false);
    renderer.domElement.addEventListener('mousemove', onMouseMove, false);
    renderer.domElement.addEventListener('wheel', mouseWheel, false);
    renderer.domElement.addEventListener('mousedown', mouseDownListener, false);
    renderer.domElement.addEventListener('mousemove', mouseMoveListener, false);
    renderer.domElement.addEventListener('mouseup', mouseUpListener, false);

    // Add double-click event listener
    renderer.domElement.addEventListener('dblclick', autoZoomToFit);
    
    
// Prevent context menu on Ctrl+click or right-click
    renderer.domElement.addEventListener('contextmenu', (event) => {
        event.preventDefault();
    });

    eventListenersAdded = true;

// onWindowResize (unchanged)
    function onWindowResize() {
        console.log('Window resize event triggered');
        rendererWidth = window.innerWidth;
        rendererHeight = window.innerHeight;
        camera.aspect = rendererWidth / rendererHeight;
        const sceneInfo = JSON.stringify([rendererWidth, rendererHeight, camera.position.x, camera.position.y, camera.position.z, camera.rotation.x, camera.rotation.y, camera.rotation.z]);
        dotNetObj.invokeMethodAsync("SaveSceneInfo", sceneInfo);
        camera.updateProjectionMatrix();
        scene.updateMatrixWorld(true);
        renderer.setSize(rendererWidth, rendererHeight);
        render();
    }

// onKeyDown (unchanged, as it works correctly)
    function onKeyDown(event) {
        console.log(`Key down event triggered: ${event.key}, Key code: ${event.code}`);
        if (event.key === 'Control') {
            ctrlKeyPressed = true;
        }
        if (event.key === 'Shift') {
            shiftKeyPressed = true;
        }

        if (event.key === 'Escape') {
            // clear all the clicked reference points to start afres
            clearRefPoints();
        }
        
        showHideShadowRingNPosinLines();
        console.log(`Key pressed event after: ${event.key}, Key code: ${event.code}, 
        Ctrl key pressed status: ${ctrlKeyPressed}, Shift key pressed status: ${shiftKeyPressed}`);
    }

// onKeyUp (unchanged, as it works correctly)
    function onKeyUp(event) {
        console.log(`Key released event triggered: ${event.key}, Key code: ${event.code}`);
        if (event.key === 'Control') {
            ctrlKeyPressed = false;
        }
        if (event.key === 'Shift') {
            shiftKeyPressed = false;
        }
        showHideShadowRingNPosinLines();
        console.log(`Key released event after: ${event.key}, Key code: ${event.code}, 
        Ctrl key pressed status: ${ctrlKeyPressed}, Shift key pressed status: ${shiftKeyPressed}`);
    }

// onMouseMove (unchanged, as it works with global ctrlKeyPressed)
    function onMouseMove(event) {
        if (ctrlKeyPressed) {
            [mouse, pos] = findCoordinate(event);
            console.log("Control Key pressed and mouse move ", pos);
            scene.getObjectByName('shadowRing').position.set(pos.x, pos.y, plotElevtn + 0.01);
            var linePosition = topPlotPlanIntersectPosition(event);
            scene.remove(scene.getObjectByName('positionlinex'));
            var pointsx = [];
            pointsx.push(new THREE.Vector3(linePosition.x + 100, linePosition.y, linePosition.z));
            pointsx.push(new THREE.Vector3(linePosition.x - 100, linePosition.y, linePosition.z));
            const geometry1 = new THREE.BufferGeometry().setFromPoints(pointsx);
            const material1 = new THREE.LineBasicMaterial({ color: 0xff0000 });
            linex = new THREE.Line(geometry1, material1);
            linex.name = 'positionlinex';
            scene.add(linex);
            scene.remove(scene.getObjectByName('positionliney'));
            var pointsy = [];
            pointsy.push(new THREE.Vector3(linePosition.x, linePosition.y + 100, linePosition.z));
            pointsy.push(new THREE.Vector3(linePosition.x, linePosition.y - 100, linePosition.z));
            const geometry2 = new THREE.BufferGeometry().setFromPoints(pointsy);
            const material2 = new THREE.LineBasicMaterial({ color: 0x0000ff });
            liney = new THREE.Line(geometry2, material2);
            liney.name = 'positionliney';
            scene.add(liney);
            render();
        }
    }

    let drag = false;

    function mouseDownListener(event) {
        drag = false;
    }

    function mouseMoveListener(event) {
        drag = true;
    }

    function mouseUpListener(event) {
        if (drag) {
            console.log('Drag Detected');
        } else {
            console.log('Click Detected');
            // Only handle left-clicks
            if (event.button === 0) {
                console.log('Left-click detected');
                // Use event.ctrlKey and event.shiftKey for reliability
                console.log(`Click event: Ctrl key pressed status: ${event.ctrlKey}, Shift key pressed status: ${event.shiftKey}`);

                // Moved from raycast and onClick
                [mouse, pos] = findCoordinate(event);
                
                var linePosition = topPlotPlanIntersectPosition(event);
                var objectPosition = topObjectIntersectPosition(event);
                console.log(`Mouse position: Mouse:[${mouse.x}, ${mouse.y}],  `
                    + `Position:[${pos.x}, ${pos.y}],  Client:[${event.clientX}, ${event.clientY}],  `
                    + `Screen:[${event.screenX}, ${event.screenY}],  Page:[${event.pageX}, ${event.pageY}],  `
                    + `Offset:[${event.offsetX}, ${event.offsetY}],  Layer:[${event.layerX}, ${event.layerY}],  `
                    + `EvenXY:[${event.x}, ${event.y}],  LinePosition :[${linePosition.x}, ${linePosition.y}, ${linePosition.z}],  `
                    + `Render Width Height:[${rendererWidth}, ${rendererHeight}]`);

                dotNetObj.invokeMethodAsync("MouseClick", pos.x, pos.y, pos.z, event.clientX, event.clientY,
                    event.pageX, event.pageY, event.offsetX, event.offsetY, event.layerX, event.layerY,
                    event.x, event.y, mouse.x, mouse.y, linePosition.x, linePosition.y, linePosition.z);

                if (!event.ctrlKey) {
                    console.log(`Select Object`);
                    // castObject(event); // Uncomment if needed
                }
                if (!event.ctrlKey && event.shiftKey) {
                    console.log(`De-select Object`);
                    // castObject(event); // Uncomment if needed
                }

                if (event.ctrlKey) {
                    console.log(`Clicked while Ctrl key is pressed`);
                    let point = intersectPoint(currentPlane.Tag);
                    point.z +=0.01;
                    //refPoints.push([point.x, point.y, mouse.x, mouse.y]);

                    refPoints.push([linePosition.x, linePosition.y, mouse.x, mouse.y]);
                    
                    let refPointText="";

                    refPointText =  refPointTexts[refPoints.length-1];
                    
                    
                    // if (scalePlaneDoneStatus === false){
                    //     refPointText =  refPointTexts[refPoints.length-1];
                    // }else {
                    //     // centre
                    //     refPointText = refPointTexts[-1];
                    // }
                    scene.add(drawRefPointMesh(refPointText, linePosition, .5));
                    render();
                    console.log(`Draw UpdateRefPoints ${refPoints.toString()}`);
                    dotNetObj.invokeMethodAsync("UpdateRefPoints", JSON.stringify(refPoints));
                    // if (refPoints.length ===4 || scalePlaneDoneStatus === true && refPoints.length ===1) {
                    //     console.log(`Draw UpdateRefPoints ${refPoints.toString()}`);
                    //     dotNetObj.invokeMethodAsync("UpdateRefPoints", JSON.stringify(refPoints));
                    //     }
                }
            } else if (event.button === 1) {
                console.log('Middle-click detected');
                // Add middle-click behavior if needed
            } else if (event.button === 2) {
                console.log('Right-click detected');
                // Add right-click behavior if needed
            }
        }
    }

    function findCoordinate(event) {
        // calculate mouse position in normalized device coordinates (-1 to +1) for both components
        var rect = event.target.getBoundingClientRect();
        var mouse = new THREE.Vector2();

        var x = event.clientX - rect.left; //x position within the element.
        var y = event.clientY - rect.top;  //y position within the element.
        mouse.x = (x / rendererWidth) * 2 - 1;
        mouse.y = -(y / rendererHeight) * 2 + 1;
        //
        var vec = new THREE.Vector3(); // create once and reuse
        var pos = new THREE.Vector3(); // create once and reuse
        //
        vec.set(
            ((event.clientX - rect.left) / rendererWidth) * 2 - 1,
            -((event.clientY - rect.top) / rendererHeight) * 2 + 1,
            0);
        vec.unproject(camera);
        vec.sub(camera.position).normalize();
        var distance = -camera.position.z / vec.z;
        pos.copy(camera.position).add(vec.multiplyScalar(distance));
        // rounding to two decimal
        pos.x = Math.round(pos.x * 100) / 100;
        pos.y = Math.round(pos.y * 100) / 100;
        return [mouse, pos];
    }

    function animate() {
        requestAnimationFrame(animate);

        //cube.rotation.x += 0.01;
        //cube.rotation.y += 0.01;

        renderer.render(scene, camera);
    }

    function GetSceneInfo(scene, camera) {
        var sceneInfo = {
            "sceneName": scene.Name,
            "cameraPosition": {
                "x": camera.position.x,
                "y": camera.position.y,
                "z": camera.position.z
            },
            "cameraRotation": {
                "x": camera.rotation.x,
                "y": camera.rotation.y,
                "z": camera.rotation.z
            },
            "rendererWidth": rendererWidth,
            "rendererHeight": rendererHeight,

            //"getWorlDirection": JSON.stringify(cameraTargetVector),

        };
        return JSON.stringify(sceneInfo);
    }

    animate();
}

function drawCube3Js() {
    let side = Math.random() * 1;
    let geometry = new THREE.BoxGeometry(side, side, side);
    let color = new THREE.Color();
    color.setHex(`0x${parseInt(Math.random() * 255)}${parseInt(Math.random() * 255)}${parseInt(Math.random() * 255)}`);
    let material = new THREE.MeshBasicMaterial({color: color});
    let cube = new THREE.Mesh(geometry, material);
    cube.position.x = Math.random() * 50;
    cube.position.y = Math.random() * 50;
    cube.position.z = Math.random() * 10;
    scene.add(cube);
    animate();
}

// Utility function (as above)
function getObjectsByName(scene, name) {
    const objects = [];
    scene.traverse((obj) => {
        if (obj.name === name) {
            objects.push(obj);
        }
    });
    return objects;
}





function clearScene3Js() {
    scene.children.forEach(obj => {
        if (obj.isMesh) {
            scene.remove(obj);
        }
    });
    renderer.render(scene, camera);
}

function hide3D3Js(hidePP) {
    scene.children.forEach(child => {
        if (child !== undefined) {
            if (child.Tag !== undefined) {
                if (child.Tag.includes("plotplan")) {
                    child.visible = hidePP;
                }
            }
        }
    });
    renderer.render(scene, camera);
}

// function drawPlane3Js(planeName, planeTag, imageString, scaleX, scaleY, centreX, centreY, elevation, opacity) {
//     //console.log("Step 3: Drawing Plane from three.js drawPlane : ", planeTag);
//     scene.add(drawPlaneMesh(rendererWidth, rendererHeight, planeName, planeTag, imageString, scaleX, scaleY, centreX, centreY, elevation, opacity));
//     currentPlane = scene.children[scene.children.length - 1];
//     scalePlaneDoneStatus = false;
// }


function updateRefPointTexts3Js(texts){
    console.log("updateRefPointTexts3Js", texts);
    refPointTexts = texts;
    console.log("updateRefPointTexts3Js", refPointTexts);
    //PLANE.updateRefPointTexts(texts);
}


function drawPlane3Js(planeName, planeTag, imageString, scaleX, scaleY, centreX, centreY, elevation, opacity) {
    scene.add(PLANE.drawPlane(planeName, planeTag, imageString, scaleX, scaleY, centreX, centreY, elevation, opacity));
    currentPlane = scene.children[scene.children.length - 1];
    scalePlaneDoneStatus = false;
}



function rotatePlaneJs(angle) {
    if(currentPlane){
        if (angle !== undefined ) {
            currentPlane.rotateZ(angle);
            renderer.render(scene, camera);
        }
    }
}

let scalePlaneDoneStatus = false;
function scalePlane3Js(scaleX, scaleY) {
    if(currentPlane){
        if (scaleX !== undefined && scaleY !== undefined) {
            if (scaleX !== 0 && scaleY !== 0) {
                currentPlane.scale.set(scaleX, scaleY, 1);
                currentPlane.updateMatrixWorld();
                clearRefPoints();
                orthoCamera = new THREE.OrthographicCamera(rendererWidth / -2, rendererWidth / 2, rendererHeight / 2, rendererHeight / -2, 1, 1000);
                renderer.render(scene, orthoCamera);
                scalePlaneDoneStatus = true;
            }
        }
    }
}

function centrePlan3Js(centreX, centreY, elevation) {
    console.log("centrePlan3Js", centreX, centreY, elevation);
    // align to centre
    if(currentPlane) {
        if (centreX !== undefined && centreY !== undefined) {
            currentPlane.position.set(-centreX, -centreY, elevation);
            currentPlane.updateMatrixWorld();
            clearRefPoints();
            // orthoCamera = new THREE.OrthographicCamera(rendererWidth / -2, rendererWidth / 2, rendererHeight / 2, rendererHeight / -2, 1, 1000);
            renderer.render(scene, orthoCamera);
        }
    }
}

function removePlane3Js(tag) {
    return PLANE.removePlane(tag);
}

function removeAllPlotPlans3Js() {
    return PLANE.removeAllPlotPlans();
}

function repositionAllPlanes3Js(deltaX, deltaY) {
    PLANE.repositionAllPlanes(deltaX, deltaY);
}

function setPlaneOpacity3Js(tag, opacity) {
    PLANE.setPlaneOpacity(tag, opacity);
}

function setPlaneVisibility3Js(tag, visible) {
    PLANE.setPlaneVisibility(tag, visible);
}

function debugPlotPlans3Js() {
    return PLANE.debugPlotPlans();
}

function drawLadder3Js(tag, jsonPoints, color, opacity) {
    scene.add(drawLadderMesh(tag, jsonPoints, ladderMaterial, color, opacity));
}

function drawLadderChunk3Js(jsonDataList, opacity) {
    var dataList = JSON.parse(jsonDataList);
    dataList.forEach((item, index) => {
        scene.add(drawLadderMesh(item.Tag, item.JsonPoints, ladderMaterial, item.Color, opacity))
    });
}

function drawBend3Js(tag, jsonFacePoints, color, opacity) {
    scene.add(drawBendMesh(tag, jsonFacePoints, ladderMaterial, color, opacity));
}

function drawTee3Js(tag, jsonFacePoints, color, opacity) {
    scene.add(drawTeeMesh(tag, jsonFacePoints, ladderMaterial, color, opacity));
}

function drawCross3Js(tag, jsonFacePoints, color, opacity) {
    scene.add(drawCrossMesh(tag, jsonFacePoints, ladderMaterial, color, opacity));
}

function drawNode3Js(tag, jsonPoint, color, opacity) {
    scene.add(drawNodeMesh(tag, jsonPoint, pointsMaterial, color, opacity));
}

function drawSleeve3Js(tag, jsonPoints, radious, segment, radialSegments, color, opacity) {
    scene.add(drawSleeveMesh(tag, jsonPoints, radious, segment, radialSegments, sleeveMaterial, color, opacity));
    renderer.render(scene, camera);
}

function drawEquipment3Js(tag, x, y, z, w, d, h, a, color, opacity, colortext) {
    scene.add(drawEquipmentMesh(tag, x, y, z, w, d, h, a, equipmentMaterial, color, opacity, colortext));
}


function getRandomColor() {
    new THREE.Color();
    color.setHex(`0x${parseInt(Math.random() * 255)}${parseInt(Math.random() * 255)}${parseInt(Math.random() * 255)}`);
    return color;
}


export {
    clearScene3Js,
    hide3D3Js,
    drawScene3Js,
    drawCube3Js,
    updateRefPointTexts3Js,
    drawPlane3Js,
    rotatePlaneJs,
    scalePlane3Js,
    centrePlan3Js,
    removePlane3Js,           // NEW
    removeAllPlotPlans3Js,    // NEW
    repositionAllPlanes3Js,   // NEW
    setPlaneOpacity3Js,       // NEW
    setPlaneVisibility3Js,    // NEW
    debugPlotPlans3Js,        // NEW
    drawLadder3Js,
    drawLadderChunk3Js,
    drawNode3Js,
    drawBend3Js,
    drawTee3Js,
    drawCross3Js,
    drawSleeve3Js,
    drawEquipment3Js
}


window.Layout3d = {
    load: (state, div1, guiDiv, file, scaleX, scaleY, centreX, centreY, elevation, opacity, reference) => {
        loadScene3d(state, div1, guiDiv, file, scaleX, scaleY, centreX, centreY, elevation, opacity, reference);
    },
    scalePlotPlan: (scaleX, scaleY) => {
        scalePlotPlan(scaleX, scaleY);
    },
    centrePlotPlan: (centreX, centreY, elevation) => {
        centrePlotPlan(centreX, centreY, elevation);
    },
    //drawLadder: (tag, type, jsonPoints, opacity, color, animationText, progress) => { drawLadder(tag, type, jsonPoints, opacity, color, animationText, progress); },
    //drawBend: (tag, jsonPoints, opacity, color) => { drawBend(tag, jsonPoints, opacity, color); },
    //drawTee: (tag, jsonPoints, opacity, color) => { drawTee(tag, jsonPoints, opacity, color); },
    //drawCross: (tag, jsonPoints, opacity, color) => { drawCross(tag, jsonPoints, opacity, color); },
    drawCable: (tag, jsonPointSetSegments, tubeTubularSegments, dia, tubeRadialSegments, opacity, color, option) => {
        drawCable(tag, jsonPointSetSegments, tubeTubularSegments, dia, tubeRadialSegments, opacity, color, option);
    },

    //drawNode: (tag, jsonPoint, color) => { drawNode(tag, jsonPoint, color); },
    //drawSleeve: (tag, radious, jsonPoints, opacity, color) => { drawSleeve(tag, radious, jsonPoints, opacity, color); },
    drawBoard: (tagName, jsonPoint, v, w, d, h, angle, color, opacity) => {
        drawBoard(tagName, jsonPoint, v, w, d, h, angle, color, opacity);
    },
    equipment: (tag, x1, y1, z1, x2, y2, z2, w, d, h, a, color, opacity, colortext) => {
        equipment(tag, x1, y1, z1, x2, y2, z2, w, d, h, a, color, opacity, colortext);
    },
    saveSceneGLTF: () => {
        saveSceneGLTF();
    },
    removeItem: (tag) => {
        removeItem(tag);
    },
    searchItem: (tag) => {
        searchItem(tag);
    },
    toggleFunction: () => {
        toggleFunction();
    },
    hidePlotPlan: (hidePP) => {
        hidePlotPlan(hidePP);
    },
    orthoView: (ortho, xp, yp, zp) => {
        orthoView(ortho, xp, yp, zp);
    },

    clearClickedPoints: () => {
        clearClickedPoints();
    },
    selectObjectHighlight: (tag) => {
        selectObjectHighlight(tag);
    },
    copyText: function (text) {
        navigator.clipboard.writeText(text).then(function () {
            alert(text, " copied to clipboard!");
        })
            .catch(function (error) {
                alert(error);
            });
    }

};


var projectName = "BlazorNPM";

var caller;
var clock = new THREE.Clock();
var mixer, cameraPerspective, orthoCamera;
var xscalefactor = 1.00, yscalefactor = 1.00, lxcentreworld = -110.62, lycentreworld = -72.08; // without scalling to match ladder coordinates
var planeMesh, lxmin, lxmax, lymin, lymax, xxmin, xxmax, yymin, yymax; // global variable for plot plan scale
var plotElevtn = 0;
//var rendererWidth = window.innerWidth - 60;
//var rendererHeight = Math.min(window.innerHeight - 100, rendererWidth * 0.6);

var selectedObject = [], selectedInputTab, clickCoordinate = [], clickedPointSeq = [];
var refPoints = [], refPointTexts = []; //['X-Left', 'X-Right', 'Y-Bottom', 'Y-Top', 'Centre (0,0)','Ref-New Plan', 'Ref-Key Plan'];

var mouse = new THREE.Vector2();
var pos = new THREE.Vector3();

var raycaster;

// ***** Clipping planes: *****
//const localPlane = new THREE.Plane(new THREE.Vector3(0, - 1, 0), 0.8);
//const xPlane = new THREE.Plane(new THREE.Vector3(- 1, 0, 0), 0.1);
//const yPlane = new THREE.Plane(new THREE.Vector3(0, -1, 0), 0.1);
//const zPlane = new THREE.Plane(new THREE.Vector3(0, 0, -1), 0.1);
var startTime;

// ***** Clipping planes: *****

const localPlane = new THREE.Plane(new THREE.Vector3(0, -1, 0), 400);
const globalPlane = new THREE.Plane(new THREE.Vector3(-1, 0, 0), 20);
const globalPlane1 = new THREE.Plane(new THREE.Vector3(0, -1, 0), 0.1);
const localPlane1 = new THREE.Plane(new THREE.Vector3(0, 0, 1), 0.1);

// ***** Clipping setup (renderer): *****
const globalPlanes = [globalPlane], Empty = Object.freeze([]);
const globalPlanesz = [globalPlane1];

//const xPlanes = [xPlane], Empty = Object.freeze([]);
//const yPlanes = [yPlane], Empty = Object.freeze([]);
//const zPlane = [zPlanes], Empty = Object.freeze([]);


// ***** Clipping setup (renderer): *****
//const Empty = Object.freeze([]), xPlanes = [xPlane], yPlanes = [yPlane], zPlanes = [zPlane];

//materials
var material = new THREE.MeshPhongMaterial({
    color: 0x80ee10,
    shininess: 100,
    side: THREE.DoubleSide,
    clippingPlanes: [localPlane],
    clipShadows: true
});
var defaultOpacity = 0.9;
var defaultColor = 0xc8c8c8;
var defaultColorLadder = 0xc80000;
var defaultColorEq = 0x7f868a, defaultColorSSFloor = 0x424242;
var defaultColorSleeve = 0xc8c8b8;
var defaultColorConcrete = 0xc8c898;
var defaultColorTrench = 0xc8c878;
var defaultColorMCT = 0xc8c838;
var substationFloorMaterial = new THREE.MeshStandardMaterial({
    color: defaultColorSSFloor,
    transparent: true,
    opacity: 0.5,
    side: THREE.DoubleSide,
    flatShading: true,
    clippingPlanes: [localPlane],
    clipShadows: true
});
var laddermaterial = new THREE.MeshPhongMaterial({
    color: defaultColorLadder,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true,
    clippingPlanes: [localPlane],
    clipShadows: true
});
var equipmentmaterial = new THREE.MeshPhongMaterial({
    color: defaultColorEq,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true,
    clippingPlanes: [localPlane],
    clipShadows: true
});
var sleevematerial = new THREE.MeshPhongMaterial({
    color: defaultColorSleeve,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true,
    clippingPlanes: [localPlane],
    clipShadows: true
});
var sleeveRadialSegments = 30; // no of radial segments for tubes as sleeves
var sleeveTubularSegments = 30; // no of tubular segments for tubes as sleeves
var trenchmaterial = new THREE.MeshPhongMaterial({
    color: defaultColorTrench,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true,
    clippingPlanes: [localPlane],
    clipShadows: true
});
var concretematerial = new THREE.MeshPhongMaterial({
    color: defaultColorConcrete,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true,
    clippingPlanes: [localPlane],
    clipShadows: true
});
var mctmaterial = new THREE.MeshPhongMaterial({
    color: defaultColorMCT,
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true,
    clippingPlanes: [localPlane],
    clipShadows: true
});
var pointsMaterial = new THREE.PointsMaterial({color: 0x0080ff, size: 0.02, alphaTest: 0.5});
var redRingMaterial = new THREE.MeshBasicMaterial({color: 0xff0000, side: THREE.DoubleSide});
var yellowRingMaterial = new THREE.MeshBasicMaterial({color: 0xffff00, side: THREE.DoubleSide});
var selectedObjectMaterial = new THREE.MeshPhongMaterial({
    color: 0xffff00,
    transparent: true,
    opacity: 0.8,
    side: THREE.DoubleSide,
    flatShading: true,
    clippingPlanes: [localPlane],
    clipShadows: true
});
var savedObject, savedObjectMaterial; // storing previously selecyed object and its meterials properties
var selectItemColor = new THREE.MeshPhongMaterial({
    color: 'yellow',
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true,
    clippingPlanes: [localPlane],
    clipShadows: true
});
var loader = new FontLoader();
/*var textMesh, textGeometry;*/
var textMaterial = new THREE.MeshStandardMaterial({
    color: 'brown',
    transparent: true,
    opacity: defaultOpacity,
    side: THREE.DoubleSide,
    flatShading: true,
    clippingPlanes: [localPlane],
    clipShadows: true
});



var linex, liney;






function animate1() {
    //requestID = requestAnimationFrame(animate);
    // need to update the orbitcontrols for autorotate camera to take effect
    //controls.update();
    requestAnimationFrame(animate);
    controls.update();


    arrowCamera.position.copy(camera.position);
    arrowCamera.position.sub(controls.target);
    arrowCamera.position.setLength(300);

    arrowCamera.lookAt(arrowScene.position);

    render();
}


// Function to auto-zoom to fit the scene
function autoZoomToFit() {
    console.log("double click to auto fit");
    // Create a Box3 to calculate the bounding box of the scene
    const box = new THREE.Box3();
    scene.traverse((object) => {
        if (object.isMesh) {
            box.expandByObject(object);
        }
    });

    // Get the size and center of the bounding box
    const size = box.getSize(new THREE.Vector3());
    const center = box.getCenter(new THREE.Vector3());

    // Set the camera to look at the center
    controls.target.copy(center);
    camera.lookAt(center);

    // Calculate the distance the camera needs to be from the scene
    const maxDim = Math.max(size.x, size.y, size.z);
    const fov = camera.fov * (Math.PI / 180); // Convert FOV to radians
    const cameraZ = maxDim / (2 * Math.tan(fov / 2));

    // Adjust camera position
    camera.position.copy(center);
    camera.position.z += cameraZ * 1.5; // Add some padding (adjust multiplier as needed)

    // Update the controls
    controls.update();
}




function render() {
    var a = clock.getDelta();
    var delta = clock.getDelta();
    if (mixer !== undefined) {
        mixer.update(delta);
    }
    //fitCameraToObject(camera, planeMesh, 50);

    if (resizeRendererToDisplaySize(renderer)) {
        const canvas = renderer.domElement;
        camera.aspect = canvas.clientWidth / canvas.clientHeight;
        camera.updateProjectionMatrix();
    }
    //console.log('Render:', renderer.info.render.calls);
    renderer.render(scene, camera);
}


// var docDiv1 = document.getElementById(div1)
// docDiv1.body.appendChild(stats.dom)
// function animate() {
//     stats.begin();
//     renderer.render(scene, camera);
//     stats.end();
//     requestAnimationFrame(animate);
// }


// glf exporter
function saveSceneGLTF() {
    //https://github.com/mrdoob/three.js/blob/master/examples/misc_exporter_gltf.html#L46
    const gltfExporter = new GLTFExporter();
    //
    var hideObjectTypesForExport = ["rawladder", "ladder", "bend", "tee", "cross", "node", "sleeve", "equipment"];
    scene.traverse(child => {
        if (child instanceof THREE.Mesh) {
            if (hideObjectTypesForExport.includes(child.type)) {
                child.visible = false;
            }
        }
    });
    //
    const options = {
        trs: false,
        onlyVisible: true,
        binary: false,
        maxTextureSize: 4096,
    };
    gltfExporter.parse(
        scene,
        function (result) {
            if (result instanceof ArrayBuffer) {
                saveArrayBuffer(result, 'scene.glb');
            } else {
                const output = JSON.stringify(result, null, 2);
                saveString(output, 'scene.gltf');
            }
        },
        function (error) {
            console.log('An error happened during parsing', error);
        },
        options
    );
    //
    scene.traverse(child => {
        if (child instanceof THREE.Mesh) {
            child.visible = false;
        }
    });
}

function save(blob, filename) {
    const link = document.createElement('a');
    link.style.display = 'none';
    document.body.appendChild(link); // Firefox workaround, see #6594
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    link.click();
    // URL.revokeObjectURL( url ); breaks Firefox...
}

function saveString(text, filename) {
    save(new Blob([text], {type: 'text/plain'}), filename);
}

function saveArrayBuffer(buffer, filename) {
    save(new Blob([buffer], {type: 'application/octet-stream'}), filename);
}


function loadScene3d(state, div1, guiDiv, file, scaleX, scaleY, centreX, centreY, elevation, opacity, reference) {
    console.log("State : ", state);
    //update widow size to server
    const sceneInfo = JSON.stringify([rendererWidth, rendererHeight, camera.position.x, camera.position.y, camera.position.z, camera.rotation.x, camera.rotation.y, camera.rotation.z]);
    dotNetObj.invokeMethodAsync("SaveSceneInfo", sceneInfo);
    
    //DotNet.invokeMethodAsync(projectName, "UpdateOnWindowResize", rendererWidth, rendererHeight);
    

    dotNetObj = reference;
    caller = reference;
    //function draw3d(file, scaleX, scaleY, centreX, centreY) {
    //var div = 'layoutPage3d'
    //var div = "layoutPage3d1"
    canvas = document.getElementById(div1);


    if (!canvas) {
        return;
    }

    if (!arrowScene) {
        //https://jsfiddle.net/b97zd1a3/16/
        //var CANVAS_WIDTH = 200;
        //var CANVAS_HEIGHT = 200;
        //var arrowRenderer = new THREE.WebGLRenderer({ alpha: true }); // clear
        //arrowRenderer.setClearColor(0x000000, 0);
        //arrowRenderer.setSize(CANVAS_WIDTH, CANVAS_HEIGHT);

        //var arrowCanvas = document.body.appendChild(arrowRenderer.domElement);
        //arrowCanvas.setAttribute('id', 'arrowCanvas');
        //arrowCanvas.style.width = CANVAS_WIDTH;
        //arrowCanvas.style.height = CANVAS_HEIGHT;

        //var arrowScene = new THREE.Scene();

        //var arrowCamera = new THREE.PerspectiveCamera(50, CANVAS_WIDTH / CANVAS_HEIGHT, 1, 1000);
        //arrowCamera.up = camera.up; // important!

        //var arrowPos = new THREE.Vector3(0, 0, 0);
        //arrowScene.add(new THREE.ArrowHelper(new THREE.Vector3(1, 0, 0), arrowPos, 60, 0x7F2020, 20, 10));
        //arrowScene.add(new THREE.ArrowHelper(new THREE.Vector3(0, 1, 0), arrowPos, 60, 0x207F20, 20, 10));
        //arrowScene.add(new THREE.ArrowHelper(new THREE.Vector3(0, 0, 1), arrowPos, 60, 0x20207F, 20, 10));
    }


    if (!scene) {
        //create the scene and other initialiasations
        console.log("No scene found XXXXXXXXXXXXXXXXXXXXXX");
        scene = new THREE.Scene();
        scene.background = new THREE.Color(0x000000);

        //renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer = new THREE.WebGLRenderer({antialias: true, preserveDrawingBuffer: true, premultipliedAlpha: false});
        renderer.setPixelRatio(window.devicePixelRatio);
        renderer.setSize(rendererWidth, rendererHeight);
        renderer.shadowMap.enabled = true; // Enable shadow mapping
        renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        canvas.appendChild(renderer.domElement);

        renderer.clippingPlanes = Empty; // GUI sets it to globalPlanes
        renderer.localClippingEnabled = false;


        //
        //https://threejs.org/examples/webgl_clipping.html
        //// GUI
        const gui = new GUI({autoPlace: false}),
            folderLocal = gui.addFolder('Local Clipping'),
            propsLocal = {
                get 'Enabled'() {
                    return renderer.localClippingEnabled;
                },
                set 'Enabled'(v) {
                    renderer.localClippingEnabled = v;
                },
                get 'Plane'() {
                    return localPlane.constant;
                },
                set 'Plane'(v) {
                    localPlane.constant = v;
                }
            },
            folderLocal1 = gui.addFolder('Z-axis Clipping'),
            propsLocal1 = {
                get 'Enabled'() {
                    return renderer.localClippingEnabled;
                },
                set 'Enabled'(v) {
                    renderer.localClippingEnabled = v;
                },
                get 'Plane'() {
                    return localPlane1.constant;
                },
                set 'Plane'(v) {
                    localPlane1.constant = v;
                }
            },
            folderY = gui.addFolder('Y-axis Clipping'),
            propsY = {
                get 'Enabled'() {
                    return renderer.clippingPlanes !== Empty;
                },
                set 'Enabled'(v) {
                    renderer.clippingPlanes = v ? globalPlanes1 : Empty;
                },
                get 'Plane'() {
                    return globalPlane1.constant;
                },
                set 'Plane'(v) {
                    globalPlane1.constant = v;
                }
            },
            folderX = gui.addFolder('X-axis Clipping'),
            propsX = {
                get 'Enabled'() {
                    return renderer.clippingPlanes !== Empty;
                },
                set 'Enabled'(v) {
                    renderer.clippingPlanes = v ? globalPlanes : Empty;
                },
                get 'Plane'() {
                    return globalPlane.constant;
                },
                set 'Plane'(v) {
                    globalPlane.constant = v;
                }
            };
        var customContainer = document.getElementById(div1);
        //customContainer.appendChild(gui.domElement);
        gui.domElement.id = 'gui';
        var guiContainer = document.getElementById(guiDiv);
        //gui_container.appendChild(gui.domElement);
        guiContainer.appendChild(gui.domElement);

        // Start

        //startTime = Date.now();
        folderLocal.open()
        folderLocal.add(propsLocal, 'Enabled');
        folderLocal.add(propsLocal, 'Plane', -50, 500);
        folderLocal1.open()
        folderLocal1.add(propsLocal1, 'Enabled');
        folderLocal1.add(propsLocal1, 'Plane', -5, 50);
        folderX.open()
        folderX.add(propsX, 'Enabled');
        folderX.add(propsX, 'Plane', -50, 500);
        folderY.open()
        folderY.add(propsY, 'Enabled');
        folderY.add(propsY, 'Plane', -50, 300);


        var frameWidth = canvas.clientWidth;
        var frameHeight = canvas.clientHeight;

        //camera
        const fov = 20;
        var aspectRatio = rendererWidth / rendererHeight;
        const frustumSize = 1000;
        const near = 0.001;
        const far = 4000;
        cameraPerspective = new THREE.PerspectiveCamera(fov, aspectRatio, near, far);
        //camera = new THREE.OrthographicCamera( frustumSize * aspect / - 2, frustumSize * aspect / 2, frustumSize / 2, frustumSize / - 2, 1, 1000 );
        cameraPerspective.position.set(0, 0, 400);
        //camera.up = new THREE.Vector3(0,0,1);
        cameraPerspective.lookAt(scene.position);
        cameraPerspective.add(new THREE.PointLight(0xffffff, 1));

        camera = cameraPerspective;
        scene.add(camera);

        orthoCamera = new THREE.OrthographicCamera(rendererWidth / -2, rendererWidth / 2, rendererHeight / 2, rendererHeight / -2, 1, 1000);
        orthoCamera.add(new THREE.PointLight(0xffffff, 1));
        // helper
        //var arrowHelper = new THREE.ArrowHelper( new THREE.Vector3( 1, 0, 0 ), new THREE.Vector3( -100, 0, 0 ), 400, 0xff0000 );
        var axis = new THREE.AxesHelper(300);
        scene.add(axis);





        createWheelStopListener(renderer.domElement, function () {
            doAfterEndScrol();
        });


        raycaster = new THREE.Raycaster();


        //-----------------------

        var segments = [];
        var maxCount = 1000;
        var count = 0;
        var thisgroup, segType = "LV", opacity = 0.8;
        var w = .1, t = 0.02, deg = 0;
        var Option = 'JS';
        Option = 'CS';
        var t0 = performance.now();


        //window.getElementById("randomNumberSpan").innerText = count;	

        //var geometry = new THREE.PlaneBufferGeometry(30, 30);
        //var plane = new THREE.Mesh(geometry, new THREE.MeshPhongMaterial({ side: THREE.DoubleSide }));
        //scene.add(plane);

        //light
        scene.add(new THREE.AmbientLight(0xffffff, 0.9));
        scene.add(new THREE.HemisphereLight(0xffffff, 0x080820, 1));
        const pointLight = new THREE.PointLight(0xffffff, 1, 100);
        pointLight.position.set(rendererWidth / 2, rendererHeight / 2, 100);
        scene.add(pointLight);


        //   renderer = new THREE.WebGLRenderer({ antialias: true });
        //   renderer.setPixelRatio(window.devicePixelRatio);
        //renderer.setSize(frameWidth, frameHeight);

        while (canvas.lastElementChild) {
            canvas.removeChild(canvas.lastElementChild);
        }

        canvas.appendChild(renderer.domElement);

        controls = new OrbitControls(camera, renderer.domElement);
        controls.screenSpacePanning = true;
        controls.minDistance = 5;
        controls.maxDistance = 10000;
        //controls.target.set(1, 0, 0);
        //controls.maxPolarAngle = Math.PI;
        //controls.enableDamping = true;
        //controls.dampingFactor = 0.05;
        //controls.autoRotate = true;
        //controls.listenToKeyEvents(window); // optional
        //controls.addEventListener('change', render);
        controls.update();


        // draw mouse position lines (visible while ctrl key is pressed)
        var linePosition = new THREE.Vector3(10, 10, .1);
        if (linex == undefined) {
            // create new position line parallel to x-axis
            var pointsx = [];
            pointsx.push(new THREE.Vector3(linePosition.x + 100, linePosition.y, linePosition.z));
            pointsx.push(new THREE.Vector3(linePosition.x - 100, linePosition.y, linePosition.z));
            const geometry2 = new THREE.BufferGeometry().setFromPoints(pointsx);
            const material2 = new THREE.LineBasicMaterial({color: 0xff0000});
            linex = new THREE.Line(geometry2, material2);
            linex.name = 'positionlinex';
            scene.add(linex);
            linex.visible = false;
        }
        if (liney == undefined) {
            // create new position line parallel to y-axis
            var pointsy = [];
            pointsy.push(new THREE.Vector3(linePosition.x, linePosition.y + 100, linePosition.z));
            pointsy.push(new THREE.Vector3(linePosition.x, linePosition.y - 100, linePosition.z));
            const geometry1 = new THREE.BufferGeometry().setFromPoints(pointsy);
            const material1 = new THREE.LineBasicMaterial({color: 0x0000ff});
            const liney = new THREE.Line(geometry1, material1);
            liney.name = 'positionliney';
            scene.add(liney);
            liney.visible = false;
        }

    }

    if (file !== null) {
        loadPlotPlan(file, scaleX, scaleY, centreX, centreY, elevation, opacity);
    }

    animate();


}




function fitCameraToObject(camera, object, offset) {

    offset = offset || 1.5;

    const boundingBox = new THREE.Box3();

    boundingBox.setFromObject(object);

    const center = boundingBox.getCenter(new THREE.Vector3());
    const size = boundingBox.getSize(new THREE.Vector3());

    const startDistance = center.distanceTo(camera.position);
    // here we must check if the screen is horizontal or vertical, because camera.fov is
    // based on the vertical direction.
    const endDistance = camera.aspect > 1 ?
        ((size.y / 2) + offset) / Math.abs(Math.tan(camera.fov / 2)) :
        ((size.y / 2) + offset) / Math.abs(Math.tan(camera.fov / 2)) / camera.aspect;


    camera.position.set(
        camera.position.x * endDistance / startDistance,
        camera.position.y * endDistance / startDistance,
        camera.position.z * endDistance / startDistance,
    );
    camera.lookAt(center);

}




function resizeRendererToDisplaySize(renderer) {
    const canvas = renderer.domElement;
    const width = canvas.clientWidth;
    const height = canvas.clientHeight;
    const needResize = canvas.width !== width || canvas.height !== height;
    if (needResize) {
        renderer.setSize(width, height, false);
    }
    return needResize;
}


function intersectPoint(planeTag) {
    try {
        raycaster.setFromCamera(mouse, camera);
        var intersects = raycaster.intersectObjects(scene.children);
        if (intersects.length !== 0){
            intersects = intersects.filter(item => item.object.Tag !== undefined && item.object.Tag===planeTag);
        }
        if (intersects.length !== 0){            
            return new THREE.Vector3(
                Math.round(intersects[0].point.x * 1000) / 1000, 
                Math.round(intersects[0].point.y * 1000) / 1000, 
                Math.round(intersects[0].point.z * 1000) / 1000);
        }else{
            return new THREE.Vector3(mouse.x, mouse.y,0);
        }
    } catch (e) {
        console.log(e);
    }
    return new THREE.Vector3();
}

function topPlotPlanIntersectPosition(event) {
    try {
        raycaster.setFromCamera(mouse, camera);
        var intersects = raycaster.intersectObjects(scene.children);
        if (intersects.length != 0) {
            // filter intersect of plotplans
            intersects = intersects.filter(item => item.object.Tag != undefined && item.object.Tag.includes("plotplan"));
            // if intersect is plot plan choose the next non plotplan item
            if (intersects.length > 0) {
                //console.log(intersects[0].point.x, intersects[0].point.y, intersects[0].point.z);
                return new THREE.Vector3(Math.round(intersects[0].point.x * 1000) / 1000, Math.round(intersects[0].point.y * 1000) / 1000, Math.round(intersects[0].point.z * 1000) / 1000);
            }
        }
    } catch (e) {
        console.log(e);
    }
    return new THREE.Vector3();
}


function topObjectIntersectPosition(event) {
    try {
        raycaster.setFromCamera(mouse, camera);
        var intersects = raycaster.intersectObjects(scene.children);
        if (intersects.length != 0) {
            if (intersects.length > 0) {
                return new THREE.Vector3(Math.round(intersects[0].point.x * 1000) / 1000, Math.round(intersects[0].point.y * 1000) / 1000, Math.round(intersects[0].point.z * 1000) / 1000);
            }
        }
    } catch (e) {
        console.log(e);
    }

}


function loadPlotPlanNotUsed(userImageFile, scaleX, scaleY, centreX, centreY, elevation, opacity) {
    //online pdf to png converter: https://pdf2png.com
    //online image editor: https://www.online-image-editor.com
    // remove if there is any existing plot
    //

    if (userImageFile === undefined) {
        alert("Select the plot plan background image.");
        light
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
    var planeGeometry = new THREE.PlaneGeometry(rendererWidth, rendererHeight);
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
    return false;
}

function getObjectByNameArray(parentObject, childObjectName) {
    var match = [];
    parentObject.traverse(function (child) {
        if (child.name === childObjectName) {
            match.push(child);
        }
    });
    return match;
}

function scalePlotPlanNotUsed(scaleX, scaleY) {
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

function clearRefPoints() {
    while (scene.getObjectByName('refPoint')) {
        scene.remove(scene.getObjectByName('refPoint'));
    }
    renderer.render(scene, camera);
    refPoints = [];
    dotNetObj.invokeMethodAsync("UpdateRefPoints","[]");
}







function showHideShadowRingNPosinLines() {
    if (scene.getObjectByName('shadowRing') && scene.getObjectByName('positionlinex') && scene.getObjectByName('positionliney')) {
        if (ctrlKeyPressed) {
            scene.getObjectByName('shadowRing').visible = true;
            scene.getObjectByName('positionlinex').visible = true;
            scene.getObjectByName('positionliney').visible = true;
        } else {
            scene.getObjectByName('shadowRing').visible = false;
            scene.getObjectByName('positionlinex').visible = false;
            scene.getObjectByName('positionliney').visible = false;
        }
        render();
    }
}

function shadowRing(x, y, z) {

    var shadowRingMaterial = new THREE.MeshPhongMaterial({color: 0xc3c383, side: THREE.DoubleSide});
    var shadowRing = new THREE.Mesh(new THREE.RingGeometry(0.4, 0.5, 20), shadowRingMaterial);

    //shadowRing.name = str == "" ? 'shadowRing' : str;
    shadowRing.name = 'shadowRing';
    shadowRing.position.set(y, x, z + 0.01);
    //scene.getObjectByName('plotplan').add(shadowRing)
    scene.add(shadowRing);
    render();
}

function hideObject() {
    selectedObject.forEach(el => {
        el.visible = false;
    });
    render();
}

function resetObject() {
    selectedObject.forEach(el => {
        el.visible = true;
        el.material.color = el.OriginalColor;
    });
    //clear selectedObject array
    selectedObject.length = 0;
    render();
}


function commonAction(event) {
    if (window.event.shiftKey && (window.event.key == 'h' || window.event.key == 'H')) {
        hideObject();
    }
    if (window.event.key === "Escape") {
        resetObject();
    }
}

var mouseWheelStatus = false;
var hideObjectTypesOnScrolling = ["rawladder", "ladder", "bend", "tee", "cross", "node", "sleeve", "equipment"];
var raycastObjectTypes = ["rawladder", "ladder", "bend", "tee", "cross", "node", "sleeve", "board", "cable"];

function mouseWheel() {
    if (!mouseWheelStatus) {
        scene.traverse(child => {
            if (child instanceof THREE.Mesh) {
                if (hideObjectTypesOnScrolling.includes(child.type)) {
                    child.visible = false;
                }
            }
        });
        //console.log("mouseWheel");
        mouseWheelStatus = true;
    }
}

function createWheelStopListener(element, callback, timeout) {
    var handle = null;
    var onScroll = function () {
        if (handle) {
            clearTimeout(handle);
        }
        handle = setTimeout(callback, timeout || 200); // default 200 ms
    };
    element.addEventListener('wheel', onScroll);
    return function () {
        element.removeEventListener('wheel', onScroll);
    };
}

function doAfterEndScrol() {
    if (mouseWheelStatus) {
        scene.traverse(child => {
            if (child instanceof THREE.Mesh) {
                if (hideObjectTypesOnScrolling.includes(child.type)) {
                    child.visible = true;
                }
            }
        });
        //console.log("scroll end");
        mouseWheelStatus = false;
    }
}



//let isClicked = false;
//function mouseClick() {
//    console.log('click');
//    isClicked = true;
//}
//function mouseMove(e) {
//    if (isClicked) {
//        console.log('clicked and draged')
//    } else {
//        console.log('drag');
//        isClicked = false;
//    }
//}


function raycastNotUsed(event) {
    // clicked event
    [mouse, pos] = findCoordinate(event);
    var linePosition = topPlotPlanIntersectPosition(event);
    var objectPosition = topObjectIntersectPosition(event);
    if (linePosition == undefined) {
        linePosition = new THREE.Vector3();
    }
    console.log("MouseClick2", pos.x, pos.y, pos.z, event.clientX, event.clientY,
        event.pageX, event.pageY, event.offsetX, event.offsetY, event.layerX, event.layerY,
        event.x, event.y, mouse.x, mouse.y, linePosition.x, linePosition.y, linePosition.z);
    //DotNet.invokeMethodAsync(projectName, "UpdateLayout3DMousePosition", pos.x, pos.y, pos.z, event.clientX, event.clientY, event.pageX, event.pageY, event.offsetX, event.offsetY, event.layerX, event.layerY, event.x, event.y, mouse.x, mouse.y, linePosition.x, linePosition.y, linePosition.z);
    dotNetObj.invokeMethodAsync("MouseClick2", pos.x, pos.y, pos.z, event.clientX, event.clientY,
        event.pageX, event.pageY, event.offsetX, event.offsetY, event.layerX, event.layerY,
        event.x, event.y, mouse.x, mouse.y, linePosition.x, linePosition.y, linePosition.z);

    if (!event.ctrlKey && !event.shiftKey) {
        castObject(event)
    }
    //
    try {
        if (event.ctrlKey && !event.shiftKey) { // if ctrl key is not pressed, the raycast selects a point

            drawRefPoint(planeName);
            console.log();

            console.log("Mouse posion : "
                + "Mouse: [" + mouse.x + ", " + mouse.y + "] "
                + "Position:[" + pos.x + "," + pos.y + "] "
                + "Client: [" + event.clientX + "," + event.clientY + "] "
                + "Screen:[" + event.screenX + "," + event.screenY + "] "
                + "Page:[" + event.pageX + "," + event.pageY + "] "
                + "Offset:[" + event.offsetX + "," + event.offsetY + "] "
                + "Layer:[" + event.layerX + "," + event.layerY + "] "
                + "EvenXY:[" + event.x + "," + event.y + "] "
                + "LinePosition :[" + linePosition.x, linePosition.y, linePosition.z + "] ");

            pos.z = plotElevtn + 0.01; // plotplan elevation as default clicked coordinate elevation, .01 to make visible
            //
            var groupredRing = new THREE.Group();
            var redRingMaterial = new THREE.MeshBasicMaterial({color: 0xff0000, side: THREE.DoubleSide});
            var yellowRingMaterial = new THREE.MeshBasicMaterial({color: 0xffff00, side: THREE.DoubleSide});
            var redCircleGeometry = new THREE.CircleGeometry(0.1, 10);
            var redCircle = new THREE.Mesh(redCircleGeometry, redRingMaterial);
            groupredRing.add(redCircle);
            var redRingGeometryIn = new THREE.RingGeometry(0.1, 0.3, 20);
            var redRingMeshIn = new THREE.Mesh(redRingGeometryIn, yellowRingMaterial);
            groupredRing.add(redRingMeshIn);
            var redRingGeometryOut = new THREE.RingGeometry(0.3, 0.35, 20);
            var redRingMeshOut = new THREE.Mesh(redRingGeometryOut, redRingMaterial);
            groupredRing.add(redRingMeshOut);
            //groupredRing.scale(0.5, 0.5, 0.5);
            clickedPointSeq.push("clickedPoint" + clickedPointSeq.length);
            groupredRing.name = clickedPointSeq[clickedPointSeq.length - 1];
            groupredRing.name = 'clickedPoints';
            groupredRing.position.set(linePosition.x, linePosition.y, linePosition.z);
            //clickedPointsGroup.add(groupredRing);
            scene.add(groupredRing);
            // storing clicked coordinates for adding segment
            clickCoordinate.push([linePosition.x, linePosition.y, linePosition.z]);
            //DotNet.invokeMethodAsync(projectName, "UpdateLayout3DScale", pos.x, pos.y, event.clientX, event.clientY, event.pageX, event.pageY, event.offsetX, event.offsetY, event.layerX, event.layerY, event.x, event.y, mouse.x, mouse.y, linePosition.x, linePosition.y, linePosition.z);
            dotNetObj.invokeMethodAsync("MouseClick", pos.x, pos.y, pos.z, event.clientX, event.clientY,
                event.pageX, event.pageY, event.offsetX, event.offsetY, event.layerX, event.layerY,
                event.x, event.y, mouse.x, mouse.y, linePosition.x, linePosition.y, linePosition.z);
            render();
        }
    } catch (e) {
        console.log(e)
    }
    //

}

//

function findCoordinate(event) {
    // calculate mouse position in normalized device coordinates (-1 to +1) for both components
    var rect = event.target.getBoundingClientRect();
    var mouse = new THREE.Vector2();

    var x = event.clientX - rect.left; //x position within the element.
    var y = event.clientY - rect.top;  //y position within the element.
    mouse.x = (x / rendererWidth) * 2 - 1;
    mouse.y = -(y / rendererHeight) * 2 + 1;
    //
    var vec = new THREE.Vector3(); // create once and reuse
    var pos = new THREE.Vector3(); // create once and reuse
    //
    vec.set(
        ((event.clientX - rect.left) / rendererWidth) * 2 - 1,
        -((event.clientY - rect.top) / rendererHeight) * 2 + 1,
        0);
    vec.unproject(camera);
    vec.sub(camera.position).normalize();
    var distance = -camera.position.z / vec.z;
    pos.copy(camera.position).add(vec.multiplyScalar(distance));
    // rounding to two decimal
    pos.x = Math.round(pos.x * 100) / 100;
    pos.y = Math.round(pos.y * 100) / 100;
    return [mouse, pos];
}


window.invokeDotnetStaticFunction = window.invokeDotnetStaticFunction || function () {
    DotNet.invokeMethodAsync('BlazorJSDemoUI', 'CalculateLadderPoints')
        .then(data => {
            return data;
        });
}


window.giveMerandomInt = window.giveMerandomInt || function (n) {
    DotNet.invokeMethodAsync(projectName, 'GenerateRandomInt', n)
        .then(result => {
            document.getElementById('randomNumberSpan').innerText = result;
        });
}


//


// window.drawLadder = function (tag, type, jsonPoints, opacity, color) {
//     let start = Date.now();
//     if (!canvas) {
//         return;
//     }
//     try {
//         var clr = JSON.parse(color);
//         var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
//         var points = JSON.parse(jsonPoints);
//         var vertices1 = [];
//         var pushorder = [0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5, 2, 3, 7, 2, 7, 6];
//         pushorder.forEach(i => {
//             vertices1.push(new THREE.Vector3(points[i].X, points[i].Y, points[i].Z));
//         });
//         var pointsGeometry = new THREE.BufferGeometry().setFromPoints(vertices1, 3);
//
//         //for wireframe
//         const wireframe = new THREE.WireframeGeometry(pointsGeometry);
//         const line = new THREE.LineSegments(wireframe);
//         line.material.depthTest = false;
//         line.material.opacity = 0.5;
//         line.material.transparent = true;
//         var wireFrameColor = new THREE.Color(0, 0, 1);
//         line.material.color = setColor;
//         //scene.add(line);
//
//         //for solid display
//         var material = new THREE.MeshPhongMaterial({
//             color: setColor,
//             transparent: true,
//             opacity: opacity,
//             side: THREE.DoubleSide,
//             flatShading: true,
//             clippingPlanes: [localPlane],
//             clipShadows: true
//         });
//         laddermaterial.color = setColor;
//         var ladderMesh = new THREE.Mesh(pointsGeometry, material);
//         ladderMesh.type = type; // "ladder" or "rawladder"
//         //mesh.material.color.set(setColor);
//         //mesh.material.opacity = opacity;
//         //var cx = (points[1].X + points[6].X) / 2;
//         //var cy = (points[1].Y + points[6].Y) / 2;
//         //var cz = (points[1].Z + points[6].Z) / 2;
//         ladderMesh.Tag = tag;
//         ladderMesh.Clicked = false;
//         ladderMesh.OriginalColor = ladderMesh.material.color;
//         //let box = new THREE.Box3().setFromObject(ladderMesh);
//         //let sphere = box.getBoundingSphere();
//         //let centerPoint = box.getCenter();
//         //let centerPoint = sphere.center;
//         //ladderMesh.updateMatrixWorld();
//         //ladderMesh.position.set(cx, cy, cz);
//         var mesh = ladderMesh;
//         var center = new THREE.Vector3();
//         mesh.geometry.computeBoundingBox();
//         mesh.geometry.boundingBox.getCenter(center);
//         mesh.geometry.center();
//         mesh.position.copy(center);
//         //doAddScene(ladderMesh, animationText, progress, start, drawLadderCallback);
//         scene.add(ladderMesh);
//         //render();
//         pointsGeometry.dispose();
//     } catch (e) {
//         console.log(tag + e);
//     }
//     //
//     //if (animationText != "") {
//     //    var progress = parseInt(animationText);
//     //    DotNet.invokeMethodAsync('WslEncompass', "SegmentPageUpdateLadderProgress", progress);
//     //}
//     //DotNet.invokeMethodAsync(projectName, "ItemDrawn", tag);
//     return tag;
// }
let requestID;

function doAddScene(meshObject, animationText, progress, start, callback) {
    scene.add(meshObject);
    let timeTaken = Date.now() - start;
    console.log("Total time taken for " + meshObject.Tag + " : " + timeTaken + " milliseconds");
    callback(animationText, progress);
}

//function drawLadderCallback(animationText, progress) {

//    if (animationText == "first") {
//        cancelAnimationFrame(requestID);
//        console.log("cancelleing animation");
//    } else if (animationText == "last") {
//        animate();
//        console.log("reverting animation");
//    } else if (animationText == "progress") {
//        DotNet.invokeMethodAsync('WslEncompass', 'UpdateProgressLayoutComponent', progress);
//        console.log("show progress animation : " + progress);
//    };
//}

window.drawBendNotUsed = function (tag, jsonFacePoints, opacity, color) {
    if (!canvas) {
        return;
    }
    var clr = JSON.parse(color);
    var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
    var faces = JSON.parse(jsonFacePoints);
    var vertices1 = [];
    faces.forEach(face => {
        face.forEach(pt => {
            vertices1.push(new THREE.Vector3(pt.X, pt.Y, pt.Z));
        });
    });
    try {
        var pointsGeometry = new THREE.BufferGeometry().setFromPoints(vertices1, 3);
        pointsGeometry.computeBoundingSphere();
        if (isNaN(pointsGeometry.boundingSphere.radius)) {
            geometry.boundingSphere.radius = 1; // Set a default value or any suitable value
        }
        var material = new THREE.MeshPhongMaterial({
            color: setColor,
            transparent: true,
            opacity: opacity,
            side: THREE.DoubleSide,
            flatShading: true,
            clippingPlanes: [localPlane],
            clipShadows: true
        });

        var bendMesh = new THREE.Mesh(pointsGeometry, material);

        bendMesh.Tag = tag;
        bendMesh.Clicked = false;
        bendMesh.OriginalColor = bendMesh.material.color;
        bendMesh.Type = "bend";

        var mesh = bendMesh;
        var center = new THREE.Vector3();
        mesh.geometry.computeBoundingBox();
        mesh.geometry.boundingBox.getCenter(center);
        mesh.geometry.center();
        mesh.position.copy(center);
        scene.add(bendMesh);
        pointsGeometry.dispose();
    } catch (e) {
        console.log(tag + e);
    }
    //
    return tag;
}

window.drawTeeNotUsed = function (tag, jsonFacePoints, opacity, color) {
    if (!canvas) {
        return;
    }
    var clr = JSON.parse(color);
    var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
    var faces = JSON.parse(jsonFacePoints);
    var vertices1 = [];
    faces.forEach(face => {
        face.forEach(pt => {
            vertices1.push(new THREE.Vector3(pt.X, pt.Y, pt.Z));
        });
    });
    try {
        var pointsGeometry = new THREE.BufferGeometry().setFromPoints(vertices1, 3);
        var material = new THREE.MeshPhongMaterial({
            color: setColor,
            transparent: true,
            opacity: opacity,
            side: THREE.DoubleSide,
            flatShading: true,
            clippingPlanes: [localPlane],
            clipShadows: true
        });

        var teeMesh = new THREE.Mesh(pointsGeometry, material);

        teeMesh.Tag = tag;
        teeMesh.Clicked = false;
        teeMesh.OriginalColor = teeMesh.material.color;
        teeMesh.Type = "tee";

        var mesh = teeMesh;
        var center = new THREE.Vector3();
        mesh.geometry.computeBoundingBox();
        mesh.geometry.boundingBox.getCenter(center);
        mesh.geometry.center();
        mesh.position.copy(center);


        scene.add(teeMesh);
        pointsGeometry.dispose();
    } catch (e) {
        console.log(tag + e);
    }
    //
    //DotNet.invokeMethodAsync(projectName, "ItemDrawn", tag);
    return tag;
}

window.drawCrossNotUsed = function (tag, jsonFacePoints, opacity, color) {
    if (!canvas) {
        return;
    }
    var clr = JSON.parse(color);
    var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
    var faces = JSON.parse(jsonFacePoints);
    var vertices1 = [];
    faces.forEach(face => {
        face.forEach(pt => {
            vertices1.push(new THREE.Vector3(pt.X, pt.Y, pt.Z));
        });
    });
    try {
        var pointsGeometry = new THREE.BufferGeometry().setFromPoints(vertices1, 3);
        var material = new THREE.MeshPhongMaterial({
            color: setColor,
            transparent: true,
            opacity: opacity,
            side: THREE.DoubleSide,
            flatShading: true,
            clippingPlanes: [localPlane],
            clipShadows: true
        });
        var crossMesh = new THREE.Mesh(pointsGeometry, material);

        crossMesh.Tag = tag;
        crossMesh.Clicked = false;
        crossMesh.OriginalColor = crossMesh.material.color;
        crossMesh.Type = "cross";

        var mesh = crossMesh;
        var center = new THREE.Vector3();
        mesh.geometry.computeBoundingBox();
        mesh.geometry.boundingBox.getCenter(center);
        mesh.geometry.center();
        mesh.position.copy(center);

        scene.add(crossMesh);
        pointsGeometry.dispose();
    } catch (e) {
        console.log(tag + e);
    }
    //
    return tag;
}


window.drawSleeveNotUsed = function (tag, radious, jsonPoints, opacity, color) {
    opacity = 1;
    var clr = JSON.parse(color);
    var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
    var rawPoints = JSON.parse(jsonPoints);
    //const segment = 20;
    //const radialSegments = 8;
    const material = new THREE.MeshBasicMaterial({color: 0x00ff00});
    var points = [];
    rawPoints.forEach(p => {
        points.push(new THREE.Vector3(p.X, p.Y, p.Z));
    });
    try {
        var curve = new THREE.CatmullRomCurve3(points);
        var tubeGeometry = new THREE.TubeGeometry(curve, 20, radious, 8, false);
        var tube = new THREE.Mesh(tubeGeometry, material);
        tube.Type = "sleeve";
        tube.Tag = tag;
        tube.Clicked = false;
        tube.OriginalColor = tube.material.color;
        //tube.name = "CB" + icable;
        //cableData[icable][11] = cableData[icable][11] + curve.getLength();//cable total length
        //cableLaid.add( tube );
        scene.add(tube);
    } catch (e) {
        console.log(tag + e);
    }
    var pp = curve.getPoints(20);
    var pps = [];
    pp.forEach(p => {
        pps.push([p.x, p.y, p.z]);
    });
    return [tag, pps.toString()];
}


function drawCable(tag, jsonPointSetSegments, tubeTubularSegments, dia, tubeRadialSegments, opacity, color, option, percent) {
    if (!canvas) {
        return;
    }
    var clr = JSON.parse(color);
    var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
    var pointSetSegments = JSON.parse(jsonPointSetSegments);
    var material = new THREE.MeshPhongMaterial({
        color: setColor,
        transparent: true,
        opacity: opacity,
        side: THREE.DoubleSide,
        flatShading: true,
        clippingPlanes: [localPlane],
        clipShadows: true
    });
    for (let i = 0; i < pointSetSegments.length; i = i + 4) {
        var ptFrom = new THREE.Vector3(pointSetSegments[i].X, pointSetSegments[i].Y, pointSetSegments[i].Z);
        var v1 = new THREE.Vector3(pointSetSegments[i + 1].X, pointSetSegments[i + 1].Y, pointSetSegments[i + 1].Z);
        var v2 = new THREE.Vector3(pointSetSegments[i + 2].X, pointSetSegments[i + 2].Y, pointSetSegments[i + 2].Z);
        var ptTo = new THREE.Vector3(pointSetSegments[i + 3].X, pointSetSegments[i + 3].Y, pointSetSegments[i + 3].Z);
        if (v1.x == 0 & v1.y == 0 && v1.z == 0 && v2.x == 0 & v2.y == 0 && v2.z == 0 || option != "curve") {
            // case straight section
            var curve = new THREE.LineCurve(ptFrom, ptTo);
        } else {
            //case bend
            var curve = new THREE.CubicBezierCurve3(ptFrom, v1, v2, ptTo);
        }
        var tubeGeometry = new THREE.TubeGeometry(curve, tubeTubularSegments, dia / 2, tubeRadialSegments, false);
        var tube = new THREE.Mesh(tubeGeometry, material);
        //const wireframe = new THREE.WireframeGeometry(tubeGeometry);
        //const tube = new THREE.LineSegments(wireframe)
        tube.Tag = tag;
        tube.Type = "cable";
        scene.add(tube);
        tubeGeometry.dispose();
        if (percent == 1) {
            tube.onAfterRender = function () {
                console.log("last cable drawn");
            }
        }
    }


    // return curve.getLength();//cable total length
    //
}

window.drawCableFromRoute = function (tag, jsonPoints, tubeTubularSegments, dia, tubeRadialSegments, opacity, color) {
    if (!canvas) {
        return;
    }
    var clr = JSON.parse(color);
    var pts = JSON.parse(jsonPoints);
    if (pts.length == 0) {
        return;
    }
    var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
    var material = new THREE.MeshPhongMaterial({
        color: setColor,
        transparent: true,
        opacity: opacity,
        side: THREE.DoubleSide,
        flatShading: true,
        clippingPlanes: [localPlane],
        clipShadows: true
    });



    try {


        // Create an array of points
        var points = [];
        pts.forEach(pt => points.push(new THREE.Vector3(pt.X, pt.Y, pt.Z)));

        // Create a Catmull-Rom curve
        //var spline = new THREE.Curve3(points);

        // Define the radius of the tube
        var radius = dia / 2;

        // Define the number of radial segments
        var radialSegments = tubeTubularSegments;

        //// Define the number of tubular segments
        //var tubularSegments = tubeTubularSegments;

        //// Set closed to true if you want the tube to be closed
        //var closed = false;

        //// Create the tube geometry
        //var geometry = new THREE.TubeGeometry(spline, points.length, radius, radialSegments, closed);

        //// Create a material
        ////var material = new THREE.MeshBasicMaterial({ color: 0xff0000 });

        //// Create the tube mesh
        //var tubeMesh = new THREE.Mesh(geometry, material);

        //// Add the tube to the scene
        //tubeMesh.Tag = tag;
        //tubeMesh.type = "cable";
        //scene.add(tubeMesh);
        //geometry.dispose();


        // Create an array to hold vertices and faces
        var vertices = [];
        var indices = [];

        // Iterate over each pair of consecutive points
        for (var i = 0; i < points.length - 1; i++) {
            var start = points[i];
            var end = points[i + 1];

            // Compute the direction vector between the points
            var direction = new THREE.Vector3().subVectors(end, start).normalize();

            // Compute the tangent vector (perpendicular to the direction)
            var tangent = new THREE.Vector3().crossVectors(direction, new THREE.Vector3(0, 0, 1)).normalize();

            // Compute the binormal vector (perpendicular to both direction and tangent)
            var binormal = new THREE.Vector3().crossVectors(direction, tangent).normalize();

            // Compute vertices for the pipe section
            var theta = Math.PI * 2 / radialSegments;
            for (var j = 0; j <= radialSegments; j++) {
                var segment = tangent.clone().multiplyScalar(Math.cos(theta * j) * radius)
                    .add(binormal.clone().multiplyScalar(Math.sin(theta * j) * radius));
                vertices.push(start.clone().add(segment));
            }
        }

        // Generate indices for faces
        for (var i = 0; i < points.length - 1; i++) {
            for (var j = 0; j < radialSegments; j++) {
                var v0 = i * (radialSegments + 1) + j;
                var v1 = v0 + 1;
                var v2 = (i + 1) * (radialSegments + 1) + j;
                var v3 = v2 + 1;
                indices.push(v0, v1, v2);
                indices.push(v1, v3, v2);
            }
        }

        // Create a geometry
        var geometry = new THREE.BufferGeometry();
        geometry.setFromPoints(vertices);
        geometry.setIndex(indices);

        // Create a material
        var material = new THREE.MeshBasicMaterial({color: 0xff0000});

        // Create the pipe mesh
        var pipeMesh = new THREE.Mesh(geometry, material);
        pipeMesh.Tag = tag;
        pipeMesh.Type = "cable";
        // Add the pipe to the scene
        scene.add(pipeMesh);


    } catch (e) {
        console.log(tag + e);
    }


    //
    return tag;
}

window.drawNodeNotUsed = function (tag, jsonPoint, color) {
    if (!canvas) {
        return;
    }
    var clr = JSON.parse(color);
    var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
    var nodePoint = JSON.parse(jsonPoint);
    const geometry = new THREE.SphereGeometry(0.03, 5, 5); // radious, widthsegment, heightsegments
    var material = new THREE.MeshPhongMaterial({
        color: setColor,
        transparent: true,
        opacity: 0.5,
        side: THREE.DoubleSide,
        flatShading: true,
        clippingPlanes: [localPlane],
        clipShadows: true
    });
    const sphere = new THREE.Mesh(geometry, material);
    sphere.position.set(nodePoint.X, nodePoint.Y, nodePoint.Z);
    //sphere.translate(new THREE.Vector3(nodePoint.X, nodePoint.Y, nodePoint.Z));
    sphere.Type = "node";
    scene.add(sphere);
    sphere.Tag = tag;
    sphere.Clicked = false;
    sphere.OriginalColor = sphere.material.color;
    geometry.dispose();
    //
    return tag;
}


function equipmentNotUsed(tag, x, y, z, w, d, h, a, color, opacity, colortext) {
    var clr = JSON.parse(color);
    var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
    var clrT = JSON.parse(colortext);
    var textColor = new THREE.Color(clrT[0] / 255, clrT[1] / 255, clrT[2] / 255);
    var geometry = new THREE.BoxGeometry(w, d, h);  // l along x-axis, w along y axis, d along z axis
    var material = new THREE.MeshPhongMaterial({
        color: setColor,
        transparent: true,
        opacity: opacity,
        side: THREE.DoubleSide,
        flatShading: true,
        clippingPlanes: [localPlane],
        clipShadows: true
    });
    var cube = new THREE.Mesh(geometry, material);
    //
    // Point P -> x1, y1, z1
    // Point Q -> x2, y2, z2
    // Vector  PQ' is the projection of vector PQ on x-y plane
    // vector along the width PW = cross of Z axis vector and PQ'
    // vector along depth PD = PW cross PQ'
    //
    //var tempv1 = new THREE.Vector3(0, 0, 0);
    //var tempv2 = new THREE.Vector3(0, 0, 0);
    //var tempv3 = new THREE.Vector3(0, 0, 0);
    //var tempv4 = new THREE.Vector3(0, 0, 0);
    //tempv1.set(x2 - x1, y2 - y1, z2 - z1); // pQ
    //tempv2.set(x2 - x1, y2 - y1, 0); //PQ'
    //tempv3.set(0, 0, 1); // Z axis
    //tempv4 = tempv2.clone().cross(tempv3); // PW
    //cube.rotateOnAxis(tempv3, Math.atan((y2 - y1) / (x2 - x1)));  // rotate cube at x-y plane , own axis and world axis is same
    //cube.rotateOnWorldAxis(tempv4.normalize(), tempv1.angleTo(tempv2));  // rotate along width world axis
    //cube.position.x = (x1 + x2) / 2; cube.position.y = (y1 + y2) / 2; cube.position.z = (z1 + z2) / 2; // position
    //const fontSize = 0.8 * Math.min(1, Math.max(w, d) / tag.length);

    var fontSize = 0.3 * Math.min(w, d, 1);
    if (tag.length * fontSize > Math.max(w, d)) {
        fontSize = Math.max(w, d) / tag.length;
    }
    const fontHeight = fontSize / 5; // Thickness to extrude text
    //if (fontHeight > 0.3 * Math.min(w, d, 1)) { fontHeight = 0.3 * Math.min(w, d, 1); fontSize = 4 * fontHeight; }
    //if (fontHeight > 0.3 * Math.min(w, d, 1)) { fontHeight = 0.3 * Math.min(w, d, 1); fontSize = 4 * fontHeight; }
    loader.load('https://unpkg.com/three@0.77.0/examples/fonts/helvetiker_regular.typeface.json', (font) => {

        // Create the text geometry
        const textGeometry = new TextGeometry(tag, {
            font: font,
            height: fontHeight,
            size: fontSize,
            color: '#5C4033'
            //, curveSegments: 32,
            //bevelEnabled: true,
            //bevelThickness: 0.5,
            //bevelSize: 0.5,
            //bevelSegments: 8,
        });
        // Geometries are attached to meshes so that they get rendered
        const textMesh = new THREE.Mesh(textGeometry, textMaterial);
        textMesh.geometry.center();
        // Update positioning of the text
        textMesh.position.set(0, 0, h / 2);
        textMesh.rotateOnAxis(new THREE.Vector3(0, 0, 1), Math.PI * (-1 + w < d ? 1 / 2 : 0));
        cube.add(textMesh);
    });
    cube.rotateOnAxis(new THREE.Vector3(0, 0, 1), a);

    cube.position.set(x, y, z);
    cube.updateMatrixWorld();
    cube.OriginalColor = cube.material.color;
    cube.Tag = tag;
    cube.Type = "equipment";
    cube.Clicked = false;
    scene.add(cube);
    geometry.dispose();
    //
}

function drawBoard(tagName, jsonPoint, v, w, d, h, angle, color, opacity) {
    if (!canvas) {
        return;
    }
    //tagName, jsonPoints, v, w, d, h, angle, JsonConvert.SerializeObject(color), opacity
    var Point = JSON.parse(jsonPoint);
    var clr = JSON.parse(color);
    var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
    // text label orientation
    // label on 'front' means, the text on 'xz' plane along x axis. 
    // label on 'top' means xy plane along y axis
    var writePlane = 'xz'; // default 'front'
    var textcolor = 0x1000000 - defaultColorEq // tag label of the equipment with invert colour
    //draw each vertical
    for (let i = 0; i < v; i++) {
        //ith vertical
        var x = -w / 2 + (i + 1 / 2) * w / v;
        var y = d / 2;
        var z = h / 2;
        var drawEdge = true;
        var drawMesh = true;
        // draw each verticals
        functionVertical(x, y, z, w / v, d, h, defaultColorEq, opacity, drawEdge, drawMesh);
        // add label for each verticals on front
        if (v != 1 && writeLabelText) {
            // writing the label
            var labelText = 'PNL#' + (i + 1).toString();
            var textSize = 0.1;
            // coordinate of the text
            // text width is assumed as 40% of text height
            var labelCoordinate = [x - textSize * labelText.length * 0.4, y - d / 2 - 0.001, z];
            //
            //writeLabel(labelText, textSize, labelCoordinate, textcolor, groupEq);
            //
        }
    }
    var writeLabelText = true;
    //adding tag label of the equipment
    if (writeLabelText) {
        var textSize = 0.2;
        // if switchgears (multiple verticals) write in front, else for pumps, compressors, etc., write on top
        if (v != 1) { // switchgear
            var textCoordinate = [-w / 2 + textSize, -0.001, z + h / 2 - 2 * textSize];
        } else {  // other equipment like pump, etc.
            writePlane = 'xy';
            var textCoordinate = [textSize / 2, d / 2 - textSize * tagName.length * 0.4, h + 0.005]
        }
        //writeLabel(tagName, textSize, textCoordinate, textcolor);
    }
    //
    // rotating the equipment as per the face direction
    // and positioning the panel at the required coordinates
    //tempv1.set(Point.X, Point.Y, Point.Z);
    //tempv1.sub((tempv2.normalize()).multiplyScalar(d / 2));
    //groupEq.rotateZ(angle * Math.PI / 180);
    //groupEq.position.set(tempv1.x, tempv1.y, tempv1.z);
    //groupEq.lookAt(tempv2); // not used
    //
    //cube.Clicked = false;
    //cube.OriginalColor = cube.material.color;
    //crossMesh.type = "board";
}

//

// function for Single Vertical Panel
function functionVertical(x, y, z, w, d, h, colr, opec, drawEdge, drawMesh) {
    // equipment is drawn facing -y axis, i.e., from South
    var geometry = new THREE.BoxGeometry(w, d, h);
    // drawing mesh
    if (drawMesh) {
        var material = new THREE.MeshPhongMaterial();
        material.clone(equipmentmaterial);
        var cube = new THREE.Mesh(geometry, material);
        cube.position.x = x;
        cube.position.y = y;
        cube.position.z = z;
        cube.material.color.setHex(colr);
        cube.material.opacity = opec;
        //scene.add(cube);
    }
    //
    //drawing outerline/edge
    if (drawEdge) {
        var edges = new THREE.EdgesGeometry(geometry)
        var line = new THREE.LineSegments(edges);
        line.material.depthTest = false;
        line.material.opacity = 0.1;
        line.material.transparent = true;
        line.position.set(x, y, z);
        var color = new THREE.Color(0x000ff)
        line.material.color = color;
        scene.add(line);
    }
    geometry.dispose();
}

function searchItem(tag) {
    var selectedObject = scene.children.filter(child => child.Tag && child.Tag.includes(tag));
    if (selectedObject[0] != undefined) {
        for (let i = 0; i < selectedObject.length; i++) {
            selectedObject[i].material.color = selectItemColor.color;
        }
        camera.lookAt(selectedObject[0].position);
        controls.target = new THREE.Vector3(selectedObject[0].position.x, selectedObject[0].position.y, selectedObject[0].position.z);
        //controls.minDistance = 5;
        //controls.maxDistance = 20;
        //controls.update();
        //camera.zoom = 100;
        animate();
    }
}


function hidePlotPlan(hidePP) {
    //var selectedObject = scene.children.filter(child => child.Tag.includes("plotplan"));
    scene.children.forEach(child => {
        if (child != undefined) {
            if (child.Tag != undefined) {
                if (child.Tag.includes("plotplan")) {
                    child.visible = hidePP;
                }
            }
        }
    });
    animate();
}

function orthoView(ortho, xp, yp, zp) {
    //orthographic camera view;
    //https://stackoverflow.com/questions/48758959/what-is-required-to-convert-threejs-perspective-camera-to-orthographic
    //https://www.google.ae/search?q=threejs+camera&sca_esv=562123659&tbm=vid&sxsrf=AB5stBih_AvvkPKwAIYnui6TpwA4RL21aA:1693635801552&source=lnms&sa=X&ved=2ahUKEwjtgdKCpYuBAxVhRKQEHQ7fAMwQ_AUoA3oECAEQBQ&biw=1727&bih=1076&dpr=1.5#fpstate=ive&vld=cid:af7537a2,vid:FwcXultcBl4
    //https://sbcode.net/view_source/section.html
    //
    if (ortho == true) {
        var v3_object = scene.children.filter(child => child.hasOwnProperty('Tag') && child.Tag.includes("plotplan"))[0];
        if (v3_object == undefined) return;
        var v3_camera = cameraPerspective.position;

        var line_of_sight = new THREE.Vector3();
        cameraPerspective.getWorldDirection(line_of_sight);
        //var v3_object_position = v3_object.position;
        var v3_object_position = new THREE.Vector3(v3_object.position.x, v3_object.position.y, v3_object.position.z);
        var v3_distance = v3_object_position.sub(v3_camera);
        //var v3_distance = (v3_object.clone()).sub(v3_camera);
        var depth = v3_distance.dot(line_of_sight);


        var fov_y = cameraPerspective.fov;
        var height_ortho = depth * 2 * Math.atan(fov_y * (Math.PI / 180) / 2)
        //var aspect = width / height;
        var width_ortho = height_ortho * cameraPerspective.aspect;
        //var width_ortho = height_ortho * aspect;

        var neworthoCamera = new THREE.OrthographicCamera(
            width_ortho / -2, width_ortho / 2,
            height_ortho / 2, height_ortho / -2,
            cameraPerspective.near, cameraPerspective.far);
        neworthoCamera.name = 'orthoCamera';
        neworthoCamera.lookAt(v3_object_position);
        neworthoCamera.position.set(xp, yp, zp);
        neworthoCamera.updateProjectionMatrix();
        //neworthoCamera.quaternion.copy(cameraPerspective.quaternion);
        camera = neworthoCamera;
    } else {
        camera = cameraPerspective;
        scene.remove(scene.getObjectByName('orthoCamera'));
        renderer.render(scene, camera);
    }
    //animate(); // animate includes render
    renderer.render(scene, camera);
}

function removeItem(tag) {
    var selectedObject = scene.children.filter(child => child.Tag == tag);
    if (selectedObject != undefined) {
        scene.remove(selectedObject);
        animate();
    }
}

function writeLabel(labelText, textSize, labelCoordinate, textcolor) {
    // write label facing -y axis at the given coordinate
    const loader = new FontLoader();
    //loader.load('..fonts/helvetiker_regular.typeface.json', function (font) {
    //    //helvetiker_regular
    //    //gentilis_regular.typeface
    //    //const color = 0xffff00;
    //    const textMaterial = new THREE.MeshBasicMaterial({
    //        color: textcolor,
    //        transparent: true,
    //        opacity: 0.9,
    //        side: THREE.DoubleSide
    //    });
    //    const shapes = font.generateShapes(labelText, textSize);
    //    const textGeometry = new THREE.ShapeBufferGeometry(shapes);
    //    const text = new THREE.Mesh(textGeometry, textMaterial);
    //    text.position.set(labelCoordinate[0], labelCoordinate[1], labelCoordinate[2]);
    //    scene.add(text);
    //    textGeometry.dispose();
    //    render();
    //});
}


function castObject(event) {
    // select an object by clicking it 
    // if ctrl key is not pressed, the raycast selects or deselects the object (cable, ladeder, etc.)
    // find intersections
    //2. set the picking ray from the camera position and mouse coordinates

    try {
        raycaster.setFromCamera(mouse, camera);
        //
        //3. compute intersections (no 2nd parameter true anymore)
        var intersects = raycaster.intersectObjects(scene.children);
        // remove hidden items from intersects if raycasted: not required as its covered in below while loop
        //var intersects = intersectstemp.filter(function (val) {
        //        return selectedObject.indexOf(val) == -1;
        //    });
        if (intersects.length == 0) return;
        if (!(intersects[0].object.Tag == undefined)) {
            var intersectItemIndex = 0;
            // if intersect is plot plan choose the next non plotplan item
            intersects = intersects.filter(item => !(item.object.Tag.includes("plotplan")));
            if (intersects.length == 0) return; // no object other than plotplan
            while (intersects[intersectItemIndex].object.visible == false
            && intersectItemIndex < intersects.length) {
                intersectItemIndex++;
            }
            var castObjectTags = [];
            var castObjectUIDs = [];
            if (intersects.length > 0) {
                controls.target = new THREE.Vector3(intersects[0].object.position.x, intersects[0].object.position.y, intersects[0].object.position.z);
                if (selectedObject.includes(intersects[intersectItemIndex].object)) {
                    var indx = selectedObject.indexOf(intersects[intersectItemIndex].object);
                    selectedObject.splice(indx, 1);
                } else {
                    selectedObject.push(intersects[intersectItemIndex].object);
                }
                intersects.forEach(el => {
                    if (el.object.Tag != undefined) {
                        castObjectTags.push(el.object.Tag);
                        castObjectUIDs.push(el.object.UID == undefined ? newGuid() : el.object.UID);
                    }
                });
                if (intersects[intersectItemIndex].object.Clicked) {
                    intersects[intersectItemIndex].object.material.color = intersects[intersectItemIndex].object.OriginalColor;

                } else {
                    intersects[intersectItemIndex].object.material.color = selectItemColor.color;
                }
                intersects[intersectItemIndex].object.Clicked = !intersects[intersectItemIndex].object.Clicked;
                //
                var castObjects = intersects.filter(item => item.object.visible == true);
                var castObjectHidden = intersects[0];
                var casObjectTag = "";
                //
                var x = 0, y = 0, z = 0, xh = castObjectHidden.object.position.x,
                    yh = castObjectHidden.object.position.y, zh = castObjectHidden.object.position.z;
                if (castObjects.length > 0) {
                    casObjectTag = castObjects[0].object.Tag;
                    x = castObjects[0].object.position.x;
                    y = castObjects[0].object.position.y;
                    z = castObjects[0].object.position.z;
                }
                DotNet.invokeMethodAsync(projectName, "UpdateCastObject", JSON.stringify(castObjectUIDs), JSON.stringify(castObjectTags), casObjectTag, x, y, z, castObjectHidden.object.Tag, xh, yh, zh);
            }
        }
    } catch (e) {
        console.log(e)
    }
}


window.selectObjectHighlight = (tag) => {
    var selectObject = scene.children.filter(child => child.Tag == tag);
    var returnColour = selectObject.mesh.color;
    return returnColour;
}

function toggleFunction() {
    var toggle = document.getElementById("layoutPage3dSegment");
    if (toggle.style.display === "none") {
        toggle.style.display = "block";
    } else {
        toggle.style.display = "none";
    }
}

function newGuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g,
        function (c) {
            var uuid = Math.random() * 16 | 0, v = c == 'x' ? uuid : (uuid & 0x3 | 0x8);
            return uuid.toString(16);
        });
}


window.checkElementExists = function(elementId) {
    return document.getElementById(elementId) !== null;
}