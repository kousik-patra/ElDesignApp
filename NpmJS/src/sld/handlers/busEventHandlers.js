/**
 * Event handlers for busbar elements
 */

import {sldState} from '../state/sldState.js';


// ── Bus end-drag interaction ─────────────────────────────────────────────

export function setupPointerOnBusBar(paper) {

    let elementMoveInteractive = sldState.getElementMoveInteractive();

    let end;
    let oposx;
    let owidth;
    let cposx;
    let cwidth;

    paper.on('element:pointerdown', (elementView, _event, x) => {
        
        // return if the item type is not bus
        if (elementView.model.prop('elementType') !== 'bus') return;

        // disable bus element movement if clicked on either ends.
        // allow movement only if clicked in the middle of the bus bar
        // default elementMove : interactive is true, allow movement on pointermove
        
            // check the location of the click: end or middle of the bus line
            const pos = elementView.model.prop('position');
            const width = elementView.model.prop('size/width');

            oposx = elementView.model.prop('position/x');
            owidth = elementView.model.prop('size/width');

            end = x - pos.x < 10 ? 'left' : pos.x + width - x < 10 ? 'right' : 'middle';
            
            elementMoveInteractive = end === 'middle';
            sldState.setElementMoveInteractive(elementMoveInteractive);

            console.log(`Bus '${elementView.model.prop('tag')}', position: (${pos.x},${pos.y}), width:${width}, ` +
                `clicked at '${end}', interaction: ${elementMoveInteractive}`);


        paper.setInteractivity({elementMove: elementMoveInteractive});
    });

    // bus left/right end movement
    paper.on('element:pointermove', (elementView, _event, x) => {
        if (elementView.model.prop('elementType') !== 'bus' || !end || end === 'middle') return;


        setTimeout(() => {
            if (end === 'left') {
                cposx = Math.min(x, oposx + owidth - 50);
                cwidth = oposx + owidth - cposx;
            } else if (end === 'right') {
                cposx = oposx;
                cwidth = Math.max(x, oposx + 50) - oposx;
            }
            elementView.model.prop('position/x', cposx);
            elementView.model.prop('size/width', cwidth);
            elementView.model.prop('attrs/body/x2', cwidth);

            // Persist to sldComponentsJS
            let sldComponents = sldState.getSLDComponents() ? sldState.getSLDComponents() : [];
            sldComponents = sldComponents.filter(item =>
                !(item.Tag === elementView.model.prop('tag') && item.Type === 'bus' && item.SLD === 'key'));
            sldComponents.push({
                Type: 'bus',
                Tag: elementView.model.prop('tag'),
                SLD: 'key',
                PropertyJSON: JSON.stringify({
                    //node: elementView.model.node,
                    position: elementView.model.attributes.position,
                    length: cwidth
                })
            });
            sldState.setSLDComponents(sldComponents);

            // Update enclosing switchboard if any
            let switchboards = sldState.getSwitchboards();
            let busTag = elementView.model.prop('tag');
            const swbd = switchboards.find(item => JSON.parse(item[1]).includes(busTag));

            if (swbd) {
                let busesElement = sldState.getBusesElement();
                let swbdElement = sldState.getSwbdElement();
                let swbdTag = swbd[0];
                let updatedSwbdModel = updateSwbdPositionSizeByBus(busTag, busesElement,
                    switchboards, swbdElement, 30, 20, 30, 20);

                swbdElement.filter(item => item.prop('tag') === swbdTag);
                swbdElement.push(updatedSwbdModel);
                sldState.setSwbdElement(swbdElement);
            }
        }, 10);

    });

    paper.on('element:pointerup', (elementView) => {
        if (elementView.model.prop('elementType') !== 'bus') return;
        
        // reset interactive mode
        paper.setInteractivity({elementMove: true});
        sldState.setElementMoveInteractive(true);

    });

    // Bus double-click: toggle node ↔ bus
    paper.on('element:pointerdblclick', (elementView) => {
        if (elementView.model.prop('elementType') !== 'bus') return;
        const ports = elementView.model.getGroupPorts('in');
        if (ports.length > 2) return;

        // two ports are not catering to the top or bottom links
        // (i.e., their positions are different, then this shall remain as bus)
        if (ports.length === 2) {
            const b = elementView.model.getPortsPositions('in');
            if (b[1].x !== b[2].x) return;
        }

        // also no node if the bus is part of a switchboard which has more than one buses
        let switchboards = sldState.getSwitchboards();
        const swbd = switchboards.find(item => JSON.parse(item[1]).includes(elementView.model.prop('tag')));
        if (swbd && JSON.parse(swbd[1]).length > 1) return;

        elementView.model.node = !elementView.model.node;
        let busesElement = sldState.getBusesElement();
        let updatedBusModel = updateNodeOrBus(elementView.model);
        busesElement.filter(item => item.prop('tag') !== elementView.model.prop('tag'));
        busesElement.push(updatedBusModel);
        sldState.setBusesElement(busesElement);

    });

}


function updateSwbdPositionSizeByBus(busTag, busesElement, switchboards, swbdElement, dx1, dx2, dy1, dy2) {
    let swbd = switchboards.find(item => JSON.parse(item[1]).includes(busTag));
    let swbdModel = swbdElement.find(item => item.prop('tag') === swbd[0]);
    let busTags = JSON.parse(swbd[1]);
    // var busSections = JSON.parse(swbd[2]); not used
    // find the enclosing area
    let x1 = Number.MAX_SAFE_INTEGER;
    let y1 = Number.MAX_SAFE_INTEGER;
    let x2 = 0;
    let y2 = 0;
    busTags.forEach(bustag => {
        const bbox = busesElement.find(item => item.prop('tag') === bustag).getBBox();
        x1 = Math.min(x1, bbox.x);
        y1 = Math.min(y1, bbox.y);
        x2 = Math.max(x2, bbox.x + bbox.width);
        y2 = Math.max(y2, bbox.y + bbox.height);
    });
    x2 += dx2;
    y2 += dy2;
    x1 -= dx1;
    y1 -= dy1;
    swbdModel.position(x1, y1);
    swbdModel.resize(x2 - x1, y2 - y1);
    return swbdModel;
}


/**
 * Update node or bus display mode
 * @param {Object} busModel - JointJS bus model
 * @returns {Object} Updated bus model
 */
export function updateNodeOrBus(busModel) {
    if (busModel.node) {
        // Node mode - hide bus line and parameters
        busModel.attr('label/textAnchor', 'end');
        busModel.attr('label/refX', '50%');
        busModel.attr('label/dx', -10);
        busModel.attr('label/refY', 0);
        busModel.attr('body/visibility', 'hidden');
        busModel.attr('ratedSC/visibility', 'hidden');
        busModel.attr('ratedVoltage/visibility', 'hidden');
        busModel.attr('busFault/visibility', 'hidden');
        busModel.attr('operatingVoltage/visibility', 'hidden');
    } else {
        // Bus mode - show bus line and parameters
        busModel.attr('label/textAnchor', 'start');
        busModel.attr('label/refY', -12);
        busModel.attr('body/visibility', 'visible');
        busModel.attr('ratedSC/visibility', 'visible');
        busModel.attr('ratedVoltage/visibility', 'visible');
        busModel.attr('busFault/visibility', 'visible');
        busModel.attr('operatingVoltage/visibility', 'visible');
    }
    return busModel;
}



