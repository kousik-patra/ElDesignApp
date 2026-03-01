/**
 * SelectionManager.js
 *
 * Single-element selection (JointJS built-in stroke highlighter) +
 * ghost-follow duplicate mode + Blazor table sync via JSInterop.
 *
 * Plugs into the existing sldState singleton — no duplicate paper/graph refs.
 *
 * ── Blazor methods invoked FROM here ─────────────────────────────────────────
 *   dotNetObjSLD.invokeMethodAsync('OnElementSelected',  { id, tag, elementType })
 *   dotNetObjSLD.invokeMethodAsync('OnElementDeselected', {})
 *   dotNetObjSLD.invokeMethodAsync('OnElementPlaced',    { id, tag, elementType, sourceId })
 *
 * ── JS functions called FROM Blazor ──────────────────────────────────────────
 *   window.sld.startDuplicate()            — Duplicate button click
 *   window.sld.selectById(elementId)       — Table row click
 *   window.sld.setAllItemTags(tagArray)    — Called once on page load with full tag list
 *
 * ── Tag naming rule ───────────────────────────────────────────────────────────
 *   1st copy of "T1"  →  "T1-Copy"
 *   2nd copy of "T1"  →  "T1-Copy1"
 *   3rd copy of "T1"  →  "T1-Copy2"   (i.e. suffix is n-1 for n >= 2)
 *   Checks against sldState.allItemTags so in-session AND pre-existing tags
 *   from the server are both respected.
 *
 * ── Blazor side: serialising _allItemsDictionary ──────────────────────────────
 *   Your C# type is:  List<Dictionary<(string category, string tag), Guid>>
 *   Flatten it to a plain array before sending to JS:
 *
 *   var tagArray = _allItemsDictionary
 *       .SelectMany(dict => dict.Keys)
 *       .Select(k => new { category = k.Item1, tag = k.Item2 })
 *       .ToList();
 *   await JS.InvokeVoidAsync("sld.setAllItemTags", tagArray);
 */

import { highlighters } from '@joint/core';
import { sldState } from '../state/sldState.js';
import {safeInvokeAsync} from "../utils/helpers";

// ─────────────────────────────────────────────────────────────────────────────
// Highlighter config — applied to the ElementView, not the model.
// Works for every element shape without touching any element definition file.
// ─────────────────────────────────────────────────────────────────────────────
const HIGHLIGHT_ID = 'sld-selection';
const HIGHLIGHT_OPTIONS = {
    padding: 6,
    rx: 3,
    ry: 3,
    attrs: {
        stroke: 'dodgerblue',
        'stroke-width': 2.5,
        'stroke-dasharray': '6,3',
        fill: 'none'
    }
};

// ─────────────────────────────────────────────────────────────────────────────
// setupSelectionHandlers(paper)
//
// Called from setupAllHandlers() in eventHandlers.js, alongside the existing
// setupElementHandlers, setupLinkHandlers, etc.
// ─────────────────────────────────────────────────────────────────────────────
export function setupSelectionHandlers(paper) {
    _bindPaperEvents(paper);
    _bindKeyboard();
    _createDuplicateButton(paper);
}


// _____________
// Duplicate button
// -------------

function _createDuplicateButton(paper) {
    // Create button and hint elements
    const btn = document.createElement('button');
    btn.id = 'btn-duplicate';
    btn.textContent = '⧉ Duplicate';
    btn.title = 'Duplicate selected element';
    btn.style.cssText = `
        position: absolute;
        top: 8px;
        right: 8px;
        z-index: 10;
        padding: 4px 10px;
        font-size: 12px;
        cursor: pointer;
        background: #0d6efd;
        color: white;
        border: none;
        border-radius: 4px;
        opacity: 0.4;
        pointer-events: none;
    `;

    const hint = document.createElement('span');
    hint.id = 'duplicate-hint';
    hint.textContent = 'Click on diagram to place  |  Esc to cancel';
    hint.style.cssText = `
        position: absolute;
        top: 12px;
        right: 110px;
        z-index: 10;
        font-size: 11px;
        color: #666;
        display: none;
    `;

    // Button must be inside the paper's parent so positioning works
    const container = paper.el.parentElement;
    container.style.position = 'relative';  // ensure absolute children position correctly
    container.appendChild(btn);
    container.appendChild(hint);

    btn.addEventListener('click', () => {
        if (!sldState.getSelectedModel() || sldState.getSelectionMode() !== 'idle') return;
        _enterGhostMode();
        _setGhostModeUI(true);
    });

    return { btn, hint };
}

