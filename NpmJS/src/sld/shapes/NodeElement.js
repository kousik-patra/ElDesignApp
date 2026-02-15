import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

export const NodeElement = dia.Element.define('CustomNodeElement', {
    elementType: 'node',
    tag: '',
    clicked: false,
    node: true,
    attrs: {
        root: { magnet: false },
        label: { fill: 'blue', fontWeight: 'bold', textAnchor: 'start', fontSize: 8, refX: '0%', refY: -12 },
        ratedSC: { textAnchor: 'start', fontSize: 8, fill: 'brown', refX: '0%', refY: 5 },
        ratedVoltage: { textAnchor: 'start', fontSize: 8, fill: 'blue', refX: 25, refY: 5 },
        busFault: { textAnchor: 'end', fontSize: 8, fill: 'red', refX: '100%', refY: -12 },
        operatingVoltage: { textAnchor: 'end', fontSize: 8, fill: 'blue', refX: '100%', refY: 5 }
    },
    ports: {
        groups: {
            'in': {
                position: { name: 'absolute', args: { x: '50%', y: 0 } },
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
        { tagName: 'text', selector: 'label' },
        { tagName: 'text', selector: 'ratedSC' },
        { tagName: 'text', selector: 'ratedVoltage' },
        { tagName: 'text', selector: 'busFault' },
        { tagName: 'text', selector: 'operatingVoltage' }
    ]
});
