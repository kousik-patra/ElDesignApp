import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

export const CapacitorElement = dia.Element.define('CapacitorElement', {
    elementType: 'capacitor',
    tag: '',
    clicked: false,
    attrs: {
        root: { magnet: false },
        body: {
            refWidth: '100%', refHeight: '100%',
            refX: '0%', refY: '0%', stroke: 'black', strokeWidth: 2
        },
        label: { fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: 15, refY: -25 },
        operatingPower: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -15 },
        rating: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -5 }
    },
    ports: {
        groups: {
            'in': {
                position: { ref: 'body', name: 'absolute', args: { x: '0%', y: -10 } },
                label: {
                    position: { name: 'right', args: { x: 10, y: -5 } },
                    markup: [{ tagName: 'text', selector: 'label' }]
                },
                attrs: {
                    portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047' },
                    label: { text: '1', fontSize: 8 }
                },
                markup: [{ tagName: 'circle', selector: 'portBody' }]
            }
        },
        items: [{ id: 'portIn', group: 'in' }]
    }
}, {
    markup: [
        {
            tagName: 'path', selector: 'body',
            attributes: { d: 'M -20 15 L 20 15 L -20 15 Z M -21 30 C -10 16 10 16 20 30 m 0 0 C 10 16 -10 16 -21 30 M 0 20 L 0 37 M 0 15 L 0 0 Z' }
        },
        { tagName: 'text', selector: 'label' },
        { tagName: 'text', selector: 'operatingPower' },
        { tagName: 'text', selector: 'rating' }
    ]
});
