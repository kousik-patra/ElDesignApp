import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

export const BusDuctElement = dia.Element.define('BusDuctElement', {
    elementType: 'busDuct',
    tag: '',
    clicked: false,
    attrs: {
        root: { magnet: false },
        body: {
            refWidth: '100%', refHeight: '100%',
            refX: '0%', refY: '0%', stroke: 'black', strokeWidth: 1
        },
        label: { fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: 15, refY: -25 },
        size: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -15 },
        length: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -5 },
        impedance: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: 5 },
        operatingCurrent: { fill: 'blue', textAnchor: 'right', fontSize: 8, refX: 15, refY: 15 }
    },
    ports: {
        groups: {
            'in': {
                position: { name: 'left', args: { y: '-50%' } },
                label: { position: { name: 'right', args: { x: 10 } }, markup: [{ tagName: 'text', selector: 'label' }] },
                attrs: {
                    portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: 'white', stroke: 'black' },
                    label: { text: 'from', fontSize: 8 }
                },
                markup: [{ tagName: 'circle', selector: 'portBody' }]
            },
            'out': {
                position: { name: 'left', args: { y: '50%' } },
                label: { position: { name: 'right', args: { x: 10 } }, markup: [{ tagName: 'text', selector: 'label' }] },
                attrs: {
                    portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: 'white', stroke: 'black' },
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
            attributes: { d: 'M -3 15 L -5 25 L -3 15 L -3 -15 L -5 -25 L -3 -15 M 0 -25 L 0 25 M 3 15 L 5 25 L 3 15 L 3 -15 L 5 -25 L 3 -15' }
        },
        { tagName: 'text', selector: 'label' },
        { tagName: 'text', selector: 'size' },
        { tagName: 'text', selector: 'length' },
        { tagName: 'text', selector: 'impedance' },
        { tagName: 'text', selector: 'operatingCurrent' }
    ]
});
