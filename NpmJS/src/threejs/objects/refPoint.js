// refPoint.js - Location Pin Markers with Helpers
// Updated version with pin markers, cross lines, and concentric circles

import * as THREE from 'three';
import { TextGeometry } from 'three/examples/jsm/geometries/TextGeometry.js';
import { FontLoader } from "three/examples/jsm/loaders/FontLoader";

// ============ Configuration ============
const PIN_CONFIG = {
    pinColor: 0xe74c3c,          // Red pin
    pinHoverColor: 0xf39c12,     // Orange on hover
    pinSelectedColor: 0x2ecc71,  // Green when selected
    pinHeight: 2.5,              // Height of the pin
    pinRadius: 0.5,              // Radius of pin head
    helperColor: 0x3498db,       // Blue helpers
    helperOpacity: 0.6,
    crossLineLength: 5,          // Length of cross lines
    circleRadii: [1, 2, 3.5, 5], // Radii for concentric circles
    showHelpers: true,
    helperType: 'both'           // 'circles', 'cross', 'both'
};

// ===== Configuration for constant screen size =====
const PIN_SCREEN_CONFIG = {
    targetHeightPixels: 30,      // Desired height in pixels
    referenceDistance: 100,      // Reference distance where scale = 1
    minScale: 0.1,               // Minimum scale (when very close)
    maxScale: 10                 // Maximum scale (when very far)
};

// Store for managing pins and helpers
let pinStore = {
    pins: new Map(),
    helpers: null,
    scene: null
};

// ============ Original Function (Kept for Backward Compatibility) ============

function drawRefPointMesh(tag, point, opacity) {
    let refPoint;
    try {
        refPoint = new THREE.Group();

        // Create materials with transparency and opacity
        const redRingMaterial = new THREE.MeshBasicMaterial({
            color: 0xff0000,
            side: THREE.DoubleSide,
            transparent: false,
            opacity: opacity
        });
        const yellowRingMaterial = new THREE.MeshBasicMaterial({
            color: 0xffff00,
            side: THREE.DoubleSide,
            transparent: false,
            opacity: opacity
        });

        const textMaterial = new THREE.MeshStandardMaterial({
            color: 0xffff00,
            transparent: false,
            opacity: 1,
            side: THREE.DoubleSide,
        });

        // Create geometries and meshes for circle and rings
        const redCircleGeometry = new THREE.CircleGeometry(0.1, 10);
        const redCircle = new THREE.Mesh(redCircleGeometry, redRingMaterial);
        refPoint.add(redCircle);

        const redRingGeometryIn = new THREE.RingGeometry(0.1, 0.3, 20);
        const redRingMeshIn = new THREE.Mesh(redRingGeometryIn, yellowRingMaterial);
        refPoint.add(redRingMeshIn);

        const redRingGeometryOut = new THREE.RingGeometry(0.3, 0.35, 20);
        const redRingMeshOut = new THREE.Mesh(redRingGeometryOut, redRingMaterial);
        refPoint.add(redRingMeshOut);

        // Load font and create text mesh
        const fontSize = .3;
        const fontHeight = fontSize / 5;
        const loader = new FontLoader();
        loader.load('https://unpkg.com/three@0.77.0/examples/fonts/helvetiker_regular.typeface.json', (font) => {
            const textGeometry = new TextGeometry(tag, {
                font: font,
                depth: fontHeight,
                size: fontSize,
                color: '#0000ff',
                bevelEnabled: false,
            });
            const textMesh = new THREE.Mesh(textGeometry, textMaterial);
            textMesh.geometry.center();
            textMesh.position.set(.5, .5, 0);
            refPoint.add(textMesh);
        });

        // Set group position
        refPoint.position.set(point.x, point.y, point.z);

        // Set custom properties
        refPoint.Tag = tag;
        refPoint.Type = "refPoint";
        refPoint.Clicked = false;
        refPoint.name = 'refPoint';

        // Dispose geometries
        redCircleGeometry.dispose();
        redRingGeometryIn.dispose();
        redRingGeometryOut.dispose();

    } catch (e) {
        console.log(tag + ' Error: ' + e);
    }
    return refPoint;
}


// ============ NEW: Pin Marker Functions ============

