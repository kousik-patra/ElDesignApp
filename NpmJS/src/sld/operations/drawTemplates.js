
import {
    GridElement,
    TransformerElement,
    BusElement,
    CableElement,
    BusDuctElement,
    MotorElement,
    HeaterElement,
    CapacitorElement,
    LumpLoadElement,
    SwitchElement
} from '../shapes';

import { sldState } from '../state/sldState.js';


// ── Template elements (palette on left side) ────────────────────────────
export function drawTemplates() {

    drawGridTemplate(true);
    drawBusTemplate(true);
    drawCableTemplate(true);
    drawTransformerTemplate(true);
    drawBusDuctTemplate(true);
    drawCapacitorTemplate(true);
    drawMotorTemplate(true);
    drawHeaterTemplate(true);
    drawLumpLoadTemplate(true);
    drawLumpLoadTemplate(true);
    drawSwitchTemplate(true);
}

export function drawGridTemplate(clone = false) {
    let graph = sldState.getGraph();

    const templateGridElement = new GridElement({
        tag: 'templateGridElement',
        position: { x: 50, y: 100 },
        size: { width: 20, height: 80 }
    });
    templateGridElement.addTo(graph);
    if(clone) {
        const clonedElement = templateGridElement.clone();
        clonedElement.addTo(graph);
    }

    sldState.setGraph(graph);
    return templateGridElement;
}

export function drawBusTemplate(clone = false) {
    let graph = sldState.getGraph();

    const templateBusElement = new BusElement({
        position: {x: 25, y: 200},
        size: {width: 50, height: 0},
        attrs: {body: {x1: 0, y1: 0, x2: 50, y2: 0}}
    });
    templateBusElement.addTo(graph);
    if(clone) {
        const clonedElement = templateBusElement.clone();
        clonedElement.addTo(graph);
    }

    sldState.setGraph(graph);
    return templateBusElement;
}

export function drawCableTemplate(clone = false) {
    let graph = sldState.getGraph();

    const templateCableElement = new CableElement({
        tag: 'templateCableElement',
        position: { x: 50, y: 300 },
        size: { width: 10, height: 60 }
    });
    templateCableElement.addTo(graph);
    if(clone) {
        const clonedElement = templateCableElement.clone();
        clonedElement.addTo(graph);
    }

    sldState.setGraph(graph);
    return templateCableElement;
}

export function drawTransformerTemplate(clone = false) {
    let graph = sldState.getGraph();

    const templateTransformerElement = new TransformerElement({
        tag: 'templateTransformerElement',
        position: { x: 50, y: 400 },
        size: { width: 15, height: 15 }
    });
    templateTransformerElement.addTo(graph);
    if(clone) {
        const clonedElement = templateTransformerElement.clone();
        clonedElement.addTo(graph);
    }

    sldState.setGraph(graph);
    return templateTransformerElement;
}

export function drawBusDuctTemplate(clone = false) {
    let graph = sldState.getGraph();

    const templateBusDuctElement = new BusDuctElement({
        tag: 'templateBusDuctElement',
        position: { x: 50, y: 500 },
        size: { width: 10, height: 60 }
    });
    templateBusDuctElement.addTo(graph);
    if(clone) {
        const clonedElement = templateBusDuctElement.clone();
        clonedElement.addTo(graph);
    }

    sldState.setGraph(graph);
    return templateBusDuctElement;
}

export function drawCapacitorTemplate(clone = false) {
    let graph = sldState.getGraph();

    const templateCapacitorElement = new CapacitorElement({
        tag: 'templateCapacitorElement',
        position: { x: 50, y: 600 },
        size: { width: 30, height: 60 }
    });
    templateCapacitorElement.addTo(graph);
    if(clone) {
        const clonedElement = templateCapacitorElement.clone();
        clonedElement.addTo(graph);
    }

    sldState.setGraph(graph);
    return templateCapacitorElement;
}

export function drawMotorTemplate(clone = false) {
    let graph = sldState.getGraph();

    const templateMotorElement = new MotorElement({
        tag: 'templateMotorElement',
        position: { x: 50, y: 700 },
        size: { width: 30, height: 60 }
    });
    templateMotorElement.addTo(graph);
    if(clone) {
        const clonedElement = templateMotorElement.clone();
        clonedElement.addTo(graph);
    }

    sldState.setGraph(graph);
    return templateMotorElement;
}

export function drawHeaterTemplate(clone = false) {
    let graph = sldState.getGraph();

    const templateHeaterElement = new HeaterElement({
        tag: 'templateHeaterElement',
        position: { x: 50, y: 800 },
        size: { width: 30, height: 60 }
    });
    templateHeaterElement.addTo(graph);
    if(clone) {
        const clonedElement = templateHeaterElement.clone();
        clonedElement.addTo(graph);
    }

    sldState.setGraph(graph);
    return templateHeaterElement;
}

export function drawLumpLoadTemplate(clone = false) {
    let graph = sldState.getGraph();

    const templateLumpLoadElement = new LumpLoadElement({
        tag: 'templateLumpLoadElement',
        position: { x: 50, y: 900 },
        size: { width: 30, height: 60 }
    });
    templateLumpLoadElement.addTo(graph);
    if(clone) {
        const clonedElement = templateLumpLoadElement.clone();
        clonedElement.addTo(graph);
    }
    
    sldState.setGraph(graph);   
    return templateLumpLoadElement;
}

export function drawSwitchTemplate(clone = false) {
    let graph = sldState.getGraph();

    const templateSwitchElement = new SwitchElement({
        tag: 'templateGridElement',
        position: { x: 50, y: 1000 },
        size: { width: 20, height: 60 }
    });
    templateSwitchElement.addTo(graph);
    if(clone) {
        const clonedElement = templateSwitchElement.clone();
        clonedElement.addTo(graph);
    }

    sldState.setGraph(graph);
    return templateSwitchElement;
}