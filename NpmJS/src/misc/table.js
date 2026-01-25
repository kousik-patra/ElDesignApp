/**
 * Table Component JavaScript Module
 * Handles dynamic sizing and other DOM operations for the Table Blazor component
 *
 * @module misc/table
 */

// Store references for cleanup
const tableInstances = new Map();

/**
 * Default configuration
 */
const defaultConfig = {
    widthPercentage: 0.8,
    minWidth: 300,
    maxWidth: 0
};

/**
 * Sets the width of an element based on window size
 * @param {string} elementId - The ID of the element to resize
 * @param {object} config - Configuration options
 * @returns {boolean} - True if element was found and resized
 */
function setDynamicWidth(elementId, config = {}) {
    const element = document.getElementById(elementId);

    // Silently return if element not found - it may have been disposed
    if (!element) {
        return false;
    }

    const settings = { ...defaultConfig, ...config };
    const windowWidth = window.innerWidth;
    let newWidth = windowWidth * settings.widthPercentage;

    // Apply min/max constraints
    if (settings.minWidth > 0) {
        newWidth = Math.max(newWidth, settings.minWidth);
    }
    if (settings.maxWidth > 0) {
        newWidth = Math.min(newWidth, settings.maxWidth);
    }

    element.style.width = `${newWidth}px`;
    return true;
}

/**
 * Initializes dynamic resizing for a table container
 * @param {string} elementId - The ID of the container element
 * @param {object} config - Configuration options
 * @returns {boolean} - True if initialization was successful
 */
function initializeDynamicResize(elementId, config = {}) {
    // Check if already initialized
    if (tableInstances.has(elementId)) {
        console.debug(`[Table.js] Element '${elementId}' already initialized`);
        return true;
    }

    const element = document.getElementById(elementId);
    if (!element) {
        console.warn(`[Table.js] Element '${elementId}' not found during initialization`);
        return false;
    }

    const settings = { ...defaultConfig, ...config, elementId };

    // Create bound handlers for this instance
    const resizeHandler = () => {
        // Only resize if element still exists and is in DOM
        if (document.getElementById(elementId)) {
            setDynamicWidth(elementId, settings);
        } else {
            // Element removed from DOM - auto cleanup
            cleanupInstance(elementId);
        }
    };

    const clickHandler = () => {
        // Only resize if element still exists and is in DOM
        if (document.getElementById(elementId)) {
            setDynamicWidth(elementId, settings);
        } else {
            // Element removed from DOM - auto cleanup
            cleanupInstance(elementId);
        }
    };

    // Store instance data for cleanup
    tableInstances.set(elementId, {
        config: settings,
        handlers: {
            resize: resizeHandler,
            click: clickHandler
        }
    });

    // Add event listeners
    window.addEventListener('resize', resizeHandler);
    document.addEventListener('click', clickHandler);

    // Initial sizing
    setDynamicWidth(elementId, settings);

    console.debug(`[Table.js] Initialized dynamic resize for '${elementId}'`);
    return true;
}

/**
 * Internal cleanup function - removes listeners and map entry
 * @param {string} elementId
 */
function cleanupInstance(elementId) {
    const instance = tableInstances.get(elementId);
    if (!instance) {
        return;
    }

    // Remove event listeners
    if (instance.handlers) {
        window.removeEventListener('resize', instance.handlers.resize);
        document.removeEventListener('click', instance.handlers.click);
    }

    // Remove from map
    tableInstances.delete(elementId);
    console.debug(`[Table.js] Cleaned up instance for '${elementId}'`);
}

/**
 * Disposes of event listeners for a table instance
 * @param {string} elementId - The ID of the container element
 * @returns {boolean} - True if disposal was successful
 */
function dispose(elementId) {
    if (!tableInstances.has(elementId)) {
        // Already disposed or never initialized - that's fine
        return true;
    }

    cleanupInstance(elementId);
    console.debug(`[Table.js] Disposed instance for '${elementId}'`);
    return true;
}

/**
 * Disposes of all table instances
 */
function disposeAll() {
    const elementIds = Array.from(tableInstances.keys());
    elementIds.forEach(id => cleanupInstance(id));
    console.debug(`[Table.js] Disposed all instances (${elementIds.length} total)`);
}

/**
 * Manually triggers a resize for a specific element
 * @param {string} elementId - The ID of the container element
 * @returns {boolean} - True if resize was triggered
 */
