/**
 * Event handlers for switch elements
 */

import {sldState} from '../state/sldState.js';


export function setupSwitch(paper) {
    
    // Switch double-click: toggle open ↔ close
    paper.on('element:pointerdblclick', (elementView) => {
        
        // exit if the element type is not switch
        if (elementView.model.prop('elementType') !== 'switch') return;

        elementView.model.prop('isOpen', !elementView.model.prop('isOpen'));
        
        let switchesElement = sldState.getSwitchElement();
        let updatedModel = updateSwitchState(elementView.model);
        switchesElement.filter(item => item.prop('tag') !== elementView.model.prop('tag'));
        switchesElement.push(updatedModel);
        sldState.setSwitchElement(switchesElement);

    });

}


/**
 * Toggle switch between open and closed display states
 * @param {Object} switchModel - JointJS switch element model
 * @returns {Object} Updated switch model
 */
export function updateSwitchState(switchModel) {
    const isOpen = switchModel.prop('isOpen');

    if (isOpen) {
        switchModel.attr({
            bladeClosed: { visibility: 'hidden' },
            bladeOpen:   { visibility: 'visible' },
            status:      { text: 'NO', fill: 'red' }
        });
    } else {
        switchModel.attr({
            bladeClosed: { visibility: 'visible' },
            bladeOpen:   { visibility: 'hidden' },
            status:      { text: 'NC', fill: 'green' }
        });
    }

    return switchModel;
}

