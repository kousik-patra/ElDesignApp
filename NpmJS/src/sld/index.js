/**
 * Main entry point for SLD (Single Line Diagram) module
 * 
 * This is the refactored version of mySLD.js, organized into modular components.
 * 
 * Directory structure:
 * - config/       : Configuration constants
 * - utils/        : Utility functions (helpers, position utilities)
 * - state/        : Global state management
 * - shapes/       : JointJS shape definitions
 * - tools/        : JointJS tools and buttons
 * - components/   : Component update functions
 * - handlers/     : Event handlers
 * - links/        : Link operations
 * - operations/   : Main operations (draw, update)
 */
//
// // Export main operations
// export {
//     drawSLD,
//     updateSLD,
//     updateSLDItem,
//     updateSLDWithStudyResults
// } from './operations/index.js';
//
// // Export state management for cleanup
// export { sldState } from './state/sldState.js';
//
// /**
//  * Dispose/cleanup SLD resources
//  * @returns {void}
//  */
// export function disposeSLD() {
//     const { sldState } = require('./state/sldState.js');
//     sldState.clear();
// }
