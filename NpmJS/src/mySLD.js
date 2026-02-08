import {dia, elementTools, linkTools, shapes} from '@joint/core';

export {drawSLD, updateSLD, updateSLDItem, updateSLDWithStudyResults}


const CONFIG = {
    DEFAULT_FONT_SIZE: 10,
    PORT_RADIUS: 3,
    GRID_SIZE: 5,
    STROKE_WIDTH: 2,
    BATCH_SIZE: 10,
    MAX_OVERLAP_ITERATIONS: 100,
    OVERLAP_SPACING: 20
};


async function safeInvokeAsync(dotNetObj, methodName, ...args) {
    if (!dotNetObj) {
        throw new Error('DotNet object reference is null');
    }

    try {
        return await dotNetObj.invokeMethodAsync(methodName, ...args);
    } catch (error) {
        console.error(`Error invoking ${methodName}:`, error);
        throw error;
    }
}

function sanitizeText(text) {
    if (typeof text !== 'string') return String(text);
    return text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

var dotNetObjDraw;
var dotNetObjSLD;
var graph, paper;
var sldComponentsJS = [];
var sldComponentsString1;


var propertyButton = new elementTools.Button({
    focusOpacity: 0.5,
    // slightly right corner
    x: '0%',
    y: '50%',
    offset: {x: 10, y: 0},
    action: function (evt) { 
        safeInvokeAsync(dotNetObjSLD, 'PropertyUpdate', this.model.tag, this.model.type)
            .then(r => console.log(r)) ;
    },
    markup: [{
        tagName: 'circle',
        selector: 'button',
        attributes: {
            'r': 7,
            'fill': '#001DFF',
            'cursor': 'pointer'
        }
    }, {
        tagName: 'path',
        selector: 'icon',
        attributes: {
            'd': 'M -2 4 2 4 M 0 3 0 0 M -2 -1 1 -1 M -1 -4 1 -4',
            'fill': 'none',
            'stroke': '#FFFFFF',
            'stroke-width': 2,
            'pointer-events': 'none'
        }
    }]
});


var infoButton = new linkTools.Button({
    markup: [{
        tagName: 'circle',
        selector: 'button',
        attributes: {'r': 7, 'fill': '#001DFF', 'cursor': 'pointer'}
    }, {
        tagName: 'path',
        selector: 'icon',
        attributes: {
            'd': 'M -2 4 2 4 M 0 3 0 0 M -2 -1 1 -1 M -1 -4 1 -4',
            'fill': 'none',
            'stroke': '#FFFFFF',
            'stroke-width': 2,
            'pointer-events': 'none'
        }
    }],
    distance: '50%',
    offset: 0,
    action: function (evt) {
        console.log('View id: ' + this.id + '\n' + 'Model id: ' + this.model.id);
    }
});

var removeButton = new linkTools.Button({
    markup: [{
        tagName: 'circle',
        selector: 'button',
        attributes: {'r': 7, 'fill': 'red', 'cursor': 'pointer'}
    }, {
        tagName: 'path',
        selector: 'icon',
        attributes: {
            'd': 'M -4 4 L 4 -4 M 4 4 L -4 -4',
            'fill': 'none',
            'stroke': '#FFFFFF',
            'stroke-width': 2,
            'pointer-events': 'none'
        }
    }],
    distance: '25%',
    offset: 0,
    action: function (evt) {
        // Get the link by its ID
        const link = graph.getCell(this.model.id);
        if (link && link.isLink()) {
            removeLink(link);
        } else {
            console.log(`Link id '${this.id}' of link model '${this.model.id}' not available.`);
        }
    }
});

var validateButton = new linkTools.Button({
    markup: [{
        tagName: 'circle',
        selector: 'button',
        attributes: {'r': 7, 'fill': 'green', 'cursor': 'pointer'}
    }, {
        tagName: 'path',
        selector: 'icon',
        attributes: {
            'd': 'M -2.5 -0.2 l 0.23 -0.24 c 0.34 0.1 1 0.35 1.7 0.72 c 0.6 -0.8 2 -1.9 2.7 -2.4 l 0.2 0.2 l -2.8 3.78 l -2 -2 z',
            'fill': 'none',
            'stroke': '#FFFFFF',
            'stroke-width': 2,
            'pointer-events': 'none'
        }
    }],
    distance: '75%',
    offset: 0,
    action: function (evt) {
        // Get the link by its ID
        const link = graph.getCell(this.model.id);
        if (link && link.isLink()) {
            validateLinkFromServer(link);
        } else {
            console.log(`Link id '${this.id}' of link model '${this.model.id}' not available.`);
        }
    }
});


async function updateSLD() {
    console.log("Client side Update SLD");
    var allChildren = graph.getElements();
    //console.log(`Graph has ${allChildren.length} children. sldComponents has ${sldComponentsJS.length} items.`);
    
    const componentsCopy = [...sldComponentsJS];
    sldComponentsJS = [];

    for (let i = 0; i < componentsCopy.length; i += 10) {
        const batch = componentsCopy.slice(i, Math.min(componentsCopy.length, i + 10));
        const componentsString = JSON.stringify(batch);

        try {
            await safeInvokeAsync(
                dotNetObjSLD, 'SLDComponentUpdate', componentsString);
        } catch (e) {
            console.error('Error updating SLD components:', e);
        }
    }
}


function updateSLDItem(itemJSON, modalType, originalTag, branchesString) {
    console.log("Client side Update SLDItem", modalType, itemJSON);
    let item = JSON.parse(itemJSON);
    let branches = JSON.parse(branchesString);
    let itemModel = graph.getElements().find(el => el.tag === originalTag);

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
            itemModel = updateCapacitor(itemModel, item, branches);
            break;
        case "Motor":
            itemModel = updateMotor(itemModel, item, branches);
            break;
        case "Heater":
            itemModel = updateHeater(itemModel, item, branches);
            break;
        case "LumpLoad":
            itemModel = updateLumpLoad(itemModel, item, branches);
            break;
        default:
            //itemModel = updateBus(itemModel, item, branches);
            break;
    }

    // check if there is any change in the tag of the item
    // then the link tag connecting to the item should be updated
    var itemModels = graph.getLinks().filter(el =>
        el.source && el.source.hasOwnProperty('tag') && el.source.tag === originalTag ||
        el.target && el.target.hasOwnProperty('tag') && el.target.tag === originalTag);
    var itemModels1 = graph.getLinks().filter(el =>
        el.source && el.source.hasOwnProperty('tag') && el.source.tag === item.Tag ||
        el.target && el.target.hasOwnProperty('tag') && el.target.tag === item.Tag);

    // Retrieve all link models in the graph
    const links = graph.getLinks(); // or graph.getElements() if you want all elements

    // Filter links based on the property value
    const filteredLinks = links.filter(link => link.attr('source/tag') === originalTag);


    itemModels.forEach(itemModel => itemModel = getLinkTag(itemModel));
}


function updateSLDWithStudyResults(busesString, switchboardString, switchString, branchesString, loadsString, transformersString, cableBranchesString, busDuctsString) {


    let buses = JSON.parse(busesString);
    let switchboards = JSON.parse(switchboardString);
    let switches = JSON.parse(switchString);
    let branches = JSON.parse(branchesString);
    let loads = JSON.parse(loadsString);
    let transformers = JSON.parse(transformersString);
    let cableBranches = JSON.parse(cableBranchesString);
    let busDucts = JSON.parse(busDuctsString);


    buses.forEach(item => {
        let itemModel = graph.getElements().find(el => el.tag && el.tag === item.Tag);
        if (itemModel) itemModel = updateBus(itemModel, item);
    })

    transformers.forEach(item => {
        if (!branches.find(br => br.Tag === item.Tag)) return;
        let itemModel = graph.getElements().find(el => el.tag && el.tag === item.Tag);
        if (itemModel) itemModel = updateTransformer(itemModel, item, branches);
    })

    cableBranches.forEach(item => {
        if (!branches.find(br => br.Tag === item.Tag)) return;
        let itemModel = graph.getElements().find(el => el.tag && el.tag === item.Tag);
        if (itemModel) itemModel = updateCable(itemModel, item, branches);
    })

    busDucts.forEach(item => {
        if (!branches.find(br => br.Tag === item.Tag)) return;
        let itemModel = graph.getElements().find(el => el.tag && el.tag === item.Tag);
        if (itemModel) itemModel = updateBusDuct(itemModel, item, branches);
    })

    loads.forEach(item => {
        var itemModel = graph.getElements().find(el => el.tag && el.tag === item.Tag);
        if (itemModel) {
            if (item.Category === "Motor") {
                itemModel = updateMotor(itemModel, item, branches);
            } else if (item.Category === "Heater") {
                itemModel = updateHeater(itemModel, item, branches);
            } else if (item.Category === "LumpLoad") {
                itemModel = updateLumpLoad(itemModel, item, branches);
            }
        }
    })

    switches.forEach(item => {
        let itemModel = graph.getElements().find(el => el.tag === item.Tag);
        if (itemModel) itemModel = updateSwitch(itemModel, item, branches);
    })


}

/**
 * Draws the Single Line Diagram
 * @param {string} divString - ID of the container div
 * @param {number} xGridSize - Width of the grid
 * @param {number} yGridSize - Height of the grid
 * @param {number} leftSpacing - Left spacing
 * @param {number} topSpacing - Top spacing
 * @param {number} xGridSpacing - X grid spacing
 * @param {number} yGridSpacing - Y grid spacing
 * @param {string} busesString - JSON string of buses
 * @param {string} switchboardString - JSON string of switchboards
 * @param {string} branchesString - JSON string of branches
 * @param {string} loadsString - JSON string of loads
 * @param {string} transformersString - JSON string of transformers
 * @param {string} cablesString - JSON string of cables
 * @param {string} busDuctsString - JSON string of bus ducts
 * @param {string} xyString - JSON string of XY coordinates
 * @param {string} sldComponentsString - JSON string of SLD components
 * @param {*} dotNetObjRef - .NET object reference for drawing
 * @param {*} dotNetObjSLDRef - .NET object reference for SLD
 * @returns {void}
 */
