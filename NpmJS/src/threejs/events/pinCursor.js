// NpmJS/src/threejs/events/pinCursor.js
// Pin cursor and keyboard handling for pin placement mode
// ES Module version for import in mouseEvents.js



// ============================================================
// COMPLETE FLOW DIAGRAM
// ============================================================

/*
┌─────────────────────────────────────────────────────────────────────────────┐
│                            RAZOR PAGE                                       │
│  (e.g., PlotEdit.razor)                                                     │
│                                                                             │
│  1. User adds tags to _refPinTagList                                        │
│  2. User clicks "Start Pin Placement"                                       │
│  3. Calls: PinService.StartPlacement(_refPinTagList)                        │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PinPlacementService.cs                              │
│                                                                             │
│  • Sets IsActive = true                                                     │
│  • Stores tag list                                                          │
│  • Fires OnStateChanged event                                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        SharedSceneHost.razor                                │
│                                                                             │
│  • Subscribes to PinService.OnStateChanged                                  │
│  • Calls JS: setPinModeActive(true, currentTag)                             │
│  • Shows progress indicator in UI                                           │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           pinCursor.js                                      │
│                                                                             │
│  • Stores isActive = true, currentTag                                       │
│  • Listens for Shift key press                                              │
│  • When Shift pressed: adds 'pin-mode' class to container                   │
│  • Shows tooltip with current tag near cursor                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ User holds SHIFT + clicks
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          mouseEvents.js                                     │
│                                                                             │
│  • executeClick() detects shift+click                                      │
│  • Checks: PinCursor.isPinModeActive()                                     │
│  • If true: calls handlePinPlacement()                                     │
│    - Gets tag from PinCursor.getCurrentPinTag()                            │
│    - Calls addPin(scene, tag, point, ...)                                  │
│    - Sets eventData.eventType = 'pinPlaced'                                │
│    - Calls notifyBlazor(eventData)                                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                             Draw.cs                                         │
│                                                                             │
│  • OnSceneClick receives JSON with eventType='pinPlaced'                   │
│  • HandlePinPlaced() processes it                                          │
│  • Sends SceneMessage to UI                                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        SharedSceneHost.razor                                │
│                                                                             │
│  • HandleSceneMessage() receives message                                   │
│  • Calls PinService.GetNextTag() to advance                                │
│  • Calls JS: updatePinModeTag(nextTag)                                     │
│  • Updates message bar UI                                                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Repeat until all tags placed
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PinPlacementService.cs                              │
│                                                                             │
│  • When all tags placed: fires OnAllPinsPlaced                             │
│  • Sets IsActive = false                                                   │
│  • Pin mode ends                                                           │
└─────────────────────────────────────────────────────────────────────────────┘

*/
// NpmJS/src/threejs/events/pinCursor.js
// Simplified pin cursor with crosshair preview

// pinCursor.js - Debug version with logging

import * as THREE from 'three';

// ===== State =====
const pinModeState = {
    isActive: false,
    shiftPressed: false,
    currentTag: null,
    dotNetHelper: null,
    tooltipElement: null,
    mouseX: 0,
    mouseY: 0,
    crosshair: null,
    scene: null,
    camera: null,
    renderer: null,
    raycaster: null,
    lastPreviewPoint: null
};

const CROSSHAIR_CONFIG = {
    color: 0x00ff00,
    centerColor: 0xff0000,
    length: 2,
    lineWidth: 2
};

// ===== Crosshair Functions =====

function createCrosshair() {
    //console.log('[PinCursor DEBUG] Creating crosshair...');

    const group = new THREE.Group();
    group.name = 'pinCrosshair';

    const material = new THREE.LineBasicMaterial({
        color: CROSSHAIR_CONFIG.color,
        linewidth: CROSSHAIR_CONFIG.lineWidth,
        depthTest: false,
        transparent: true,
        opacity: 0.9
    });

    const len = CROSSHAIR_CONFIG.length;

    // X-axis line
    const xGeom = new THREE.BufferGeometry().setFromPoints([
        new THREE.Vector3(-len, 0, 0),
        new THREE.Vector3(len, 0, 0)
    ]);
    group.add(new THREE.Line(xGeom, material));

    // Y-axis line
    const yGeom = new THREE.BufferGeometry().setFromPoints([
        new THREE.Vector3(0, -len, 0),
        new THREE.Vector3(0, len, 0)
    ]);
    group.add(new THREE.Line(yGeom, material));

    // Center dot
    const dotGeom = new THREE.CircleGeometry(0.1, 16);
    const dotMat = new THREE.MeshBasicMaterial({
        color: CROSSHAIR_CONFIG.centerColor,
        side: THREE.DoubleSide,
        depthTest: false
    });
    const dot = new THREE.Mesh(dotGeom, dotMat);
    dot.position.z = 0.01;
    group.add(dot);

    group.renderOrder = 9999;

    //console.log('[PinCursor DEBUG] Crosshair created with', group.children.length, 'children');
    return group;
}

