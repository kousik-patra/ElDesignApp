/**
 * pinFunctions.js - Window Function Registration for Pin Operations
 *
 * This module registers all pin-related window functions that can be called
 * from Blazor via JSInterop.
 *
 * Usage in sceneManager.js:
 *   import { initPinFunctions } from './objects/pinFunctions';
 *
 *   // In initialize() method after scene is ready:
 *   initPinFunctions(this);
 *
 * Blazor calls:
 *   await JsRuntime.InvokeVoidAsync("addPin", tag, x, y, z);
 *   await JsRuntime.InvokeVoidAsync("removePin", tag);
 *   await JsRuntime.InvokeVoidAsync("clearAllPins");
 *   await JsRuntime.InvokeVoidAsync("getAllPins");
 *   await JsRuntime.InvokeVoidAsync("updateRefPointTexts", tagList);
 */

import {
    addPin,
    removePin,
    clearAllPins,
    getAllPins,
    showPinHelpers,
    hidePinHelpers,
    setPinState,
    configurePins,
    PIN_CONFIG
} from '../objects/refPoint';

import {
    setPinModeActive,
    updatePinModeTag,
    isPinModeActive,
    getCurrentPinTag
} from '../events/pinCursor';

// ============================================================================
// MODULE STATE
// ============================================================================

let _sceneManager = null;
let _expectedTags = [];  // List of expected pin tags for the current operation

// ============================================================================
// INITIALIZATION
// ============================================================================

/**
 * Initialize pin functions with the scene manager reference
 * Call this after your sceneManager is ready
 *
 * @param {Object} sceneManager - Your Three.js scene manager instance
 */