// ── UI helpers called on state changes ───────────────────────────────────────
function _setButtonEnabled(enabled) {
    const btn = document.getElementById('btn-duplicate');
    if (!btn) return;
    btn.style.opacity        = enabled ? '1'    : '0.4';
    btn.style.pointerEvents  = enabled ? 'auto' : 'none';
}

function _setGhostModeUI(active) {
    const hint = document.getElementById('duplicate-hint');
    if (hint) hint.style.display = active ? 'inline' : 'none';
    _setButtonEnabled(!active);   // disable button while ghost is floating
}


// ─────────────────────────────────────────────────────────────────────────────
// Paper events
// ─────────────────────────────────────────────────────────────────────────────
function _bindPaperEvents(paper) {

    // ── Element clicked ──────────────────────────────────────────────────────
    paper.on('element:pointerclick', (view, evt) => {
        if (sldState.getSelectionMode() === 'ghost') return;

        const model = view.model;

        // Skip templates, selectboxes, nodes — same guard used in existing handlers
        const tag = model.prop('tag');
        if (!tag
            || tag.includes('template')
            || tag.includes('selectbox')
            || model.prop('node') === true
        ) return;

        // Clicking the already-selected element → deselect
        if (sldState.getSelectedModel()?.id === model.id) {
            _clearHighlight();
            _notifyBlazor('OnElementDeselected', {});
            return;
        }

        _applyHighlight(model, view);
        _setButtonEnabled(true);
        _notifyBlazor('OnElementSelected', {
            id:          model.id,
            tag:         model.prop('tag')         ?? '',
            elementType: model.prop('elementType') ?? ''
        });
    });

    // ── Blank paper clicked ──────────────────────────────────────────────────
    // NOTE: blank:pointerdown/move/up are already used by your drag-select box.
    // blank:pointerclick fires on a clean click (no drag), so it is safe to use
    // here without conflicting with dragStart / drag / dragEnd.
    paper.on('blank:pointerclick', (evt, x, y) => {
        if (sldState.getSelectionMode() === 'ghost') {
            _placeGhost(x, y);
            return;
        }
        _clearHighlight();
        _notifyBlazor('OnElementDeselected', {});
    });

    // ── Mousemove for ghost follow ───────────────────────────────────────────
    // Attached to the raw SVG element so it fires even when the cursor is over
    // other elements (JointJS blank:pointermove only fires over empty paper).
    paper.el.addEventListener('mousemove', (evt) => {
        if (sldState.getSelectionMode() !== 'ghost') return;
        if (!sldState.getGhostModel()) return;

        const localPt = paper.clientToLocalPoint({
            x: evt.clientX,
            y: evt.clientY
        });
        sldState.setGhostLatestCursor(localPt);

        if (!sldState.getGhostRafPending()) {
            sldState.setGhostRafPending(true);
            requestAnimationFrame(() => {
                const ghost  = sldState.getGhostModel();
                const cursor = sldState.getGhostLatestCursor();
                if (ghost && cursor) {
                    // _anchorOffset stored on the ghost model itself for simplicity
                    ghost.position(
                        cursor.x + (ghost._anchorDx ?? 0),
                        cursor.y + (ghost._anchorDy ?? 0)
                    );
                }
                sldState.setGhostRafPending(false);
            });
        }
    });
}

// ─────────────────────────────────────────────────────────────────────────────
// Keyboard
// ─────────────────────────────────────────────────────────────────────────────
function _bindKeyboard() {
    document.addEventListener('keydown', (evt) => {
        const focused = document.activeElement?.tagName;
        if (focused === 'INPUT' || focused === 'TEXTAREA' || focused === 'SELECT') return;

        if (evt.key === 'Escape') {
            if (sldState.getSelectionMode() === 'ghost') {
                _cancelGhost();           // silent — Blazor NOT notified
            } else {
                _clearHighlight();
                _notifyBlazor('OnElementDeselected', {});
            }
        }
    });
}

