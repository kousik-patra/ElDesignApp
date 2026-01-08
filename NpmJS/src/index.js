import 'bootstrap';
import sceneManager, { LAYERS } from './sceneManager';
import { drawPlaneMesh } from './threejs/objects/plane';
import { drawLadderMesh } from './threejs/objects/ladder';
import { drawBendMesh } from './threejs/objects/bend';
import { drawTeeMesh } from './threejs/objects/tee';
import { drawCrossMesh } from './threejs/objects/cross';
import { drawNodeMesh } from './threejs/objects/node';
import { drawSleeveMesh } from './threejs/objects/sleeve';
import { drawEquipmentMesh } from './threejs/objects/equipment';
import { drawSLD, updateSLD, updateSLDItem, updateSLDWithStudyResults } from './mySLD';
import {
    focusElement,
    getModalDialogRect,
    setModalPosition,
    startModalDrag,
    stopModalDrag
} from './modal/modal-interop';

// Expose sceneManager to window
window.sceneManager = sceneManager;
window.SCENE_LAYERS = LAYERS;

// ============ CONSOLE LOG ============
window.consoleLog = function (logString) {
    console.log(logString);
};

// ============ SCENE INITIALIZATION ============
window.initializeScene = function(containerId, stateJson, dotNetObjRef) {
    console.log('=== initializeScene START ===');
    console.log('Container ID:', containerId);

    try {
        const result = sceneManager.initialize(containerId, dotNetObjRef, stateJson);
        console.log('=== initializeScene RESULT:', result, '===');
        return result;
    } catch (e) {
        console.error('=== initializeScene ERROR:', e, '===');
        return false;
    }
};

window.isSceneReady = function() {
    return sceneManager.isReady ? sceneManager.isReady() : false;
};

window.getSceneState = function() {
    return sceneManager.getState ? sceneManager.getState() : '';
};

// ============ LAYER MANAGEMENT ============
window.setLayerVisibility = function(layer, visible) {
    if (sceneManager) {
        sceneManager.setLayerVisibility(layer, visible);
    }
};

window.setPageContext = function(pageName, layersJson) {
    if (sceneManager && sceneManager.setPageContext) {
        const layers = JSON.parse(layersJson);
        sceneManager.setPageContext(pageName, layers);
    }
};

window.clearLayer = function(layer) {
    if (sceneManager && sceneManager.clearLayer) {
        sceneManager.clearLayer(layer);
    }
};

window.clearAllSceneLayers = function() {
    if (sceneManager && sceneManager.clearAllLayers) {
        sceneManager.clearAllLayers();
        return true;
    }
    return false;
};

// ============ VISIBILITY TOGGLE ============
window.toggleSceneVisibility = function(visible) {
    const container = document.getElementById('shared-scene-container');
    if (container) {
        container.style.display = visible ? 'block' : 'none';
        if (visible && sceneManager && sceneManager.onWindowResize) {
            sceneManager.onWindowResize();
        }
    }
};

// ============ DRAWING FUNCTIONS ============
window.drawPlane = function(planeName, planeTag, imageString, scaleX, scaleY, centreX, centreY, elevation, opacity) {
    if (sceneManager && sceneManager.scene) {
        const mesh = drawPlaneMesh(
            sceneManager.renderer?.domElement?.width || 800,
            sceneManager.renderer?.domElement?.height || 600,
            planeName, planeTag, imageString, scaleX, scaleY, centreX, centreY, elevation, opacity
        );
        if (mesh) {
            sceneManager.addObject(mesh, planeTag, LAYERS.PLOT_PLAN);
        }
    }
};

window.drawLadder = function(tag, jsonPoints, color, opacity) {
    if (sceneManager && sceneManager.scene) {
        // Use your existing drawLadderMesh function
        // sceneManager.addObject(mesh, tag, LAYERS.LADDERS);
        console.log('drawLadder called:', tag);
    }
};

window.drawEquipment = function(tag, x, y, z, w, d, h, a, color, opacity, colortext) {
    if (sceneManager && sceneManager.scene) {
        // Use your existing drawEquipmentMesh function
        // sceneManager.addObject(mesh, tag, LAYERS.EQUIPMENT);
        console.log('drawEquipment called:', tag);
    }
};

window.drawCube = function() {
    if (sceneManager && sceneManager.scene) {
        console.log('drawCube called');
    }
};

window.hide3D = function(hide) {
    if (sceneManager) {
        // Toggle visibility
        console.log('hide3D called:', hide);
    }
};

window.clearScene = function() {
    if (sceneManager && sceneManager.clearAllLayers) {
        sceneManager.clearAllLayers();
    }
};

// ============ ELEMENT CHECK ============
window.checkElementExists = function(elementId) {
    return document.getElementById(elementId) !== null;
};

// ============ FILE SAVE ============
window.saveAsFile = function(fileName, bytesBase64) {
    var link = document.createElement('a');
    link.download = fileName;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// ============ SLD ============
window.drawSLD = drawSLD;
window.updateSLD = updateSLD;
window.updateSLDItem = updateSLDItem;
window.updateSLDWithStudyResults = updateSLDWithStudyResults;

// ============ MODAL ============
window.startModalDrag = startModalDrag;
window.stopModalDrag = stopModalDrag;
window.getModalDialogRect = getModalDialogRect;
window.setModalPosition = setModalPosition;

