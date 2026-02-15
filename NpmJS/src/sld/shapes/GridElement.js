import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

export const GridElement = dia.Element.define('CustomGridElement', {
    elementType: 'grid',
    tag: '',
    clicked: false,
    attrs: {
        body: { refWidth: '100%', refHeight: '100%', refX: '50%', refY: '10%' },
        label: { fill: 'blue', fontWeight: 'bold', textAnchor: 'middle', fontSize: 10, refX: '50%', refY: -45 },
        ratedSC: { fill: 'black', textAnchor: 'end', fontSize: 10, refX: '50%', refY: -35, dx: -2 },
        ratedVoltage: { fill: 'black', textAnchor: 'start', fontSize: 10, refX: '50%', refY: -35, dx: 2 },
        busFaultkA: { fill: 'red', fontWeight: 'bold', textAnchor: 'end', fontSize: 10, refX: '50%', refY: 5, dx: -10 },
        operatingVoltage: { fill: 'blue', textAnchor: 'start', fontSize: 10, refX: '50%', refY: 5, dx: 10 }
    },
    ports: {
        groups: {
            'in': {
                position: { ref: 'body', name: 'absolute', args: { x: '50%', y: 10 } },
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
        {
            tagName: 'path',
            selector: 'body',
            attributes: {
                d: 'M 0 0 v -6 h -9 v -18 h 18 v 18 h -9 m -3 0 l -6 -6 l 12 -12 l 6 6 l -12 12 m 6 0 l -12 -12 l 6 -6 l 12 12 l -6 6 m 6 0 l -18 -18 m 18 0 l -18 18 z',
                stroke: 'blue', strokeWidth: 1, fill: 'none'
            }
        },
        { tagName: 'text', selector: 'label' },
        { tagName: 'text', selector: 'ratedSC' },
        { tagName: 'text', selector: 'ratedVoltage' },
        { tagName: 'text', selector: 'busFaultkA' },
        { tagName: 'text', selector: 'operatingVoltage' }
    ]
});