/**
 * Create a location pin marker (Google Maps style)
 * @param {string} tag - Unique identifier for the pin
 * @param {Object} point - {x, y, z} coordinates
 * @param {number} opacity - Opacity of the pin (0-1)
 * @param {boolean} showLabel - Whether to show text label
 * @returns {THREE.Group} - The pin marker group
 */
function drawPinMarker(tag, point, opacity = 1, showLabel = true) {
    const pinGroup = new THREE.Group();

    try {
        const height = PIN_CONFIG.pinHeight;
        const radius = PIN_CONFIG.pinRadius;

        // === Pin Head (Sphere) ===
        const headGeometry = new THREE.SphereGeometry(radius, 32, 32);
        const headMaterial = new THREE.MeshPhongMaterial({
            color: PIN_CONFIG.pinColor,
            shininess: 100,
            transparent: opacity < 1,
            opacity: opacity
        });
        const head = new THREE.Mesh(headGeometry, headMaterial);
        head.position.z = height;
        head.userData.isPin = true;
        head.userData.part = 'head';
        pinGroup.add(head);

        // === Inner Dot (White) ===
        const dotGeometry = new THREE.SphereGeometry(radius * 0.35, 16, 16);
        const dotMaterial = new THREE.MeshBasicMaterial({ color: 0xffffff });
        const dot = new THREE.Mesh(dotGeometry, dotMaterial);
        dot.position.z = height + radius * 0.3;
        pinGroup.add(dot);

        // === Pin Stem (Cone pointing down) ===
        const stemHeight = height - radius * 0.5;
        const stemGeometry = new THREE.ConeGeometry(radius * 0.3, stemHeight, 16);
        const stemMaterial = new THREE.MeshPhongMaterial({
            color: PIN_CONFIG.pinColor,
            shininess: 80,
            transparent: opacity < 1,
            opacity: opacity
        });
        const stem = new THREE.Mesh(stemGeometry, stemMaterial);
        stem.position.z = stemHeight / 2;
        stem.rotation.x = Math.PI; // Point downward
        stem.userData.isPin = true;
        stem.userData.part = 'stem';
        pinGroup.add(stem);

        // === Ground Shadow ===
        const shadowGeometry = new THREE.CircleGeometry(radius * 1.5, 32);
        const shadowMaterial = new THREE.MeshBasicMaterial({
            color: 0x000000,
            transparent: true,
            opacity: 0.3,
            side: THREE.DoubleSide
        });
        const shadow = new THREE.Mesh(shadowGeometry, shadowMaterial);
        shadow.position.z = 0.02; // Slightly above ground to prevent z-fighting
        pinGroup.add(shadow);

        // === Ground Contact Point (small red dot) ===
        const contactGeometry = new THREE.CircleGeometry(0.08, 16);
        const contactMaterial = new THREE.MeshBasicMaterial({
            color: PIN_CONFIG.pinColor,
            side: THREE.DoubleSide
        });
        const contact = new THREE.Mesh(contactGeometry, contactMaterial);
        contact.position.z = 0.03;
        pinGroup.add(contact);

        // === Text Label ===
        if (showLabel && tag) {
            const loader = new FontLoader();
            loader.load('https://unpkg.com/three@0.77.0/examples/fonts/helvetiker_regular.typeface.json', (font) => {
                const textGeometry = new TextGeometry(tag, {
                    font: font,
                    size: 0.4,
                    depth: 0.05,
                    bevelEnabled: false
                });
                const textMaterial = new THREE.MeshBasicMaterial({ color: 0xffffff });
                const textMesh = new THREE.Mesh(textGeometry, textMaterial);
                textMesh.geometry.center();
                textMesh.position.set(radius + 0.8, 0, height);
                pinGroup.add(textMesh);
            });
        }

        // Set position
        pinGroup.position.set(point.x, point.y, point.z || 0);

        // Set custom properties
        pinGroup.userData = {
            tag: tag,
            type: 'pinMarker',
            worldX: point.x,
            worldY: point.y,
            worldZ: point.z || 0,
            clicked: false
        };
        pinGroup.name = 'pinMarker_' + tag;

    } catch (e) {
        console.error('drawPinMarker Error:', e);
    }

    return pinGroup;
}