function showCrosshair() {
    //console.log('[PinCursor DEBUG] showCrosshair called');
    //console.log('[PinCursor DEBUG] scene:', pinModeState.scene ? 'EXISTS' : 'NULL');
    //console.log('[PinCursor DEBUG] camera:', pinModeState.camera ? 'EXISTS' : 'NULL');
    //console.log('[PinCursor DEBUG] renderer:', pinModeState.renderer ? 'EXISTS' : 'NULL');

    if (!pinModeState.scene) {
        //console.error('[PinCursor DEBUG] Cannot show crosshair - scene is null!');
        return;
    }

    if (!pinModeState.crosshair) {
        pinModeState.crosshair = createCrosshair();
        //console.log('[PinCursor DEBUG] New crosshair created');
    }

    if (!pinModeState.crosshair.parent) {
        pinModeState.scene.add(pinModeState.crosshair);
        //console.log('[PinCursor DEBUG] Crosshair added to scene');
    }

    pinModeState.crosshair.visible = true;
    //console.log('[PinCursor DEBUG] Crosshair visibility set to true');

    updateCrosshairPosition();
}

function hideCrosshair() {
    if (pinModeState.crosshair) {
        pinModeState.crosshair.visible = false;
        //console.log('[PinCursor DEBUG] Crosshair hidden');
    }
}

function updateCrosshairPosition() {
    if (!pinModeState.crosshair?.visible) {
        //console.log('[PinCursor DEBUG] updateCrosshairPosition - crosshair not visible, skipping');
        return;
    }

    if (!pinModeState.scene || !pinModeState.camera || !pinModeState.renderer) {
        console.error('[PinCursor DEBUG] updateCrosshairPosition - missing references:', {
            scene: !!pinModeState.scene,
            camera: !!pinModeState.camera,
            renderer: !!pinModeState.renderer
        });
        return;
    }

    if (!pinModeState.raycaster) {
        pinModeState.raycaster = new THREE.Raycaster();
    }

    const mouse = new THREE.Vector2();
    const rect = pinModeState.renderer.domElement.getBoundingClientRect();

    mouse.x = ((pinModeState.mouseX - rect.left) / rect.width) * 2 - 1;
    mouse.y = -((pinModeState.mouseY - rect.top) / rect.height) * 2 + 1;

    console.log('[PinCursor DEBUG] Mouse NDC:', mouse.x.toFixed(3), mouse.y.toFixed(3));

    pinModeState.raycaster.setFromCamera(mouse, pinModeState.camera);

    // Find intersection
    let point = null;
    const intersects = pinModeState.raycaster.intersectObjects(pinModeState.scene.children, true);

    console.log('[PinCursor DEBUG] Raycast found', intersects.length, 'intersections');

    for (const hit of intersects) {
        if (isPartOfCrosshair(hit.object)) continue;
        if (!hit.object.visible) continue;
        point = hit.point;
        console.log('[PinCursor DEBUG] Using intersection with:', hit.object.name || hit.object.type, 'at', point.x.toFixed(2), point.y.toFixed(2), point.z.toFixed(2));
        break;
    }

    // Fallback to z=0 plane
    if (!point) {
        console.log('[PinCursor DEBUG] No intersection, using z=0 plane fallback');
        const plane = new THREE.Plane(new THREE.Vector3(0, 0, 1), 0);
        point = new THREE.Vector3();
        const intersected = pinModeState.raycaster.ray.intersectPlane(plane, point);
        if (!intersected) {
            console.error('[PinCursor DEBUG] Failed to intersect z=0 plane!');
            return;
        }
    }

    if (point) {
        pinModeState.crosshair.position.set(point.x, point.y, point.z + 0.05);
        pinModeState.lastPreviewPoint = point.clone();
        console.log('[PinCursor DEBUG] Crosshair positioned at:', point.x.toFixed(2), point.y.toFixed(2), point.z.toFixed(2));
    }
}

