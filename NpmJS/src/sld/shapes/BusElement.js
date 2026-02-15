import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

export const BusElement = dia.Element.define('CustomBusElement', {
    elementType: 'bus',
    tag: '',
    clicked: false,
    node: false,
    attrs: {
        root: { magnet: false },
        body: { stroke: 'blue', strokeWidth: 5, fill: 'transparent' },
        label: { ref: 'body', fill: 'blue', fontWeight: 'bold', textAnchor: 'start', fontSize: 8, refX: '0%', refY: -12 },
        ratedSC: { ref: 'body', textAnchor: 'start', fontSize: 8, fill: 'brown', refX: '0%', refY: 5 },
        ratedVoltage: { ref: 'body', textAnchor: 'start', fontSize: 8, fill: 'blue', refX: 25, refY: 5 },
        busFault: { ref: 'body', textAnchor: 'end', fontSize: 8, fill: 'red', refX: '100%', refY: -12 },
        operatingVoltage: { ref: 'body', textAnchor: 'end', fontSize: 8, fill: 'blue', refX: '100%', refY: 5 }
    },
    ports: {
        groups: {
            'in': {
                position: { ref: 'body', name: 'absolute', args: { x: '50%', y: 0 } },
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
        items: [{ id: '1', group: 'in' }]
    }
}, {
    markup: [
        { tagName: 'line', selector: 'body' },
        { tagName: 'text', selector: 'label' },
        { tagName: 'text', selector: 'ratedSC' },
        { tagName: 'text', selector: 'ratedVoltage' },
        { tagName: 'text', selector: 'busFault' },
        { tagName: 'text', selector: 'operatingVoltage' }
    ]
});