function initPinFunctions(sceneManager) {
    if (!sceneManager) {
        console.error('[pinFunctions] initPinFunctions: sceneManager is required');
        return;
    }

    _sceneManager = sceneManager;
    registerWindowFunctions();

    console.log('[pinFunctions] Pin functions initialized');
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Get the scene from scene manager
 */
function getScene() {
    return _sceneManager?.scene || null;
}

/**
 * Validate scene manager is initialized
 */
function validateSceneManager(functionName) {
    if (!_sceneManager || !_sceneManager.scene) {
        console.error(`[pinFunctions] ${functionName}: sceneManager not initialized. Call initPinFunctions() first.`);
        return false;
    }
    return true;
}

// ============================================================================
// WINDOW FUNCTION REGISTRATION
// ============================================================================

function registerWindowFunctions() {

    // ===== Pin Management =====

    /**
     * Add a pin at the specified location
     * @param {string} tag - Unique identifier for the pin
     * @param {number} x - X coordinate
     * @param {number} y - Y coordinate
     * @param {number} z - Z coordinate (elevation)
     * @param {boolean} [useSprite=true] - Use sprite-based pin (always faces camera)
     * @param {boolean} [showHelpers=true] - Show crosshairs and circles
     */
    window.addPin = function(tag, x, y, z = 0, useSprite = true, showHelpers = true) {
        if (!validateSceneManager('addPin')) return null;

        const point = { x, y, z };
        const pinTag = addPin(getScene(), tag, point, useSprite, showHelpers);

        console.log(`[pinFunctions] addPin: '${pinTag}' at (${x.toFixed(2)}, ${y.toFixed(2)}, ${z.toFixed(2)})`);
        return pinTag;
    };

    /**
     * Remove a specific pin by tag
     * @param {string} tag - Tag of the pin to remove
     */
    window.removePin = function(tag) {
        if (!validateSceneManager('removePin')) return false;

        removePin(getScene(), tag);
        console.log(`[pinFunctions] removePin: '${tag}'`);
        return true;
    };

    /**
     * Clear all pins from the scene
     */
    window.clearAllPins = function() {
        if (!validateSceneManager('clearAllPins')) return false;

        clearAllPins(getScene());
        _expectedTags = [];
        console.log('[pinFunctions] clearAllPins: All pins removed');
        return true;
    };

    /**
     * Get all pins currently in the scene
     * @returns {Array} Array of pin objects with tag, x, y, z
     */
    window.getAllPins = function() {
        if (!validateSceneManager('getAllPins')) return [];

        return getAllPins();
    };

    /**
     * Get pin data by tag
     * @param {string} tag - Tag of the pin
     * @returns {Object|null} Pin data or null if not found
     */
    window.getPinByTag = function(tag) {
        if (!validateSceneManager('getPinByTag')) return null;

        const allPins = getAllPins();
        return allPins.find(p => p.tag === tag) || null;
    };

    // ===== Pin Visual State =====

    /**
     * Set the visual state of a pin (hover, selected, default)
     * @param {string} tag - Tag of the pin
     * @param {string} state - 'hover', 'selected', or 'default'
     */
    window.setPinState = function(tag, state) {
        if (!validateSceneManager('setPinState')) return false;

        setPinState(tag, state);
        return true;
    };

    /**
     * Show pin helpers (crosshairs, circles) at a location
     * @param {number} x - X coordinate
     * @param {number} y - Y coordinate
     * @param {number} z - Z coordinate
     * @param {string} [helperType='both'] - 'cross', 'circles', or 'both'
     */
    window.showPinHelpers = function(x, y, z = 0, helperType = 'both') {
        if (!validateSceneManager('showPinHelpers')) return false;

        showPinHelpers(getScene(), { x, y, z }, helperType);
        return true;
    };

    /**
     * Hide pin helpers
     */
    window.hidePinHelpers = function() {
        if (!validateSceneManager('hidePinHelpers')) return false;

        hidePinHelpers(getScene());
        return true;
    };

    // ===== Reference Point Tag Management =====

    /**
     * Update the list of expected reference point tags
     * This is used by PlotEdit.razor to define what pins should be placed
     * @param {string[]} tagList - Array of tag names (e.g., ['X-Left', 'X-Right', 'Y-Bottom', 'Y-Top'])
     */
    window.updateRefPointTexts = function(tagList) {
        if (!Array.isArray(tagList)) {
            console.error('[pinFunctions] updateRefPointTexts: tagList must be an array');
            return false;
        }

        _expectedTags = [...tagList];

        // If pin mode is active, update the current tag to the first unplaced one
        if (isPinModeActive()) {
            const allPins = getAllPins();
            const placedTags = allPins.map(p => p.tag);
            const nextTag = _expectedTags.find(t => !placedTags.includes(t));

            if (nextTag) {
                updatePinModeTag(nextTag);
            }
        }

        console.log(`[pinFunctions] updateRefPointTexts: ${tagList.length} tags set:`, tagList);
        return true;
    };

    /**
     * Get the list of expected reference point tags
     * @returns {string[]} Array of expected tag names
     */
    window.getRefPointTexts = function() {
        return [..._expectedTags];
    };

    /**
     * Get the next unplaced tag from the expected list
     * @returns {string|null} Next tag to place, or null if all placed
     */
    window.getNextUnplacedTag = function() {
        const allPins = getAllPins();
        const placedTags = allPins.map(p => p.tag);
        return _expectedTags.find(t => !placedTags.includes(t)) || null;
    };

    /**
     * Check if all expected pins have been placed
     * @returns {boolean}
     */
    window.areAllPinsPlaced = function() {
        if (_expectedTags.length === 0) return true;

        const allPins = getAllPins();
        const placedTags = allPins.map(p => p.tag);
        return _expectedTags.every(t => placedTags.includes(t));
    };

    /**
     * Get placement progress
     * @returns {Object} { placed: number, total: number, percent: number }
     */
    window.getPinPlacementProgress = function() {
        const allPins = getAllPins();
        const placedTags = allPins.map(p => p.tag);
        const placed = _expectedTags.filter(t => placedTags.includes(t)).length;
        const total = _expectedTags.length;

        return {
            placed,
            total,
            percent: total > 0 ? Math.round((placed / total) * 100) : 0
        };
    };

    // ===== Pin Mode Control (delegates to pinCursor.js) =====

    /**
     * Start pin placement mode
     * @param {string[]} tagList - List of tags to place
     */
    window.startPinPlacement = function(tagList) {
        if (!Array.isArray(tagList) || tagList.length === 0) {
            console.error('[pinFunctions] startPinPlacement: tagList must be a non-empty array');
            return false;
        }

        // Clear existing pins for these tags
        tagList.forEach(tag => {
            removePin(getScene(), tag);
        });

        // Set expected tags
        _expectedTags = [...tagList];

        // Activate pin mode with first tag
        setPinModeActive(true, tagList[0]);

        console.log(`[pinFunctions] startPinPlacement: Started with ${tagList.length} tags`);
        return true;
    };

    /**
     * Stop pin placement mode
     */
    window.stopPinPlacement = function() {
        setPinModeActive(false, null);
        console.log('[pinFunctions] stopPinPlacement: Pin mode deactivated');
        return true;
    };

    /**
     * Advance to the next pin tag (called after a pin is placed)
     * @returns {string|null} The next tag, or null if all placed
     */
    window.advanceToNextPin = function() {
        const nextTag = window.getNextUnplacedTag();

        if (nextTag) {
            updatePinModeTag(nextTag);
            console.log(`[pinFunctions] advanceToNextPin: Now placing '${nextTag}'`);
        } else {
            // All pins placed, deactivate pin mode
            setPinModeActive(false, null);
            console.log('[pinFunctions] advanceToNextPin: All pins placed, mode deactivated');
        }

        return nextTag;
    };

    /**
     * Check if pin placement mode is active
     * @returns {boolean}
     */
    window.isPinModeActive = function() {
        return isPinModeActive();
    };

    /**
     * Get the current pin tag being placed
     * @returns {string|null}
     */
    window.getCurrentPinTag = function() {
        return getCurrentPinTag();
    };

    // ===== Configuration =====

    /**
     * Configure pin appearance
     * @param {Object} config - Configuration object
     */
    window.configurePins = function(config) {
        configurePins(config);
        return true;
    };

    /**
     * Get current pin configuration
     * @returns {Object}
     */
    window.getPinConfig = function() {
        return { ...PIN_CONFIG };
    };
}

// ============================================================================
// EXPORTS
// ============================================================================

export { initPinFunctions };