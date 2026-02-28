import {sldState} from "../state/sldState";

import{ drawGridTemplate,
    drawCableTemplate,
    drawBusDuctTemplate,
    drawCapacitorTemplate, 
    drawHeaterTemplate, 
    drawLumpLoadTemplate,
    drawMotorTemplate,
    drawTransformerTemplate
} from "../operations/drawTemplates";
import {safeInvokeAsync} from "../utils/helpers";



// ── Template drag-drop handler ──────────────────────────────────────────
export function setupTemplateDragHandlers(paper) {

    // each template has a clone positioned on the same location.
    // drag event only drags the clone element while the actual element remains at the same place
    // the tag of the dragged element need to be provided with a new tag no and new item shall be added to list
    // new clone to be created and positioned at the corresponding original template place.
    // newly added item must also be added to the Table
    
    paper.on('element:pointerup', async (elementView, evt, x, y) => {

        if (!elementView.model.prop('tag').includes('template')) return;

        let graph = sldState.getGraph();
        const tag = elementView.model.prop('tag');
        const elementType = elementView.model.prop('elementType');

        // ── Canceled drag: dropped within palette area ──────────────
        if (x < 250) {
            // Snap the dragged clone back on top of the original
            const original = graph.getElements().find(
                el => el.prop('tag') === tag && el.id !== elementView.model.id
            );
            if (original) {
                const origPos = original.get('position');
                const origSize = original.get('size');
                elementView.model.position(origPos.x, origPos.y);
                if (origSize) elementView.model.resize(origSize.width, origSize.height);
            }

            console.log('Template Element ' + tag + ' dropped within pallet area ' + x + ',' + y);
            return; // no new item
        }

        let copyTag = elementView.model.prop('tag');
        if (copyTag.includes('template')) copyTag = "";

        try {
            await safeInvokeAsync(
                sldState.getDotNetObjSLD(),
                'SLDComponentAdd',
                elementType, copyTag
            );
        } catch (e) {
            console.error('Error adding SLD components:', e);
        }

        // ── Valid drop: rename + create fresh clone ──────────────────

        let busesElement = sldState.getBusesElement();
        let branchElement = sldState.getBranchElement();
        let loadElement = sldState.getLoadElement();

        console.log('Template Element ' + tag + ' moved to ' + x + ',' + y);

        if (tag === 'templateGridElement') {
            busesElement.push(elementView.model);
            const newTag = `Grid-${busesElement.filter(e =>
                e.prop('elementType') === 'grid').length.toString().padStart(3, '0')}`;
            elementView.model.prop('tag', newTag);
            elementView.model.attr({
                label: {text: newTag}, ratedSC: {text: '0kA'},
                ratedVoltage: {text: '0kV'}, busFaultkA: {text: '0kA'},
                operatingVoltage: {text: '0% ∠0°'}
            });
            drawGridTemplate();


        } else if (tag === 'templateBusElement') {
            busesElement.push(elementView.model);
            const newTag = `Bus-${busesElement.filter(e =>
                e.prop('elementType') === 'bus').length.toString().padStart(3, '0')}`;
            elementView.model.prop('tag', newTag);
            elementView.model.attr({
                label: {text: newTag}, ratedSC: {text: '0kA'},
                ratedVoltage: {text: '0kV'}, busFault: {text: '0kA'},
                operatingVoltage: {text: '0% ∠0°'}
            });
            drawBusDuctTemplate();

        } else if (tag === 'templateCableElement') {
            branchElement.push(elementView.model);
            const newTag = `Cable-${branchElement.filter(e =>
                e.prop('elementType') === 'cable').length.toString().padStart(3, '0')}`;
            elementView.model.prop('tag', newTag);
            elementView.model.attr({
                label: {text: newTag}, size: {text: '?Rx?Cx?'},
                length: {text: '?m, ?-j?Ω/km'}, impedance: {text: 'R:?, X:?'},
                operatingCurrent: {text: '0A ∠0°'}
            });
            drawCableTemplate();

        } else if (tag === 'templateTransformerElement') {
            branchElement.push(elementView.model);
            const newTag = `Transformer-${branchElement.filter(e =>
                e.prop('elementType') === 'transformer').length.toString().padStart(3, '0')}`;
            elementView.model.prop('tag', newTag);
            elementView.model.attr({
                tag: {text: newTag}, voltage: {text: '0kV'},
                kVArating: {text: '0kVA'}, impedance: {text: '0%'},
                loading: {text: '0kW 0kVAR'}
            });
            drawTransformerTemplate();

        } else if (tag === 'templateBusDuctElement') {
            branchElement.push(elementView.model);
            const newTag = `BusDuct-${branchElement.filter(e =>
                e.prop('elementType') === 'busduct').length.toString().padStart(3, '0')}`;
            elementView.model.prop('tag', newTag);
            elementView.model.attr({
                label: {text: newTag}, size: {text: '0A'},
                length: {text: '0m'}, impedance: {text: '0%'},
                operatingCurrent: {text: '0A ∠0°'}
            });
            drawBusDuctTemplate();

        } else if (tag === 'templateCapacitorElement') {
            loadElement.push(elementView.model);
            const newTag = `Capacitor-${loadElement.filter(e =>
                e.prop('elementType') === 'capacitor').length.toString().padStart(3, '0')}`;
            elementView.model.prop('tag', newTag);
            elementView.model.attr({
                label: {text: newTag}, operatingPower: {text: '0kVAR'}, rating: {text: '0kVAR'}
            });
            drawCapacitorTemplate();

        } else if (tag === 'templateMotorElement') {
            loadElement.push(elementView.model);
            const newTag = `Motor-${loadElement.filter(e =>
                e.prop('elementType') === 'motor').length.toString().padStart(3, '0')}`;
            elementView.model.prop('tag', newTag);
            elementView.model.attr({
                label: {text: newTag}, operatingPower: {text: '0kW'}, rating: {text: '0kVA'}
            });
            drawMotorTemplate();

        } else if (tag === 'templateHeaterElement') {
            loadElement.push(elementView.model);
            const newTag = `Heater-${loadElement.filter(e =>
                e.prop('elementType') === 'heater').length.toString().padStart(3, '0')}`;
            elementView.model.prop('tag', newTag);
            elementView.model.attr({
                label: {text: newTag}, operatingPower: {text: '0kW'}, rating: {text: '0kVA'}
            });
            drawHeaterTemplate();

        } else if (tag === 'templateLumpLoadElement') {
            loadElement.push(elementView.model);
            const newTag = `LumpLoad-${loadElement.filter(e =>
                e.prop('elementType') === 'lumpload').length.toString().padStart(3, '0')}`;
            elementView.model.prop('tag', newTag);
            elementView.model.attr({
                label: {text: newTag}, operatingPower: {text: '0kW 0kVAR'}, rating: {text: '0kVA'}
            });
            drawLumpLoadTemplate();
        } else if (elementView.model.prop('tag') === 'templateOtherElement') {
            // provide logic for other element
        } else {
            return;
        }


        // Create a fresh clone on top of the original for the next drag.
        // The dragged element's tag has been renamed above, so the only
        // element still carrying the template tag is the original.
        createFreshClone(tag, graph);


        sldState.setBusesElement(busesElement);
        sldState.setBranchElement(branchElement);
        sldState.setLoadElement(loadElement);
        sldState.setGraph(graph);
    });
}

/**
 * Find the original template element (the one that stays at the palette)
 * and create a clone on top of it for the next drag-drop cycle.
 */
function createFreshClone(templateTag, graph) {
    const original = graph.getElements().find(el => el.prop('tag') === templateTag);
    if (!original) return;

    const freshClone = original.clone();
    freshClone.prop('elementType', original.prop('elementType'));
    freshClone.prop('tag', original.prop('tag'));
    freshClone.clicked = false;
    freshClone.addTo(graph);
}