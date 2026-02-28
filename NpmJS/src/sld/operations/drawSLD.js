/**
 * Main SLD drawing operation
 */
import { dia, shapes } from '@joint/core';
import { sldState } from '../state/sldState.js';
import { CONFIG } from '../config/constants.js';
import { sanitizeText } from '../utils/helpers.js';
import {
    GridElement,
    SwitchboardElement,
    TransformerElement,
    BusElement,
    CableElement,
    BusDuctElement,
    MotorElement,
    HeaterElement,
    CapacitorElement,
    LumpLoadElement,
    NodeElement,
    SwitchElement,
    BusBarLinkElement,
    FuseElement
} from '../shapes';
import { getLinkTag, updateLinkVertices } from '../links/linkOperations.js';
import { updateItemPosition, updatePositionLength } from '../utils/positionUtils.js';
import { updateTransformer, updateCable, updateBusDuct } from '../components/updaters.js';
import { busPortDistribution } from '../utils/busPortDistribution.js';
import {setupAllHandlers} from '../handlers/eventHandlers'
import{drawTemplates} from './drawTemplates.js';
import {updateNodeOrBus } from '../handlers/busEventHandlers'


export function drawSLD(
    divString, xGridSize, yGridSize,
    leftSpacing, topSpacing, xGridSpacing, yGridSpacing,
    busesString, switchboardString, branchesString, loadsString,
    transformersString, cablesString, busDuctsString,
    xyString, sldComponentsString,
    dotNetObjRef, dotNetObjSLDRef,
    chainElementCoordsJson,     // coordinates for all chain elements
    chainsJson,                 // chain structure (element ordering, bus ends)
    switchesJson,               // switch data
    fusesJson,                  // fuse data
    busBarLinksJson,             // bus bar link data
    tagArrayJson
) {

    // Branch link template
    const branchLink = new shapes.standard.Link({
        elementType: 'link',
        tag: '',
        sourceTag:'',
        targetTag:'',
        clicked: false,
        router: { name: 'manhattan' },
        connector: { name: 'rounded', args: { radius: 2 }, jumpover: { size: 6 } },
        attrs: {
            line: {
                stroke: '#333333', strokeWidth: 2,
                sourceMarker: { type: 'circle', r: 2, fill: 'none' },
                targetMarker: { type: 'circle', r: 2, fill: 'none' },
            },
        }
    });
    
    sldState.setDotNetObjDraw(dotNetObjRef);
    sldState.setDotNetObjSLD(dotNetObjSLDRef);

    // ── Parse JSON ──────────────────────────────────────────────────────────
    let buses, branches, loads, transformers, cables, busDucts, sldComponents, switchboards;
    let chainElementCoords, chains, switches, fuses, busBarLinks, tagArray;
    try {
        buses        = JSON.parse(busesString);
        branches     = JSON.parse(branchesString);
        loads        = JSON.parse(loadsString);
        transformers = JSON.parse(transformersString);
        cables       = JSON.parse(cablesString);
        busDucts     = JSON.parse(busDuctsString);
        sldComponents = JSON.parse(sldComponentsString);
        switchboards = JSON.parse(switchboardString);

        chainElementCoords = chainElementCoordsJson ? JSON.parse(chainElementCoordsJson) : [];
        chains             = chainsJson             ? JSON.parse(chainsJson)             : [];
        switches           = switchesJson           ? JSON.parse(switchesJson)           : [];
        fuses              = fusesJson              ? JSON.parse(fusesJson)              : [];
        busBarLinks        = busBarLinksJson         ? JSON.parse(busBarLinksJson)        : [];

        tagArray        = tagArrayJson         ? JSON.parse(tagArrayJson)        : [];


        buses        = Array.isArray(buses)        ? buses        : [];
        branches     = Array.isArray(branches)     ? branches     : [];
        loads        = Array.isArray(loads)        ? loads        : [];
        transformers = Array.isArray(transformers) ? transformers : [];
        cables       = Array.isArray(cables)       ? cables       : [];
        busDucts     = Array.isArray(busDucts)     ? busDucts     : [];
        sldComponents = Array.isArray(sldComponents) ? sldComponents : [];
        switchboards = Array.isArray(switchboards) ? switchboards : [];

        chainElementCoords = Array.isArray(chainElementCoords) ? chainElementCoords : [];
        chains        = Array.isArray(chains)         ? chains        : [];
        switches      = Array.isArray(switches)       ? switches      : [];
        fuses         = Array.isArray(fuses)          ? fuses         : [];
        busBarLinks   = Array.isArray(busBarLinks)    ? busBarLinks   : [];
        
        sldState.setAllItemTags(tagArray);
        
    } catch (e) {
        console.error('drawSLD: Error parsing JSON parameters:', e);
        return;
    }

    
    
    
    // ── Build coordinate lookup: Tag → {x, y} ──────────────────────────────
    // These are the server-computed positions for ALL chain elements
    // (branches + non-branches). Bus positions are on the bus objects themselves.
    const coordMap = {};
    chainElementCoords.forEach(c => {
        coordMap[c.Tag] = { x: c.X, y: c.Y };
    });
    console.log(`📍 Loaded ${Object.keys(coordMap).length} chain element coordinates from server`);

// ── DIAGNOSTIC — add temporarily after coordMap is built ────────────────
    console.log('📊 DIAGNOSTIC:');
    console.log('  coordMap entries:', Object.keys(coordMap).length);
    console.log('  chains:', chains.length);
    console.log('  switches to draw:', switches.length);


    sldState.setSLDComponentsString(sldComponentsString);

    const namespace = {
        shapes,
        GridElement, SwitchboardElement, TransformerElement,
        BusElement, CableElement, BusDuctElement,
        MotorElement, HeaterElement, CapacitorElement, LumpLoadElement, NodeElement, 
        SwitchElement, FuseElement, BusBarLinkElement
    };

    // ── Graph & Paper ───────────────────────────────────────────────────────
    const graph = new dia.Graph(
        {}, { cellNamespace: namespace });
    sldState.setGraph(graph);


    // controls whether element movement is allowed (disabled when dragging bus ends)
    let interaction = sldState.getElementMoveInteractive(); // true

    const paper = new dia.Paper({
        el: document.getElementById(divString),
        model: graph,
        width: xGridSize,
        height: yGridSize,
        gridSize: CONFIG.GRID_SIZE,
        drawGrid: true,
        background: { color: '#F5F5F5' },
        cellViewNamespace: namespace,
        interactive: (cellView) => {
            if (cellView.model.isElement()) return { elementMove: interaction };
            return true;
        },
        defaultLink: () => new shapes.standard.Link({
            router: { name: 'manhattan' },
            connector: { name: 'rounded', args: { radius: 3 }, jumpover: { size: 6 } },
            attrs: {
                line: {
                    stroke: '#000000', strokeWidth: 2,
                    sourceMarker: { type: 'circle', r: CONFIG.PORT_RADIUS, fill: 'yellow' },
                    targetMarker: { type: 'circle', r: CONFIG.PORT_RADIUS, fill: 'green' }
                },
                label: { textAnchor: 'middle', refX: 0.5, refY: -10, fontSize: 12, fill: '#000000' }
            }
        }, {
            markup: [{ tagName: 'path', selector: 'line' }, { tagName: 'text', selector: 'label' }]
        }),
        linkPinning: false,
        validateConnection: (cellViewS, magnetS, cellViewT, magnetT, end, linkView) => {
            if (magnetS === magnetT) return false;
            if (cellViewS === cellViewT) return false;
            const sourcePortId = linkView.model.prop('source/port');
            const targetPortId = linkView.model.prop('target/port');
            const sourcePortLinks = graph.getConnectedLinks(cellViewS.model).filter(l => l.prop('source/port') === sourcePortId);
            const targetPortLinks = graph.getConnectedLinks(cellViewT.model).filter(l => l.prop('target/port') === targetPortId);

            console.log(`Source Element '${cellViewS.model.prop('tag')}' at port '${sourcePortId}' has 
            ${sourcePortLinks.length} connections and Target Element '${cellViewT.model.prop('tag')}' 
            at port '${targetPortId}' has ${targetPortLinks.length} connections.`);

            linkView.model.on('change', function () {console.log('link changed'); })

            if (sourcePortLinks.length > 3) return false;
            return !(targetPortLinks && targetPortLinks.length > 3);            
        },
        validateMagnet: (_cellView, magnet) => magnet.getAttribute('magnet') !== 'passive',
        snapLinks: { radius: 20 },
    });
    paper.el.style.border = '1px solid #2E2E2E';

    sldState.setPaper(paper);
    setupAllHandlers(paper);

    // ── Element arrays ──────────────────────────────────────────────────────

    const swbdElement    = [];
    const busesNodeElement = [];

    let busesElement = sldState.getBusesElement();
    let branchElement = sldState.getBranchElement();
    let loadElement = sldState.getLoadElement();

    // Lookup of ALL drawn JointJS models by Tag — used for chain link creation
    const allElementsByTag = {};

    // Array for non-branch element models
    const nonBranchElements = [];

    // Build a set of bus tags for quick lookup during link creation
    const busTagSet = new Set(buses.map(b => b.Tag));

    drawTemplates();

    // ══════════════════════════════════════════════════════════════════════════
    //  1. DRAW BUSES
    // ══════════════════════════════════════════════════════════════════════════
    buses.forEach((bus, index) => {
        if (!bus || !bus.Tag) { console.warn(`Skipping invalid bus at index ${index}`); return; }

        const cordX   = typeof bus.CordX    === 'number' ? bus.CordX    : 0;
        const cordY   = typeof bus.CordY    === 'number' ? bus.CordY    : 0;
        const length  = typeof bus.Length   === 'number' ? bus.Length   : 100;
        const isc     = typeof bus.ISC      === 'number' ? bus.ISC      : 0;
        const vr      = typeof bus.VR       === 'number' ? bus.VR       : 0;
        const sckAaMax = typeof bus.SCkAaMax === 'number' ? bus.SCkAaMax : 0;

        let operatingVoltage = 'N/A';
        if (bus.Vo && typeof bus.Vo.Magnitude === 'number' && typeof bus.Vo.Phase === 'number') {
            operatingVoltage = `${Math.round(10000 * bus.Vo.Magnitude) / 100}% ∠${Math.round(bus.Vo.Phase * 1800 / Math.PI) / 10}°`;
        }

        if (bus.IsSwing) {
            busesElement[index] = new GridElement({ 
                tag: bus.Tag,
                position: { x: cordX, y: cordY },
                size: { width: 40, height: 40 },
                attrs: {
                    label: { text: sanitizeText('Grid' + bus.Tag) },
                    ratedSC: { text: Math.round(10 * isc) / 10 + 'kA' },
                    ratedVoltage: { text: vr / 1000 + 'kV' },
                    busFaultkA: { text: Math.round(10 * sckAaMax) / 10 + 'kA' },
                    operatingVoltage: { text: operatingVoltage }
                }
            });
        } else {
            busesElement[index] = new BusElement({
                tag: bus.Tag,
                position: { x: cordX - length / 2, y: cordY },
                size: { width: length, height: 0 },
                attrs: {
                    body: { x1: 0, y1: 0, x2: length, y2: 0 },
                    label: { text: sanitizeText(bus.Tag) },
                    ratedSC: { text: Math.round(10 * isc) / 10 + 'kA' },
                    ratedVoltage: { text: vr / 1000 + 'kV' },
                    busFault: { text: Math.round(10 * sckAaMax) / 10 + 'kA' },
                    operatingVoltage: { text: operatingVoltage }
                }
            });
        }
        
        
        busesElement[index].addTo(graph);
        try {
            const updated = updatePositionLength(busesElement[index], sldComponents);
            if (updated) busesElement[index] = updated;
        } catch (e) {
            console.error(`Error updating position/length for bus ${bus.Tag}:`, e);
        }
        if(bus.IsNode){
            busesElement[index].node = true;
            busesElement[index] = updateNodeOrBus(busesElement[index]);
        }
        
        allElementsByTag[bus.Tag] = busesElement[index];
    });

    console.log(`✅ Drew ${busesElement.filter(Boolean).length} bus elements`);8

    console.log('  buses registered:', Object.keys(allElementsByTag).length);  // move this after buses are drawn

    // ══════════════════════════════════════════════════════════════════════════
    //  2. DRAW SWITCHBOARDS (after buses, so bboxes are available)
    // ══════════════════════════════════════════════════════════════════════════
    switchboards.forEach((swbd, index) => {
        swbdElement[index] = new SwitchboardElement({
            tag: swbd[0]
            });

        const anyBusTag = JSON.parse(swbd[1])[0];
        swbdElement[index] = updateSwbdPositionSizeByBus(anyBusTag, busesElement, switchboards, swbdElement, 30, 20, 30, 20);
        swbdElement[index].addTo(graph);
    });

    console.log(`✅ Drew ${swbdElement.filter(Boolean).length} switchboard elements`);

    // ══════════════════════════════════════════════════════════════════════════
    //  3. DRAW BRANCH ELEMENTS (Cable, Transformer, BusDuct)
    //     Position: from server-computed coordMap (NOT from bus midpoints)
    //
    //     WHAT CHANGED from v1:
    //       OLD: position = midpoint of fromBus/toBus OR targetBus - yGridSpacing/2
    //       NEW: position = coordMap[branch.Tag] computed by SLDLayoutService
    //            considering the full chain length & element slot heights
    // ══════════════════════════════════════════════════════════════════════════
    branches.forEach((branch, index) => {
        const coord     = coordMap[branch.Tag];
        const sourceBus = buses.find(b => b.Tag === branch.FromBus);
        const targetBus = buses.find(b => b.Tag === branch.ToBus);
        if (!coord && (!sourceBus || !targetBus)) {
            console.warn(`Skipping branch ${branch.Tag}: no coords and missing bus`);
            return;
        }

        // Server-computed center position; fallback to legacy midpoint
        let posX, posY;
        if (coord) {
            posX = coord.x;
            posY = coord.y;
        } else {
            console.warn(`Branch '${branch.Tag}': server coords missing, using legacy fallback`);
            posX = (targetBus.CordX + sourceBus.CordX) / 2;
            posY = (targetBus.CordY + sourceBus.CordY) / 2;
        }

        if (branch.Category === 'Cable') {
            const cable = cables.find(c => c.Tag === branch.Tag);
            if (!cable) return;
            branchElement[index] = new CableElement({
                tag: cable.Tag,
                position: { x: posX - 5, y: posY - 30 }
            });
            branchElement[index].resize(10, 60);
            branchElement[index] = updateCable(branchElement[index], cable, branches);

        } else if (branch.Category === 'Transformer') {
            const transformer = transformers.find(t => t.Tag === branch.Tag);
            if (!transformer) return;
            branchElement[index] = new TransformerElement({
                tag: transformer.Tag,
                position: { x: posX, y: posY }
            });
            branchElement[index].resize(15, 15);
            branchElement[index] = updateTransformer(branchElement[index], transformer, branches);

        } else if (branch.Category === 'BusDuct') {
            const busDuct = busDucts.find(bd => bd.Tag === branch.Tag);
            if (!busDuct) return;
            branchElement[index] = new BusDuctElement({
                tag: busDuct.Tag,
                position: { x: posX - 5, y: posY - 30 }
            });
            branchElement[index].resize(10, 60);
            branchElement[index] = updateBusDuct(branchElement[index], busDuct, branches);
        }

        if (!branchElement[index]) return;

        // Apply SLDComponent overrides (user-saved drag positions from DB)
        branchElement[index] = updateItemPosition(branchElement[index], sldComponents);
        branchElement[index].addTo(graph);

        // Register
        allElementsByTag[branch.Tag] = branchElement[index];
        

    // // Links
    //     const srcBusEl  = busesElement.find(e => e && e.prop('tag') === branch.FromBus);
    //     const tgtBusEl  = busesElement.find(e => e && e.prop('tag') === branch.ToBus);
    //     const thisBranch = branchElement[index];
    //     if (!srcBusEl || !tgtBusEl || !thisBranch) return;
    //
    //     let fromLink = branchLink.clone();
    //     fromLink.set({
    //         source: { id: srcBusEl.id,  port: srcBusEl.getPorts()[0].id },
    //         target: { id: thisBranch.id, port: thisBranch.getPorts()[0].id },
    //         sourceTag: srcBusEl.prop('tag'),
    //         targetTag: thisBranch.prop('tag')
    //     });
    //     fromLink.attr({ target: { magnet: false } });
    //     fromLink.prop('tag', getLinkTag(fromLink));
    //     fromLink = updateLinkVertices(fromLink, sldComponents, graph);
    //     fromLink.prop('elementType','link');
    //     graph.addCell(fromLink);
    //
    //     let toLink = branchLink.clone();
    //     toLink.set({
    //         source: { id: tgtBusEl.id,  port: tgtBusEl.getPorts()[0].id },
    //         target: { id: thisBranch.id, port: thisBranch.getPorts()[1].id },
    //         sourceTag: tgtBusEl.prop('tag'),
    //         targetTag: thisBranch.prop('tag')
    //     });
    //     toLink.attr({ target: { magnet: false } });
    //     toLink.prop('tag', getLinkTag(toLink));
    //     toLink      = updateLinkVertices(toLink, sldComponents, graph);
    //     toLink.prop('elementType','link');
    //     graph.addCell(toLink);

    });

    console.log(`✅ Drew ${branchElement.filter(Boolean).length} branch elements`);


    // ══════════════════════════════════════════════════════════════════════════
    //  4. DRAW NON-BRANCH ELEMENTS (Switch, Fuse, BusBarLink)       ← NEW
    //     Position: from server-computed coordMap
    //     Only elements that appear in a chain (have coords) are drawn.
    // ══════════════════════════════════════════════════════════════════════════

    /**
     * Creates a JointJS element for a non-branch item, positions it,
     * applies SLDComponent overrides, adds to graph, and registers in allElementsByTag.
     */
    function drawNonBranchElement(item, ShapeClass, elementType) {
        const coord = coordMap[item.Tag];
        if (!coord) return null;   // not in any visible chain

        const el = new ShapeClass({
            tag: item.Tag,
            elementType: elementType,
            position: { x: coord.x - 5, y: coord.y - 30 }
        });
        el.resize(10, 60);

        // Label
        try { el.attr('label/text', sanitizeText(item.Tag)); } catch (_) { /* shape may lack label */ }

        // SLDComponent override (user drag)
        const updated = updateItemPosition(el, sldComponents);
        const finalEl = updated || el;

        finalEl.addTo(graph);
        nonBranchElements.push(finalEl);
        allElementsByTag[item.Tag] = finalEl;
        return finalEl;
    }

    let nbDrawn = 0;
    switches.forEach(sw  => { if (drawNonBranchElement(sw,  SwitchElement,     'switch'))     nbDrawn++; });
    fuses.forEach(f      => { if (drawNonBranchElement(f,   FuseElement,       'fuse'))       nbDrawn++; });
    busBarLinks.forEach(b => { if (drawNonBranchElement(b,  BusBarLinkElement, 'busbarlink')) nbDrawn++; });

    console.log(`✅ Drew ${nbDrawn} non-branch elements (switch/fuse/busbarlink)`);


    // After ALL elements are drawn (after non-branch section), add:
    console.log('  allElementsByTag keys:', Object.keys(allElementsByTag));

    // ══════════════════════════════════════════════════════════════════════════
    //  5. DRAW LINKS BASED ON CHAIN SEQUENCES                        ← NEW
    //
    //     WHAT CHANGED from v1:
    //       OLD: For each branch, always two links: fromBus→branch, toBus→branch
    //       NEW: For each chain, links follow the full element sequence:
    //            FromBus → elem[0] → elem[1] → ... → elem[N] → ToBus
    //            This correctly handles non-branch items between bus and branch.
    //
    //     Chain JSON structure (from C# SLDLayoutService):
    //       { FromBus, ToBus, Elements: [tag1, tag2, ...],
    //         ContainsBranch, BranchTag, Orientation, ParallelIndex, ParallelCount }
    // ══════════════════════════════════════════════════════════════════════════
    let linkCount = 0;

    // Check what the first chain expects vs what exists:
    if (chains.length > 0) {
        const seq = [chains[0].FromBus, ...chains[0].Elements, chains[0].ToBus];
        seq.forEach(tag => {
            console.log(`    ${tag}: ${allElementsByTag[tag] ? '✅ found' : '❌ MISSING'}`);
        });
    }

    chains.forEach((chain) => {
        const { FromBus, ToBus, Elements } = chain;

        if (!FromBus || !ToBus || !Elements || Elements.length === 0) {
            console.warn('Skipping chain: missing bus or empty elements', chain);
            return;
        }

        // Full sequence: [FromBus, elem0, elem1, ..., elemN, ToBus]
        const sequence = [FromBus, ...Elements, ToBus];

        for (let i = 0; i < sequence.length - 1; i++) {
            const srcTag = sequence[i];
            const tgtTag = sequence[i + 1];

            const srcEl = allElementsByTag[srcTag];
            const tgtEl = allElementsByTag[tgtTag];

            if (!srcEl || !tgtEl) {
                console.warn(`  Chain link skip: no element for '${srcTag}' or '${tgtTag}'`);
                continue;
            }

            // ── Port selection logic ────────────────────────────────────
            //
            //  Element type      | As SOURCE (leaving)  | As TARGET (arriving)
            //  ──────────────────|──────────────────────|─────────────────────
            //  Bus / Grid        | port[0]              | port[0]
            //  Branch / NonBranch| port[1] (To / bottom)| port[0] (From / top)
            //
            const srcPorts = srcEl.getPorts ? srcEl.getPorts() : [];
            const tgtPorts = tgtEl.getPorts ? tgtEl.getPorts() : [];

            if (srcPorts.length === 0 || tgtPorts.length === 0) {
                console.warn(`  Chain link skip: no ports on '${srcTag}' or '${tgtTag}'`);
                continue;
            }

            const srcIsBus = busTagSet.has(srcTag);
            const tgtIsBus = busTagSet.has(tgtTag);

            // Source leaving → bus uses port[0]; branch/NB uses port[1] (bottom/To)
            const srcPort = srcIsBus
                ? srcPorts[0]
                : (srcPorts.length > 1 ? srcPorts[1] : srcPorts[0]);

            // Target arriving → bus uses port[0]; branch/NB uses port[0] (top/From)
            const tgtPort = tgtPorts[0];

            let link = branchLink.clone();
            link.set({
                source:    { id: srcEl.id, port: srcPort.id },
                target:    { id: tgtEl.id, port: tgtPort.id },
                sourceTag: srcTag,
                targetTag: tgtTag
            });
            link.attr({ target: { magnet: false } });
            link.prop('tag', getLinkTag(link));
            link = updateLinkVertices(link, sldComponents, graph);
            link.prop('elementType', 'link');
            graph.addCell(link);
            linkCount++;
        }
    });

    console.log(`✅ Drew ${linkCount} chain links across ${chains.length} chains`);





    // ── Distribute bus ports after all links exist ──────────────────────────
    busesElement.forEach(busEl => {
        if (busEl) busPortDistribution(busEl.id, graph, CONFIG.PORT_RADIUS);
    });
    
    
    sldState.setBusesElement(busesElement);
    sldState.setSwbdElement(swbdElement);
    sldState.setSwitchboards(switchboards);

    sldState.setBranchElement(branchElement);
    sldState.setLoadElement(loadElement);

    console.log('✅ SLD diagram initialised successfully');
}

