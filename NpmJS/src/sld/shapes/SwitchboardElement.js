import { dia } from '@joint/core';

export const SwitchboardElement = dia.Element.define('SwitchboardElement', {
    elementType: 'switchBoard',
    tag: '',
    clicked: false,
    attrs: {
        root: { magnet: false },
        body: {
            refWidth: '100%', refHeight: '100%', refCx: '0%', refCy: '100%',
            strokeWidth: 1, strokeDasharray: '5,5', stroke: 'brown', fill: 'none',
            refX: '0%', refY: '0%', alphaValue: 0.4
        },
        tag: { ref: 'body', fill: 'blue', fontWeight: 'bold', textAnchor: 'start', fontSize: 8, refX: 5, refY: 5 },
        '.': { 'pointer-events': 'none' }
    }
}, {
    markup: [
        { tagName: 'rect', selector: 'body' },
        { tagName: 'text', selector: 'tag' }
    ]
});
