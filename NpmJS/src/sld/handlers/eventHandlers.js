/**
 * Event handlers for paper and elements
 */
import {dia, linkTools, shapes} from '@joint/core';
import { propertyButton, infoButton, removeButton, validateButton } from '../tools/buttons.js';
import { sldState } from '../state/sldState.js';
import {setupSwitch } from './switchEventHandle';
import {setupTemplateDragHandlers } from './templateDragDrop';
import {setupPointerOnBusBar } from './busEventHandlers';
import { setupSelectionHandlers } from './elementSelectHandle'

/**
 * Setup element event handlers
 * @param {Object} paper - JointJS paper instance
 */
export function setupElementHandlers(paper) {
    // Element mouse enter - show property button
    paper.on('element:mouseenter', function (elementView) {
        let tools = new dia.ToolsView({tools: [propertyButton]});

        // no need to add property if i) template, ii) select box, iii) node
        if (elementView.model.prop('tag')  
            && !elementView.model.prop('tag').includes('template')
            && !elementView.model.prop('tag').includes('selectbox')
            && !(elementView.model.prop('node') === true) 
        ) {
            elementView.addTools(tools);
        }
    });

    // Element mouse leave - remove tools
    paper.on('element:mouseleave', function (elementView) {
        if (elementView.hasTools) elementView.removeTools();
    });
}

/**
 * Setup link event handlers
 * @param {Object} paper - JointJS paper instance
 */
export function setupLinkHandlers(paper) {
    // Link mouse enter - show tools
    paper.on('link:mouseenter', function (linkView) {
        var tools = new dia.ToolsView({
            tools: [
                new linkTools.Vertices(),
                new linkTools.Segments(),
                infoButton,
                removeButton,
                validateButton
            ]
        });

        var toolsR = new dia.ToolsView({
            tools: [
                new linkTools.Vertices(),
                new linkTools.Segments(),
                infoButton,
                removeButton
            ]
        });

        linkView.addTools(tools);
    });

    // Link mouse leave - remove tools
    paper.on('link:mouseleave', function (linkView) {
        linkView.removeTools();
    });
}

/**
 * Setup position change handlers
 * @param {Object} paper - JointJS paper instance
 */
export function setupPositionHandlers(paper) {
    const sldComponents = sldState.getSLDComponents();

    // Element position change handler
    paper.on('element:pointerup', function (elementView, evt, x, y) {
        console.log("eventhandler poiner up");
        if (!elementView.model.prop('tag') || elementView.model.prop('tag').includes('template')) return; // template handled separately
        if (elementView.model.prop('elementType') === 'bus') return;  // bus click handler is separate

        console.log("eventhandler poiner up - non-template , non-bus");
        
        const tag = elementView.model.prop('tag');
        const type = elementView.model.prop('elementType');
        const position = elementView.model.prop('position');
        
        let componentData = {
            SLD: 'key',
            Tag: tag,
            Type: type,
            PropertyJSON: JSON.stringify(position)
        };

        // Update or add to sldComponents
        const existingIndex = sldComponents.findIndex(c => c.Tag === tag && c.Type === type);
        if (existingIndex >= 0) {
            sldComponents[existingIndex] = componentData;
        } else {
            sldComponents.push(componentData);
        }
        
        sldState.setSLDComponents(sldComponents);
    });

    // Link vertices change handler
    paper.on('link:pointerup', function (linkView, evt, x, y) {
        const link = linkView.model;
        if (!link.prop('tag')) return;

        const vertices = link.vertices();
        
        let componentData = {
            SLD: 'key',
            Tag: link.prop('tag'),
            Type: "link",
            PropertyJSON: JSON.stringify(vertices)
        };

        // Update or add to sldComponents
        const existingIndex = sldComponents.findIndex(c => c.Tag === link.prop('tag') && c.Type === "link");
        if (existingIndex >= 0) {
            sldComponents[existingIndex] = componentData;
        } else {
            sldComponents.push(componentData);
        }
        
        sldState.setSLDComponents(sldComponents);
    });
}

export function setupPointerOnBlankArea(paper) {
    paper.on("blank:pointerdown", (evt, x, y) => dragStart(paper, evt, x, y));
    paper.on("blank:pointermove", (evt, x, y) => drag(paper, evt, x, y));
    paper.on("blank:pointerup", (evt) => dragEnd(paper, evt));
}

/**
 * Setup all event handlers
 * @param {Object} paper - JointJS paper instance
 */
