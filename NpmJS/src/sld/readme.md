# SLD (Single Line Diagram) Module - Refactored

This is the refactored and modularized version of `mySLD.js`, organized into a clean, maintainable structure.

## 📁 Directory Structure

```
src/sld/
├── index.js                    # Main entry point - exports all public APIs
├── config/
│   └── constants.js            # Configuration constants (port radius, grid size, etc.)
├── utils/
│   ├── helpers.js              # Utility functions (sanitizeText, safeInvokeAsync)
│   └── positionUtils.js        # Position and overlap detection utilities
├── state/
│   └── sldState.js             # Global state management (graph, paper, dotNet refs)
├── shapes/
│   ├── index.js                # Central export for all shapes
│   ├── GridElement.js          # Grid element shape definition
│   ├── SwitchboardElement.js   # Switchboard element shape definition
│   ├── TransformerElement.js   # Transformer element shape definition
│   └── ...                     # Add more shape files as needed
├── tools/
│   └── buttons.js              # JointJS tools and buttons (property, remove, validate)
├── components/
│   ├── index.js                # Central export for component updates
│   ├── busUpdates.js           # Bus component update functions
│   ├── branchUpdates.js        # Branch component updates (transformer, cable, busduct)
│   └── loadUpdates.js          # Load component updates (motor, heater, capacitor)
├── handlers/
│   └── eventHandlers.js        # Event handlers for paper, elements, and links
├── links/
│   └── linkOperations.js       # Link management (create, validate, remove)
└── operations/
    ├── index.js                # Central export for operations
    ├── drawSLD.js              # Main drawing operation
    └── updateOperations.js     # Update operations (updateSLD, updateSLDItem, etc.)
```

## TODO List
Clean up the code: remove all unnecessary functions and variables
Draw all other items (Switch, Fuse, BusLink)
Draw all other SLDS
Any new item drag and drop shall be properly reflected in the table
Any change in the FromElement/ToElement/ConnectedBus properties in any Element shall update teh SLS accordingly

## 🚀 Usage

### Basic Import

```javascript
// In your main application file
import {
    drawSLD,
    updateSLD,
    updateSLDItem,
    updateSLDWithStudyResults,
    disposeSLD
} from './sld/index.js';

// Use the functions
drawSLD(divString, xGridSize, yGridSize, ...otherParams);
```

### Advanced Import (Accessing Internal Modules)

```javascript
// Import specific components if needed
import { CONFIG } from './sld/config/constants.js';
import { sldState } from './sld/state/sldState.js';
import { updateBus } from './sld/components/busUpdates.js';
```

## 🔨 Exported Functions

### Main Operations

- **`drawSLD(...)`** - Initialize and draw the SLD diagram
- **`updateSLD()`** - Update SLD with latest component data
- **`updateSLDItem(itemJSON, modalType, originalTag, branchesString)`** - Update individual item
- **`updateSLDWithStudyResults(...)`** - Update SLD with study results
- **`disposeSLD()`** - Cleanup and dispose SLD resources

## 📝 Migration Guide

### Before (Original Structure)
```javascript
// Everything in one large file
import { drawSLD, updateSLD } from './mySLDBackUp.js';
```

### After (Refactored Structure)
```javascript
// Cleaner imports from organized modules
import { drawSLD, updateSLD } from './sld/index.js';
```

The API remains the same - only the internal organization has changed!

## 🔧 Adding New Components

### 1. Add a New Shape

Create a new shape file in `shapes/`:

```javascript
// shapes/MyNewElement.js
import { dia } from '@joint/core';
import { CONFIG } from '../config/constants.js';

export function createMyNewElement() {
    return dia.Element.define('MyNewElement', {
        // Shape definition
    }, {
        markup: [/* markup */]
    });
}
```

Then export it in `shapes/index.js`:

```javascript
export { createMyNewElement } from './MyNewElement.js';
```

### 2. Add Component Update Function

Create update function in appropriate file:

```javascript
// components/myUpdates.js
export function updateMyComponent(model, data, branches) {
    // Update logic
    return model;
}
```

Export in `components/index.js`:

```javascript
export { updateMyComponent } from './myUpdates.js';
```

### 3. Add Event Handler

Add to `handlers/eventHandlers.js` or create a new handler file.

## 🐛 TODO / Known Issues

### Shapes to Complete
- [ ] CableElement.js
- [ ] BusDuctElement.js
- [ ] MotorElement.js
- [ ] HeaterElement.js
- [ ] CapacitorElement.js
- [ ] LumpLoadElement.js
- [ ] NodeElement.js
- [ ] BusElement.js
- [ ] BusNodeElement.js
- [ ] LoadElement.js

### Bugs to Fix (from original code)
- **updateMotor**: Uses wrong variable `busDuctdata` instead of `motordata`
- **updateHeater**: Uses wrong variable `busDuctdata` instead of `heaterdata`
- **updateCapacitor**: Uses wrong variable `busDuctdata` instead of `capacitordata`
- **updateLumpLoad**: Uses wrong variable `lumploadModel` and `busDuctdata`
- **updateSwitch**: Uses wrong variables

### Features to Add
- Complete the drawSLD function with element creation logic
- Add port distribution functions
- Add template element creation
- Add link connection validation

## 🎯 Benefits of Refactoring

1. **Maintainability**: Easy to find and modify specific functionality
2. **Testability**: Each module can be tested independently
3. **Reusability**: Components can be imported and used elsewhere
4. **Collaboration**: Multiple developers can work on different modules
5. **Debugging**: Easier to isolate and fix issues
6. **Documentation**: Self-documenting through file organization
7. **Performance**: Tree-shaking can remove unused code

## 📚 Related Files

Your original file is located at:
```
/mnt/user-data/uploads/mySLD.js
```

The refactored version is in:
```
/home/claude/sld-refactor/src/sld/
```

## 🤝 Contributing

When adding new features:
1. Create appropriate file in the correct directory
2. Follow existing naming conventions
3. Export through index.js files
4. Update this README
5. Add JSDoc comments to functions