/**
 * Update all pin scales to maintain constant screen size
 * Call this in your animation/render loop
 * @param {THREE.Camera} camera - The camera
 * @param {THREE.WebGLRenderer} renderer - The renderer (for screen height)
 */
export function updatePinScales(camera, renderer) {
    if (!pinStore.pins || pinStore.pins.size === 0) return;

    const screenHeight = renderer.domElement.clientHeight;

    // Calculate scale factor based on camera type
    let scaleFactor;

    if (camera.isPerspectiveCamera) {
        // For perspective camera, scale based on distance
        const vFOV = camera.fov * Math.PI / 180;  // Convert to radians
        const baseScale = (PIN_SCREEN_CONFIG.targetHeightPixels / screenHeight) * 2;

        pinStore.pins.forEach((pin, tag) => {
            // Get distance from camera to pin
            const pinPosition = new THREE.Vector3();
            pin.getWorldPosition(pinPosition);
            const distance = camera.position.distanceTo(pinPosition);

            // Calculate scale to maintain constant screen size
            // At distance d, an object of size s appears as s / (2 * d * tan(fov/2)) of screen height
            const scale = baseScale * distance * Math.tan(vFOV / 2);

            // Clamp scale
            const clampedScale = Math.max(
                PIN_SCREEN_CONFIG.minScale,
                Math.min(PIN_SCREEN_CONFIG.maxScale, scale)
            );

            // Apply scale
            pin.scale.setScalar(clampedScale);
        });
    } else if (camera.isOrthographicCamera) {
        // For orthographic camera, scale based on camera zoom
        const orthoScale = (camera.top - camera.bottom) / screenHeight;
        const scale = PIN_SCREEN_CONFIG.targetHeightPixels * orthoScale;

        const clampedScale = Math.max(
            PIN_SCREEN_CONFIG.minScale,
            Math.min(PIN_SCREEN_CONFIG.maxScale, scale)
        );

        pinStore.pins.forEach((pin) => {
            pin.scale.setScalar(clampedScale);
        });
    }
}

/**
 * Configure pin screen size settings
 * @param {object} config - Configuration object
 */
export function configurePinScreenSize(config) {
    Object.assign(PIN_SCREEN_CONFIG, config);
}




/**
 * Create a 2D sprite-based pin (always faces camera)
 * @param {string} tag - Unique identifier
 * @param {Object} point - {x, y, z} coordinates
 * @param {number} opacity - Opacity (0-1)
 * @returns {THREE.Group} - The sprite pin group
 */
function drawSpritePinMarker(tag, point, opacity = 1) {
    const pinGroup = new THREE.Group();

    try {
        // Create canvas for pin texture
        const canvas = document.createElement('canvas');
        canvas.width = 64;
        canvas.height = 96;
        const ctx = canvas.getContext('2d');

        // Clear canvas
        ctx.clearRect(0, 0, 64, 96);

        // Draw pin shape (teardrop)
        const gradient = ctx.createLinearGradient(0, 0, 64, 96);
        gradient.addColorStop(0, '#ff6b6b');
        gradient.addColorStop(1, '#c0392b');

        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.moveTo(32, 90);  // Bottom point
        ctx.bezierCurveTo(32, 55, 4, 45, 4, 26);   // Left curve
        ctx.arc(32, 26, 28, Math.PI, 0, false);    // Top arc
        ctx.bezierCurveTo(60, 45, 32, 55, 32, 90); // Right curve
        ctx.closePath();
        ctx.fill();

        // Add highlight
        ctx.fillStyle = 'rgba(255, 255, 255, 0.3)';
        ctx.beginPath();
        ctx.ellipse(22, 20, 8, 12, -0.3, 0, Math.PI * 2);
        ctx.fill();

        // Inner white circle
        ctx.fillStyle = '#ffffff';
        ctx.beginPath();
        ctx.arc(32, 26, 10, 0, Math.PI * 2);
        ctx.fill();

        // Inner dot
        ctx.fillStyle = '#c0392b';
        ctx.beginPath();
        ctx.arc(32, 26, 4, 0, Math.PI * 2);
        ctx.fill();

        // Create sprite
        const texture = new THREE.CanvasTexture(canvas);
        texture.needsUpdate = true;

        const spriteMaterial = new THREE.SpriteMaterial({
            map: texture,
            transparent: true,
            opacity: opacity,
            depthTest: true,
            depthWrite: false
        });

        const sprite = new THREE.Sprite(spriteMaterial);
        sprite.scale.set(1.5, 2.25, 1);
        sprite.position.set(0, 0, 1.125); // Offset so bottom of pin touches ground
        sprite.userData.isPin = true;
        pinGroup.add(sprite);

        // Ground shadow
        const shadowGeometry = new THREE.CircleGeometry(0.5, 32);
        const shadowMaterial = new THREE.MeshBasicMaterial({
            color: 0x000000,
            transparent: true,
            opacity: 0.25,
            side: THREE.DoubleSide
        });
        const shadow = new THREE.Mesh(shadowGeometry, shadowMaterial);
        shadow.position.z = 0.01;
        pinGroup.add(shadow);

        // Position
        pinGroup.position.set(point.x, point.y, point.z || 0);

        // Properties
        pinGroup.userData = {
            tag: tag,
            type: 'spritePinMarker',
            worldX: point.x,
            worldY: point.y,
            worldZ: point.z || 0
        };
        pinGroup.name = 'pinMarker_' + tag;

    } catch (e) {
        console.error('drawSpritePinMarker Error:', e);
    }

    return pinGroup;
}


