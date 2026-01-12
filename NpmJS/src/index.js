import 'bootstrap';
import sceneManager, { LAYERS } from './threejs/sceneManager'

import { MouseEventHandler } from './threejs/events/mouseEvents';
import { drawLadderMesh } from './threejs/objects/ladder';
import { drawBendMesh } from './threejs/objects/bend';
import { drawTeeMesh } from './threejs/objects/tee';
import { drawCrossMesh } from './threejs/objects/cross';
import { drawNodeMesh } from './threejs/objects/node';
import { drawSleeveMesh } from './threejs/objects/sleeve';
import { drawEquipmentMesh } from './threejs/objects/equipment';
import { drawSLD, updateSLD, updateSLDItem, updateSLDWithStudyResults } from './mySLD';
import { focusElement, getModalDialogRect, setModalPosition, startModalDrag, stopModalDrag} from './modal/modal-interop';
import { addPin, removePin, clearAllPins, showPinHelpers, hidePinHelpers, drawRefPointMesh} from "./threejs/objects/refPoint";
// Import pin cursor module
import * as PinCursor from './threejs/events/pinCursor'



// Expose sceneManager to window
window.sceneManager = sceneManager;
window.SCENE_LAYERS = LAYERS;

// ===== Expose Pin Cursor functions to window for Blazor interop =====
window.initPinPlacementMode = PinCursor.initPinPlacementMode;
window.disposePinPlacementMode = PinCursor.disposePinPlacementMode;
window.setPinModeActive = PinCursor.setPinModeActive;
window.updatePinModeTag = PinCursor.updatePinModeTag;
window.shouldPlacePin = PinCursor.shouldPlacePin;
window.getCurrentPinTag = PinCursor.getCurrentPinTag;
window.isPinModeActive = PinCursor.isPinModeActive;
window.isShiftPressed = PinCursor.isShiftPressed;

// ===== Expose pin management functions to window for Blazor interop =====
window.addPinMarker = function(x, y, z = 0, tag = null) {
    if (!window.sceneManager?.scene) {
        console.error('Scene not initialized');
        return null;
    }
    return addPin(window.sceneManager.scene, tag, { x, y, z }, true, true);
};

window.removePinMarker = function(tag) {
    if (!window.sceneManager?.scene) return;
    removePin(window.sceneManager.scene, tag);
};

window.clearAllPinMarkers = function() {
    if (!window.sceneManager?.scene) return;
    clearAllPins(window.sceneManager.scene);
};

window.showPinHelpersAt = function(x, y, z = 0, type = 'both') {
    if (!window.sceneManager?.scene) return;
    showPinHelpers(window.sceneManager.scene, { x, y, z }, type);
};

window.hidePinHelpers = function() {
    if (!window.sceneManager?.scene) return;
    hidePinHelpers(window.sceneManager.scene);
};


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

window.drawRefPoint = function(tag, point, opacity) {
    if (sceneManager && sceneManager.scene) {
        const mesh = drawRefPointMesh(tag, point, opacity
        );
        if (mesh) {
            sceneManager.addObject(mesh, tag, LAYERS.PLOT_PLAN);
            console.log('window.drawRefPoint :', tag, ' added.');
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

// ========== UPDATE DOM ELEMENT BY ID =========
window.updateElementById = (id, text, color="black") => {
    const el = document.getElementById(id);
    if (el) {
        el.innerText = text;
        el.style.color = color;
    }
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

