//// Module Manager for registering the modules of the chart
//import { ModuleManager } from 'igniteui-webcomponents-core';
//// Radial Gauge Module
//import { IgcRadialGaugeModule } from 'igniteui-webcomponents-gauges';


//// register the modules
//ModuleManager.register(
//    IgcRadialGaugeModule
//);

import 'bootstrap';
import {
    drawScene3Js,
    drawPlane3Js,
    rotatePlaneJs,
    scalePlane3Js,
    centrePlan3Js,
    drawBend3Js,
    drawCross3Js,
    drawCube3Js,
    drawEquipment3Js,
    drawLadder3Js,
    drawLadderChunk3Js,
    drawNode3Js,
    drawSleeve3Js,
    drawTee3Js,
    hide3D3Js,
} from './myThree'
import {drawSLD, updateSLD, updateSLDItem, updateSLDWithStudyResults} from './mySLD'
import {
    focusElement,
    //getModalDialogElement,
    getModalDialogRect,
    setModalPosition,
    //setupDraggableModal,
    startModalDrag,
    stopModalDrag
} from '../src/modal/modal-interop';


window.consoleLog = function (logString) {
    console.log(logString);
}


//drawBend, drawTee, drawCross, drawCable
window.drawScene = function (divId, sceneJSON = "", dotNetObjRef) {
    drawScene3Js(divId, sceneJSON, dotNetObjRef);
}
window.drawCube = drawCube3Js
window.hide3D = hide3D3Js
window.clearScene = function () {
    clearScene();
}

window.drawPlane = drawPlane3Js;
window.rotatePlane = rotatePlaneJs;
window.scalePlane = scalePlane3Js;
window.centrePlane = centrePlan3Js;




window.updateValue = function (value) {
    var rg = document.getElementById("rg");
    rg.value = value;
}

window.drawLadder = drawLadder3Js
window.drawLadderChunk = drawLadderChunk3Js

window.drawBend = drawBend3Js
window.drawTee = drawTee3Js
window.drawCross = drawCross3Js
window.drawNode = drawNode3Js
window.drawSleeveZ = drawSleeve3Js
window.drawEquipment = drawEquipment3Js

var dotNetObj;
window.initialiseObjectRef = function (dotNetObjRef) {
    dotNetObj = dotNetObjRef;
}
// eventlisteners
window.addEventListener('resize', onWindowResize, false);

function onWindowResize() {
    var windowWidth = parseInt(window.innerWidth);
    var windowHeight = parseInt(window.innerHeight);
    //dotNetObj.invokeMethodAsync("WindowSize", windowWidth, windowHeight);
}


window.saveAsFile = function (fileName, bytesBase64) {
    var link = document.createElement('a');
    link.download = fileName;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link); // Needed for Firefox
    link.click();
    document.body.removeChild(link);
}


window.drawSLD = drawSLD
window.updateSLD = updateSLD
window.updateSLDItem = updateSLDItem
window.updateSLDWithStudyResults = updateSLDWithStudyResults


//window.setupDraggableModal = setupDraggableModal;
window.startModalDrag = startModalDrag;
window.stopModalDrag = stopModalDrag;
window.getModalDialogRect = getModalDialogRect;
//window.getModalDialogElement = getModalDialogElement;
window.setModalPosition = setModalPosition;
//window.focusElement = focusElement;