// ============ NEW: Helper Functions (Cross Lines & Circles) ============

/**
 * Create helper visuals at a point (cross lines + concentric circles)
 * @param {Object} point - {x, y, z} coordinates
 * @param {string} helperType - 'cross', 'circles', or 'both'
 * @returns {THREE.Group} - The helpers group
 */
function createPinHelpers(point, helperType = 'both') {
    const helperGroup = new THREE.Group();
    helperGroup.userData.type = 'pinHelpers';

    const z = (point.z || 0) + 0.05; // Slightly above ground

    // === Cross Lines ===
    if (helperType === 'cross' || helperType === 'both') {
        const lineLength = PIN_CONFIG.crossLineLength;

        // Solid center lines
        const solidMaterial = new THREE.LineBasicMaterial({
            color: PIN_CONFIG.helperColor,
            transparent: true,
            opacity: PIN_CONFIG.helperOpacity,
            linewidth: 2
        });

        // X-axis line (horizontal)
        const xLineGeometry = new THREE.BufferGeometry().setFromPoints([
            new THREE.Vector3(-lineLength, 0, 0),
            new THREE.Vector3(lineLength, 0, 0)
        ]);
        const xLine = new THREE.Line(xLineGeometry, solidMaterial);
        helperGroup.add(xLine);

        // Y-axis line (vertical in plan view)
        const yLineGeometry = new THREE.BufferGeometry().setFromPoints([
            new THREE.Vector3(0, -lineLength, 0),
            new THREE.Vector3(0, lineLength, 0)
        ]);
        const yLine = new THREE.Line(yLineGeometry, solidMaterial);
        helperGroup.add(yLine);

        // Extended dashed lines
        const dashedMaterial = new THREE.LineDashedMaterial({
            color: PIN_CONFIG.helperColor,
            transparent: true,
            opacity: PIN_CONFIG.helperOpacity * 0.5,
            dashSize: 0.3,
            gapSize: 0.2
        });

        const extLength = lineLength * 3;

        // Extended X
        const extXGeometry = new THREE.BufferGeometry().setFromPoints([
            new THREE.Vector3(-extLength, 0, 0),
            new THREE.Vector3(extLength, 0, 0)
        ]);
        const extXLine = new THREE.Line(extXGeometry, dashedMaterial);
        extXLine.computeLineDistances();
        helperGroup.add(extXLine);

        // Extended Y
        const extYGeometry = new THREE.BufferGeometry().setFromPoints([
            new THREE.Vector3(0, -extLength, 0),
            new THREE.Vector3(0, extLength, 0)
        ]);
        const extYLine = new THREE.Line(extYGeometry, dashedMaterial);
        extYLine.computeLineDistances();
        helperGroup.add(extYLine);

        // Small tick marks at intervals
        const tickMaterial = new THREE.LineBasicMaterial({
            color: PIN_CONFIG.helperColor,
            transparent: true,
            opacity: PIN_CONFIG.helperOpacity * 0.7
        });

        for (let i = 1; i <= lineLength; i++) {
            const tickSize = 0.15;

            // Ticks on X axis
            [i, -i].forEach(pos => {
                const tickGeom = new THREE.BufferGeometry().setFromPoints([
                    new THREE.Vector3(pos, -tickSize, 0),
                    new THREE.Vector3(pos, tickSize, 0)
                ]);
                helperGroup.add(new THREE.Line(tickGeom, tickMaterial));
            });

            // Ticks on Y axis
            [i, -i].forEach(pos => {
                const tickGeom = new THREE.BufferGeometry().setFromPoints([
                    new THREE.Vector3(-tickSize, pos, 0),
                    new THREE.Vector3(tickSize, pos, 0)
                ]);
                helperGroup.add(new THREE.Line(tickGeom, tickMaterial));
            });
        }
    }

    // === Concentric Circles ===
    if (helperType === 'circles' || helperType === 'both') {
        PIN_CONFIG.circleRadii.forEach((radius, index) => {
            // Ring geometry for thin circles
            const ringGeometry = new THREE.RingGeometry(
                radius - 0.03,
                radius + 0.03,
                64
            );
            const ringMaterial = new THREE.MeshBasicMaterial({
                color: PIN_CONFIG.helperColor,
                transparent: true,
                opacity: PIN_CONFIG.helperOpacity * (1 - index * 0.15),
                side: THREE.DoubleSide
            });
            const ring = new THREE.Mesh(ringGeometry, ringMaterial);
            helperGroup.add(ring);
        });
    }

    // Position helpers at the point
    helperGroup.position.set(point.x, point.y, z);

    return helperGroup;
}