function drawSLD(divString, xGridSize, yGridSize, leftSpacing, topSpacing, xGridSpacing, yGridSpacing,
                 busesString, switchboardString, branchesString, loadsString, transformersString, cablesString,
                 busDuctsString, xyString, sldComponentsString, dotNetObjRef, dotNetObjSLDRef) {


    // Define the custom drawing elements
    // grid   
    let GridElement = dia.Element.define('CustomGridElement', {
        attrs: {
            // size 40 x 40
            body: {refWidth: '100%', refHeight: '100%', refX: '50%', refY: '10%',},
            // textAnchor: middle: Align text to the middle horizontally
            label: {fill: 'blue', fontWeight: 'bold', textAnchor: 'middle', fontSize: 10, refX: '50%', refY: -45,},
            // textAnchor end: Right align text// Adjust horizontal position relative to refX
            ratedSC: {fill: 'black', textAnchor: 'end', fontSize: 10, refX: '50%', refY: -35, dx: -2,},
            ratedVoltage: {fill: 'black', textAnchor: 'start', fontSize: 10, refX: '50%', refY: -35, dx: 2,},
            busFaultkA: {
                fill: 'red',
                fontWeight: 'bold',
                textAnchor: 'end',
                fontSize: 10,
                refX: '50%',
                refY: 5,
                dx: -10,
            },
            operatingVoltage: {fill: 'blue', textAnchor: 'start', fontSize: 10, refX: '50%', refY: 5, dx: 10,}
        },
        ports: {
            groups: {
                'in': {
                    position: {ref: 'body', name: 'absolute', args: {x: '50%', y: 10}},
                    label: {
                        position: {name: 'right', args: {x: 10, y: -5}},
                        markup: [{tagName: 'text', selector: 'label'}]
                    },
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047'},
                        label: {text: '1', fontSize: 8}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                },
            },
            items: [{id: '1', group: 'in'}] // bus can have muliple ports
        },
    }, {
        markup: [
            {
                //https://yqnn.github.io/svg-path-editor/
                tagName: 'path', selector: 'body',
                attributes: {
                    d: 'M 0 0 v -6 h -9 v -18 h 18 v 18 h -9 m -3 0 l -6 -6 l 12 -12 l 6 6 l -12 12 m 6 0 l -12 -12 l 6 -6 l 12 12 l -6 6 m 6 0 l -18 -18 m 18 0 l -18 18 z',
                    stroke: 'blue', strokeWidth: 1, fill: 'none'
                },
            },
            {tagName: 'text', selector: 'label'},
            {tagName: 'text', selector: 'ratedSC'},
            {tagName: 'text', selector: 'ratedVoltage'},
            {tagName: 'text', selector: 'busFaultkA'},
            {tagName: 'text', selector: 'operatingVoltage'},
        ]
    });

    // switchboard    
    let SwitchboardElement = dia.Element.define('SwitchboardElement', {
        attrs: {
            root: {magnet: false},
            body: { refWidth: '100%', refHeight: '100%', refCx: '0%', refCy: '100%', strokeWidth: 1, strokeDasharray: '5,5', stroke: 'brown', fill: 'none', refX: '0%', refY: '0%',  alphaValue: 0.4},
            tag: {ref: 'body', fill: 'blue', fontWeight: 'bold', textAnchor: 'start', fontSize: 8, refX: 5, refY: 5,},
            '.': {'pointer-events': 'none'}
        }
    }, {
        markup: [
            {tagName: 'rect', selector: 'body'},
            {tagName: 'text', selector: 'tag'},
        ]
    });

    // node 
    let NodeElement = dia.Element.define('CustomNodeElement', {
        attrs: {
            root: {magnet: false},
            label: {fill: 'blue', fontWeight: 'bold', textAnchor: 'start', fontSize: 8, refX: '0%', refY: -12},
            ratedSC: {textAnchor: 'start', fontSize: 8, fill: 'brown', refX: '0%', refY: 5},
            ratedVoltage: {textAnchor: 'start', fontSize: 8, fill: 'blue', refX: 25, refY: 5},
            busFault: {textAnchor: 'end', fontSize: 8, fill: 'red', refX: '100%', refY: -12},
            operatingVoltage: {textAnchor: 'end', fontSize: 8, fill: 'blue', refX: '100%', refY: 5}
        },
        ports: {
            groups: {
                'in': {
                    position: {name: 'absolute', args: {x: '50%', y: 0}},
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047'},
                        label: {text: '1', fontSize: 8}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                },
            },
            items: [{id: '1', group: 'in'}] // node has only one port
        }
    }, {
        markup: [
            {tagName: 'text', selector: 'label'},
            {tagName: 'text', selector: 'ratedSC'},
            {tagName: 'text', selector: 'ratedVoltage'},
            {tagName: 'text', selector: 'busFault'},
            {tagName: 'text', selector: 'operatingVoltage'},
        ]
    });

    // bus
    let BusElement = dia.Element.define('CustomBusElement', {
        attrs: {
            root: {magnet: false},
            body: {stroke: 'blue', strokeWidth: 5, fill: 'transparent'},
            label: {ref: 'body', fill: 'blue', fontWeight: 'bold', textAnchor: 'start', fontSize: 8, refX: '0%', refY: -12},
            ratedSC: {ref: 'body', textAnchor: 'start', fontSize: 8, fill: 'brown', refX: '0%', refY: 5},
            ratedVoltage: { ref: 'body',textAnchor: 'start',fontSize: 8, fill: 'blue', refX: 25, refY: 5},
            busFault: {ref: 'body',textAnchor: 'end',fontSize: 8, fill: 'red', refX: '100%', refY: -12 },
            operatingVoltage: {ref: 'body', textAnchor: 'end', fontSize: 8, fill: 'blue', refX: '100%', refY: 5}
        },
        ports: {
            groups: {
                'in': {
                    position: {ref: 'body', name: 'absolute', args: {x: '50%', y: 0}},
                    label: {
                        position: {name: 'right', args: {x: 10, y: -5}},
                        markup: [{tagName: 'text', selector: 'label'}]
                    },
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047'},
                        label: {text: '1', fontSize: 8}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                },
                //'out': {
                //    position: { name: 'absolute', args: { x: '50%', y: 2 } },
                //    label: { position: { name: 'right', args: { x: 10, y: 5 } }, markup: [{ tagName: 'text', selector: 'label' }] },
                //    attrs: { portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: '#E6A502', stroke: '#023047' }, label: { text: 'to', fontSize: 8, } },
                //    markup: [{ tagName: 'circle', selector: 'portBody' }],
                //}
            },
            items: [{id: '1', group: 'in'}] // bus can have muliple ports
        }
    }, {
        markup: [
            {tagName: 'line', selector: 'body'},
            {tagName: 'text', selector: 'label'},
            {tagName: 'text', selector: 'ratedSC'},
            {tagName: 'text', selector: 'ratedVoltage'},
            {tagName: 'text', selector: 'busFault'},
            {tagName: 'text', selector: 'operatingVoltage'},
        ]
    });

    // node 
    let BusNodeElement = dia.Element.define('BusNodeElement', {
        attrs: {
            body: {
                r: 14, // Radius of the circle
                cx: '50%', // Center x-coordinate (relative to the element's width)
                cy: '50%', // Center y-coordinate (relative to the element's height)
                strokeWidth: 1,
                stroke: '#000000',
                fill: 'yellow',
            }
        }
    }, {
        markup: [
            {
                tagName: 'circle',
                selector: 'body'
            }
        ]
    });

    // load
    let LoadElement = dia.Element.define('LoadElement', {
        attrs: {
            body: {
                refWidth: '100%', // Full width of the element
                refHeight: '100%', // Full height of the element
                strokeWidth: 1,
                stroke: '#000000',
                fill: 'pink'
            },
            label: {
                fill: 'blue',
                fontWeight: 'bold',
                textAnchor: 'middle', // Center-align text
                fontSize: 8,
                refX: 0, // Center alignment horizontally
                refY: 35, // Adjust as needed
            },
            operatingPower: {
                fill: 'blue',
                textAnchor: 'middle', // Center-align text
                fontSize: 8,
                refX: 0, // Center alignment horizontally
                refY: 45, // Adjust as needed
            },
            rating: {
                fill: 'black',
                textAnchor: 'middle', // Center-align text
                fontSize: 8,
                refX: 0, // Center alignment horizontally
                refY: 55, // Adjust as needed
            }
        }
    }, {
        markup: [
            {tagName: 'rect', selector: 'body'},
            {tagName: 'text', selector: 'label'},
            {tagName: 'text', selector: 'operatingPower'}, // Adjusted order
            {tagName: 'text', selector: 'rating'} // Adjusted order
        ]
    });

    // lump load 
    let LumpLoadElement = dia.Element.define('LumpLoadElement', {
        attrs: {
            root: {magnet: false},
            body1: {refRCircumscribed: '25%', refCx: '0%', refCy: '50%', strokeWidth: 1, stroke: 'black', fill: 'cyan', refX: '0%',refY: '-15%', alphaValue: 0.4 },
            body2: {refWidth: '100%', refHeight: '100%', refX: '0%', refY: '0%', stroke: 'black', strokeWidth: 1, fill: 'none' },
            label: {fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: 15, refY: -25,},
            operatingPower: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -15,},
            rating: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -5,},
        },
        ports: {
            groups: {
                'in': {
                    position: {ref: 'body', name: 'absolute', args: {x: '0%', y: -10}},
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047'},
                        label: {text: '1', fontSize: 8}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                }
            },
            items: [{id: 'portIn', group: 'in'}]
        }
    }, {
        markup: [
            {tagName: 'circle', selector: 'body1'},
            {
                tagName: 'path',
                selector: 'body2',
                attributes: {d: 'm 0 15 L -15 15 L 0 38 L 15 15 L 0 15 m 0 0 v -20 Z'},
            },
            {tagName: 'text', selector: 'label'},
            {tagName: 'text', selector: 'operatingPower'},
            {tagName: 'text', selector: 'rating'},
        ]
    });

    // capacitor 
    let CapacitorElement = dia.Element.define('CapacitorElement', {
        attrs: {
            root: {magnet: false},
            body: {refWidth: '100%', refHeight: '100%', refX: '0%', refY: '0%', stroke: 'black', strokeWidth: 2},
            label: {fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: 15, refY: -25,},
            operatingPower: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -15,},
            rating: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -5,},
        },
        ports: {
            groups: {
                'in': {
                    position: {ref: 'body', name: 'absolute', args: {x: '0%', y: -10}},
                    label: {
                        position: {name: 'right', args: {x: 10, y: -5}},
                        markup: [{tagName: 'text', selector: 'label'}]
                    },
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047'},
                        label: {text: '1', fontSize: 8}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                }
            },
            items: [{id: 'portIn', group: 'in'}]
        }
    }, {
        markup: [
            {
                tagName: 'path',
                selector: 'body',
                attributes: {d: 'M -20 15 L 20 15 L -20 15 Z M -21 30 C -10 16 10 16 20 30 m 0 0 C 10 16 -10 16 -21 30 M 0 20 L 0 37 M 0 15 L 0 0 Z'},
            },
            {tagName: 'text', selector: 'label'},
            {tagName: 'text', selector: 'operatingPower'},
            {tagName: 'text', selector: 'rating'},
        ]
    });

    // transformer element   
    let TransformerElement = dia.Element.define('TransformerElement', {
        attrs: {
            root: {magnet: false},
            // primary: 100%/sqrt(2), i.e., radious is 100% of the width/heght of the element size width
            primary: {
                refRCircumscribed: '71%',
                refCx: '0%',
                refCy: '50%',
                strokeWidth: 1,
                stroke: 'black',
                fill: 'aquamarine',
                refX: '0%',
                refY: '-75%',
                alphaValue: 0.4
            },
            secondary: {
                refRCircumscribed: '71%',
                refCx: '0%',
                refCy: '50%',
                strokeWidth: 1,
                stroke: 'black',
                fill: 'aquamarine',
                refX: '%',
                refY: '75%',
            },
            tag: {fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: '150%', refY: -15,},
            voltage: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: '150%', refY: -5,},
            kVArating: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: '150%', refY: 5,},
            impedance: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: '150%', refY: 15,},
            loading: {fill: 'blue', textAnchor: 'right', fontSize: 8, refX: '150%', refY: 25,}
        },
        ports: {
            groups: {
                'in': {
                    position: {name: 'left', args: {y: -22}},
                    label: {position: {name: 'right', args: {x: 10}}, markup: [{tagName: 'text', selector: 'label'}]},
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: 'black'},
                        label: {text: 'from', fontSize: 8}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                },
                'out': {
                    position: {name: 'left', args: {y: 37}},
                    label: {position: {name: 'right', args: {x: 10}}, markup: [{tagName: 'text', selector: 'label'}]},
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: 'black'},
                        label: {text: 'to', fontSize: 8,}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                }
            },
            items: [{id: 'portIn', group: 'in'}, {id: 'portOut', group: 'out'}]
        }
    }, {
        markup: [
            {tagName: 'circle', selector: 'primary'},
            {tagName: 'circle', selector: 'secondary'},
            {tagName: 'text', selector: 'tag'},
            {tagName: 'text', selector: 'voltage'},
            {tagName: 'text', selector: 'kVArating'},
            {tagName: 'text', selector: 'impedance'},
            {tagName: 'text', selector: 'loading'}
        ]
    });

    // bus duct
    let BusDuctElement = dia.Element.define('BusDuctElement', {
        attrs: {
            root: {magnet: false},
            body: {refWidth: '100%', refHeight: '100%', refX: '0%', refY: '0%', stroke: 'black', strokeWidth: 1},
            label: {fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: 15, refY: -25,},
            size: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -15,},
            length: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -5,},
            impedance: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: 5,},
            operatingCurrent: {fill: 'blue', textAnchor: 'right', fontSize: 8, refX: 15, refY: 15,}
        },
        ports: {
            groups: {
                'in': {
                    position: {name: 'left', args: {y: '-50%'}},
                    label: {position: {name: 'right', args: {x: 10}}, markup: [{tagName: 'text', selector: 'label'}]},
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'white', stroke: 'black'},
                        label: {text: 'from', fontSize: 8}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                },
                'out': {
                    position: {name: 'left', args: {y: '50%'}},
                    label: {position: {name: 'right', args: {x: 10}}, markup: [{tagName: 'text', selector: 'label'}]},
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'white', stroke: 'black'},
                        label: {text: 'to', fontSize: 8,}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                }
            },
            items: [{id: 'portIn', group: 'in'}, {id: 'portOut', group: 'out'}]
        }
    }, {
        markup: [
            {
                //https://yqnn.github.io/svg-path-editor/
                tagName: 'path',
                selector: 'body',
                attributes: {d: 'M -3 15 L -5 25 L -3 15 L -3 -15 L -5 -25 L -3 -15 M 0 -25 L 0 25 M 3 15 L 5 25 L 3 15 L 3 -15 L 5 -25 L 3 -15'},
            },
            {tagName: 'text', selector: 'label'},
            {tagName: 'text', selector: 'size'},
            {tagName: 'text', selector: 'length'},
            {tagName: 'text', selector: 'impedance'},
            {tagName: 'text', selector: 'operatingCurrent'}
        ]
    });


    // cable 
    let CableElement = dia.Element.define('CableElement', {
        attrs: {
            root: {magnet: false},
            //body: { refWidth: '100%', refHeight: '100%', strokeWidth: 1, stroke: '#A00000', fill: 'orange', refX: 0, refY: -25 },
            body: {refWidth: '100%', refHeight: '100%', refX: '50%', refY: '10%',},
            label: {fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: 25, refY: -20},
            size: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 25, refY: -10},
            length: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 25, refY: 0},
            impedance: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 25, refY: 10},
            operatingCurrent: {fill: 'blue', textAnchor: 'right', fontSize: 8, refX: 25, refY: 20}
        },
        ports: {
            groups: {
                'in': {
                    position: {name: 'left', args: {x: 5, y: -25}},
                    label: {position: {name: 'right', args: {x: 10}}, markup: [{tagName: 'text', selector: 'label'}]},
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047'},
                        label: {text: 'from', fontSize: 8}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                },
                'out': {
                    position: {name: 'left', args: {x: 5, y: 32}}, // size 10x60
                    label: {position: {name: 'right', args: {x: 10}}, markup: [{tagName: 'text', selector: 'label'}]},
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: '#E6A502', stroke: '#023047'},
                        label: {text: 'to', fontSize: 8,}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                }
            },
            items: [{id: 'portIn', group: 'in'}, {id: 'portOut', group: 'out'}]
        }
    }, {
        markup: [
            {
                //https://yqnn.github.io/svg-path-editor/
                tagName: 'path', selector: 'body',
                attributes: {
                    d: 'M 5 -25 C 3 -20 -3 -20 -5 -25 L -5 20 C -3 15 3 15 5 20 L 5 -25 C 3 -30 -3 -30 -5 -25 M -5 20 C -3 25 3 25 5 20',
                    stroke: 'black', strokeWidth: 1, fill: 'orange'
                },
            },
            //{ tagName: 'rect', selector: 'body' },
            {tagName: 'text', selector: 'label'},
            {tagName: 'text', selector: 'size'},
            {tagName: 'text', selector: 'length'},
            {tagName: 'text', selector: 'impedance'},
            {tagName: 'text', selector: 'operatingCurrent'}
        ]
    });

    // motor
    let MotorElement = dia.Element.define('MotorElement', {
        attrs: {
            root: {magnet: false},
            body1: { refRCircumscribed: '25%', refCx: '0%', refCy: '50%', strokeWidth: 1, stroke: 'black', fill: 'azure', refX: '0%', refY: '-15%', alphaValue: 0.4},
            body2: { refWidth: '100%', refHeight: '100%', refX: '0%', refY: '0%', stroke: 'black', strokeWidth: 1, fill: 'none' },
            label: {fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: 15, refY: -25,},
            operatingPower: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -15,},
            rating: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -5,},
        },
        ports: {
            groups: {
                'in': {
                    position: {ref: 'body', name: 'absolute', args: {x: '0%', y: -10}},
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047'},
                        label: {text: '1', fontSize: 8}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                }
            },
            items: [{id: 'portIn', group: 'in'}]
        }
    }, {
        markup: [
            {tagName: 'circle', selector: 'body1'},
            {
                tagName: 'path',
                selector: 'body2',
                attributes: {d: 'm -8 30 L -8 15 L 0 25 L 8 15 L 8 30 L 8 15 L 0 25 L -8 15 L -8 30  m 8 -25 v -13 Z'},
            },
            {tagName: 'text', selector: 'label'},
            {tagName: 'text', selector: 'operatingPower'},
            {tagName: 'text', selector: 'rating'},
        ]
    });

    // heater
    let HeaterElement = dia.Element.define('Heaterlement', {
        attrs: {
            root: {magnet: false},
            body: {
                refWidth: '100%',
                refHeight: '100%',
                refX: '0%',
                refY: '0%',
                stroke: 'black',
                strokeWidth: 1,
                fill: 'cornsilk'
            },
            label: {fill: 'blue', fontWeight: 'bold', textAnchor: 'right', fontSize: 8, refX: 15, refY: -25,},
            operatingPower: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -15,},
            rating: {fill: 'black', textAnchor: 'right', fontSize: 8, refX: 15, refY: -5,},
        },
        ports: {
            groups: {
                'in': {
                    position: {ref: 'body', name: 'absolute', args: {x: '0%', y: -10}},
                    attrs: {
                        portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047'},
                        label: {text: '1', fontSize: 8}
                    },
                    markup: [{tagName: 'circle', selector: 'portBody'}],
                }
            },
            items: [{id: 'portIn', group: 'in'}]
        }
    }, {
        markup: [
            {
                tagName: 'path',
                selector: 'body',
                attributes: {d: 'm -15 5 L 15 5 L 15 55 L -15 55 L -15 5 m 0 10 L 15 15 M 15 25 L -15 25 M -15 35 L 15 35 M 15 45 L -15 45 m 15 -40 v -14 Z'},
            },
            {tagName: 'text', selector: 'label'},
            {tagName: 'text', selector: 'operatingPower'},
            {tagName: 'text', selector: 'rating'},
        ]
    });

    // link
    let branchLink = new shapes.standard.Link({
        router: {name: 'manhattan'},
        connector: {name: 'rounded', args: {radius: 2}, jumpover: {size: 6}},
        attrs: {
            line: {
                stroke: '#333333',
                strokeWidth: 2,
                sourceMarker: {type: 'circle', r: 2, fill: 'none'},
                targetMarker: {type: 'circle', r: 2, fill: 'none'},
            },
        }
    });









    dotNetObjDraw = dotNetObjRef;
    dotNetObjSLD = dotNetObjSLDRef;

