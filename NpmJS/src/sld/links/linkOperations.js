// sld/links/linkOperations.js
/**
 * Link operations and management
 */
import { safeInvokeAsync } from '../utils/helpers.js';
import { sldState } from '../state/sldState.js';

/**
 * Get link tag from link model
 * Format: "HIGHER_TAG:portIndex - LOWER_TAG:portIndex"
 * Tags are alphabetically sorted for consistency
 * @param {Object} link - JointJS link model
 * @returns {string} Link tag
 */
export function getLinkTag(link) {
    const graph = sldState.graph;

    let source = graph.getCell(link.get('source'));
    let target = graph.getCell(link.get('target'));
    let sourcePortId = link.prop('source/port');
    let targetPortId = link.prop('target/port');
    let sourcePortIndex = source.getPortIndex(sourcePortId);
    let targetPortIndex = target.getPortIndex(targetPortId);

    return (source.prop('tag') > target.prop('tag')) ?
        `${source.prop('tag')}:${sourcePortIndex} - ${target.prop('tag')}:${targetPortIndex}` :
        `${target.prop('tag')}:${targetPortIndex} - ${source.prop('tag')}:${sourcePortIndex}`;
}

/**
 * Get link data for server communication
 * @param {Object} link - JointJS link model
 * @returns {Object} Link data object
 */
export function getLinkData(link) {
    const graph = sldState.graph;

    const sPort = link.prop('source/port');
    const tPort = link.prop('target/port');

    // Use find() so that a missing element returns undefined rather than
    // throwing, giving us a cleaner error message below.
    const sM = graph.getElements().find(el => el.id === link.prop('source').id);
    const tM = graph.getElements().find(el => el.id === link.prop('target').id);

    if (!sM || !tM) {
        console.error(
            'getLinkData: could not find source or target element in graph.',
            'source id:', link.prop('source').id,
            'target id:', link.prop('target').id
        );
    }

    return {
        sourceTag:  sM ? sM.prop('tag') : '',
        targetTag:  tM ? tM.prop('tag') : '',
        sourcePort: sPort ?? '',
        targetPort: tPort ?? ''
    };
}

/**
 * Validate link from server
 * @param {Object} link - JointJS link model
 * @returns {Promise<Object>} Validated link
 */
export async function validateLinkFromServer(link) {
    var linkData = getLinkData(link);
    const dotNetObjSLD = sldState.getDotNetObjSLD();

    try {
        const success = await safeInvokeAsync(
            dotNetObjSLD,
            'SLDValidateLink',
            JSON.stringify(linkData)
        );

        link.set('valid', success);

        if (!success) {
            console.warn(`Link validation failed: ${linkData.sourceTag} → ${linkData.targetTag}`);
        }
    } catch (e) {
        console.error(`Validation error:`, e);
        link.set('valid', false);
    }

    return link;
}

/**
 * Remove link from graph and server
 * @param {Object} link - JointJS link model
 * @returns {Promise<void>}
 */
export async function removeLink(link) {
    var linkData = getLinkData(link);
    const dotNetObjSLD = sldState.getDotNetObjSLD();

    var success = await safeInvokeAsync(
        dotNetObjSLD,
        'SLDRemoveLink',
        JSON.stringify(linkData)
    );
    
    if (success) {
        link.remove();
        console.log(`Removal success of the Link between '${linkData.sourceTag} and ${linkData.targetTag}' : ${success}.`);
    }
}

/**
 * Update link vertices from server data
 * Matches links by source/target tags, not by link.prop('tag') (which may not be set yet)
 * @param {Object} link - JointJS link model
 * @param {Array} sldComponents - SLD components data
 * @param {Object} graph - JointJS graph
 * @returns {Object} Updated link
 */
export function updateLinkVertices(link, sldComponents, graph) {
    // Get source and target element tags
    const sourceId = link.prop('source/id');
    const targetId = link.prop('target/id');

    if (!sourceId || !targetId) return link;

    const sourceElement = graph.getCell(sourceId);
    const targetElement = graph.getCell(targetId);

    if (!sourceElement || !targetElement) return link;

    const sourceTag = sourceElement.prop('tag');
    const targetTag = targetElement.prop('tag');

    // Find the saved link data by matching source/target tags
    // The Tag field in database is in format "TAG1:port - TAG2:port"
    // So we check if both tags appear in the saved Tag
    const existingLinkData = sldComponents.find(item => {
        if (item.Type !== 'link') return false;
        if (!item.Tag) return false;

        // Check if this Tag contains both our source and target tags
        return item.Tag.includes(sourceTag) && item.Tag.includes(targetTag);
    });

    if (existingLinkData && existingLinkData.PropertyJSON) {
        try {
            const vertices = JSON.parse(existingLinkData.PropertyJSON);
            if (vertices && Array.isArray(vertices) && vertices.length > 0) {
                link.vertices(vertices);
            }
        } catch (e) {
            console.warn('Failed to parse vertices for link:', existingLinkData.Tag, e);
        }
    }

    return link;
}

// Function to check if a port has links
export function hasLink(element, portName) {
    // Get all links (edges) connected to the element
    const graph = sldState.graph;
    const links = graph.getLinks();

    // Check if any link uses the specified port as source or target
    return links.some(link =>
        link.get('source').id === element.id && link.get('source').port === portName ||
        link.get('target').id === element.id && link.get('target').port === portName
    );
}


// Function to get the port ID from an element and a magnet
export function getPortId(element, magnet) {
    // Iterate over all ports of the element
    for (let portName in element.getPorts()) {
        const port = element.getPort(portName);
        // Check if the magnet matches the port element
        if (port && port.attrs.body.el === magnet) {
            return portName; // Return the port ID (name)
        }
    }
    return null; // Port not found
}
