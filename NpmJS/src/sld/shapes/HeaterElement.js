import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

// NOTE: Type name is 'Heaterlement' (missing capital E) — this matches the original
// mySLD.js exactly. Do NOT change it or saved diagram JSON will break.
export const HeaterElement = dia.Element.define('Heaterlement', {
    elementType: 'heater',
    tag: '',
    clicked: false,
    attrs: {
        root: { magnet: false },
        body: {
            refWidth: '100%', refHeight: '100%',
            refX: '0%', refY: '0%',
            stroke: 'black', strokeWidth: 1, fill: 'cornsilk'
        },
        label: { fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: 15, refY: -25 },
        operatingPower: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -15 },
        rating: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -5 }
    },
    ports: {
        groups: {
            'in': {
                position: { ref: 'body', name: 'absolute', args: { x: '0%', y: -10 } },
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
            attributes: { d: 'm -15 5 L 15 5 L 15 55 L -15 55 L -15 5 m 0 10 L 15 15 M 15 25 L -15 25 M -15 35 L 15 35 M 15 45 L -15 45 m 15 -40 v -14 Z' }
        },
        { tagName: 'text', selector: 'label' },
        { tagName: 'text', selector: 'operatingPower' },
        { tagName: 'text', selector: 'rating' }
    ]
});
