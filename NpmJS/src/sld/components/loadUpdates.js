/**
 * Load component update functions (Motor, Heater, Capacitor, LumpLoad)
 */
import { sanitizeText } from '../utils/helpers.js';

/**
 * Update motor element with data
 * @param {Object} motorModel - JointJS motor model
 * @param {Object} motorData - Motor information data
 * @returns {Object} Updated motor model
 */
export function updateMotor(motorModel, motorData) {
    motorModel.attr({
        label: { text: sanitizeText(motorData.Tag) },
        operatingPower: { text: `${Math.round(10 * motorData.KW) / 10}kW` },
        rating: { text: `${motorData.HP}HP` },
    });
    return motorModel;
}

/**
 * Update heater element with data
 * @param {Object} heaterModel - JointJS heater model
 * @param {Object} heaterData - Heater information data
 * @returns {Object} Updated heater model
 */
export function updateHeater(heaterModel, heaterData) {

    heaterModel.attr({
        label: { text: sanitizeText(heaterData.Tag) },
        operatingPower: { text: `${Math.round(10 * heaterData.KW) / 10}kW` },
        rating: { text: `${heaterData.KW}kW` },
    });
    return heaterModel;
}

/**
 * Update capacitor element with data
 * @param {Object} capacitorModel - JointJS capacitor model
 * @param {Object} capacitorData - Capacitor information data
 * @returns {Object} Updated capacitor model
 */
export function updateCapacitor(capacitorModel, capacitorData) {
    
    capacitorModel.attr({
        label: { text: sanitizeText(capacitorData.Tag) },
        operatingPower: { text: `${Math.round(10 * capacitorData.KVAR) / 10}kVAR` },
        rating: { text: `${capacitorData.KVAR}kVAR` },
    });
    return capacitorModel;
}

/**
 * Update lump load element with data
 * @param {Object} lumpLoadModel - JointJS lump load model
 * @param {Object} lumpLoadData - Lump load information data
 * @returns {Object} Updated lump load model
 */
export function updateLumpLoad(lumpLoadModel, lumpLoadData) {

    lumpLoadModel.attr({
        label: { text: sanitizeText(lumpLoadData.Tag) },
        operatingPower: { text: `${Math.round(10 * lumpLoadData.KW) / 10}kW ${Math.round(10 * lumpLoadData.KVAR) / 10}kVAR` },
        rating: { text: `${Math.round(10 * lumpLoadData.KVA) / 10}kVA` },
    });
    return lumpLoadModel;
}
