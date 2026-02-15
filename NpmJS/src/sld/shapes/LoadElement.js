import { dia } from '@joint/core';

export const LoadElement = dia.Element.define('LoadElement', {
    elementType: 'load',
    tag: '',
    clicked: false,
    attrs: {
        body: {
            refWidth: '100%', refHeight: '100%',
            strokeWidth: 1, stroke: '#000000', fill: 'pink'
        },
        label: {
            fill: 'blue', fontWeight: 'bold', textAnchor: 'middle',
            fontSize: 8, refX: 0, refY: 35
        },
        operatingPower: {
            fill: 'blue', textAnchor: 'middle',
            fontSize: 8, refX: 0, refY: 45
        },
        rating: {
            fill: 'black', textAnchor: 'middle',
            fontSize: 8, refX: 0, refY: 55
        }
    }
}, {
    markup: [
        { tagName: 'rect', selector: 'body' },
        { tagName: 'text', selector: 'label' },
        { tagName: 'text', selector: 'operatingPower' },
        { tagName: 'text', selector: 'rating' }
    ]
});
