import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

/**
 * FuseElement — Fuse for SLD
 *
 * Visual structure (52px tall):
 *      o          ← top port (circle)
 *      |          ← top line (from port outline at y≈4 down to y=14)
 *      x          ← cross mark (thin, y=10 to y=18)
 *     /|  or  /   ← blade (closed = slight incline touching x, open = angled)
 *      •          ← pivot point
 *      |          ← bottom line
 *      o          ← bottom port
 *
 * Toggle with: updateSwitchState(switchModel)
 */
export const FuseElement = dia.Element.define('FuseElement', {
    elementType: 'switch',
    tag: '',
    clicked: false,
    isOpen: false,
    size: { width: 30, height: 52 },
    attrs: {
        // ── Top line: starts from port circle OUTLINE (y≈4), not center ──
        topLine: {
            d: 'M 15 4 V 14',
            stroke: '#000', strokeWidth: 2.5, fill: 'none'
        },
        // ── Cross mark (x) — thinner, centered at y=14 ──
        crossMark: {
            d: 'M 11 10 L 19 18 M 19 10 L 11 18',
            stroke: '#000', strokeWidth: 1.5, fill: 'none'
        },
        // ── Bottom contact: vertical stub from pivot down to port ──
        bottomLine: {
            d: 'M 15 52 V 38',
            stroke: '#000', strokeWidth: 2.5, fill: 'none'
        },
        // ── Pivot point (small dot at bottom contact) ──
        pivot: {
            cx: 15, cy: 38, r: 2,
            fill: '#000', stroke: '#000'
        },
        // ── Blade: closed (slightly inclined, just touching right edge of x) ──
        bladeClosed: {
            d: 'M 15 38 L 18 18',
            stroke: '#000', strokeWidth: 3, fill: 'none',
            visibility: 'visible'
        },
        // ── Blade: open position (angled left — disconnected) ──
        bladeOpen: {
            d: 'M 15 38 L 2 16',
            stroke: '#000', strokeWidth: 3, fill: 'none',
            visibility: 'hidden'
        },
        // ── Tag label (to the right of the element) ──
        label: {
            text: '', fill: 'blue', fontWeight: 'bold',
            textAnchor: 'start', fontSize: 10,
            refX: '50%', refY: '50%', dx: 10, dy: 5,
        },
        // ── Status text: "Open (NO)" / "Closed NC" (to the left) ──
        status: {
            text: 'NC', fill: 'green', fontWeight: 'bold',
            textAnchor: 'end', fontSize: 12,
            refX: '50%', refY: '50%', dx: -10, dy: 2,
        }
    },
    ports: {
        groups: {
            'in': {
                position: { name: 'absolute', args: { x: 15, y: 0 } },
                label: {
                    position: { name: 'right', args: { x: 10, y: -5 } },
                    markup: [{ tagName: 'text', selector: 'label' }]
                },
                attrs: {
                    portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047' },
                    label: { text: '1', fontSize: 8 }
                },
                markup: [{ tagName: 'circle', selector: 'portBody' }]
            },
            'out': {
                position: { name: 'absolute', args: { x: 15, y: 52 } },
                label: {
                    position: { name: 'right', args: { x: 10, y: 5 } },
                    markup: [{ tagName: 'text', selector: 'label' }]
                },
                attrs: {
                    portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047' },
                    label: { text: '2', fontSize: 8 }
                },
                markup: [{ tagName: 'circle', selector: 'portBody' }]
            }
        },
        items: [
            { id: '1', group: 'in' },
            { id: '2', group: 'out' }
        ]
    }
}, {
    markup: [
        { tagName: 'path',   selector: 'topLine' },
        { tagName: 'path',   selector: 'crossMark' },
        { tagName: 'path',   selector: 'bottomLine' },
        { tagName: 'circle', selector: 'pivot' },
        { tagName: 'path',   selector: 'bladeClosed' },
        { tagName: 'path',   selector: 'bladeOpen' },
        { tagName: 'text',   selector: 'label' },
        { tagName: 'text',   selector: 'status' }
    ]
});