// Declare variables BEFORE try-catch
    let buses, branches, loads, transformers, cables, busDucts, sldComponents, switchboards;

    try {
        buses = JSON.parse(busesString);
        branches = JSON.parse(branchesString);
        loads = JSON.parse(loadsString);
        transformers = JSON.parse(transformersString);
        cables = JSON.parse(cablesString);
        busDucts = JSON.parse(busDuctsString);
        sldComponents = JSON.parse(sldComponentsString);
        sldComponentsString1 = sldComponentsString;
        switchboards = JSON.parse(switchboardString);

        buses = Array.isArray(buses) ? buses : [];
        branches = Array.isArray(branches) ? branches : [];
        loads = Array.isArray(loads) ? loads : [];
        transformers = Array.isArray(transformers) ? transformers : [];
        cables = Array.isArray(cables) ? cables : [];
        busDucts = Array.isArray(busDucts) ? busDucts : [];
        sldComponents = Array.isArray(sldComponents) ? sldComponents : [];
        switchboards = Array.isArray(switchboards) ? switchboards : [];
    } catch(e) {
        console.error('Error parsing JSON parameters:', e);
        // Initialize with empty arrays to prevent undefined errors
        buses = [];
        branches = [];
        loads = [];
        transformers = [];
        cables = [];
        busDucts = [];
        sldComponents = [];
        switchboards = [];
        return; // Exit early if parsing fails
    }

    //var xy = JSON.parse(xyString);
    //console.log("hello" + JSON.stringify(buses));

    // define namespace, includes default imported joint standrad namespace 'shapes'
    // and other shapes (e.g.,RectangleTwoLabels) under 'custom' namespace
    const namespace = {
        shapes, BusElement, SwitchboardElement, CableElement, TransformerElement, LoadElement, BusNodeElement,
        BusDuctElement, GridElement, LumpLoadElement, CapacitorElement, NodeElement
    };

    let defaultGrid = buses.filter(item => item.Category === "Swing")[0];
    let defaultBus = buses.filter(item => item.Category !== "Swing")[0];
    let defaultCable = cables[0];
    let defaultTransformer = transformers[0];
    let defaultBusduct = busDucts[0];

    graph = new dia.Graph({}, {cellNamespace: namespace});

    paper = new dia.Paper({
        el: document.getElementById(divString),
        model: graph,
        width: xGridSize,
        height: yGridSize,
        gridSize: 5,
        drawGrid: true,
        background: {color: '#F5F5F5'},
        cellViewNamespace: namespace,
        interactive: function (cellView) {
            if (cellView.model.isElement()) {
                return {elementMove: interaction};
            }
            return true;
        },
        defaultLink: () => new shapes.standard.Link({
            router: {name: 'manhattan'},
            connector: {name: 'rounded', args: {radius: 3}, jumpover: {size: 6}},
            attrs: {
                line: {
                    stroke: '#000000', // Ensure the line is visible with a color
                    strokeWidth: 2, // Adjust stroke width as needed
                    sourceMarker: {type: 'circle', r: CONFIG.PORT_RADIUS, fill: 'yellow'},
                    targetMarker: {type: 'circle', r: CONFIG.PORT_RADIUS, fill: 'green'}
                },
                label: {
                    textAnchor: 'middle', // Center the text horizontally
                    refX: 0.5, // Adjust as needed
                    refY: -10, // Position label above the line
                    fontSize: 12, // Adjust font size as needed
                    fill: '#000000', // Color for the label text
                }
            }
        }, {
            markup: [
                {tagName: 'path', selector: 'line'},
                {tagName: 'text', selector: 'label'}
            ]
        }),
        linkPinning: false,
        validateConnection: function (cellViewS, magnetS, cellViewT, magnetT, end, linkView) {
            // Prevent loop linking
            if (magnetS === magnetT) return false;
            // Prevent the link to self
            if (cellViewS === cellViewT) return false;

            // Prevent linking more than one link
            const sourcePortId = linkView.model.prop('source/port');
            const targetPortId = linkView.model.prop('target/port');

            const allLinksSource = graph.getConnectedLinks(cellViewS.model);
            const sourcePortLinks = allLinksSource.filter(link => link.prop('source/port') == sourcePortId);

            const allLinksTarget = graph.getConnectedLinks(cellViewT.model);
            const targetPortLinks = allLinksTarget.filter(link => link.prop('target/port') == targetPortId);

            console.log(`Source Element '${cellViewS.model.tag}' at port '${sourcePortId}' has ${sourcePortLinks.length} connections and ` +
                `Target Element '${cellViewT.model.tag}' at port '${targetPortId}' has ${targetPortLinks.length} connections.`);
            if (sourcePortLinks.length > 3) return false;
            if (targetPortLinks && targetPortLinks.length > 3) return false;

            linkView.model.on('change', function () {
                console.log('link changed');
            })

            // Prevent linking from input ports
            //if (magnetS && magnetS.getAttribute('port-group') === 'in') return false;
            // Prevent linking from output ports to input ports within one element
            //if (cellViewS === cellViewT) return false;
            // Prevent linking to output ports
            //return magnetT && magnetT.getAttribute('port-group') === 'in';

            // end of linking
            var sM, tM, sPort, tPort, linkM;

            sM = cellViewS.model;
            tM = cellViewT.model;
            linkM = linkView.model;
            sPort = linkM.prop('source/port');
            tPort = linkM.prop('target/port');
            return true;
        },
        validateMagnet: function (cellView, magnet, _evt) {
            // Note that this is the default behaviour. It is shown for reference purposes.
            // Disable linking interaction for magnets marked as passive
            return magnet.getAttribute('magnet') !== 'passive';
        },
        // Enable link snapping within 20px lookup radius
        snapLinks: {radius: 20},

    });
    paper.el.style.border = '1px solid #2E2E2E';


    // Function to check if a port has links
    function hasLink(element, portName) {
        // Get all links (edges) connected to the element
        const links = graph.getLinks();

        // Check if any link uses the specified port as source or target
        return links.some(link =>
            link.get('source').id === element.id && link.get('source').port === portName ||
            link.get('target').id === element.id && link.get('target').port === portName
        );
    }


    // Function to get the port ID from an element and a magnet
    function getPortId(element, magnet) {
        // Iterate over all ports of the element
        for (let portName in element.getPorts()) {
            const port = element.getPort(portName);
            // Check if the magnet matches the port element
            if (port && port.attrs.body.el === magnet) {
                return portName; // Return the port ID (name)
            }
        }
        return null; // Port not found
    }


    //const rect11 = new Rectangle();
    //rect11.addTo(graph);



    //var Container = shapes.container.Parent;
    //var Child = shapes.container.Child;
    //var Link = shapes.container.Link;
    //var BusElement = shapes.standard.Rectangle;
    var saveDataButton = new shapes.standard.Rectangle();
    saveDataButton.resize(100, 30);
    saveDataButton.position(50, 30);
    saveDataButton.attr('root/title', 'joint.shapes.standard.Rectangle');
    saveDataButton.attr('label/text', 'Save Data');
    saveDataButton.attr('label/text/fill', 'white');
    saveDataButton.attr('body/fill', 'blue');
    saveDataButton.tag = "saveData";
    saveDataButton.addTo(graph);

    // create template custom shapes
    var templateGridElement = new GridElement;
    templateGridElement.type = "grid";
    templateGridElement.tag = "templateGridElement";
    templateGridElement.clicked = false;
    templateGridElement.position(50, 100);
    templateGridElement.resize(20, 80);
    templateGridElement.addTo(graph);
    let newGridElement = templateGridElement.clone();
    newGridElement.type = "grid";
    newGridElement.tag = "templateGridElement";
    newGridElement.addTo(graph);
    //
    var templateBusElement = new BusElement({
        position: {x: 25, y: 200},
        size: {width: 50, height: 0},
        attrs: {body: {x1: 0, y1: 0, x2: 50, y2: 0},}
    });
    templateBusElement.type = "bus";
    templateBusElement.tag = "templateBusElement";
    templateBusElement.clicked = false;
    templateBusElement.addTo(graph);
    let newBusElement = templateBusElement.clone();
    newBusElement.type = "bus";
    newBusElement.tag = "templateBusElement";
    newBusElement.addTo(graph);

    var templateCableElement = new CableElement();
    templateCableElement.type = "cable";
    templateCableElement.tag = "templateCableElement";
    templateCableElement.clicked = false;
    templateCableElement.position(50, 300);
    templateCableElement.resize(10, 60);
    templateCableElement.addTo(graph);
    let newCableElement = templateCableElement.clone();
    newCableElement.type = "cable";
    newCableElement.tag = "templateCableElement";
    newCableElement.addTo(graph);

    var templateTransformerElement = new TransformerElement();
    templateTransformerElement.type = "transformer";
    templateTransformerElement.tag = "templateTransformerElement";
    templateTransformerElement.clicked = false;
    templateTransformerElement.position(50, 400);
    templateTransformerElement.resize(15, 15);
    templateTransformerElement.addTo(graph);
    let newTransformerElement = templateTransformerElement.clone();
    newTransformerElement.type = "transformer";
    newTransformerElement.tag = "templateTransformerElement";
    newTransformerElement.addTo(graph);

    var templateBusDuctElement = new BusDuctElement();
    templateBusDuctElement.type = "busduct";
    templateBusDuctElement.tag = "templateBusDuctElement";
    templateBusDuctElement.clicked = false;
    templateBusDuctElement.position(50, 500);
    templateBusDuctElement.resize(10, 60);
    templateBusDuctElement.addTo(graph);
    let newBusDuctElement = templateBusDuctElement.clone();
    newBusDuctElement.type = "busduct";
    newBusDuctElement.tag = "templateBusDuctElement";
    newBusDuctElement.addTo(graph);

    var templateCapacitorElement = new CapacitorElement();
    templateCapacitorElement.type = "capacitor";
    templateCapacitorElement.tag = "templateCapacitorElement";
    templateCapacitorElement.clicked = false;
    templateCapacitorElement.position(50, 600);
    templateCapacitorElement.resize(30, 60);
    templateCapacitorElement.addTo(graph);
    let newCapacitorElement = templateCapacitorElement.clone();
    newCapacitorElement.type = "capacitor";
    newCapacitorElement.tag = "templateCapacitorElement";
    newCapacitorElement.addTo(graph);

    var templateMotorElement = new MotorElement();
    templateMotorElement.type = "motor";
    templateMotorElement.tag = "templateMotorElement";
    templateMotorElement.clicked = false;
    templateMotorElement.position(50, 700);
    templateMotorElement.resize(30, 60);
    templateMotorElement.addTo(graph);
    let newMotorElement = templateMotorElement.clone();
    newMotorElement.type = "motor";
    newMotorElement.tag = "templateMotorElement";
    newMotorElement.addTo(graph);

    var templateHeaterElement = new HeaterElement();
    templateHeaterElement.type = "heater";
    templateHeaterElement.tag = "templateHeaterElement";
    templateHeaterElement.clicked = false;
    templateHeaterElement.position(50, 800);
    templateHeaterElement.resize(30, 60);
    templateHeaterElement.addTo(graph);
    let newHeaterElement = templateHeaterElement.clone();
    newHeaterElement.type = "heater";
    newHeaterElement.tag = "templateHeaterElement";
    newHeaterElement.addTo(graph);

    var templateLumpLoadElement = new LumpLoadElement();
    templateLumpLoadElement.type = "lumpload";
    templateLumpLoadElement.tag = "templateLumpLoadElement";
    templateLumpLoadElement.clicked = false;
    templateLumpLoadElement.position(50, 900);
    templateLumpLoadElement.resize(30, 60);
    templateLumpLoadElement.addTo(graph);
    let newLumpLoadElement = templateLumpLoadElement.clone();
    newLumpLoadElement.type = "lumpload";
    newLumpLoadElement.tag = "templateLumpLoadElement";
    newLumpLoadElement.addTo(graph);


    var container = [];
    var child = [];
    var link = [];
    var linkCount = 0;
    var branchElement = [];
    var busesDone = [];
    var busesElement = [];
    var swbdElement = [];
    var busesNodeElement = []; // dots on bus
    var loadElement = [];


    // new items - drag from template
    paper.on('element:pointerup', (elementView, event, x, y) => {
        if (!elementView.model.tag.includes('template')) return;
        console.log('Template Element ' + elementView.model.tag + 'moved to ' + x + ',' + y);
        if (isOverlapping(elementView.model, graph)) {
            // Move the element to the nearest empty position
            const newPosition = findNearestEmptyPosition(elementView.model, graph, 50);
            elementView.model.set('position', newPosition);
        }

        if (elementView.model.tag == 'templateGridElement') {
            busesElement.push(elementView.model);
            busesElement.at(-1).tag = `Grid-${busesElement.filter(br => br.type = "grid").length.toString().padStart(3, '0')}`;

            busesElement.at(-1).attr({
                label: {text: busesElement.at(-1).tag},
                ratedSC: {text: "0kA"},
                ratedVoltage: {text: "0kV"},
                busFaultkA: {text: "0kA"},
                operatingVoltage: {text: "0% ∠0°"}
            });

            let newGridElement = templateGridElement.clone();
            newGridElement.type = "gid";
            newGridElement.tag = "templateGridElement";
            newGridElement.addTo(graph);

        } else if (elementView.model.tag == 'templateBusElement') {
            busesElement.push(elementView.model);
            busesElement.at(-1).tag = `Bus-${busesElement.filter(br => br.type = "bus").length.toString().padStart(3, '0')}`;

            busesElement.at(-1).attr({
                label: {text: busesElement.at(-1).tag},
                ratedSC: {text: "0kA"},
                ratedVoltage: {text: "0kV"},
                busFaultkA: {text: "0kA"},
                operatingVoltage: {text: "0% ∠0°"}
            });

            let newBusElement = templateBusElement.clone();
            newBusElement.type = "gid";
            newBusElement.tag = "templateBusElement";
            newBusElement.addTo(graph);

        } else if (elementView.model.tag == 'templateCableElement') {
            branchElement.push(elementView.model);
            branchElement.at(-1).tag = `Cablebranch-${branchElement.filter(br => br.type = "cable").length.toString().padStart(3, '0')}`;

            branchElement.at(-1).attr({
                label: {text: branchElement.at(-1).tag},
                size: {text: '3Cx16'},
                length: {text: `300m, .04l-j.07Ω/km`},
                impedance: {text: `R:.2, X:.3`},
                operatingCurrent: {text: `0A ∠0°`}
            });
            let newCableElement = templateCableElement.clone();
            newCableElement.type = "cable";
            newCableElement.tag = "templateCableElement";
            newCableElement.addTo(graph);

        } else if (elementView.model.tag == 'templateTransformerElement') {
            branchElement.push(elementView.model);
            branchElement.at(-1).tag = `Transformer-${branchElement.filter(br => br.type = "transformer").length.toString().padStart(3, '0')}`;
            branchElement.at(-1).attr({
                label: {text: branchElement.at(-1).tag},
                voltage: {text: `0kV`},
                kVArating: {text: `0kVA`},
                impedance: {text: `0%`},
                loading: {text: `0kW 0kVAR`},
            });
            let newTransformerElement = templateTransformerElement.clone();
            newTransformerElement.type = "lumpload";
            newTransformerElement.tag = "templateTransformerElement";
            newTransformerElement.addTo(graph);

        } else if (elementView.model.tag == 'templateBusDuctElement') {
            branchElement.push(elementView.model);
            branchElement.at(-1).tag = `BusDuct-${branchElement.filter(br => br.type = "transformer").length.toString().padStart(3, '0')}`;
            branchElement.at(-1).attr({
                label: {text: branchElement.at(-1).tag},
                voltage: {text: `0kV`},
                kVArating: {text: `0kVA`},
                impedance: {text: `0%`},
                loading: {text: `0kW 0kVAR`},
            });
            let newBusDuctElement = templateBusDuctElement.clone();
            newBusDuctElement.type = "busduct";
            newBusDuctElement.tag = "templateBusDuctElement";
            newBusDuctElement.addTo(graph);

        } else if (elementView.model.tag == 'templateCapacitorElement') {
            loadElement.push(elementView.model);
            loadElement.at(-1).tag = `Capacitor-${capacitorElement.length.toString().padStart(3, '0')}`;
            loadElement.at(-1).attr({
                label: {text: capacitorElement.at(-1).tag},
                operatingPower: {text: `0kVAR`},
                rating: {text: `0kVAR`},
            });

            let newCapacitorElement = templateCapacitorElement.clone();
            newCapacitorElement.type = "capacitor";
            newCapacitorElement.tag = "templateCapacitorElement";
            newCapacitorElement.addTo(graph);

        } else if (elementView.model.tag == 'templateMotorElement') {
            loadElement.push(elementView.model);
            loadElement.at(-1).tag = `Motor-${loadElement.filter(br => br.type = "motor").length.toString().padStart(3, '0')}`;
            loadElement.at(-1).attr({
                label: {text: loadElement.at(-1).tag},
            });

            let newMotorElement = templateMotorElement.clone();
            newMotorElement.type = "motor";
            newMotorElement.tag = "templateMotorElement";
            newMotorElement.addTo(graph);

        } else if (elementView.model.tag == 'templateHeaterlement') {
            loadElement.push(elementView.model);
            loadElement.at(-1).tag = `Heater-${loadElement.filter(br => br.type = "heater").length.toString().padStart(3, '0')}`;
            loadElement.at(-1).attr({
                label: {text: loadElement.at(-1).tag},
            });
            let newHeaterElement = templateHeaterElement.clone();
            newHeaterElement.type = "heater";
            newHeaterElement.tag = "templateHeaterElement";
            newHeaterElement.addTo(graph);

        } else if (elementView.model.tag == 'templateLumpLoadElement') {
            loadElement.push(elementView.model);
            loadElement.at(-1).tag = `LumpLoad-${loadElement.filter(br => br.type = "lumpload").length.toString().padStart(3, '0')}`;
            loadElement.at(-1).attr({
                label: {text: loadElement.at(-1).tag},
                operatingPower: {text: `${4}kW ${3}kVAR`},
                rating: {text: `${5}kVA`},
            });

            newLumpLoadElement = templateLumpLoadElement.clone();
            newLumpLoadElement.type = "lumpload";
            newLumpLoadElement.tag = "templateLumpLoadElement";
            newLumpLoadElement.addTo(graph);

        } else if (elementView.model.tag == 'templateOtherElement') {

        }
    });


    // delete items
    // buses.forEach((bus, index) => {
    //
    //     if (bus.Category === "Swing") {
    //         // grid            
    //         busesElement[index] = new GridElement;
    //         busesElement[index].type = "swing";
    //         busesElement[index].tag = bus.Tag;
    //         busesElement[index].clicked = false;
    //         busesElement[index].position(bus.CordX, bus.CordY);
    //         busesElement[index].resize(40, 40);
    //
    //         //busesElement[index] = updateBus(busesElement[index], bus);
    //
    //
    //         busesElement[index].attr({
    //             label: {text: "Grid" + bus.Tag},
    //             ratedSC: {text: Math.round(10 * bus.ISC) / 10 + "kA"},
    //             ratedVoltage: {text: bus.VR / 1000 + "kV"},
    //             busFaultkA: {text: Math.round(10 * bus.SCkAaMax) / 10 + "kA"},
    //             operatingVoltage: {text: Math.round(10000 * bus.Vo.Magnitude) / 100 + "% ∠" + Math.round(bus.Vo.Phase * 1800 / Math.PI) / 10 + "°"}
    //         });
    //         busesElement[index].addTo(graph);
    //
    //     } else {
    //         // for node, the bus line and parameter display is off
    //         // other bus
    //         // one way to draw the bus by "Expanding parent area to cover its children"
    //         // where children are at both ends
    //         // another was is to draw two edge and draw a link as the bus
    //         // in this way all the links can be perpendicular to the bus bar (link)
    //
    //         busesElement.push(new BusElement({
    //             position: {x: bus.CordX - bus.Length / 2, y: bus.CordY},
    //             // Width represents the length of the line
    //             size: {width: bus.Length, height: 0},
    //             attrs: {
    //                 body: {x1: 0, y1: 0, x2: bus.Length, y2: 0},
    //                 label: {text: bus.Tag},
    //                 ratedSC: {text: Math.round(10 * bus.ISC) / 10 + "kA"},
    //                 ratedVoltage: {text: bus.VR / 1000 + "kV"},
    //                 busFault: {text: Math.round(10 * bus.SCkAaMax) / 10 + "kA"},
    //                 operatingVoltage: {text: Math.round(10000 * bus.Vo.Magnitude) / 100 + "% ∠" + Math.round(bus.Vo.Phase * 1800 / Math.PI) / 10 + "°"}
    //             }
    //         }));
    //
    //         //busesElement.at(-1) = updateBus(busesElement.at(-1), bus);
    //
    //         busesElement.at(-1).type = "bus";
    //         busesElement.at(-1).tag = bus.Tag;
    //         busesElement.at(-1).clicked = false;
    //         busesElement.at(-1).addTo(graph);
    //     }
    //
    //     // check if exising server data has customised bus position and length for this bus
    //     busesElement[index] = updatePositionLength(busesElement[index], sldComponents);
    // });

    console.log('═══════════════════════════════════════════════════════');
    console.log('🔍 DIAGNOSTIC: Analyzing buses data');
    console.log('═══════════════════════════════════════════════════════');
    console.log('Total buses:', buses.length);
    console.log('buses is array?', Array.isArray(buses));

