/**
 * Utility functions for SLD operations
 */

/**
 * Safely invoke async methods on DotNet object
 * @param {Object} dotNetObj - DotNet object reference
 * @param {string} methodName - Method name to invoke
 * @param {...any} args - Arguments to pass
 * @returns {Promise<any>}
 */
export async function safeInvokeAsync(dotNetObj, methodName, ...args) {
    if (!dotNetObj) {
        throw new Error('DotNet object reference is null');
    }

    try {
        return await dotNetObj.invokeMethodAsync(methodName, ...args);
    } catch (error) {
        console.error(`Error invoking ${methodName}:`, error);
        throw error;
    }
}

/**
 * Sanitize text for safe HTML rendering
 * @param {*} text - Text to sanitize
 * @returns {string} Sanitized text
 */
export function sanitizeText(text) {
    if (typeof text !== 'string') return String(text ?? '');
    return text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}