function isPartOfCrosshair(object) {
    let current = object;
    while (current) {
        if (current === pinModeState.crosshair) return true;
        current = current.parent;
    }
    return false;
}

function disposeCrosshair() {
    if (pinModeState.crosshair) {
        if (pinModeState.crosshair.parent) {
            pinModeState.crosshair.parent.remove(pinModeState.crosshair);
        }
        pinModeState.crosshair.traverse((child) => {
            if (child.geometry) child.geometry.dispose();
            if (child.material) child.material.dispose();
        });
        pinModeState.crosshair = null;
    }
}

// ===== Event Handlers =====

function handleKeyDown(event) {
    if (event.key === 'Shift' && !pinModeState.shiftPressed) {
        console.log('[PinCursor DEBUG] Shift pressed, isActive:', pinModeState.isActive);

        pinModeState.shiftPressed = true;
        updateCursor();
        updateTooltip();

        if (pinModeState.isActive) {
            showCrosshair();
        } else {
            console.log('[PinCursor DEBUG] Pin mode not active, crosshair not shown');
        }

        if (pinModeState.dotNetHelper && pinModeState.isActive) {
            pinModeState.dotNetHelper.invokeMethodAsync('OnShiftKeyChanged', true).catch(console.error);
        }
    }

    if (event.key === 'Escape' && pinModeState.isActive) {
        hideCrosshair();
        if (pinModeState.dotNetHelper) {
            pinModeState.dotNetHelper.invokeMethodAsync('OnPinModeCancelled').catch(console.error);
        }
    }
}

function handleKeyUp(event) {
    if (event.key === 'Shift') {
        console.log('[PinCursor DEBUG] Shift released');
        pinModeState.shiftPressed = false;
        updateCursor();
        updateTooltip();
        hideCrosshair();

        if (pinModeState.dotNetHelper && pinModeState.isActive) {
            pinModeState.dotNetHelper.invokeMethodAsync('OnShiftKeyChanged', false).catch(console.error);
        }
    }
}

function handleMouseMove(event) {
    pinModeState.mouseX = event.clientX;
    pinModeState.mouseY = event.clientY;

    if (pinModeState.tooltipElement) {
        pinModeState.tooltipElement.style.left = event.clientX + 'px';
        pinModeState.tooltipElement.style.top = event.clientY + 'px';
    }

    if (pinModeState.isActive && pinModeState.shiftPressed) {
        updateCrosshairPosition();
    }
}

function updateCursor() {
    const container = document.getElementById('shared-scene-container');
    if (!container) return;
    container.classList.toggle('pin-mode', pinModeState.isActive && pinModeState.shiftPressed);
}

function updateTooltip() {
    if (pinModeState.isActive && pinModeState.shiftPressed && pinModeState.currentTag) {
        showTooltip(pinModeState.currentTag);
    } else {
        hideTooltip();
    }
}

function showTooltip(tag) {
    if (!pinModeState.tooltipElement) {
        pinModeState.tooltipElement = document.createElement('div');
        pinModeState.tooltipElement.style.cssText = `
            position: fixed;
            background: rgba(0, 0, 0, 0.85);
            color: white;
            padding: 6px 12px;
            border-radius: 4px;
            font-family: Consolas, monospace;
            font-size: 13px;
            pointer-events: none;
            z-index: 10000;
            transform: translate(-50%, -100%);
            margin-top: -12px;
        `;
        document.body.appendChild(pinModeState.tooltipElement);
    }
    pinModeState.tooltipElement.textContent = `📍 ${tag}`;
    pinModeState.tooltipElement.style.left = pinModeState.mouseX + 'px';
    pinModeState.tooltipElement.style.top = pinModeState.mouseY + 'px';
    pinModeState.tooltipElement.style.display = 'block';
}

function hideTooltip() {
    if (pinModeState.tooltipElement) {
        pinModeState.tooltipElement.style.display = 'none';
    }
}

// ===== Exported Functions =====

