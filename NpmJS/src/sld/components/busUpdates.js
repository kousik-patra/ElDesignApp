/**
 * Bus component update functions
 */
import { sanitizeText } from '../utils/helpers.js';

/**
 * Update bus element with data
 * @param {Object} busModel - JointJS bus model
 * @param {Object} busInfo - Bus information data
 * @returns {Object} Updated bus model
 */
export function updateBus(busModel, busInfo) {
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
            label: { text: "Grid" + sanitizeText(busInfo.Tag) },
            ratedSC: { text: sanitizeText(Math.round(10 * busInfo.ISC) / 10 + "kA") },
            ratedVoltage: { text: sanitizeText(busInfo.VR / 1000 + "kV") },
            busFaultkA: { text: sanitizeText(Math.round(10 * busInfo.SCkAaMax) / 10 + "kA") },
            operatingVoltage: {
                text: sanitizeText(
                    Math.round(10000 * busInfo.Vo.Magnitude) / 100 + "% ∠" +
                    Math.round(busInfo.Vo.Phase * 1800 / Math.PI) / 10 + "°"
                )
            }
        });
    } else {
        // other bus
        busModel.attr({
            label: { text: sanitizeText(busInfo.Tag) },
            ratedSC: { text: sanitizeText(Math.round(10 * busInfo.ISC) / 10 + "kA") },
            ratedVoltage: { text: sanitizeText(busInfo.VR / 1000 + "kV") },
            busFault: { text: sanitizeText(Math.round(10 * busInfo.SCkAaMax) / 10 + "kA") },
            operatingVoltage: {
                text: sanitizeText(
                    Math.round(10000 * busInfo.Vo.Magnitude) / 100 + "% ∠" +
                    Math.round(busInfo.Vo.Phase * 1800 / Math.PI) / 10 + "°"
                )
            }
        });
    }
    return busModel;
}


