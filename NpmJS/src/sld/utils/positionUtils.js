/**
 * Position and overlap utilities
 */
import { CONFIG } from '../config/constants.js';
import {updateNodeOrBus} from "./busPortDistribution";

/**
 * Check if an element overlaps with any other elements
 * @param {Object} element - JointJS element
 * @param {Object} graph - JointJS graph
 * @returns {boolean} True if overlapping
 */
export function isOverlapping(element, graph) {
    const bbox = element.getBBox();
    return graph.getCells().some(cell => {
        if (cell === element) return false;
        const cellBBox = cell.getBBox();
        return bbox.intersect(cellBBox);
    });
}

/**
 * Find the nearest empty position for an element
 * @param {Object} element - JointJS element
 * @param {Object} graph - JointJS graph
 * @param paper
 * @param {number} spacing - Spacing between elements (default: CONFIG.OVERLAP_SPACING)
 * @returns {Object} New position {x, y}
 */
export function findNearestEmptyPosition(element, graph, paper, spacing = CONFIG.OVERLAP_SPACING) {
    let position = element.get('position');
    let newPosition = { ...position };

    while (isOverlapping(element, graph)) {
        newPosition.x += spacing;
        if (newPosition.x > paper.options.width) {
            newPosition.x = 0;
            newPosition.y += spacing;
        }

        // Ensure newPosition is within paper bounds
        // and away from template (x>100) and top margin 50
        newPosition.x = Math.max(100, Math.min(newPosition.x, paper.options.width - element.getBBox().width));
        newPosition.y = Math.max(50, Math.min(newPosition.y, paper.options.height - element.getBBox().height));

        element.set('position', newPosition);
    }

    return newPosition;
}

/**
 * Update item position from server data
 * @param {Object} cell - JointJS cell
 * @param {Array} sldComponents - SLD components data
 * @returns {Object} Updated cell
 */
export function updateItemPosition(cell, sldComponents) {
    let serverData = sldComponents.find(item =>
        cell && item.Tag === cell.prop('tag')
    );

    if (serverData) {
        let newPositionText = serverData.PropertyJSON;
        let newPosition = JSON.parse(newPositionText);
        if (newPosition) cell.prop('position', newPosition);
    }
    
    return cell;
}

/**
 * Update position and length for bus elements
 * @param {Object} busModel - JointJS bus model
 * @param {Array} sldComponents - SLD components data
 * @returns {Object} Updated bus model
 */
export function updatePositionLength(busModel, sldComponents) {
    let serverData = sldComponents.find(item =>
        busModel && item.Tag === busModel.prop('tag')
    );

    if (serverData) {
        if (busModel.prop('elementType') === "grid") {
            busModel.prop('position', JSON.parse(serverData.PropertyJSON));
        } else {
            let newPositionLengthText = serverData.PropertyJSON;
            let newPositionLength = JSON.parse(newPositionLengthText);

            if (newPositionLength.position) {
                busModel.prop('position', newPositionLength.position);
            }
            
            if (newPositionLength.length && busModel.prop('elementType') === "bus") {
                busModel.prop('size/width', newPositionLength.length);
                busModel.prop('attrs/body/x2', newPositionLength.length);
            }

            // Check if it's node or not
            if (newPositionLength.node && busModel.prop('elementType') === "bus") {
                busModel.node = !!newPositionLength.node;
                busModel = updateNodeOrBus(busModel);
            }
        }
    }
    
    return busModel;
}