// ── Local helper: size switchboard to wrap its buses ─────────────────────────
function updateSwbdPositionSizeByBus(busTag, busesElement, switchboards, swbdElement, dx1, dx2, dy1, dy2) {
    const swbd     = switchboards.find(item => JSON.parse(item[1]).includes(busTag));
    if (!swbd) return null;
    const swbdModel = swbdElement.find(item => item.prop('tag') === swbd[0]);
    if (!swbdModel) return null;

    const busTags = JSON.parse(swbd[1]);
    let x1 = Number.MAX_SAFE_INTEGER, y1 = Number.MAX_SAFE_INTEGER, x2 = 0, y2 = 0;

    busTags.forEach(tag => {
        const busModel = busesElement.find(item => item && item.prop('tag') === tag);
        if (!busModel) return;
        const bbox = busModel.getBBox();
        x1 = Math.min(x1, bbox.x);
        y1 = Math.min(y1, bbox.y);
        x2 = Math.max(x2, bbox.x + bbox.width);
        y2 = Math.max(y2, bbox.y + bbox.height);
    });

    swbdModel.position(x1 - dx1, y1 - dy1);
    swbdModel.resize((x2 + dx2) - (x1 - dx1), (y2 + dy2) - (y1 - dy1));
    return swbdModel;
}


export function disposeSLD() {
    sldState.setGraph(null);
    sldState.setPaper(null);
    sldState.setDotNetObjSLD(null);
    sldState.setDotNetObjDraw(null);
    console.log("SLD disposed");
}