// ─────────────────────────────────────────────────────────────────────────────
// Highlight helpers
// ─────────────────────────────────────────────────────────────────────────────
function _applyHighlight(model, view) {
    const prevView = sldState.getSelectedView();
    if (prevView) {
        highlighters.stroke.remove(prevView, HIGHLIGHT_ID);
    }
    sldState.setSelectedModel(model);
    sldState.setSelectedView(view);
    highlighters.stroke.add(view, 'root', HIGHLIGHT_ID, HIGHLIGHT_OPTIONS);
}

function _clearHighlight() {
    const view = sldState.getSelectedView();
    if (view) highlighters.stroke.remove(view, HIGHLIGHT_ID);
    _setButtonEnabled(false);
    _setGhostModeUI(false);
    sldState.setSelectedModel(null);
    sldState.setSelectedView(null);
}

// ─────────────────────────────────────────────────────────────────────────────
// Ghost mode
// ─────────────────────────────────────────────────────────────────────────────
function _enterGhostMode() {
    const paper        = sldState.getPaper();
    const graph        = sldState.getGraph();
    const sourceModel  = sldState.getSelectedModel();

    sldState.setSelectionMode('ghost');

    const ghost = sourceModel.clone();

    // Semi-transparent and non-interactive — pointer-events:none is critical
    // so that blank:pointerclick (placement) is not swallowed by the ghost.
    ghost.attr('root/style', 'opacity: 0.45; pointer-events: none;');

    // Anchor: cursor maps to the horizontal/vertical centre of the ghost.
    const size = sourceModel.size();
    ghost._anchorDx = -(size.width  / 2);
    ghost._anchorDy = -(size.height / 2);

    // Start near the original so it doesn't flash at a stale position
    const pos = sourceModel.position();
    ghost.position(pos.x + 20, pos.y + 20);

    graph.addCell(ghost);
    sldState.setGhostModel(ghost);

    paper.el.style.cursor = 'crosshair';
}

function _placeGhost(paperX, paperY) {
    const paper       = sldState.getPaper();
    const ghost       = sldState.getGhostModel();
    const sourceModel = sldState.getSelectedModel();
    if (!ghost || !sourceModel) return;

    // Snap to click point
    ghost.position(
        paperX + (ghost._anchorDx ?? 0),
        paperY + (ghost._anchorDy ?? 0)
    );

    // Restore full appearance — now a permanent element
    ghost.attr('root/style', 'opacity: 1; pointer-events: all;');

    // Assign copy tag
    const category = sourceModel.prop('elementType') ?? '';
    const sourceTag = sourceModel.prop('tag') ?? 'X';
    const copyTag  = _buildCopyTag(sourceModel, category);

    ghost.prop('tag', copyTag);
    try { ghost.attr('tag/text', copyTag); } catch (_) { /* selector varies by element type */ }

    // Capture before resetting state
    const placed   = ghost;
    const sourceId = sourceModel.id;

    sldState.setGhostModel(null);
    sldState.setSelectionMode('idle');
    paper.el.style.cursor = 'default';

    // Register the new tag in the live in-memory list so further copies of
    // the same source or copies of this copy don't collide.
    sldState.addItemTag(category, copyTag);

    // ── Notify Blazor — ONLY after final placement, never on Escape ───────────
    _notifyBlazor('OnElementPlaced', {
        id:          placed.id,
        sourceTag: sourceTag,
        tag:         copyTag,
        elementType: category,
        sourceId:    sourceId      // Blazor uses this to clone property values in the table
    });

    // Auto-select the placed element on paper + sync table highlight
    const placedView = paper.findViewByModel(placed);
    if (placedView) {
        _applyHighlight(placed, placedView);
        _notifyBlazor('OnElementSelected', {
            id:          placed.id,
            sourceId: sourceId,
            tag:         copyTag,
            elementType: category
        });
    }

    _setGhostModeUI(false);
    _setButtonEnabled(true);
}

function _cancelGhost() {
    const paper = sldState.getPaper();
    const ghost = sldState.getGhostModel();
    if (ghost) ghost.remove();
    _setGhostModeUI(false);
    _setButtonEnabled(true);

    sldState.setGhostModel(null);
    sldState.setSelectionMode('idle');
    paper.el.style.cursor = 'default';
    // Original element stays highlighted. Duplicate button stays enabled.
}

