/**
 * Branch component update functions (Transformer, Cable, BusDuct, Switch)
 */
import { sanitizeText } from '../utils/helpers.js';

/**
 * Update transformer element with data
 * @param {Object} trafoModel - JointJS transformer model
 * @param {Object} trafoInfo - Transformer information data
 * @param {Array} branches - Array of branch data
 * @returns {Object} Updated transformer model
 */
export function updateTransformer(trafoModel, trafoInfo, branches) {
    let branch = branches.find(br => br.Tag === trafoInfo.Tag);

    trafoModel.attr({
        tag: { text: sanitizeText(trafoInfo.Tag) },
        voltage: { text: `${trafoInfo.V1 / 1000}/${trafoInfo.V2 / 1000}kV` },
        kVArating: { text: `${trafoInfo.KVA}kVA` },
        impedance: { text: `Z:${trafoInfo.Z}%` },
        loading: { text: `${Math.round(10 * branch?.KW) / 10}KW ${Math.round(10 * branch?.KVAR) / 10}kVAR` }
    });
    return trafoModel;
}

/**
 * Update cable element with data
 * @param {Object} cableModel - JointJS cable model
 * @param {Object} cabledata - Cable information data
 * @param {Array} branches - Array of branch data
 * @returns {Object} Updated cable model
 */
export function updateCable(cableModel, cabledata, branches) {
    var branch = branches.find(br => br.Tag === cabledata.Tag);

    cableModel.attr({
        label: { text: sanitizeText(cabledata.Tag) },
        size: { text: cabledata.CblDesc },
        length: { text: `${cabledata.L}m, ${cabledata.Rl}-j${cabledata.Xl}Ω/km` },
        impedance: { text: `R:${cabledata.R}, X:${cabledata.X}` },
        operatingCurrent: {
            text: `${Math.round(10 * branch?.Io.Magnitude) / 10}A ∠${Math.round(branch?.Io.Phase * 1800 / Math.PI) / 10}°`
        }
    });

    return cableModel;
}

/**
 * Update bus duct element with data
 * @param {Object} busDuctModel - JointJS bus duct model
 * @param {Object} busDuctdata - Bus duct information data
 * @param {Array} branches - Array of branch data
 * @returns {Object} Updated bus duct model
 */
export function updateBusDuct(busDuctModel, busDuctdata, branches) {
    let branch = branches.find(br => br.Tag === busDuctdata.Tag);

    busDuctModel.attr({
        label: { text: sanitizeText(busDuctdata.Tag) },
        size: { text: `${busDuctdata.IR}A` },
        length: {
            text: `${busDuctdata.L}m, ${Math.round(1000 * busDuctdata.Rl) / 1000}-j${Math.round(1000 * busDuctdata.Xl) / 1000}Ω/km`
        },
        impedance: {
            text: `R:${Math.round(10000 * busDuctdata.R) / 10000}, X:${Math.round(10000 * busDuctdata.X) / 10000}`
        },
        operatingCurrent: {
            text: `${Math.round(10 * branch.Io.Magnitude) / 10}A ∠${Math.round(branch.Io.Phase * 1800 / Math.PI) / 10}°`
        }
    });
    return busDuctModel;
}

/**
 * Update switch element with data
 * @param {Object} switchModel - JointJS switch model
 * @param {Object} switchData - Switch information data
 * @param {Array} branches - Array of branch data
 * @returns {Object} Updated switch model
 */
export function updateSwitch(switchModel, switchData, branches) {
    var branch = branches.find(br => br.Tag == switchData.Tag);
    
    // Note: Original code had bugs using 'busDuctdata' and 'lumploadModel' instead of proper params
    // This needs to be corrected based on actual switch element structure
    switchModel.attr({
        label: { text: sanitizeText(switchData.Tag) },
        // Add appropriate switch attributes here
    });
    return switchModel;
}