// Check each bus for missing/invalid data
    buses.forEach((bus, index) => {
        const issues = [];

        if (!bus) {
            console.error(`❌ Bus ${index}: NULL or UNDEFINED`);
            return;
        }

        if (!bus.Tag) issues.push('Missing Tag');
        if (!bus.Category) issues.push('Missing Category');
        if (typeof bus.CordX !== 'number') issues.push(`Invalid CordX: ${bus.CordX}`);
        if (typeof bus.CordY !== 'number') issues.push(`Invalid CordY: ${bus.CordY}`);
        if (bus.Category !== 'Swing' && typeof bus.Length !== 'number') issues.push(`Invalid Length: ${bus.Length}`);
        if (typeof bus.ISC !== 'number') issues.push(`Invalid ISC: ${bus.ISC}`);
        if (typeof bus.VR !== 'number') issues.push(`Invalid VR: ${bus.VR}`);
        if (typeof bus.SCkAaMax !== 'number') issues.push(`Invalid SCkAaMax: ${bus.SCkAaMax}`);
        if (!bus.Vo) {
            issues.push('Missing Vo object');
        } else {
            if (typeof bus.Vo.Magnitude !== 'number') issues.push(`Invalid Vo.Magnitude: ${bus.Vo.Magnitude}`);
            if (typeof bus.Vo.Phase !== 'number') issues.push(`Invalid Vo.Phase: ${bus.Vo.Phase}`);
        }

        if (issues.length > 0) {
            console.warn(`⚠️ Bus ${index} (${bus.Tag || 'NO TAG'}) has issues:`);
            issues.forEach(issue => console.warn(`   - ${issue}`));
        } else {
            console.log(`✅ Bus ${index} (${bus.Tag}): All data valid`);
        }
    });

    console.log('═══════════════════════════════════════════════════════');



    buses.forEach((bus, index) => {

        // ✅ STEP 1: Validate bus data
        if (!bus || !bus.Tag) {
            console.warn(`⚠️ Skipping invalid bus at index ${index}:`, bus);
            return; // Skip this bus
        }

        console.log(`Processing bus ${index}: ${bus.Tag} (${bus.Category || 'unknown'})`);

        // ✅ STEP 2: Provide default values for missing properties
        const cordX = typeof bus.CordX === 'number' ? bus.CordX : 0;
        const cordY = typeof bus.CordY === 'number' ? bus.CordY : 0;
        const length = typeof bus.Length === 'number' ? bus.Length : 100;
        const isc = typeof bus.ISC === 'number' ? bus.ISC : 0;
        const vr = typeof bus.VR === 'number' ? bus.VR : 0;
        const sckAaMax = typeof bus.SCkAaMax === 'number' ? bus.SCkAaMax : 0;

        // ✅ STEP 3: Create the appropriate bus element
        if (bus.IsSwing) {
            // Create grid element
            busesElement[index] = new GridElement;
            busesElement[index].type = "swing";
            busesElement[index].tag = bus.Tag;
            busesElement[index].clicked = false;
            busesElement[index].position(cordX, cordY);
            busesElement[index].resize(40, 40);

            // Safe voltage display
            let operatingVoltage = "N/A";
            if (bus.Vo && typeof bus.Vo.Magnitude === 'number' && typeof bus.Vo.Phase === 'number') {
                const magnitude = Math.round(10000 * bus.Vo.Magnitude) / 100;
                const phase = Math.round(bus.Vo.Phase * 1800 / Math.PI) / 10;
                operatingVoltage = `${magnitude}% ∠${phase}°`;
            }

            busesElement[index].attr({
                label: {text: "Grid" + bus.Tag},
                ratedSC: {text: Math.round(10 * isc) / 10 + "kA"},
                ratedVoltage: {text: vr / 1000 + "kV"},
                busFaultkA: {text: Math.round(10 * sckAaMax) / 10 + "kA"},
                operatingVoltage: {text: operatingVoltage}
            });

            busesElement[index].addTo(graph);

        } else {
            // Create bus element - ✅ CHANGED: Use [index] instead of .push()

            // Safe voltage display
            let operatingVoltage = "N/A";
            if (bus.Vo && typeof bus.Vo.Magnitude === 'number' && typeof bus.Vo.Phase === 'number') {
                const magnitude = Math.round(10000 * bus.Vo.Magnitude) / 100;
                const phase = Math.round(bus.Vo.Phase * 1800 / Math.PI) / 10;
                operatingVoltage = `${magnitude}% ∠${phase}°`;
            }

            busesElement[index] = new BusElement({
                position: {x: cordX - length / 2, y: cordY},
                size: {width: length, height: 0},
                attrs: {
                    body: {x1: 0, y1: 0, x2: length, y2: 0},
                    label: {text: bus.Tag},
                    ratedSC: {text: Math.round(10 * isc) / 10 + "kA"},
                    ratedVoltage: {text: vr / 1000 + "kV"},
                    busFault: {text: Math.round(10 * sckAaMax) / 10 + "kA"},
                    operatingVoltage: {text: operatingVoltage}
                }
            });

            busesElement[index].type = "bus";
            busesElement[index].tag = bus.Tag;
            busesElement[index].clicked = false;
            busesElement[index].addTo(graph);
        }

        // ✅ STEP 4: Verify element was created
        if (!busesElement[index]) {
            console.error(`❌ ERROR: busesElement[${index}] is undefined after creation!`);
            return; // Skip updatePositionLength
        }

        console.log(`✅ Created busesElement[${index}] successfully`);

        // ✅ STEP 5: Safe update with position/length from server data
        try {
            const updatedElement = updatePositionLength(busesElement[index], sldComponents);

            if (updatedElement) {
                busesElement[index] = updatedElement;
            } else {
                console.warn(`⚠️ updatePositionLength returned null/undefined for bus ${bus.Tag}`);
                // Keep the original element if update fails
            }
        } catch (e) {
            console.error(`❌ Error updating position/length for bus ${bus.Tag}:`, e);
            // Keep the original element if update fails
        }
    });

    console.log(`✅ Completed buses loop. Created ${busesElement.length} bus elements`);

    // toggle bus and node by double clicking
    paper.on('element:pointerdblclick', (elementView) => {
        if (elementView.model.type != "bus") return;

        var ports = elementView.model.getGroupPorts('in');

        // if this bus has more than two ports or
        if (ports.length > 2) return;

        // two ports are not catering to the top or bottom links
        // (i.e., their positions are different, then this shall remain as bus)
        if (ports.length == 2) {
            var b = elementView.model.getPortsPositions('in');
            if (b[1].x != b[2].x) return;
        }

        // also no node if the bus is part of a switchboard whihc has more than one buses
        var swbd = switchboards.find(item => JSON.parse(item[1]).includes(elementView.model.tag));
        if (swbd) {
            var busTags = JSON.parse(swbd[1]);
            if (busTags.length > 1) return;
        }

        if (elementView.model.node) {
            // already a node, change to bus
            elementView.model.node = false;
        } else {
            // its bus, change to a node
            elementView.model.node = true;
        }
        elementView.model = updateNodeOrBus(elementView.model);
        // update SLDComponent for sending updated Bus data to server
        // remove existing sldComponentStrings with this bus tag, if exists
        sldComponentsJS = sldComponentsJS.filter(item => item.Tag != elementView.model.tag || item.Type != "bus" || item.SLD != "key");
        var props = {
            'node': elementView.model.node,
            'position': elementView.model.attributes.position,
            'length': cwidth
        };
        sldComponentsJS.push({
            'Type': "bus",
            'Tag': elementView.model.tag,
            'SLD': "key",
            'PropertyJSON': JSON.stringify(props)
        });
    });

    //
    // by default element interaction is enabled for all items
    let interaction = true;
    // original x and y
    let ox;
    let oy;
    let clickedBusTag;
    let end;
    let x1;
    let x2;
    let oposx;
    let owidth;
    let cposx;
    let cwidth;
    // bus ends drag
    paper.on('element:pointerdown', (elementView, event, x, y) => {
        if (elementView.model.type != "bus") return;
        // disable element movement for template items
        if (elementView.model.type && elementView.model.tag.includes("template")) {
            interaction = false;
        }
            // check if its a bus
        // if the clicked point is either left or right end, then prevent element movement
        else if (elementView.model.type && elementView.model.type == "bus") {

            // check the location of the click
            var pos = elementView.model.prop('position');
            var width = elementView.model.prop('size/width');
            oposx = elementView.model.prop('position/x');
            owidth = elementView.model.prop('size/width');
            end = x - pos.x < 10 ? "left" : pos.x + width - x < 10 ? "right" : "middle";
            x1 = elementView.model.prop('attrs/body/x1');
            x2 = elementView.model.prop('attrs/body/x2');
            // prevent bus element movement only if the position is not middle
            if (end != "middle") {
                interaction = false;
            } else {
                interaction = true;
            }
            clickedBusTag = elementView.model.tag;
            console.log(`Bus '${clickedBusTag}', position: (${pos.x},${pos.y}), width:${width}, x1-x2:${x2 - x1} ` +
                `clicked at (${x},${y}), '${end}' end, (ox,oy): (${ox},${oy}), x1: ${x1}, x2: ${x2}), interaction: ${interaction}`);
        } else {
            // if clicked on any ther than bus element, restore interaction
            interaction = true;
        }
    });


    let toWrite = true;
    paper.on('element:pointermove', (elementView, event, x, y) => {
        if (elementView.model.type != "bus") return;
        // bus left/right end movement
        if (elementView.model.type && elementView.model.type == "bus" && elementView.model.tag == clickedBusTag) {
            if (toWrite) {
                toWrite = false;
                setTimeout(function () {
                    if (end == "left") {
                        cposx = Math.min(x, oposx + owidth - 50);
                        cwidth = oposx + owidth - cposx;
                    }
                    if (end == "right") {
                        cposx = oposx;
                        cwidth = Math.max(x, oposx + 50) - oposx;
                    }
                    if (end == "right" || end == "left") {

                        console.log(`Bus '${clickedBusTag}', ${end} end changing ` +
                            `pos: ${oposx}-> ${cposx}, width: ${owidth} -> ${cwidth}`);

                        elementView.model.prop('position/x', cposx);
                        elementView.model.prop('size/width', cwidth);
                        elementView.model.prop('attrs/body/x2', cwidth);
                        // X1 is always = 0;
                        // Y1 and Y2 are not changed for a hirizontally oriented bus

                        // update SLDComponent for sending updated Bus data to server
                        // remove existing sldComponentStrings with this bus tag, if exists
                        sldComponentsJS = sldComponentsJS.filter(item => item.Tag != elementView.model.tag || item.Type != "bus" || item.SLD != "key");
                        var props = {
                            'node': elementView.model.node,
                            'position': elementView.model.attributes.position,
                            'length': cwidth
                        };
                        sldComponentsJS.push({
                            'Type': "bus",
                            'Tag': elementView.model.tag,
                            'SLD': "key",
                            'PropertyJSON': JSON.stringify(props)
                        });

                    }
                    toWrite = true;

                    // update switchboard, if any, for this bus
                    var swbd = switchboards.find(item => JSON.parse(item[1]).includes(elementView.model.tag));
                    if (swbd) {
                        var swbdModel = swbdElement.find(item => item.tag == swbd[0]);
                        swbdModel = updateSwbdPositionSizeByBus(elementView.model.tag, busesElement, switchboards, swbdElement, 30, 20, 30, 20);
                    }

                }, 10);
            }
        }
    });


    // switchboards
    switchboards.forEach((swbd, index) => {
        swbdElement[index] = new SwitchboardElement();
        swbdElement[index].type = "swbd";
        swbdElement[index].tag = swbd[0];
        var anyBusTagOfThisBoard = JSON.parse(swbd[1])[0];
        swbdElement[index] = updateSwbdPositionSizeByBus(anyBusTagOfThisBoard, busesElement, switchboards, swbdElement, 30, 20, 30, 20)
        swbdElement[index].attr({tag: {text: swbd[0]}});
        swbdElement[index].addTo(graph);
    });

    function updateSwbdPositionSizeByBus(busTag, busesElement, switchboards, swbdElement, dx1, dx2, dy1, dy2) {
        var swbd = switchboards.find(item => JSON.parse(item[1]).includes(busTag));
        var swbdModel = swbdElement.find(item => item.tag == swbd[0]);
        var busTags = JSON.parse(swbd[1]);
        // var busSections = JSON.parse(swbd[2]); not used
        // find the enclosing area
        var x1 = Number.MAX_SAFE_INTEGER;
        var y1 = Number.MAX_SAFE_INTEGER;
        var x2 = 0;
        var y2 = 0;
        busTags.forEach(bustag => {
            const bbox = busesElement.find(item => item.tag == bustag).getBBox();
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

    
    branches.forEach((branch, index) => {
        var sourceBus = buses.find(bus => bus.Tag == branch.FromBus);
        var targetBus = buses.find(bus => bus.Tag == branch.ToBus);
        var sourceBusNode = busesNodeElement.find(item => item.tag == `${sourceBus.Tag}_${branch.Tag}`);
        var targetBusNode = busesNodeElement.find(item => item.tag == `${targetBus.Tag}_${branch.Tag}`);

        // Handle branch elements based on type
        if (branch.Category === "Cable") {
            var cable = cables.find(cbl => cbl.Tag == branch.Tag);
            branchElement[index] = new CableElement();


            branchElement[index].type = "cable";
            branchElement[index].tag = cable.Tag;
            branchElement[index].clicked = false;
            branchElement[index].position(targetBus.CordX - 5, targetBus.CordY - yGridSpacing / 2 - 20);
            branchElement[index].resize(10, 60);

            branchElement[index] = updateCable(branchElement[index], cable, branches);
            //branchElement[index].attr({
            //    label: { text: cable.Tag },
            //    size: { text: cable.CblDesc },
            //    length: { text: `${cable.L}m, ${cable.Rl}-j${cable.Xl}Ω/km` },
            //    impedance: { text: `R:${cable.R}, X:${cable.X}` },
            //    operatingCurrent: { text: `${Math.round(10 * branch.Io.Magnitude) / 10}A ∠${Math.round(branch.Io.Phase * 1800 / Math.PI) / 10}°` }
            //});
        } else if (branch.Category === "Transformer") {
            var transformer = transformers.find(tr => tr.Tag === branch.Tag);
            branchElement[index] = new TransformerElement();
            branchElement[index].type = "transformer";
            branchElement[index].tag = transformer.Tag;
            branchElement[index].clicked = false;
            branchElement[index].position((targetBus.CordX + sourceBus.CordX) / 2, (targetBus.CordY + sourceBus.CordY) / 2);
            branchElement[index].resize(15, 15);

            branchElement[index] = updateTransformer(branchElement[index], transformer, branches);


            //branchElement[index].attr({
            //    tag: { text: transformer.Tag },
            //    voltage: { text: `${transformer.V1}/${transformer.V2}kV` },
            //    kVArating: { text: `${transformer.KVA}kVA` },
            //    impedance: { text: `Z:${transformer.Z}%` },
            //    loading: { text: `${Math.round(10 * branch.KW) / 10}KW ${Math.round(10 * branch.KVAR) / 10}kVAR` }
            //});
        } else if (branch.Category === "BusDuct") {
            var busDuct = busDucts.find(busDuct => busDuct.Tag == branch.Tag);
            branchElement[index] = new BusDuctElement();
            branchElement[index].type = "busduct";
            branchElement[index].tag = busDuct.Tag;
            branchElement[index].clicked = false;
            branchElement[index].position(targetBus.CordX - 5, targetBus.CordY - yGridSpacing / 2 - 20);
            branchElement[index].resize(10, 60);

            branchElement[index] = updateBusDuct(branchElement[index], busDuct, branches);

        } else {

            // Handle other branch types
            //
            //
        }


        // check if exising server data has customized position data for this branch cell
        branchElement[index] = updateItemPosition(branchElement[index], sldComponents);

        branchElement[index].addTo(graph);


        // Create top and bottom links for every branch connection
        // once this branch element is created
        // time to create the links dynamically
        // Create top and bottom links for this branch connections

        let fromLink = branchLink.clone();
        let toLink = branchLink.clone();
        // bus-side port index shall be the default 0
        // later it shall be distributed as per total connection


        var sourceBusElement = busesElement.find(item => item.tag == branch.FromBus);
        var targetBusElement = busesElement.find(item => item.tag == branch.ToBus);
        var thisbranchElement = branchElement.find(item => item.tag == branch.Tag);

        fromLink.set({
            source: {id: sourceBusElement.id, port: sourceBusElement.getPorts()[0].id},
            target: {id: thisbranchElement.id, port: thisbranchElement.getPorts()[0].id}
        });

        toLink.set({
            source: {id: targetBusElement.id, port: targetBusElement.getPorts()[0].id},
            target: {id: thisbranchElement.id, port: thisbranchElement.getPorts()[1].id}
        });

        fromLink.attr({target: {magnet: false}});
        toLink.attr({target: {magnet: false}});

        fromLink.tag = getLinkTag(fromLink);
        toLink.tag = getLinkTag(toLink);

        // check if exising server data has customised vertices for this link
        fromLink = updateLinkVertices(fromLink, sldComponents);
        toLink = updateLinkVertices(toLink, sldComponents);
        fromLink.type = "link";
        toLink.type = "link";
        // Add the link to the graph
        graph.addCell(fromLink);
        graph.addCell(toLink);

    });

    // once all the links are created, time to create required no of ports as per the connections
    // and distribute the port along the length of the bus
    busesElement.forEach((busElement, index) => {
        busPortDistribution(busElement.id);
        console.log(`Distributing ports for bus '${busElement.tag}'.`);
    });


    function updateLinkVertices(link, sldComponents) {
        // check if exising server data has customised vertices for this link
        var existingLinkData = sldComponents.find(item => item.Tag == link.tag && item.Type == "link");
        if (existingLinkData) {
            //var verticesText = existingData.propertyJSON.replace(/'/g, '"');
            var newVerticesText = existingLinkData.PropertyJSON;
            var newVertices = JSON.parse(newVerticesText);
            if (newVertices) link.vertices(newVertices);
            //console.log(`'${bottomLink.tag}' are updated with vertices ${newVerticesText} from DB.`);
        }
        return link;
    }

    function updateItemPosition(cell, sldComponents) {
        // check if exising server data has customised position for this cell
        var serverData = sldComponents.find(item => cell && cell.hasOwnProperty('tag') && item.Tag === cell.tag);
        if (serverData) {
            var newPositionText = serverData.PropertyJSON;
            var newPosition = JSON.parse(newPositionText)
            if (newPosition) cell.prop('position', newPosition);
        }
        return cell;
    }

    function updatePositionLength(busModel, sldComponents) {
        // check if exising server data has customised node?, position and length for this bus
        var serverData = sldComponents.find(item => busModel && busModel.hasOwnProperty('tag') && item.Tag === busModel.tag);
        if (serverData) {
            if (busModel.hasOwnProperty('type') && busModel.type === "swing") {
                busModel.prop('position', JSON.parse(serverData.PropertyJSON));
            } else {
                var newPositionLengthText = serverData.PropertyJSON;
                var newPositionLength = JSON.parse(newPositionLengthText);
                if (newPositionLength.position) {

                    busModel.prop('position', newPositionLength.position);
                }
                if (newPositionLength.length && busModel.type == "bus") {
                    busModel.prop('size/width', newPositionLength.length);
                    busModel.prop('attrs/body/x2', newPositionLength.length);
                }

                // check if its node or not
                if (newPositionLength.node && busModel.type == "bus") {
                    if (newPositionLength.node) {
                        //busModel.prop('node', true);
                        busModel.node = true;
                    } else {
                        //busModel.prop('node', false);
                        busModel.node = false;
                    }
                    busModel = updateNodeOrBus(busModel);
                }
            }
        }
        return busModel;
    }

    function updateNodeOrBus(busModel) {
        // update node status by either doubleclick or by server data
        if (busModel.node) {
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

            //label: { ref: 'body', fill: 'blue', fontWeight: 'bold', textAnchor: 'start', fontSize: 8, refX: '0%', refY: -12 },
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


    paper.on('element:mouseenter', function (elementView) {
        var tools = new dia.ToolsView({tools: [propertyButton]});
        if (elementView.model.tag && !elementView.model.tag.includes('template')) elementView.addTools(tools);
    });
    paper.on('element:mouseleave', function (elementView) {
        if (elementView.hasTools) elementView.removeTools();

    });


    //https://www.jointjs.com/demos/smart-routing
    //https://github.com/clientIO/joint/blob/master/packages/joint-core/demo/routing/src/routing.js
    paper.on('link:mouseenter', function (linkView) {
        var tools = new dia.ToolsView({
            tools: [
                new linkTools.Vertices(),
                new linkTools.Segments(),
                //new linkTools.Boundary(),
                // custom buttons complement the pre-made Remove button tool;
                //new linkTools.Remove(),
                infoButton, removeButton, validateButton
            ]
        });

        var toolsR = new dia.ToolsView({
            tools: [
                new linkTools.Vertices(),
                new linkTools.Segments(),
                //new linkTools.Boundary(),
                // custom buttons complement the pre-made Remove button tool;
                //new linkTools.Remove(),
                infoButton, removeButton
            ]
        });

        if (linkView.model.prop('validated')) {

            linkView.addTools(toolsR);
        } else {
            linkView.addTools(tools);
        }
    });

    paper.on('link:mouseleave', function (linkView) {
        linkView.removeTools();

        const link = linkView.model;
        const vertices = link.vertices();
        //console.log(`${link.tag} has ${vertices.length} vertices and they are ${JSON.stringify(vertices)}.`);

        // remove existing sldComponentStrings with this link tag, if exists
        sldComponentsJS = sldComponentsJS.filter(sldComponent => sldComponent.Tag != link.tag || sldComponent.Type != "link" || sldComponent.SLD != "key");

        // Creating an object using the sldComponentData constructor function
        // add this new json string to the list
        //if (vertices.length > 0) sldComponentsJS.push(new sldComponentData(link.tag, "link", "key", JSON.stringify(vertices)));
        if (vertices.length > 0) sldComponentsJS.push({
            'Type': "link",
            'Tag': link.tag,
            'SLD': "key",
            'PropertyJSON': JSON.stringify(vertices)
        });
        //console.log(`Total ${sldComponentsJS.length} sldComponents, tag '${link.tag}' ${(vertices.length > 0 ? "" : "not")} added.`);
    });


    toWrite = true;
    graph.on('change:position', function (cell) {

        if (cell.tag == "selectbox") return;
        if (cell.type == "link") return;
        //if (cell.isLink) return;
        // remove existing sldComponentStrings with this cell tag, if exists
        sldComponentsJS = sldComponentsJS.filter(sldComponent => sldComponent.Tag != cell.tag || sldComponent.Type != cell.type || sldComponent.SLD != "key");

        // Creating an object using the sldComponentData constructor function
        // add this new json string to the list
        //sldComponentsJS.push(new sldComponentData(cell.tag, cell.type, "key", JSON.stringify(cell.attributes.position)));
        if (cell.type == "bus") {
            // save position and length
            var props = {'position': cell.attributes.position, 'length': cell.attributes.size.width};
            sldComponentsJS.push({
                'Type': cell.type,
                'Tag': cell.tag,
                'SLD': "key",
                'PropertyJSON': JSON.stringify(props)
            });
        } else {
            sldComponentsJS.push({
                'Type': cell.type,
                'Tag': cell.tag,
                'SLD': "key",
                'PropertyJSON': JSON.stringify(cell.attributes.position)
            });
        }
        //console.log(`Total ${sldComponentsJS.length} sldComponents, tag '${link.tag}' ${(vertices.length > 0 ? "" : "not")} added.`);


        if (toWrite) {
            toWrite = false;
            setTimeout(function () {
                try {
                    console.log(`Change position function @ 500ms : Tag '${cell.tag}' of type '${cell.type}' moved to (${cell.attributes.position.x},${cell.attributes.position.y})`);
                    dotNetObjDraw.invokeMethodAsync('TagMoveUpdate', cell.type, cell.tag, cell.attributes.position.x, cell.attributes.position.y);
                    toWrite = true;
                } catch (err) {
                    console.log(err.message);
                }
            }, 500);
        }

        // if any bus changes its position, update the switchboard accordingly

        // update switchboard, if any, for this bus
        if (cell.type == "bus") {
            var swbd = switchboards.find(item => JSON.parse(item[1]).includes(cell.tag));
            if (swbd) {
                var swbdModel = swbdElement.find(item => item.tag == swbd[0]);
                swbdModel = updateSwbdPositionSizeByBus(cell.tag, busesElement, switchboards, swbdElement, 30, 20, 30, 20);
            }
        }

    });


    // Create a text element for coordinates display
    var textBlock = new shapes.standard.TextBlock();
    textBlock.resize(1000, 20);
    textBlock.position(100, 10);
    textBlock.attr('body/fill', 'none');
    textBlock.attr('body/stroke', 'none');
    textBlock.attr('label/text', '');
    textBlock.attr('label/fontSize', 8);
    // Styling of the label via `style` presentation attribute (i.e. CSS).
    textBlock.attr('label/style/color', 'red');
    textBlock.addTo(graph);

    // Update coordinates display on mouse move
    paper.on('blank:pointerclick', function (evt, x, y) {
        textBlock.attr('label/text', `X: ${x}, Y: ${y} `);
    });
    paper.on('cell:pointerclick', function (cell, evt, x, y) {
        textBlock.attr('label/text', `X: ${x}, Y: ${y} x:  ${cell.getBBox().x} y: ${cell.getBBox().y} height:  ${cell.getBBox().height} width: ${cell.getBBox().width} shift: ${cell.getBBox().height / 3.5}`);
        if (cell.model.tag && cell.model.tag.includes("template")) {
            console.log(cell.model.tag);
        }

    });


    //loads.forEach(load => {
    //    var i = loads.indexOf(load);
    //    var connectedBus = buses.filter(bus => bus.T == load.BT)[0];
    //    var connectedBranchList = branches.filter(br => br.ToBus == connectedBus.T || br.FromBus == connectedBus.T);
    //    var connectedBranch = connectedBranchList[0];
    //    var operatingPowerText = "";
    //    if (connectedBranchList.length > 0) { operatingPowerText = Math.round(10 * connectedBranch.KW) / 10 + "KW " + Math.round(10 * connectedBranch.KVAR) / 10 + "kVAR"; }
    //    var connectedBusNodeList = busesNodeElement.filter(item => item.tag == connectedBus.T + "_" + load.T);
    //    if (connectedBusNodeList.length > 0) {
    //        // do not draw load and link for loads not having connected bus
    //        loadElement[i] = new LoadElement;
    //        loadElement[i].position(connectedBus.CordX, connectedBus.CordY + yGridSpacing / 2);
    //        loadElement[i].type = "load";
    //        loadElement[i].tag = load.T;
    //        loadElement[i].clicked = false;
    //        loadElement[i].resize(20, 30);
    //        var rating = (load.T.includes("-Lump")) ? "(combined)" : "(" + Math.round(10000000 * load.P) / 10 + "kW " + Math.round(10000 * load.PF) / 100 + "% PF)";
    //        operatingPowerText = (load.T.includes("-Lump")) ? Math.round(10 * load.P) / 10 + "KW " + Math.round(10 * load.Q) / 10 + "kVAR" : operatingPowerText;
    //        loadElement[i].attr({
    //            label: { text: load.T },
    //            operatingPower: { text: operatingPowerText },
    //            rating: { text: rating },
    //        });
    //        link[linkCount] = new branchLink();  // load link
    //        link[linkCount].addTo(graph);
    //        linkCount++;
    //        link[link.length - 1].source(connectedBusNodeList[0], {
    //            anchor: { name: 'modelCenter', args: { rotate: true, dx: 0, } },
    //            connectionPoint: {
    //                name: 'bbox', args: { offset: 0, selector: 'body', }
    //            }
    //        });
    //        link[link.length - 1].target(loadElement[i], {
    //            anchor: { name: 'modelCenter', args: { rotate: true, dx: 0, } }
    //        });
    //        link[link.length - 1].router('orthogonal');
    //        link[link.length - 1].connector('jumpover', { size: 5 });
    //        link[link.length - 1].attr({
    //            line: {
    //                sourceMarker: { 'type': 'circle', 'r': 2, 'cx': 2, 'fill': '#000000', },
    //                targetMarker: { 'type': 'circle', 'r': 2, 'cx': 2, 'fill': '#000000', }
    //            }
    //        });
    //        loadElement[i].addTo(graph);
    //    }
    //});


    //// assigning preset postion to all elements
    ////busesElement.forEach(element => {
    ////    var xypos = xy.filter(item => item.Type == element.type && item.Tag == element.tag);
    ////    if (xypos.length > 0) {
    ////        element.position(xypos[0].CordX, xypos[0].CordY);
    ////    }
    ////});
    //branchElement.forEach(element => {
    //    var xypos = xy.filter(item => item.Type == element.type && item.Tag == element.tag);
    //    if (xypos.length > 0) {
    //        element.position(xypos[0].CordX, xypos[0].CordY);
    //    }
    //});
    //loadElement.forEach(element => {
    //    var xypos = xy.filter(item => item.Type == element.type && item.Tag == element.tag);
    //    if (xypos.length > 0) {
    //        element.position(xypos[0].CordX, xypos[0].CordY);
    //    }
    //});
    ////
    //function busBFS(prntBusT, thisBusT, parentContainer) {
    //    var parentBus = buses.filter(b => b.T == prntBusT)[0];
    //    var bus = buses.filter(b => b.T == thisBusT)[0];
    //    var busStr = bus.T + " (" + (bus.SCkAa).toString() + "kA)";
    //    // create a container
    //    container[buses.indexOf(bus)] = new Container({
    //        z: 1,
    //        attrs: { headerText: { text: busStr } }
    //    });
    //    graph.addCells([container[buses.indexOf(bus)]]);
    //    container[buses.indexOf(bus)].toggle(false);

    //    createChildLink(container[buses.indexOf(bus)], bus.T);

    //    buses.filter(b => b.T == thisBusT)[0].SLDL = buses.filter(b => b.T == thisBusT)[0].SLDL + 1; // bus bar length
    //    buses.filter(b => b.T == prntBusT)[0].SLDL = buses.filter(b => b.T == prntBusT)[0].SLDL + 1; // bus bar length

    //    // for all connected loads on to this bus
    //    if (loads.filter(l => l.BT == bus.T).length > 0) {
    //        loads.filter(l => l.BT == bus.T).forEach(load => {
    //            createChildLink(container[buses.indexOf(bus)], load.T);
    //        })
    //        busesDone.push(bus.T);
    //        buses.filter(b => b.T == thisBusT)[0].SLDL = buses.filter(b => b.T == thisBusT)[0].SLDL + 1; // bus bar length
    //        buses.filter(b => b.T == prntBusT)[0].SLDL = buses.filter(b => b.T == prntBusT)[0].SLDL + 1; // bus bar length

    //    }
    //    if (bus.Cn.length > 1) {
    //        // this bus has further connected bus
    //        // create a container for all downstream bus except the parentBus
    //        (bus.Cn).filter(bT => busesDone.includes(bT) != true && bT != prntBusT).forEach(bT => {
    //            var newChildBus = buses.filter(b => b.T == bT)[0];
    //            //prntBusT = bus.T
    //            //thisBusT = bT
    //            busBFS(bus.T, bT, container[buses.indexOf(bus)]);
    //        });
    //        // as this bus is completely done
    //        busesDone.push(bus.T);
    //    }


    //    paper.fitToContent();

    //    function createChildLink(mycontainer, childtText) {
    //        var ck = child.length;
    //        var lk = link.length;
    //        var posx = buses.filter(b => b.T == prntBusT)[0].CordX + 50 * buses.filter(b => b.T == prntBusT)[0].SLDL;
    //        var posy = 150 + buses.filter(b => b.T == prntBusT)[0].CordY;
    //        child[ck] = new Child({
    //            z: 2,
    //            position: { x: posx, y: posy },
    //            attrs: { label: { text: childtText } }
    //        });
    //        //
    //        link[lk] = new Link({
    //            z: 4,
    //            source: { id: mycontainer.id },
    //            target: { id: child[ck].id }
    //        });
    //        graph.addCells([child[ck], link[lk]]);

    //        mycontainer.embed(child[ck]);
    //        link[lk].reparent();
    //    }

    //}


    //----example------

    //var portsIn = {
    //    position: { name: 'top' },
    //    attrs: { portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: '#023047', stroke: '#023047' } },
    //    label: { position: { name: 'top', args: { y: 2 } }, markup: [{ tagName: 'text', selector: 'label', className: 'label-text' }] },
    //    markup: [{ tagName: 'circle', selector: 'portBody' }]
    //};

    //var portsOut = {
    //    position: { name: 'bottom' },
    //    attrs: { portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: '#E6A502', stroke: '#023047' } },
    //    label: { position: { name: 'bottom', args: { y: 2 } }, markup: [{ tagName: 'text', selector: 'label', className: 'label-text' }] },
    //    markup: [{ tagName: 'circle', selector: 'portBody' }]
    //};


    //const rect1 = new shapes.standard.Rectangle({
    //    size: { width: 40, height: 40 }, position: { x: 50, y: 1100 }, attrs: { body: { fill: '#8ECAE6' } },
    //    ports: {
    //        groups: { 'in': portsIn, 'out': portsOut }
    //    }
    //});


    //rect1.addPorts([
    //    { group: 'in', id: '1', attrs: { label: { text: '1' } } },
    //    { group: 'in', id: '2', attrs: { label: { text: '2' } } },
    //    { group: 'out', id: '3', attrs: { label: { text: '3' } } }
    //]);
    //const rect2 = rect1.clone().position(50, 1300);

    //var link35 = new dia.Link({
    //    source: { id: rect1.id, port: '3' },
    //    target: { id: rect2.id, port: '2' }
    //});

    //link35.router('orthogonal');
    //link35.connector('jumpover', { size: 10 });
    //link35.attr({
    //    line: {
    //        sourceMarker: { 'type': 'circle', 'r': 2, 'cx': 2, 'fill': '#000000', },
    //        targetMarker: { 'type': 'circle', 'r': 2, 'cx': 2, 'fill': '#000000', }
    //    }
    //});

    //graph.addCells([rect1, rect2, link35]);


    //var port = {
    //    label: {
    //        position: {
    //            name: 'left'
    //        },
    //        markup: [{
    //            tagName: 'text',
    //            selector: 'label'
    //        }]
    //    },
    //    attrs: {
    //        portBody: { magnet: true, r: 5, x: 5, y: 5, fill: '#E6A502', stroke: '#023047' },
    //        label: {
    //            text: 'port'
    //        }
    //    },
    //    markup: [{
    //        tagName: 'rect',
    //        selector: 'portBody'
    //    }]
    //};

    //var model = new shapes.standard.Rectangle({ size: { width: 200, height: 10 }, attrs: { body: { fill: '#8ECAE6' } }, ports: { groups: { 'in': portsOut } } });

    //model.addPorts([{ group: 'in', attrs: { label: { text: 'in1' } } }, { group: 'in', attrs: { label: { text: 'in2' } } }, { group: 'in', attrs: { label: { text: 'out' } } }]);

    //model.addPort(port); // add a port using Port API


    //const portId = model.getPorts()[0].id;

    //model.portProp(portId, 'attrs/portBody', { r: CONFIG.PORT_RADIUS, fill: 'darkslateblue' });
    //model.portProp(portId, 'custom', { testAttribute: true });
    //console.log(model.portProp(portId, 'custom'));


    //const rect11 = new shapes.standard.Rectangle({ size: { width: 20, height: 20 }, attrs: { body: { fill: '#8ECAE6' } } });
    //const rect12 = new shapes.standard.Rectangle({ size: { width: 20, height: 20 }, attrs: { body: { fill: '#8ECAE6' } } });

    //var link11 = new branchLink();
    //link11.source(rect11, { anchor: { name: 'bottom', args: { rotate: true, dx: 50, dy: 10 } } });
    //link11.target(rect12, { anchor: { name: 'modelCenter', args: { rotate: true } } });


    //link11.router('orthogonal');
    //link11.connector('jumpover', { size: 5 });
    //link11.vertex(0, {
    //    x: 160,
    //    y: 1450
    //});

    //graph.addCells([rect11, rect12, link11]);


    //paper.on('element:button:pointerdown', function (elementView) {
    //    var element = elementView.model;
    //    element.toggle();
    //    fitAncestors(element);
    //});

    //paper.on('element:pointermove', function (elementView) {
    //    var element = elementView.model;
    //    fitAncestors(element);
    //});

    //function fitAncestors(element) {
    //    element.getAncestors().forEach(function (container) {
    //        container.fitChildren();
    //    });
    //}


    function dragStart(evt, x, y) {
        //const data = (evt.data = {
        //    tool,
        //    ox: x,
        //    oy: y
        //});
        ox = x;
        oy = y;
        // remove previously created selectboxes
        var existingSelectBox = graph.getElements().find(el => el.tag && el.tag == "selectbox");
        if (existingSelectBox) {
            // unembed elements before removing this selectbox
            var allChildren = graph.getElements();
            //console.log(existingSelectBox.id);
            //allChildren.forEach(child => console.log(child.id, " - ", child.parent(), " ."));
            var children = allChildren.filter(el => el.get('parent') && graph.getCell(el.get('parent')).tag == "selectbox");
            if (children && children.length > 0) {
                children.forEach(child => {
                    // remove from the selection boundary
                    //console.log(`DragStart: ${child.tag} was earlier embedded in parent '${child.get('parent')}', to be un-embedded.`);
                    existingSelectBox.unembed(child);
                    //console.log(`DragStart: ${child.tag} is un-embedded now and hence has parent '${child.get('parent')}' post un-embedding.`);
                    // unhighlight the child element
                    //var childView = paper.findView(child);
                    //highlighters.mask.remove(childView);
                });
            }
            //console.log(`DragStart: Earlier Select box '${existingSelectBox.id}' removed`);
            existingSelectBox.remove()
        }
        //console.log(`Select box '${existingSelectBox.id}' removed`);
        var selectBox = new shapes.standard.Rectangle({
            position: {x: x, y: y},
            size: {width: 1, height: 1},
            attrs: {
                body: {
                    fill: 'rgba(100, 100, 0, 0.25)',
                    stroke: 'red',
                    strokeWidth: 1,
                    strokeDasharray: '5,5'
                }
            }
        });
        selectBox.tag = "selectbox";
        selectBox.type = "selectbox";
        selectBox.addTo(graph);

    }


    function drag(evt, x, y) {
        var selectBoxElement = graph.getElements().find(el => el.tag == "selectbox");
        var selectBox = paper.findViewByModel(selectBoxElement);
        if (selectBox) {
            selectBox.model.prop('size/width', Math.abs(ox - x));
            selectBox.model.prop('size/height', Math.abs(oy - y));
            selectBox.model.prop('position/x', Math.min(ox, x));
            selectBox.model.prop('position/y', Math.min(oy, y));
        }
    }

    function dragEnd(evt) {

        var selectBoxElement = graph.getElements().find(el => el.tag == "selectbox");
        if (selectBoxElement) {
            // if it is just a click without substantial drag, then the created selectBox to be removed
            var selectBox = paper.findViewByModel(selectBoxElement);
            var dx = selectBox.model.prop('size/width');
            var dy = selectBox.model.prop('size/height');
            if (dx < 5 && dy < 5) {
                // unembed elements, if any, before removing this selectbox
                var allChildren = graph.getElements();
                var children = allChildren.filter(el => el.get('parent') && graph.getCell(el.get('parent')).tag == "selectbox");
                if (children && children.length > 0) {
                    children.forEach(child => {
                        // remove from the selection boundary
                        //console.log(`DragEnd: Insufficient drag : ${child.tag} was embedded in parent '${child.get('parent')}' , to be un-embedded.`);
                        selectBoxElement.unembed(child);
                        //console.log(`DragEnd: Insufficient drag : ${child.tag} is un-embedded now and hence has parent '${child.get('parent')}' post un-embedding.`);
                    });
                }
                selectBoxElement.remove();
                //console.log(`DragEnd: Insufficient drag : ${selectBoxElement.tag} is  removed as its not substatially dragged.`)
            } else {
                // retain the select box and embed all the elements inside
                var selectedElements = graph.getElements().filter(el => el.tag && el.tag != selectBoxElement.tag && el.getBBox().intersect(selectBoxElement.getBBox()));
                if (selectedElements.length > 0) {
                    //console.log(`DragEnd: Total ${selectedElements.length} intersected items.`);
                    selectedElements.forEach(el => {
                        if (!el.get('parent')) {
                            selectBoxElement.embed(el);
                            //console.log(`DragEnd: Tag '${el.tag}' is embedded to '${el.get('parent')}'.`);
                        } else {
                            //console.log(`DragEnd: Tag '${el.tag}' has parent '${el.get('parent')}', hence embedding skipped.`);
                        }
                    });
                }
            }
        }
    }


    paper.on("blank:pointerdown", (evt, x, y) => dragStart(evt, x, y));
    //paper.on("element:pointerdown", (_, evt, x, y) => dragStart(evt, x, y));

    paper.on("blank:pointermove", (evt, x, y) => drag(evt, x, y));
    //paper.on("element:pointermove", (_, evt, x, y) => drag(evt, x, y));

    paper.on("blank:pointerup", (evt) => dragEnd(evt));
    //paper.on("element:pointerup", (_, evt) => dragEnd(evt));


    graph.on('change:source change:target', async function (link) {
        console.log(`Checking link ....`);
        if (!link instanceof shapes.standard.Link) return;
        if (link.get('source').id && link.get('target').id) {
            // both ends of the link are connected.
            var source = graph.getCell(link.get('source'));
            var target = graph.getCell(link.get('target'));
            link.attr(`line\stroke`, '#999999');
            console.log(`Checking link between '${source.tag}' and ${target.tag}....`);
            // validate the link at server side based on the business cases
            var validatedLink = await validateLinkFromServer(link);

            if (validatedLink.prop('valid')) {
                // if the validation is successsfull, update the source/target (bus/port) data
                // assign tag to this link
                var sourcePortId = link.prop('source/port');
                var targetPortId = link.prop('target/port');
                link.tag = getLinkTag(link);
                // if the source/targer are not bus, then make the magnet passive
                // so that no further connection from the same port is possible
                if (source.type !== "bus") {
                    //source.getPort(sourcePortId).getAttribute('magnet') = 'passive';
                    source.attr(`ports/${sourcePortId}/magnet`, 'passive');
                }
                if (target.type !== "bus") {
                    //target.getPort(targetPortId).getAttribute('magnet') = 'passive';
                    target.attr(`ports/${targetPortId}/magnet`, 'passive');
                }

                // if source/targets are bus, distribute the ports
                // along the bus length as per no of links connected to this bus
                if (source.type == "bus") busPortDistribution(source.id);
                if (target.type == "bus") busPortDistribution(target.id);

            } else {
                // else remove the created link
                removeLink(link);
                if (source.type == "bus") busPortDistribution(source.id);
                if (target.type == "bus") busPortDistribution(target.id);
                console.log(`Created link between '${source.tag}' and ${target.tag} not valid, hence removed`);
            }
        }
    })


    // additional code for testing purpose


    // Define the port configuration
    var port = {
        position: {name: 'right', args: {y: '0%'}},
        label: {position: {name: 'top', args: {x: 6}}, markup: [{tagName: 'text', selector: 'label'}]},
        attrs: {
            portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: 'blue', strokeWidth: 1,},
            label: {text: 'port'}
        },
        markup: [{tagName: 'circle', selector: 'portBody'},]
    };

    var portsIn = {
        position: {
            name: 'left',
            args: {y: -10} // Adjust as needed
        },
        label: {
            position: {name: 'top', args: {y: -6}},
            markup: [{tagName: 'text', selector: 'label', className: 'label-text'}]
        },
        attrs: {portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047'}, label: {text: 'in'}},
        markup: [{tagName: 'circle', selector: 'portBody'}],
    };

    var portsOut = {
        position: {name: 'left', args: {y: '100%'}},
        label: {
            position: {name: 'right', args: {y: 6}},
            markup: [{tagName: 'text', selector: 'label', className: 'label-text'}]
        },
        attrs: {portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: '#E6A502', stroke: '#023047'}, label: {text: 'out'}},
        markup: [{tagName: 'circle', selector: 'portBody'}],
    };


    const rect = new shapes.standard.Rectangle({
        position: {x: 50, y: 1250},
        size: {width: 100, height: 120},
        attrs: {
            root: {title: 'Port Example'},
            body: {fill: 'lightblue',},
            label: {text: 'Port \nRectangle', fontSize: 12},
        },
        ports: {
            groups: {'in': portsIn, 'out': portsOut, 'any': port}
        },
    });

    rect.addPorts([
        //{ group: 'in', attrs: { label: { text: 'in0' } } },
        {id: 'in1', group: 'in', attrs: {label: {text: 'in'}}},
        {id: 'out1', group: 'out', attrs: {label: {text: 'out'}}},
        {id: 'anyPort1', group: 'any', attrs: {label: {text: 'anyPort'}}}
    ]);
    rect.tag = "Port-Rectangle";
    // ....
    // add another port to group 'a'.
    //rect.addPort(port);
    rect.addTo(graph);

    // get position of the port
    var portId = 'anyPort1';
    var portIndex = rect.getPortIndex(portId);
    // set args on newly added
    rect.addPort({group: 'anyPort', args: {y: '60%'}});
    // update existing
    rect.portProp(portId, 'args/y', '30%');


    rect.portProp(portId, 'attrs/label/text', 'just chill'); // { name: 'right', args: { y: '60%' } });
    //rect.prop('ports/items/' + portIndex + '/position', { name: 'right', args: { y: '60%' } });


}


//function linkTag(source, target, sourcePortIndex, targetPortIndex) {
//    return (source.tag > target.tag) ?
//        `${source.tag}:${sourcePortIndex} - ${target.tag}:${targetPortIndex}` :
//        `${target.tag}:${targetPortIndex} - ${source.tag}:${sourcePortIndex}`;
//}


function getLinkTag(link) {
    var source = graph.getCell(link.get('source'));
    var target = graph.getCell(link.get('target'));
    var sourcePortId = link.prop('source/port');
    var targetPortId = link.prop('target/port');
    var sourcePortIndex = source.getPortIndex(sourcePortId);
    var targetPortIndex = target.getPortIndex(targetPortId);

    return (source.tag > target.tag) ?
        `${source.tag}:${sourcePortIndex} - ${target.tag}:${targetPortIndex}` :
        `${target.tag}:${targetPortIndex} - ${source.tag}:${sourcePortIndex}`;
}


function getLinkData(link) {
    var sPort = link.prop('source/port');
    var tPort = link.prop('target/port');
    //var sM = graph.getCell(link.prop('source').id);
    var sM = graph.getElements().find(el => el.id === link.prop('source').id);
    var tM = graph.getElements().find(el => el.id === link.prop('target').id);

    return {
        'sourceTag': sM.tag,
        'targetTag': tM.tag,
        'sourcePort': sPort ? sPort : "",
        'targetPort': tPort ? tPort : ""
    };
}


function busPortDistribution(busId) {
    // function to distribute the ports along the bus length
    // as per no of links connected to this bus

    // pick the bus from graph and update there, does not return any
    var bus = graph.getCell(busId);
    // all busses have only one port group 'in' and default one port
    // ports are numbered with 1, 2, 3...
    // default port 1 cannot be deleted

    // check all the links connected to this bus
    const allLinks = graph.getConnectedLinks(bus);

    // check existing ports 
    var ports = bus.getGroupPorts('in');
    var busWidth = bus.prop('size/width'); // busWidth not required as the positions are refX in percentage

    // check up-side and down-side connection requirement
    var upLinks = [];
    var downLinks = [];

    // assign other end position (x,y) fpr filter purpose
    allLinks.forEach(link => {
        var source = graph.getCell(link.prop('source').id);
        var target = graph.getCell(link.prop('target').id);
        var otherEnd = source.id === bus.id ? target : source;
        link.otherEndX = otherEnd.prop('position/x');
        link.otherEndY = otherEnd.prop('position/y');
    });

    // arrange links as per the X value of the other end of the links
    allLinks.sort((a, b) => a.otherEndX - b.otherEndX);

    allLinks.forEach(link => {
        if (link.otherEndY < bus.prop('position/y')) {
            upLinks.push(link);
        } else {
            downLinks.push(link);
        }

        // below code is to compare based on the absolute position of the ports, not just element position
        //var otherEndPortTag = source.id === bus.id ? link.prop('target/port') : link.prop('source/port');
        //var otherEndPortPosition = otherEnd.getPortsPositions(otherEndPortTag);
        //var otherEndPortPositionAbsoluteY = otherend.prop('position/y') + otherEndPortPosition.y;
    });


    // total no of ports is equal to total no of connections
    var reqPorts = upLinks.length + downLinks.length;
    console.log(`Bus port postion distribution for bus tag '${bus.tag}' having total ${allLinks.length} connections : ` +
        `Upside links : ${upLinks.length} and Downside links ${downLinks.length}, total available ports ${ports.length} and required ports ${reqPorts}.`);

    // Remove ports if the required no. of ports are less than the existing ports (however retain minimum 1)
    if (reqPorts < ports.length && ports.length > 1) {
        for (let i = ports.length; i > upLinks.length; i--) {
            bus.removePort(`{i}`);
        }
    }

    // Add ports if the required no. of ports are more than the existing ports
    if (reqPorts > ports.length) {

        // Define new ports
        var newPorts = [];
        for (let i = ports.length; i < reqPorts; i++) {
            newPorts.push({id: `${i + 1}`, group: 'in', position: {name: 'absolute', args: {x: `0%`, y: 0}}});
        }

        // Add new ports to the element
        newPorts.forEach(port => {
            bus.addPort({
                ...port,
                attrs: {
                    portBody: {magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047'},
                    label: {text: port.id, fontSize: 8}
                },
                markup: [{tagName: 'circle', selector: 'portBody'}]
            });
        });
    }

    // Define new positions to the arranged ports as per final port nos
    // and Update the ports' positions
    for (let i = 0; i < upLinks.length; i++) {
        // position for up-side ports
        var pos = `${Math.round((i + 0.5) * 100 / upLinks.length)}%`;
        console.log(`Bus tag '${bus.tag}' postion for Port #'${i + 1}' (up-link # ${i + 1}) '${pos}`);
        bus.portProp(`${i + 1}`, 'args/x', pos);
        bus.portProp(`${i + 1}`, 'attrs/label/refX', 3);
        bus.portProp(`${i + 1}`, 'attrs/label/refY', -10);
        // reassign the corresponding port for up-side links
        var end = upLinks[i].prop('source').id == bus.id ? 'source' : 'target';
        var portId = bus.getPorts()[i].id;

        if (end == 'source') upLinks[i].set({source: {id: busId, port: portId}});
        if (end == 'target') upLinks[i].set({target: {id: busId, port: portId}});


        var end = upLinks[i].prop('source') == bus.id ? 'source' : 'target';
        upLinks[i].prop(`${end}'/port`, `${i + 1}`);
    }
    for (let i = 0; i < downLinks.length; i++) {
        // position for down-side ports
        var pos = `${Math.round((i + 0.5) * 100 / downLinks.length)}%`;
        console.log(`Bus tag '${bus.tag}' postion for Port #'${upLinks.length + i + 1}' (down-link # ${i + 1}) '${pos}`);
        bus.portProp(`${upLinks.length + i + 1}`, 'args/x', pos);
        bus.portProp(`${upLinks.length + i + 1}`, 'attrs/label/refX', 3);
        bus.portProp(`${upLinks.length + i + 1}`, 'attrs/label/refY', 10);
        // reassign the corresponding port for down-side links
        var end = downLinks[i].prop('source').id == bus.id ? 'source' : 'target';
        var portId = bus.getPorts()[upLinks.length + i].id;

        if (end == 'source') downLinks[i].set({source: {id: busId, port: portId}});
        if (end == 'target') downLinks[i].set({target: {id: busId, port: portId}});

    }
    // later : if up and down are same or both are odd or in LCM, there are common ports

}


async function validateLinkFromServer(link) {

    let linkData = getLinkData(link);
    
    try {
        const success = await safeInvokeAsync(
            dotNetObjSLD,
            'SLDValidateLink',
            JSON.stringify(linkData)
        );

        link.set('valid', success);

        if (!success) {
            // Optionally notify user
            console.warn(`Link validation failed: ${linkData.sourceTag} → ${linkData.targetTag}`);
        }
    } catch (e) {
        console.error(`Validation error:`, e);
        link.set('valid', false);
        // Consider showing user notification
    }

    return link;
}


async function removeLink(link) {
    var linkData = getLinkData(link);

    var success = await safeInvokeAsync(
            dotNetObjSLD, 'SLDRemoveLink', JSON.stringify(linkData));
        if (success) {
            link.remove(); // Remove the link from the graph
            console.log(`Removal success of the Link between '${linkData.sourceTag} and ${linkData.targetTag}' : ${success}.`);
        }
}


function isOverlapping(element, graph) {
    // Function to check if an element overlaps with any other elements
    const bbox = element.getBBox();
    return graph.getCells().some(cell => {
        if (cell === element) return false;
        const cellBBox = cell.getBBox();
        return bbox.intersect(cellBBox);
    });
}


function findNearestEmptyPosition(element, graph, spacing = 20) {
    // Function to find the nearest empty position
    // Adjust spacing as needed
    let position = element.get('position');
    let newPosition = {...position};

    while (isOverlapping(element, graph)) {
        // Move the element to a new position
        newPosition.x += spacing;
        if (newPosition.x > paper.options.width) {
            newPosition.x = 0;
            newPosition.y += spacing;
        }

        // Ensure newPosition is within paper bounds
        // and away from template (x>100) and top margin 50
        newPosition.x = Math.max(100, Math.min(newPosition.x, paper.options.width - element.getBBox().width));
        newPosition.y = Math.max(50, Math.min(newPosition.y, paper.options.height - element.getBBox().height));

        element.set('position', newPosition);
    }

    return newPosition;
}


function updateBus(busModel, busInfo) {

    if (!busInfo || !busModel) {
        console.error('Invalid parameters for updateBus');
        return busModel;
    }

    // Safe access with defaults
    const isc = typeof busInfo.ISC === 'number' ? busInfo.ISC : 0;
    const vr = typeof busInfo.VR === 'number' ? busInfo.VR : 0;

    if (busInfo.Category === "Swing") {
        // grid            
        busModel.attr({
            label: {text: "Grid" + sanitizeText(busInfo.Tag)},
            ratedSC: {text: sanitizeText(Math.round(10 * busInfo.ISC) / 10 + "kA")},
            ratedVoltage: {text: sanitizeText(busInfo.VR / 1000 + "kV")},
            busFaultkA: {text: sanitizeText(Math.round(10 * busInfo.SCkAaMax) / 10 + "kA")},
            operatingVoltage: {text: sanitizeText(Math.round(10000 * busInfo.Vo.Magnitude) / 100 + "% ∠" + Math.round(busInfo.Vo.Phase * 1800 / Math.PI) / 10 + "°")}
        });
    } else {
        // the other bus
        busModel.attr({
            label: {text: sanitizeText(busInfo.Tag)},
            ratedSC: {text: sanitizeText(Math.round(10 * busInfo.ISC) / 10 + "kA")},
            ratedVoltage: {text: sanitizeText(busInfo.VR / 1000 + "kV")},
            busFault: {text: sanitizeText(Math.round(10 * busInfo.SCkAaMax) / 10 + "kA")},
            operatingVoltage: {text: sanitizeText(Math.round(10000 * busInfo.Vo.Magnitude) / 100 + "% ∠" + Math.round(busInfo.Vo.Phase * 1800 / Math.PI) / 10 + "°")}
        });
    }
    return busModel;
}


function updateTransformer(trafoModel, trafoInfo, branches) {

    let branch = branches.find(br => br.Tag === trafoInfo.Tag);

    trafoModel.attr({
        tag: {text: sanitizeText(trafoInfo.Tag)},
        voltage: {text: `${trafoInfo.V1 / 1000}/${trafoInfo.V2 / 1000}kV`},
        kVArating: {text: `${trafoInfo.KVA}kVA`},
        impedance: {text: `Z:${trafoInfo.Z}%`},
        loading: {text: `${Math.round(10 * branch?.KW) / 10}KW ${Math.round(10 * branch?.KVAR) / 10}kVAR`}
    });
    return trafoModel;
}

function updateCable(cableModel, cabledata, branches) {

    var branch = branches.find(br => br.Tag === cabledata.Tag);

    cableModel.attr({
        label: {text: sanitizeText(cabledata.Tag)},
        size: {text: cabledata.CblDesc},
        length: {text: `${cabledata.L}m, ${cabledata.Rl}-j${cabledata.Xl}Ω/km`},
        impedance: {text: `R:${cabledata.R}, X:${cabledata.X}`},
        operatingCurrent: {text: `${Math.round(10 * branch?.Io.Magnitude) / 10}A ∠${Math.round(branch?.Io.Phase * 1800 / Math.PI) / 10}°`}
    });

    return cableModel;
}

function updateBusDuct(busDuctModel, busDuctdata, branches) {

    var branch = branches.find(br => br.Tag == busDuctdata.Tag);
    busDuctModel.attr({
        label: {text: sanitizeText(busDuctdata.Tag)},
        size: {text: `${busDuctdata.IR}A`},
        length: {text: `${busDuctdata.L}m, ${Math.round(1000 * busDuctdata.Rl) / 1000}-j${Math.round(1000 * busDuctdata.Xl) / 1000}Ω/km`},
        impedance: {text: `R:${Math.round(10000 * busDuctdata.R) / 10000}, X:${Math.round(10000 * busDuctdata.X) / 10000}`},
        operatingCurrent: {text: `${Math.round(10 * branch.Io.Magnitude) / 10}A ∠${Math.round(branch.Io.Phase * 1800 / Math.PI) / 10}°`}
    });
    return busDuctModel;
}


function updateMotor(motorModel, motordata, branches) {

    var branch = branches.find(br => br.Tag == motordata.Tag);
    motorModel.attr({
        // label: { text: busDuctdata.Tag },
        // size: { text: `${busDuctdata.IR}A` },
        // length: { text: `${busDuctdata.L}m, ${Math.round(1000 * busDuctdata.Rl) / 1000}-j${Math.round(1000 * busDuctdata.Xl) / 1000}Ω/km` },
        // impedance: { text: `R:${Math.round(10000 * busDuctdata.R) / 10000}, X:${Math.round(10000 * busDuctdata.X) / 10000}` },
        // operatingCurrent: { text: `${Math.round(10 * branch.Io.Magnitude) / 10}A ∠${Math.round(branch.Io.Phase * 1800 / Math.PI) / 10}°` }
    });
    return motorModel;
}

function updateHeater(heaterModel, heaterdata, branches) {

    var branch = branches.find(br => br.Tag == heaterdata.Tag);
    heaterModel.attr({
        // label: { text: busDuctdata.Tag },
        // size: { text: `${busDuctdata.IR}A` },
        // length: { text: `${busDuctdata.L}m, ${Math.round(1000 * busDuctdata.Rl) / 1000}-j${Math.round(1000 * busDuctdata.Xl) / 1000}Ω/km` },
        // impedance: { text: `R:${Math.round(10000 * busDuctdata.R) / 10000}, X:${Math.round(10000 * busDuctdata.X) / 10000}` },
        // operatingCurrent: { text: `${Math.round(10 * branch.Io.Magnitude) / 10}A ∠${Math.round(branch.Io.Phase * 1800 / Math.PI) / 10}°` }
    });
    return heaterModel;
}

function updateCapacitor(capacitorModel, capacitordata, branches) {

    var branch = branches.find(br => br.Tag == capacitordata.Tag);
    capacitorModel.attr({
        // label: { text: busDuctdata.Tag },
        // size: { text: `${busDuctdata.IR}A` },
        // length: { text: `${busDuctdata.L}m, ${Math.round(1000 * busDuctdata.Rl) / 1000}-j${Math.round(1000 * busDuctdata.Xl) / 1000}Ω/km` },
        // impedance: { text: `R:${Math.round(10000 * busDuctdata.R) / 10000}, X:${Math.round(10000 * busDuctdata.X) / 10000}` },
        // operatingCurrent: { text: `${Math.round(10 * branch.Io.Magnitude) / 10}A ∠${Math.round(branch.Io.Phase * 1800 / Math.PI) / 10}°` }
    });
    return capacitorModel;
}

function updateLumpLoad(lumpLoadModel, lumpLoaddata, branches) {

    var branch = branches.find(br => br.Tag == lumpLoaddata.Tag);
    lumploadModel.attr({
        //label: { text: busDuctdata.Tag },
        //size: { text: `${busDuctdata.IR}A` },
        //length: { text: `${busDuctdata.L}m, ${Math.round(1000 * busDuctdata.Rl) / 1000}-j${Math.round(1000 * busDuctdata.Xl) / 1000}Ω/km` },
        //impedance: { text: `R:${Math.round(10000 * busDuctdata.R) / 10000}, X:${Math.round(10000 * busDuctdata.X) / 10000}` },
        //operatingCurrent: { text: `${Math.round(10 * branch.Io.Magnitude) / 10}A ∠${Math.round(branch.Io.Phase * 1800 / Math.PI) / 10}°` }
    });
    return lumploadModel;
}

// Add cleanup function
export function disposeSLD() {
    if (graph) {
        graph.clear();
        graph = null;
    }
    if (paper) {
        paper.remove();
        paper = null;
    }
    sldComponentsJS = [];
    dotNetObjDraw = null;
    dotNetObjSLD = null;
    console.log("SLD disposed");
}