/**
 * SLD update operations
 */
import { safeInvokeAsync } from '../utils/helpers.js';
import { sldState } from '../state/sldState.js';
import { CONFIG } from '../config/constants.js';
import {
    updateBus,
    updateTransformer,
    updateCable,
    updateBusDuct,
    updateMotor,
    updateHeater,
    updateCapacitor,
    updateLumpLoad,
    updateSwitch
} from '../components/updaters';

/**
 * Update SLD with latest component data
 * @returns {Promise<void>}
 */
export async function updateSLD() {
    console.log("Client side Update SLD");
    
    const graph = sldState.getGraph();
    const dotNetObjSLD = sldState.getDotNetObjSLD();
    const sldComponents = sldState.getSLDComponents();
    console.log('sldComponents length:', sldComponents.length);  // ← ADD THIS

    if (sldComponents.length === 0) {
        console.error('ERROR: sldComponents is empty! Check if sldState.setSLDComponents() is being called.');
        return;
    }
    const componentsCopy = [...sldComponents];
    sldState.setSLDComponents([]); // Clear for fresh update

    for (let i = 0; i < componentsCopy.length; i += CONFIG.BATCH_SIZE) {
        const batch = componentsCopy.slice(i, Math.min(componentsCopy.length, i + CONFIG.BATCH_SIZE));
        const componentsString = JSON.stringify(batch);

        try {
            await safeInvokeAsync(
                dotNetObjSLD,
                'SLDComponentUpdate',
                componentsString
            );
        } catch (e) {
            console.error('Error updating SLD components:', e);
        }
    }
}

/**
 * Update individual SLD item based on the changed property at server side modal by clicking the property button on SLD
 * @param {string} itemJSON - JSON string of item data
 * @param {string} modalType - Type of modal/item
 * @param {string} originalTag - Original tag of the item
 * @param {string} branchesString - JSON string of branches data
 */
export function updateSLDItem(itemJSON, modalType, originalTag, branchesString) {
    console.log("Client side Update SLDItem", modalType, itemJSON);
    
    const graph = sldState.getGraph();
    let item = JSON.parse(itemJSON);
    let branches = JSON.parse(branchesString);
    let itemModel = graph.getElements().find(el => el.prop('tag') === originalTag);

    switch (modalType) {
        case "Bus":
            itemModel = updateBus(itemModel, item, branches);
            break;
        case "Transformer":
            itemModel = updateTransformer(itemModel, item, branches);
            break;
        case "CableBranch":
            itemModel = updateCable(itemModel, item, branches);
            break;
        case "BusDuct":
            itemModel = updateBusDuct(itemModel, item, branches);
            break;
        case "Switch":
            itemModel = updateSwitch(itemModel, item, branches);
            break;
        case "Capacitor":
            itemModel = updateCapacitor(itemModel, item);
            break;
        case "Motor":
            itemModel = updateMotor(itemModel, item);
            break;
        case "Heater":
            itemModel = updateHeater(itemModel, item);
            break;
        case "LumpLoad":
            itemModel = updateLumpLoad(itemModel, item);
            break;
        default:
            break;
    }

    // Check if there is any change in the tag of the item
    // then the link tag connecting to the item should be updated
    updateLinksForTagChange(graph, originalTag, item.Tag);
}

/**
 * Update SLD with study results
 * @param {string} busesString - JSON string of buses data
 * @param {string} switchboardString - JSON string of switchboard data
 * @param {string} switchString - JSON string of switch data
 * @param {string} branchesString - JSON string of branches data
 * @param {string} loadsString - JSON string of loads data
 * @param {string} transformersString - JSON string of transformers data
 * @param {string} cableBranchesString - JSON string of cable branches data
 * @param {string} busDuctsString - JSON string of bus ducts data
 */
export function updateSLDWithStudyResults(
    busesString,
    switchboardString,
    switchString,
    branchesString,
    loadsString,
    transformersString,
    cableBranchesString,
    busDuctsString
) {
    const graph = sldState.getGraph();
    
    let buses = JSON.parse(busesString);
    let switchboards = JSON.parse(switchboardString);
    let switches = JSON.parse(switchString);
    let branches = JSON.parse(branchesString);
    let loads = JSON.parse(loadsString);
    let transformers = JSON.parse(transformersString);
    let cableBranches = JSON.parse(cableBranchesString);
    let busDucts = JSON.parse(busDuctsString);

    buses.forEach(item => {
        let itemModel = graph.getElements().find(el => el.prop('tag') && el.prop('tag') === item.Tag);
        if (itemModel) itemModel = updateBus(itemModel, item);
    });

    transformers.forEach(item => {
        if (!branches.find(br => br.Tag === item.Tag)) return;
        let itemModel = graph.getElements().find(el => el.prop('tag') && el.prop('tag') === item.Tag);
        if (itemModel) itemModel = updateTransformer(itemModel, item, branches);
    });

    cableBranches.forEach(item => {
        if (!branches.find(br => br.Tag === item.Tag)) return;
        let itemModel = graph.getElements().find(el => el.prop('tag') && el.prop('tag') === item.Tag);
        if (itemModel) itemModel = updateCable(itemModel, item, branches);
    });

    busDucts.forEach(item => {
        if (!branches.find(br => br.Tag === item.Tag)) return;
        let itemModel = graph.getElements().find(el => el.prop('tag') && el.prop('tag') === item.Tag);
        if (itemModel) itemModel = updateBusDuct(itemModel, item, branches);
    });

    loads.forEach(item => {
        let itemModel = graph.getElements().find(el => el.prop('tag') && el.prop('tag') === item.Tag);
        if (itemModel) {
            if (item.Category === "Motor") {
                itemModel = updateMotor(itemModel, item, branches);
            } else if (item.Category === "Heater") {
                itemModel = updateHeater(itemModel, item, branches);
            } else if (item.Category === "LumpLoad") {
                itemModel = updateLumpLoad(itemModel, item, branches);
            }
        }
    });

    switches.forEach(item => {
        let itemModel = graph.getElements().find(el => el.prop('tag') === item.Tag);
        if (itemModel) itemModel = updateSwitch(itemModel, item, branches);
    });
}

/**
 * Update links when item tag changes
 * @param {Object} graph - JointJS graph
 * @param {string} originalTag - Original tag
 * @param {string} newTag - New tag
 */
function updateLinksForTagChange(graph, originalTag, newTag) {
    if (originalTag === newTag) return;
    const links = graph.getLinks().filter(link =>
        link.prop('sourceTag') === originalTag ||
        link.prop('targetTag') === originalTag
    );

    links.forEach(link => {
        if (link.prop('sourceTag') === originalTag) {
            link.prop('sourceTag', newTag);
        }
        if (link.prop('targetTag') === originalTag) {
            link.prop('targetTag', newTag);
        }
        // Regenerate link tag if it's derived from source/target tags
        // e.g. "Bus-001_Cable-001" → "Bus-002_Cable-001"
        const linkTag = link.prop('tag');
        if (linkTag) {
            link.prop('tag', linkTag.replace(originalTag, newTag));
        }
    });
}