// ─────────────────────────────────────────────────────────────────────────────
// Copy tag builder
//
// Rule:
//   1st copy of "T1"  →  "T1-Copy"
//   2nd copy of "T1"  →  "T1-Copy1"
//   3rd copy of "T1"  →  "T1-Copy2"
//   nth copy of "T1"  →  "T1-Copy{n-1}"
//
// Checks sldState.allItemTags (which includes server-side pre-existing tags AND
// any tags added in the current session) so there are no collisions.
// ─────────────────────────────────────────────────────────────────────────────
function _buildCopyTag(sourceModel, category) {
    // Strip any existing copy suffix so chained copies stay clean:
    // "T1-Copy1" → base "T1", not "T1-Copy1-Copy"
    const rawTag = sourceModel.prop('tag') ?? 'X';
    const base   = rawTag
        .replace(/-Copy\d*$/, '');  // removes "-Copy", "-Copy1", "-Copy2", etc.

    // Try "-Copy" first (1st copy)
    const firstCandidate = `${base}-Copy`;
    if (!sldState.tagExistsInCategory(category, firstCandidate)) {
        return firstCandidate;
    }

    // Subsequent copies: "-Copy1", "-Copy2", ...
    let n = 1;
    while (true) {
        const candidate = `${base}-Copy${n}`;
        if (!sldState.tagExistsInCategory(category, candidate)) {
            return candidate;
        }
        n++;
        if (n > 9999) {
            // Safety valve — should never happen in practice
            return `${base}-Copy${Date.now()}`;
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Blazor interop — uses dotNetObjSLD, matching the existing sldState pattern
// ─────────────────────────────────────────────────────────────────────────────
async function _notifyBlazor(methodName, payload) {
    const ref = sldState.getDotNetObjSLD();
    if (!ref) return;
    // ref.invokeMethodAsync(methodName, payload)
    //     .catch(err => console.error(`[elementSelectHandle.js] ${methodName} failed:`, err));


    await safeInvokeAsync(  sldState.getDotNetObjSLD(),methodName, payload )
            .catch (err => console.error(`[elementSelectHandle.js] ${methodName} failed:`, err));
    
}

// ─────────────────────────────────────────────────────────────────────────────
// window.sld entry points — called by Blazor via IJSRuntime.InvokeVoidAsync
// ─────────────────────────────────────────────────────────────────────────────
window.sld = window.sld ?? {};

/**
 * Called by Blazor Duplicate button:
 *   await JS.InvokeVoidAsync("sld.startDuplicate");
 */
window.sld.startDuplicate = function () {
    if (!sldState.getSelectedModel() || sldState.getSelectionMode() !== 'idle') return;
    _enterGhostMode();
};

/**
 * Called by Blazor when a table row is clicked:
 *   await JS.InvokeVoidAsync("sld.selectById", item.Id);
 * Does NOT call back into Blazor — Blazor already knows (it triggered this).
 */
window.sld.selectById = function (elementId) {
    if (sldState.getSelectionMode() === 'ghost') return;

    const graph = sldState.getGraph();
    const paper = sldState.getPaper();
    const model = graph.getCell(elementId);
    if (!model || model.isLink()) return;

    const view = paper.findViewByModel(model);
    if (!view) return;

    if (sldState.getSelectedModel()?.id === elementId) return;  // already selected

    _applyHighlight(model, view);
};

/**
 * Called once on page load from Blazor with the full flat tag list:
 *   await JS.InvokeVoidAsync("sld.setAllItemTags", tagArray);
 *
 * tagArray shape: [ { category: "Transformer", tag: "T1" }, ... ]
 *
 * C# serialisation from _allItemsDictionary:
 *   var tagArray = _allItemsDictionary
 *       .SelectMany(dict => dict.Keys)
 *       .Select(k => new { category = k.Item1, tag = k.Item2 })
 *       .ToList();
 *   await JS.InvokeVoidAsync("sld.setAllItemTags", tagArray);
 */
window.sld.setAllItemTags = function (tagArray) {
    sldState.setAllItemTags(tagArray);
    console.log(`[SelectionManager] Loaded ${tagArray?.length ?? 0} item tags for copy-collision checking.`);
};