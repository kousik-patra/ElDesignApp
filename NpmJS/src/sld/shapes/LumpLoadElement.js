import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

export const LumpLoadElement = dia.Element.define('LumpLoadElement', {
    elementType: 'lumpLoad',
    tag: '',
    clicked: false,
    attrs: {
        root: { magnet: false },
        body1: {
            refRCircumscribed: '25%', refCx: '0%', refCy: '50%',
            strokeWidth: 1, stroke: 'black', fill: 'cyan',
            refX: '0%', refY: '-15%', alphaValue: 0.4
        },
        body2: {
            refWidth: '100%', refHeight: '100%',
            refX: '0%', refY: '0%', stroke: 'black', strokeWidth: 1, fill: 'none'
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
        { tagName: 'circle', selector: 'body1' },
        {
            tagName: 'path', selector: 'body2',
            attributes: { d: 'm 0 15 L -15 15 L 0 38 L 15 15 L 0 15 m 0 0 v -20 Z' }
        },
        { tagName: 'text', selector: 'label' },
        { tagName: 'text', selector: 'operatingPower' },
        { tagName: 'text', selector: 'rating' }
    ]
});
