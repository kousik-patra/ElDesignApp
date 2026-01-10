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


// ===== State =====
const pinModeState = {
    isActive: false,
    shiftPressed: false,
    currentTag: null,
    dotNetHelper: null,
    tooltipElement: null,
    mouseX: 0,
    mouseY: 0
};

// ===== Internal Functions =====

function handleKeyDown(event) {
    if (event.key === 'Shift' && !pinModeState.shiftPressed) {
        pinModeState.shiftPressed = true;
        updateCursor();
        updateTooltip();

        // Notify Blazor
        if (pinModeState.dotNetHelper && pinModeState.isActive) {
            pinModeState.dotNetHelper.invokeMethodAsync('OnShiftKeyChanged', true)
                .catch(err => console.error('Error notifying Blazor of shift key:', err));
        }
    }

    // Escape to cancel pin mode
    if (event.key === 'Escape' && pinModeState.isActive) {
        if (pinModeState.dotNetHelper) {
            pinModeState.dotNetHelper.invokeMethodAsync('OnPinModeCancelled')
                .catch(err => console.error('Error cancelling pin mode:', err));
        }
    }
}

function handleKeyUp(event) {
    if (event.key === 'Shift') {
        pinModeState.shiftPressed = false;
        updateCursor();
        updateTooltip();

        // Notify Blazor
        if (pinModeState.dotNetHelper && pinModeState.isActive) {
            pinModeState.dotNetHelper.invokeMethodAsync('OnShiftKeyChanged', false)
                .catch(err => console.error('Error notifying Blazor of shift key:', err));
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
}

function updateCursor() {
    const container = document.getElementById('shared-scene-container');
    if (!container) return;

    if (pinModeState.isActive && pinModeState.shiftPressed) {
        container.classList.add('pin-mode');
    } else {
        container.classList.remove('pin-mode');
    }
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
        pinModeState.tooltipElement.className = 'pin-next-tag-tooltip';
        pinModeState.tooltipElement.style.cssText = `
            position: fixed;
            background: rgba(0, 0, 0, 0.85);
            color: white;
            padding: 8px 14px;
            border-radius: 6px;
            font-family: 'Consolas', monospace;
            font-size: 13px;
            pointer-events: none;
            z-index: 10000;
            transform: translate(-50%, -100%);
            margin-top: -15px;
            white-space: nowrap;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
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

// ===== Exported Functions (ES Module) =====

/**
 * Initialize pin placement mode with Blazor reference
 * @param {object} dotNetRef - Blazor .NET object reference
 */
export function initPinPlacementMode(dotNetRef) {
    pinModeState.dotNetHelper = dotNetRef;

    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('keyup', handleKeyUp);
    document.addEventListener('mousemove', handleMouseMove);

    console.log('PinCursor: Initialized');
}

/**
 * Dispose pin placement mode and remove listeners
 */
export function disposePinPlacementMode() {
    document.removeEventListener('keydown', handleKeyDown);
    document.removeEventListener('keyup', handleKeyUp);
    document.removeEventListener('mousemove', handleMouseMove);
    hideTooltip();

    if (pinModeState.tooltipElement && pinModeState.tooltipElement.parentNode) {
        pinModeState.tooltipElement.parentNode.removeChild(pinModeState.tooltipElement);
        pinModeState.tooltipElement = null;
    }

    pinModeState.dotNetHelper = null;
    pinModeState.isActive = false;
    pinModeState.shiftPressed = false;
    pinModeState.currentTag = null;

    updateCursor();

    console.log('PinCursor: Disposed');
}

/**
 * Set pin mode active state
 * @param {boolean} active - Whether pin mode is active
 * @param {string} currentTag - Current tag to display/use
 */
export function setPinModeActive(active, currentTag = null) {
    pinModeState.isActive = active;
    pinModeState.currentTag = currentTag;

    updateCursor();
    updateTooltip();

    console.log(`PinCursor: active=${active}, tag=${currentTag}`);
}

/**
 * Update the current pin tag
 * @param {string} tag - New tag value
 */
export function updatePinModeTag(tag) {
    pinModeState.currentTag = tag;
    updateTooltip();
}

/**
 * Check if pin mode is active
 * @returns {boolean}
 */
export function isPinModeActive() {
    return pinModeState.isActive;
}

/**
 * Check if shift key is pressed
 * @returns {boolean}
 */
export function isShiftPressed() {
    return pinModeState.shiftPressed;
}

/**
 * Check if we should place a pin (active + shift pressed)
 * @returns {boolean}
 */
export function shouldPlacePin() {
    return pinModeState.isActive && pinModeState.shiftPressed;
}

/**
 * Get the current pin tag
 * @returns {string|null}
 */
export function getCurrentPinTag() {
    return pinModeState.currentTag;
}

/**
 * Request next tag from Blazor (async)
 * @returns {Promise<string|null>}
 */
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

// ===== Also expose to window for direct Blazor interop =====
// (So Blazor can call these without going through ES modules)

window.initPinPlacementMode = initPinPlacementMode;
window.disposePinPlacementMode = disposePinPlacementMode;
window.setPinModeActive = setPinModeActive;
window.updatePinModeTag = updatePinModeTag;
window.shouldPlacePin = shouldPlacePin;
window.getCurrentPinTag = getCurrentPinTag;
window.isPinModeActive = isPinModeActive;
window.isShiftPressed = isShiftPressed;
window.requestNextPinTag = requestNextPinTag;

// Export state for advanced usage/debugging
export { pinModeState };