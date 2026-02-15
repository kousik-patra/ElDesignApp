import { CONFIG } from '../config/constants.js';
import { sldState } from '../state/sldState.js';

/**
 * Distributes bus ports evenly along the bus length
 * based on how many links are connected and whether they
 * come from above or below the bus.
 *
 * @param {string} busId  - JointJS cell id of the bus element
 */
export function busPortDistribution(busId) {
    const graph = sldState.graph;
    const bus = graph.getCell(busId);
    if (!bus) return;

    const allLinks = graph.getConnectedLinks(bus);
    const ports    = bus.getGroupPorts('in');

    // Annotate every link with the x-position of its other end
    allLinks.forEach(link => {
        const sourceCell = graph.getCell(link.prop('source').id);
        const targetCell = graph.getCell(link.prop('target').id);
        const otherEnd   = sourceCell.id === bus.id ? targetCell : sourceCell;
        link.otherEndX   = otherEnd.prop('position/x');
        link.otherEndY   = otherEnd.prop('position/y');
    });

    // Sort by X so ports are laid out left-to-right
    allLinks.sort((a, b) => a.otherEndX - b.otherEndX);

    const upLinks   = [];
    const downLinks = [];
    allLinks.forEach(link => {
        if (link.otherEndY < bus.prop('position/y')) upLinks.push(link);
        else downLinks.push(link);
    });

    const reqPorts = upLinks.length + downLinks.length;

    // Remove extra ports (keep at least 1)
    if (reqPorts < ports.length && ports.length > 1) {
        for (let i = ports.length; i > Math.max(reqPorts, 1); i--) {
            bus.removePort(`${i}`);
        }
    }

    // Add missing ports
    if (reqPorts > ports.length) {
        for (let i = ports.length; i < reqPorts; i++) {
            bus.addPort({
                id: `${i + 1}`,
                group: 'in',
                attrs: {
                    portBody: { magnet: true, r: CONFIG.PORT_RADIUS, fill: 'orange', stroke: '#023047' },
                    label:    { text: `${i + 1}`, fontSize: 8 }
                },
                markup: [{ tagName: 'circle', selector: 'portBody' }]
            });
        }
    }

    // Position up-side ports and re-assign link ends
    for (let i = 0; i < upLinks.length; i++) {
        const pos    = `${Math.round((i + 0.5) * 100 / upLinks.length)}%`;
        const portId = bus.getPorts()[i].id;
        bus.portProp(portId, 'args/x', pos);
        bus.portProp(portId, 'attrs/label/refX', 3);
        bus.portProp(portId, 'attrs/label/refY', -10);

        const end = upLinks[i].prop('source').id === busId ? 'source' : 'target';
        if (end === 'source') upLinks[i].set({ source: { id: busId, port: portId } });
        else                  upLinks[i].set({ target: { id: busId, port: portId } });
    }

    // Position down-side ports and re-assign link ends
    for (let i = 0; i < downLinks.length; i++) {
        const pos    = `${Math.round((i + 0.5) * 100 / downLinks.length)}%`;
        const portId = bus.getPorts()[upLinks.length + i].id;
        bus.portProp(portId, 'args/x', pos);
        bus.portProp(portId, 'attrs/label/refX', 3);
        bus.portProp(portId, 'attrs/label/refY', 10);

        const end = downLinks[i].prop('source').id === busId ? 'source' : 'target';
        if (end === 'source') downLinks[i].set({ source: { id: busId, port: portId } });
        else                  downLinks[i].set({ target: { id: busId, port: portId } });
    }
}

