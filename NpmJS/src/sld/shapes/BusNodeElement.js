import { dia } from '@joint/core';

export const BusNodeElement = dia.Element.define('BusNodeElement', {
    elementType: 'busNode',
    tag: '',
    clicked: false,
    node: true,
    attrs: {
        body: {
            r: 14,
            cx: '50%',
            cy: '50%',
            strokeWidth: 1,
            stroke: '#000000',
            fill: 'yellow'
        }
    }
}, {
    markup: [
        { tagName: 'circle', selector: 'body' }
    ]
});
