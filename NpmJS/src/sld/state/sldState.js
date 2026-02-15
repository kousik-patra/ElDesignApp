/**
 * Global state management for SLD
 */

class SLDState {
    constructor() {
        this.dotNetObjDraw = null;
        this.dotNetObjSLD = null;
        this.graph = null;
        this.paper = null;
        this.sldComponents = [];
        this.sldComponentsString1 = null;
        this.ox = null;
        this.oy = null;
        this.busesElement   = [];
        this.branchElement  = [];
        this.loadElement  = [];
        this.swbdElement    = [];
        this.switchElement    = [];
        this.elementMoveInteractive = true;

        this.buses        = [];
        this.branches     = [];
        this.loads        = [];
        this.transformers = [];
        this.cables       = [];
        this.busDucts     = [];
        this.sldComponents = [];
        this.switchboards = [];
        this.switches =     [];
    }

    setDotNetObjDraw(obj) {
        this.dotNetObjDraw = obj;
    }

    setDotNetObjSLD(obj) {
        this.dotNetObjSLD = obj;
    }

    setGraph(graph) {
        this.graph = graph;
    }

    setPaper(paper) {
        this.paper = paper;
    }

    setSLDComponents(components) {
        this.sldComponents = components;
    }

    setSLDComponentsString(str) {
        this.sldComponentsString1 = str;
    }
    setOxy(x, y) {
        this.ox = x;
        this.oy = y;
    }
    
    setBusesElement(busesElement) {
        this.busesElement = busesElement;
    }
    setBranchElement(branchElement) {
        this.branchElement = branchElement;
    }

    setLoadElement(loadElement) {
        this.loadElement = loadElement;
    }
    
    setSwbdElement(swbdElement) {
        this.swbdElement = swbdElement;
    }

    setElementMoveInteractive(elementMoveInteractive) {
        this.elementMoveInteractive = elementMoveInteractive;
    }


    setBuses(list) {
        this.buses = list;
    }
    getBuses() {
        return this.buses ;
    }

    setBranches(list) {
        this.branches = list;
    }
    getBranches() {
        return this.branches ;
    }

    setLoads(list) {
        this.loads = list;
    }
    getLoads() {
        return this.loads ;
    }

    setTransformers(list) {
        this.transformers = list;
    }
    getTransformers() {
        return this.transformers ;
    }

    setCables(list) {
        this.cables = list;
    }
    getCables() {
        return this.cables ;
    }

    setBusDucts(list) {
        this.busDucts = list;
    }
    getBusDucts() {
        return this.busDucts ;
    }

    setSwitchboards(list) {
        this.switchboards = list;
    }
    getSwitchboards() {
        return this.switchboards ;
    }


    setSwitches(list) {
        this.switches = list;
    }
    getSwitches() {
        return this.switches ;
    }

    setSwitchElement(list) {
        this.switchElement = list;
    }
    getSwitchElement() {
        return this.switchElement ;
    }
  
    

    getDotNetObjDraw() {
        return this.dotNetObjDraw;
    }

    getDotNetObjSLD() {
        return this.dotNetObjSLD;
    }

    getGraph() {
        return this.graph;
    }

    getPaper() {
        return this.paper;
    }

    getSLDComponents() {
        return this.sldComponents;
    }

    getSLDComponentsString() {
        return this.sldComponentsString1;
    }

    getOxy() {
        return { ox: this.ox, oy: this.oy };
    }

    getBusesElement() {
        return this.busesElement;
    }
    getBranchElement() {
        return this.branchElement;
    }

    getLoadElement() {
        return this.loadElement;
    }
    getSwbdElement() {
        return this.swbdElement;
    }

    getElementMoveInteractive() {
        return this.elementMoveInteractive;
    }

    clear() {
        this.sldComponents = [];
        this.dotNetObjDraw = null;
        this.dotNetObjSLD = null;
        
        if (this.graph) {
            this.graph.clear();
            this.graph = null;
        }
        
        if (this.paper) {
            this.paper.remove();
            this.paper = null;
        }
        
        this.busesElement = [];
        this.branchElement = [];
        this.swbdElement = [];
        this.elementMoveInteractive = null;
        
        console.log("SLD state cleared");
    }
}

// Export singleton instance
export const sldState = new SLDState();