/**
 * Create animated pulse effect at pin location
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {Object} point - {x, y, z} coordinates
 */
function createPulseEffect(scene, point) {
    const pulseGroup = new THREE.Group();
    const z = (point.z || 0) + 0.1;

    const geometry = new THREE.RingGeometry(0.1, 0.2, 32);
    const material = new THREE.MeshBasicMaterial({
        color: PIN_CONFIG.pinColor,
        transparent: true,
        opacity: 0.8,
        side: THREE.DoubleSide
    });

    const ring = new THREE.Mesh(geometry, material);
    pulseGroup.add(ring);
    pulseGroup.position.set(point.x, point.y, z);

    scene.add(pulseGroup);

    // Animation
    const startTime = Date.now();
    const duration = 800;
    const maxScale = 6;

    function animate() {
        const elapsed = Date.now() - startTime;
        const progress = elapsed / duration;

        if (progress >= 1) {
            scene.remove(pulseGroup);
            geometry.dispose();
            material.dispose();
            return;
        }

        const scale = 1 + progress * maxScale;
        ring.scale.set(scale, scale, 1);
        material.opacity = 0.8 * (1 - progress);

        requestAnimationFrame(animate);
    }

    animate();
}


// ============ Pin Manager Functions (for use from Blazor) ============

/**
 * Initialize the pin store with scene reference
 * @param {THREE.Scene} scene - The Three.js scene
 */
function initPinManager(scene) {
    pinStore.scene = scene;
    pinStore.pins = new Map();
    pinStore.helpers = null;
    console.log('Pin manager initialized');
}

/**
 * Add a pin marker to the scene
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} tag - Unique identifier
 * @param {Object} point - {x, y, z} coordinates
 * @param {boolean} useSprite - Use 2D sprite (true) or 3D mesh (false)
 * @param {boolean} showHelpers - Show cross lines and circles
 * @returns {string} - The tag of the created pin
 */
function addPin(scene, tag, point, useSprite = true, showHelpers = true) {
    // Store scene reference
    if (!pinStore.scene) {
        pinStore.scene = scene;
    }

    // Generate tag if not provided
    const pinTag = tag || `PIN_${Date.now()}`;

    // Remove existing pin with same tag
    removePin(scene, pinTag);

    // Create pin marker
    const pin = useSprite
        ? drawSpritePinMarker(pinTag, point, 1)
        : drawPinMarker(pinTag, point, 1, true);

    // Add to scene and store
    scene.add(pin);
    pinStore.pins.set(pinTag, pin);

    // Show helpers
    if (showHelpers) {
        showPinHelpers(scene, point, PIN_CONFIG.helperType);
    }

    // Pulse effect
    createPulseEffect(scene, point);

    console.log(`Pin added: ${pinTag} at (${point.x.toFixed(2)}, ${point.y.toFixed(2)})`);

    return pinTag;
}