export function setupAllHandlers(paper) {
    setupElementHandlers(paper);
    setupLinkHandlers(paper);
    setupPositionHandlers(paper);
    setupPointerOnBlankArea(paper);
    setupTemplateDragHandlers(paper);
    setupPointerOnBusBar(paper);
    setupSwitch(paper);
    setupSelectionHandlers(paper);
}








    function dragStart(paper, evt, x, y) {
        // remove previously created selectboxes
        sldState.setOxy(x,y);
        const graph = sldState.getGraph();
        let existingSelectBox = graph.getElements().find(el => el.prop('tag') && el.prop('tag') === "selectbox");
        if (existingSelectBox) {
            // unembed elements before removing this selectbox
            let allChildren = graph.getElements();
            //console.log(existingSelectBox.id);
            //allChildren.forEach(child => console.log(child.id, " - ", child.parent(), " ."));
            let children = allChildren.filter(el => 
                el.get('parent') && graph.getCell(el.get('parent')).prop('tag') === "selectbox");
            if (children && children.length > 0) {
                children.forEach(child => {
                    // remove from the selection boundary
                    //console.log(`DragStart: ${child.prop('tag')} was earlier embedded in parent '${child.get('parent')}', to be un-embedded.`);
                    existingSelectBox.unembed(child);
                    //console.log(`DragStart: ${child.prop('tag')} is un-embedded now and hence has parent '${child.get('parent')}' post un-embedding.`);
                    // unhighlight the child element
                    //var childView = paper.findView(child);
                    //highlighters.mask.remove(childView);
                });
            }
            //console.log(`DragStart: Earlier Select box '${existingSelectBox.id}' removed`);
            existingSelectBox.remove()
        }
        //console.log(`Select box '${existingSelectBox.id}' removed`);
        let selectBox = new shapes.standard.Rectangle({
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
        selectBox.prop('tag', "selectbox");
        selectBox.prop('elementType',"selectbox");
        selectBox.addTo(graph);

        sldState.setGraph(graph)

    }


    function drag(paper, evt, x, y) {
        const graph = sldState.getGraph();
        let selectBoxElement = graph.getElements().find(el => el.prop('tag') === "selectbox");
        let {ox, oy } = sldState.getOxy();
        if (selectBoxElement) {
            selectBoxElement.prop('size/width', Math.abs(ox - x));
            selectBoxElement.prop('size/height', Math.abs(oy - y));
            selectBoxElement.prop('position/x', Math.min(ox, x));
            selectBoxElement.prop('position/y', Math.min(oy, y));
        }
    }

    function dragEnd(paper, evt) {

        const graph = sldState.getGraph();

        let selectBoxElement = graph.getElements().find(el => el.prop('tag') === "selectbox");
        if (selectBoxElement) {
            // if it is just a click without substantial drag, then the created selectBox to be removed
            let dx = selectBoxElement.prop('size/width');
            let dy = selectBoxElement.prop('size/height');
            
            if (dx < 5 && dy < 5) {
                // unembed elements, if any, before removing this selectbox
                let allChildren = graph.getElements();
                let children = allChildren.filter(el =>
                    el.get('parent') && graph.getCell(el.get('parent')).prop('tag') === "selectbox");
                if (children && children.length > 0) {
                    children.forEach(child => {
                        // remove from the selection boundary
                        //console.log(`DragEnd: Insufficient drag : ${child.prop('tag')} was embedded in parent '${child.get('parent')}' , to be un-embedded.`);
                        selectBoxElement.unembed(child);
                        //console.log(`DragEnd: Insufficient drag : ${child.prop('tag')} is un-embedded now and hence has parent '${child.get('parent')}' post un-embedding.`);
                    });
                }
                selectBoxElement.remove();
                //console.log(`DragEnd: Insufficient drag : ${selectBoxElement.prop('tag')} is  removed as it's not substatially dragged.`)
            } else {
                // retain the select box and embed all the elements inside
                let selectedElements = graph.getElements().filter(el => 
                    el.prop('tag') && el.prop('tag') !== selectBoxElement.prop('tag') && el.getBBox().intersect(selectBoxElement.getBBox()));
                if (selectedElements.length > 0) {
                    //console.log(`DragEnd: Total ${selectedElements.length} intersected items.`);
                    selectedElements.forEach(el => {
                        if (!el.get('parent')) {
                            selectBoxElement.embed(el);
                            //console.log(`DragEnd: Tag '${el.prop('tag')}' is embedded to '${el.get('parent')}'.`);
                        } else {
                            //console.log(`DragEnd: Tag '${el.prop('tag')}' has parent '${el.get('parent')}', hence embedding skipped.`);
                        }
                    });
                }
            }
        }
    }