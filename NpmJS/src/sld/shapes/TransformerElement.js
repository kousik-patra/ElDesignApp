import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

export const TransformerElement = dia.Element.define('TransformerElement', {
    elementType: 'transformer',
    tag: '',
    clicked: false,
    attrs: {
        root: { magnet: false },
        primary: {
            refRCircumscribed: '71%', refCx: '0%', refCy: '50%',
            strokeWidth: 1, stroke: 'black', fill: 'aquamarine',
            refX: '0%', refY: '-75%', alphaValue: 0.4
        },
        secondary: {
            refRCircumscribed: '71%', refCx: '0%', refCy: '50%',
            strokeWidth: 1, stroke: 'black', fill: 'aquamarine',
            refX: '%', refY: '75%'
        },
        tag: { fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: '150%', refY: -15 },
        voltage: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: '150%', refY: -5 },
        kVArating: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: '150%', refY: 5 },
        impedance: { fill: 'black', textAnchor: 'right', fontSize: 8, refX: '150%', refY: 15 },
        loading: { fill: 'blue', textAnchor: 'right', fontSize: 8, refX: '150%', refY: 25 }
    },
    ports: {
        groups: {
            'in': {
                position: { name: 'left', args: { y: -22 } },
                label: { position: { name: 'right', args: { x: 10 } }, markup: [{ tagName: 'text', selector: 'label' }] },
                attrs: {
                    portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: 'black' },
                    label: { text: 'from', fontSize: 8 }
                },
                markup: [{ tagName: 'circle', selector: 'portBody' }]
            },
            'out': {
                position: { name: 'left', args: { y: 37 } },
                label: { position: { name: 'right', args: { x: 10 } }, markup: [{ tagName: 'text', selector: 'label' }] },
                attrs: {
                    portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: 'black' },
                    label: { text: 'to', fontSize: 8 }
                },
                markup: [{ tagName: 'circle', selector: 'portBody' }]
            }
        },
        items: [{ id: 'portIn', group: 'in' }, { id: 'portOut', group: 'out' }]
    }
}, {
    markup: [
        { tagName: 'circle', selector: 'primary' },
        { tagName: 'circle', selector: 'secondary' },
        { tagName: 'text', selector: 'tag' },
        { tagName: 'text', selector: 'voltage' },
        { tagName: 'text', selector: 'kVArating' },
        { tagName: 'text', selector: 'impedance' },
        { tagName: 'text', selector: 'loading' }
    ]
});
