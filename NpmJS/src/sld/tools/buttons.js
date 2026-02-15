/**
 * JointJS tools and buttons for SLD elements
 */
import { elementTools, linkTools } from '@joint/core';
import { safeInvokeAsync } from '../utils/helpers.js';
import { sldState } from '../state/sldState.js';
import { removeLink, validateLinkFromServer } from '../links/linkOperations.js';

/**
 * Property button for elements
 */
export const propertyButton = new elementTools.Button({
    focusOpacity: 0.5,
    x: '0%',
    y: '50%',
    offset: { x: 10, y: 0 },
    action: function (evt) {
        const dotNetObjSLD = sldState.getDotNetObjSLD();
        safeInvokeAsync(dotNetObjSLD, 'PropertyUpdate', this.model.prop('tag'), this.model.prop('elementType'))
            .then(r => console.log(r));
    },
    markup: [{
        tagName: 'circle',
        selector: 'button',
        attributes: {
            'r': 7,
            'fill': '#001DFF',
            'cursor': 'pointer'
        }
    }, {
        tagName: 'path',
        selector: 'icon',
        attributes: {
            'd': 'M -2 4 2 4 M 0 4 0 -4 M 0 -4 2 -4 C 3 -4 3 -1 0 -1',
            'fill': 'none',
            'stroke': '#FFFFFF',
            'stroke-width': 2,
            'pointer-events': 'none'
        }
    }]
});

/**
 * Info button for links
 */
export const infoButton = new linkTools.Button({
    markup: [{
        tagName: 'circle',
        selector: 'button',
        attributes: { 'r': 7, 'fill': '#001DFF', 'cursor': 'pointer' }
    }, {
        tagName: 'path',
        selector: 'icon',
        attributes: {
            'd': 'M -2 4 2 4 M 0 3 0 0 M -2 -1 1 -1 M -1 -4 1 -4',
            'fill': 'none',
            'stroke': '#FFFFFF',
            'stroke-width': 2,
            'pointer-events': 'none'
        }
    }],
    distance: '50%',
    offset: 0,
    action: function (evt) {
        const link = this.model;
        const graph = sldState.graph;
        // Get source and target elements
        const sourceId = link.prop('source/id');
        const targetId = link.prop('target/id');
        const sourcePort = link.prop('source/port') ?? '';
        const targetPort = link.prop('target/port') ?? '';

        const sourceElement = graph.getCell(sourceId);
        const targetElement = graph.getCell(targetId);

        const sourceTag = sourceElement ? sourceElement.prop('tag') : sourceId;
        const targetTag = targetElement ? targetElement.prop('tag') : targetId;

        console.log(
            'Link Info:\n' +
            `  View ID: ${this.id}\n` +
            `  Model ID: ${link.id}\n` +
            `  From: ${sourceTag} (port: ${sourcePort})\n` +
            `  To: ${targetTag} (port: ${targetPort})`
        );
    }
});

/**
 * Remove button for links
 */
export const removeButton = new linkTools.Button({
    markup: [{
        tagName: 'circle',
        selector: 'button',
        attributes: { 'r': 7, 'fill': 'red', 'cursor': 'pointer' }
    }, {
        tagName: 'path',
        selector: 'icon',
        attributes: {
            'd': 'M -4 4 L 4 -4 M 4 4 L -4 -4',
            'fill': 'none',
            'stroke': '#FFFFFF',
            'stroke-width': 2,
            'pointer-events': 'none'
        }
    }],
    distance: '25%',
    offset: 0,
    action: function (evt) {
        const graph = sldState.getGraph();
        const link = graph.getCell(this.model.id);
        if (link && link.isLink()) {
            removeLink(link);
        } else {
            console.log(`Link id '${this.id}' of link model '${this.model.id}' not available.`);
        }
    }
});

/**
 * Validate button for links
 */
export const validateButton = new linkTools.Button({
    markup: [{
        tagName: 'circle',
        selector: 'button',
        attributes: { 'r': 7, 'fill': 'green', 'cursor': 'pointer' }
    }, {
        tagName: 'path',
        selector: 'icon',
        attributes: {
            'd': 'M -2.5 -0.2 l 0.23 -0.24 c 0.34 0.1 1 0.35 1.7 0.72 c 0.6 -0.8 2 -1.9 2.7 -2.4 l 0.2 0.2 l -2.8 3.78 l -2 -2 z',
            'fill': 'none',
            'stroke': '#FFFFFF',
            'stroke-width': 2,
            'pointer-events': 'none'
        }
    }],
    distance: '75%',
    offset: 0,
    action: function (evt) {
        const graph = sldState.getGraph();
        const link = graph.getCell(this.model.id);
        if (link && link.isLink()) {
            validateLinkFromServer(link);
        } else {
            console.log(`Link id '${this.id}' of link model '${this.model.id}' not available.`);
        }
    }
});