function triggerResize(elementId) {
    const instance = tableInstances.get(elementId);
    if (!instance) {
        return false;
    }

    return setDynamicWidth(elementId, instance.config);
}

/**
 * Gets the current configuration for an element
 * @param {string} elementId - The ID of the container element
 * @returns {object|null} - Configuration or null if not found
 */
function getConfig(elementId) {
    const instance = tableInstances.get(elementId);
    return instance ? { ...instance.config } : null;
}

/**
 * Updates configuration for an existing instance
 * @param {string} elementId - The ID of the container element
 * @param {object} newConfig - New configuration values
 * @returns {boolean} - True if update was successful
 */
function updateConfig(elementId, newConfig) {
    const instance = tableInstances.get(elementId);
    if (!instance) {
        return false;
    }

    instance.config = { ...instance.config, ...newConfig };
    setDynamicWidth(elementId, instance.config);
    return true;
}

/**
 * Scrolls a cell into view within the table
 * @param {string} tableId - The ID of the table element
 * @param {number} rowIndex - The row index
 * @param {number} colIndex - The column index
 * @returns {boolean} - True if scroll was successful
 */
function scrollCellIntoView(tableId, rowIndex, colIndex) {
    const table = document.getElementById(tableId);
    if (!table) {
        return false;
    }

    const row = table.querySelector(`tbody tr:nth-child(${rowIndex + 1})`);
    if (!row) {
        return false;
    }

    const cell = row.querySelector(`td:nth-child(${colIndex + 1}), th:nth-child(${colIndex + 1})`);
    if (!cell) {
        return false;
    }

    cell.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
    return true;
}

/**
 * Focuses an input element within a cell (for editing)
 * @param {string} cellSelector - CSS selector for the cell
 * @returns {boolean} - True if focus was successful
 */
function focusCellInput(cellSelector) {
    const cell = document.querySelector(cellSelector);
    if (!cell) {
        return false;
    }

    const input = cell.querySelector('input, select, textarea');
    if (!input) {
        return false;
    }

    input.focus();
    if (input.select && input.type !== 'select-one') {
        input.select();
    }
    return true;
}

/**
 * Downloads data as a file (used for Excel export)
 * @param {string} filename - The name of the file
 * @param {string} base64Data - Base64 encoded file content
 * @param {string} mimeType - MIME type of the file
 */
function saveAsFile(filename, base64Data, mimeType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet') {
    const link = document.createElement('a');
    link.download = filename;
    link.href = `data:${mimeType};base64,${base64Data}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

/**
 * Gets the current scroll position of the table container
 * @param {string} elementId - The ID of the scrollable container
 * @returns {object|null}
 */
function getScrollPosition(elementId) {
    const element = document.getElementById(elementId);
    if (!element) return null;

    return {
        scrollTop: element.scrollTop,
        scrollLeft: element.scrollLeft
    };
}

/**
 * Sets the scroll position of the table container
 * @param {string} elementId - The ID of the scrollable container
 * @param {number} scrollTop - Vertical scroll position
 * @param {number} scrollLeft - Horizontal scroll position
 * @returns {boolean}
 */
function setScrollPosition(elementId, scrollTop, scrollLeft) {
    const element = document.getElementById(elementId);
    if (!element) return false;

    element.scrollTop = scrollTop;
    element.scrollLeft = scrollLeft;
    return true;
}

/**
 * Gets count of active instances (for debugging)
 * @returns {number}
 */
function getActiveInstanceCount() {
    return tableInstances.size;
}

/**
 * Gets list of active instance IDs (for debugging)
 * @returns {string[]}
 */
function getActiveInstanceIds() {
    return Array.from(tableInstances.keys());
}

// Export for webpack/ES modules
export {
    initializeDynamicResize,
    dispose,
    disposeAll,
    triggerResize,
    setDynamicWidth,
    getConfig,
    updateConfig,
    scrollCellIntoView,
    focusCellInput,
    saveAsFile,
    getScrollPosition,
    setScrollPosition,
    getActiveInstanceCount,
    getActiveInstanceIds
};

// Create namespace object for window attachment
const TableModule = {
    initializeDynamicResize,
    dispose,
    disposeAll,
    triggerResize,
    setDynamicWidth,
    getConfig,
    updateConfig,
    scrollCellIntoView,
    focusCellInput,
    saveAsFile,
    getScrollPosition,
    setScrollPosition,
    getActiveInstanceCount,
    getActiveInstanceIds
};

export default TableModule;