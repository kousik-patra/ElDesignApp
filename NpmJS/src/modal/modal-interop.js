let currentDotNetHelper;
let currentModalDialogElement; // Store the element reference passed from Blazor

// We don't need setupDraggableModal anymore, as positioning is done via drag.
// If you want initial centering, it's better done in CSS or after rendering using modalDialogElementRef.

export const getModalDialogRect = (element) => { // Now accepts an ElementReference
    if (element) {
        return element.getBoundingClientRect();
    }
    return null;
};

export const startModalDrag = (dotNetHelper, element) => { // Now accepts ElementReference
    currentDotNetHelper = dotNetHelper;
    currentModalDialogElement = element; // Store the passed element

    if (currentModalDialogElement) {
        // Ensure the modal dialog is position: absolute for dragging
        currentModalDialogElement.style.position = 'absolute';
        currentModalDialogElement.style.margin = '0'; // Remove default margin (if Bootstrap is applying it)

        // Set initial position if it hasn't been set by CSS transform
        // Or if you want it centered initially, you can set it here based on window size
        // This is more complex to do reliably in JS without knowing viewport,
        // so rely on initial CSS transform if possible.

        document.onmousemove = (e) => {
            currentDotNetHelper.invokeMethodAsync('HandleMouseMove', e.clientX, e.clientY);
        };
        document.onmouseup = () => {
            currentDotNetHelper.invokeMethodAsync('HandleMouseUp');
            stopModalDrag();
        };
    }
};

export const stopModalDrag = () => {
    document.onmousemove = null;
    document.onmouseup = null;
    currentDotNetHelper = null;
    currentModalDialogElement = null; // Clear the stored element
};

// getModalDialogElement is now removed as it's no longer needed

export const setModalPosition = (x, y) => { // No longer accepts element, uses stored one
    if (currentModalDialogElement) {
        currentModalDialogElement.style.left = `${x}px`;
        currentModalDialogElement.style.top = `${y}px`;
    }
};

export const focusElement = (element) => {
    if (element) {
        element.focus();
    }
};