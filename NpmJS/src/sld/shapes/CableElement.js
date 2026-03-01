import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

export const CableElement = dia.Element.define('CableElement', {
    elementType: 'cableBranch',
    tag: '',
    clicked: false,
    attrs: {
        root: { magnet: false },
        body: { refWidth: '100%', refHeight: '100%', refX: '50%', refY: '10%' },
        label: { fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: 25, refY: -20 },
        size: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: 25, refY: -10 },
        length: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: 25, refY: 0 },
        impedance: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: 25, refY: 10 },
        operatingCurrent: { fill: 'blue', textAnchor: 'right', fontSize: 8, refX: 25, refY: 20 }
    },
    ports: {
        groups: {
            'in': {
                position: { name: 'left', args: { x: 5, y: -25 } },
                label: { position: { name: 'right', args: { x: 10 } }, markup: [{ tagName: 'text', selector: 'label' }] },
                attrs: {
                    portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047' },
                    label: { text: 'from', fontSize: 8 }
                },
                markup: [{ tagName: 'circle', selector: 'portBody' }]
            },
            'out': {
                position: { name: 'left', args: { x: 5, y: 32 } },
                label: { position: { name: 'right', args: { x: 10 } }, markup: [{ tagName: 'text', selector: 'label' }] },
                attrs: {
                    portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: '#E6A502', stroke: '#023047' },
                    label: { text: 'to', fontSize: 8 }
                },
                markup: [{ tagName: 'circle', selector: 'portBody' }]
            }
        },
        items: [{ id: 'portIn', group: 'in' }, { id: 'portOut', group: 'out' }]
    }
}, {
    markup: [
        {
            tagName: 'path', selector: 'body',
            attributes: {
                d: 'M 5 -25 C 3 -20 -3 -20 -5 -25 L -5 20 C -3 15 3 15 5 20 L 5 -25 C 3 -30 -3 -30 -5 -25 M -5 20 C -3 25 3 25 5 20',
                stroke: 'black', strokeWidth: 1, fill: 'orange'
            }
        },
        { tagName: 'text', selector: 'label' },
        { tagName: 'text', selector: 'size' },
        { tagName: 'text', selector: 'length' },
        { tagName: 'text', selector: 'impedance' },
        { tagName: 'text', selector: 'operatingCurrent' }
    ]
});