/**
 * Remove a pin from the scene
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {string} tag - Pin tag to remove
 */
function removePin(scene, tag) {
    const pin = pinStore.pins.get(tag);
    if (pin) {
        scene.remove(pin);

        // Dispose geometries and materials
        pin.traverse((child) => {
            if (child.geometry) child.geometry.dispose();
            if (child.material) {
                if (Array.isArray(child.material)) {
                    child.material.forEach(m => m.dispose());
                } else {
                    child.material.dispose();
                }
            }
        });

        pinStore.pins.delete(tag);
        console.log(`Pin removed: ${tag}`);
    }
}

/**
 * Clear all pins from the scene
 * @param {THREE.Scene} scene - The Three.js scene
 */
function clearAllPins(scene) {
    pinStore.pins.forEach((pin, tag) => {
        scene.remove(pin);
        pin.traverse((child) => {
            if (child.geometry) child.geometry.dispose();
            if (child.material) {
                if (Array.isArray(child.material)) {
                    child.material.forEach(m => m.dispose());
                } else {
                    child.material.dispose();
                }
            }
        });
    });
    pinStore.pins.clear();
    hidePinHelpers(scene);
    console.log('All pins cleared');
}

/**
 * Show helpers at a specific point
 * @param {THREE.Scene} scene - The Three.js scene
 * @param {Object} point - {x, y, z} coordinates
 * @param {string} helperType - 'cross', 'circles', or 'both'
 */
function showPinHelpers(scene, point, helperType = 'both') {
    // Remove existing helpers
    hidePinHelpers(scene);

    // Create new helpers
    pinStore.helpers = createPinHelpers(point, helperType);
    scene.add(pinStore.helpers);
}

/**
 * Hide/remove helpers from the scene
 * @param {THREE.Scene} scene - The Three.js scene
 */
function hidePinHelpers(scene) {
    if (pinStore.helpers) {
        scene.remove(pinStore.helpers);
        pinStore.helpers.traverse((child) => {
            if (child.geometry) child.geometry.dispose();
            if (child.material) child.material.dispose();
        });
        pinStore.helpers = null;
    }
}

/**
 * Update pin color for hover/selection states
 * @param {string} tag - Pin tag
 * @param {string} state - 'normal', 'hover', or 'selected'
 */
function setPinState(tag, state) {
    const pin = pinStore.pins.get(tag);
    if (!pin) return;

    let color;
    switch (state) {
        case 'hover':
            color = PIN_CONFIG.pinHoverColor;
            break;
        case 'selected':
            color = PIN_CONFIG.pinSelectedColor;
            break;
        default:
            color = PIN_CONFIG.pinColor;
    }

    pin.traverse((child) => {
        if (child.userData.isPin && child.material && child.material.color) {
            child.material.color.setHex(color);
        }
    });
}

/**
 * Get all pin data
 * @returns {Array} - Array of pin data objects
 */
function getAllPins() {
    const pins = [];
    pinStore.pins.forEach((pin, tag) => {
        pins.push({
            tag: tag,
            x: pin.userData.worldX,
            y: pin.userData.worldY,
            z: pin.userData.worldZ
        });
    });
    return pins;
}

/**
 * Configure pin settings
 * @param {Object} config - Configuration object
 */
function configurePins(config) {
    Object.assign(PIN_CONFIG, config);
}


// ============ Exports ============

export {
    // Original function (backward compatible)
    drawRefPointMesh,

    // New pin marker functions
    drawPinMarker,
    drawSpritePinMarker,

    // Helper functions
    createPinHelpers,
    createPulseEffect,

    // Manager functions (for Blazor integration)
    initPinManager,
    addPin,
    removePin,
    clearAllPins,
    showPinHelpers,
    hidePinHelpers,
    setPinState,
    getAllPins,
    configurePins,

    // Configuration
    PIN_CONFIG
};