export function initPinPlacementMode(dotNetRef) {
    //console.log('[PinCursor DEBUG] initPinPlacementMode called');
    pinModeState.dotNetHelper = dotNetRef;
    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('keyup', handleKeyUp);
    document.addEventListener('mousemove', handleMouseMove);
    //console.log('[PinCursor DEBUG] Event listeners added');
}

export function setSceneReferences(scene, camera, renderer) {
    //console.log('[PinCursor DEBUG] setSceneReferences called');
    //console.log('[PinCursor DEBUG] scene:', scene ? 'PROVIDED' : 'NULL');
    //console.log('[PinCursor DEBUG] camera:', camera ? 'PROVIDED' : 'NULL');
    //console.log('[PinCursor DEBUG] renderer:', renderer ? 'PROVIDED' : 'NULL');

    pinModeState.scene = scene;
    pinModeState.camera = camera;
    pinModeState.renderer = renderer;
    pinModeState.raycaster = new THREE.Raycaster();

    //console.log('[PinCursor DEBUG] Scene references stored');
}

export function disposePinPlacementMode() {
    document.removeEventListener('keydown', handleKeyDown);
    document.removeEventListener('keyup', handleKeyUp);
    document.removeEventListener('mousemove', handleMouseMove);
    hideTooltip();
    disposeCrosshair();

    if (pinModeState.tooltipElement?.parentNode) {
        pinModeState.tooltipElement.parentNode.removeChild(pinModeState.tooltipElement);
        pinModeState.tooltipElement = null;
    }

    pinModeState.dotNetHelper = null;
    pinModeState.isActive = false;
    pinModeState.shiftPressed = false;
    pinModeState.currentTag = null;
    pinModeState.scene = null;
    pinModeState.camera = null;
    pinModeState.renderer = null;
    pinModeState.raycaster = null;

    updateCursor();
    //console.log('[PinCursor DEBUG] Disposed');
}

export function setPinModeActive(active, currentTag = null) {
    //console.log('[PinCursor DEBUG] setPinModeActive:', active, 'tag:', currentTag);
    pinModeState.isActive = active;
    pinModeState.currentTag = currentTag;
    updateCursor();
    updateTooltip();    
    if (!active) hideCrosshair();
}

export function updatePinModeTag(tag) {
    pinModeState.currentTag = tag;
    updateTooltip();
}

export function isPinModeActive() { return pinModeState.isActive; }
export function isShiftPressed() { return pinModeState.shiftPressed; }
export function shouldPlacePin() { return pinModeState.isActive && pinModeState.shiftPressed; }
export function getCurrentPinTag() { return pinModeState.currentTag; }
export function getLastPreviewPoint() { return pinModeState.lastPreviewPoint; }

export async function requestNextPinTag() {
    if (pinModeState.dotNetHelper) {
        try {
            const nextTag = await pinModeState.dotNetHelper.invokeMethodAsync('GetNextPinTag');
            pinModeState.currentTag = nextTag;
            updateTooltip();
            return nextTag;
        } catch (e) {
            console.error('Error getting next pin tag:', e);
            return null;
        }
    }
    return null;
}

export function configureCrosshair(config) {
    Object.assign(CROSSHAIR_CONFIG, config);
}

// ===== Debug helper - expose to window for console testing =====
window.debugPinCursor = function() {
    console.log('=== PinCursor State ===');
    console.log('isActive:', pinModeState.isActive);
    console.log('shiftPressed:', pinModeState.shiftPressed);
    console.log('currentTag:', pinModeState.currentTag);
    console.log('scene:', pinModeState.scene ? 'EXISTS' : 'NULL');
    console.log('camera:', pinModeState.camera ? 'EXISTS' : 'NULL');
    console.log('renderer:', pinModeState.renderer ? 'EXISTS' : 'NULL');
    console.log('crosshair:', pinModeState.crosshair ? 'EXISTS' : 'NULL');
    if (pinModeState.crosshair) {
        console.log('crosshair.visible:', pinModeState.crosshair.visible);
        console.log('crosshair.position:', pinModeState.crosshair.position);
        console.log('crosshair.parent:', pinModeState.crosshair.parent ? 'IN SCENE' : 'NOT IN SCENE');
    }
    console.log('lastPreviewPoint:', pinModeState.lastPreviewPoint);
    return pinModeState;
};

export { pinModeState, CROSSHAIR_CONFIG };