// refPoint.js - Location Pin Markers with Helpers
// FIXED: Pin tip touches ground, tag label always visible

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

        const redCircleGeometry = new THREE.CircleGeometry(0.1, 10);
        const redCircle = new THREE.Mesh(redCircleGeometry, redRingMaterial);
        refPoint.add(redCircle);

        const redRingGeometryIn = new THREE.RingGeometry(0.1, 0.3, 20);
        const redRingMeshIn = new THREE.Mesh(redRingGeometryIn, yellowRingMaterial);
        refPoint.add(redRingMeshIn);

        const redRingGeometryOut = new THREE.RingGeometry(0.3, 0.35, 20);
        const redRingMeshOut = new THREE.Mesh(redRingGeometryOut, redRingMaterial);
        refPoint.add(redRingMeshOut);

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

        refPoint.position.set(point.x, point.y, point.z);
        refPoint.Tag = tag;
        refPoint.Type = "refPoint";
        refPoint.Clicked = false;
        refPoint.name = 'refPoint';

        redCircleGeometry.dispose();
        redRingGeometryIn.dispose();
        redRingGeometryOut.dispose();

    } catch (e) {
        console.log(tag + ' Error: ' + e);
    }
    return refPoint;
}


// ============ FIXED: Sprite Pin Marker ============

/**
 * Create a 2D sprite-based pin (always faces camera)
 * FIXED: Pin tip touches exactly at point.z
 * FIXED: Tag label rendered on top, always visible
 */
function drawSpritePinMarker(tag, point, opacity = 1) {
    const pinGroup = new THREE.Group();

    try {
        // Create canvas for pin texture
        // Canvas is 64x96, with pin tip at BOTTOM CENTER
        const canvas = document.createElement('canvas');
        canvas.width = 64;
        canvas.height = 96;
        const ctx = canvas.getContext('2d');

        // Clear canvas
        ctx.clearRect(0, 0, 64, 96);

        // Draw pin shape - TIP at bottom (y=96)
        const gradient = ctx.createLinearGradient(0, 0, 64, 96);
        gradient.addColorStop(0, '#ff6b6b');
        gradient.addColorStop(1, '#c0392b');

        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.moveTo(32, 96);  // Bottom tip - exactly at canvas bottom
        ctx.bezierCurveTo(32, 65, 4, 50, 4, 28);
        ctx.arc(32, 28, 28, Math.PI, 0, false);
        ctx.bezierCurveTo(60, 50, 32, 65, 32, 96);
        ctx.closePath();
        ctx.fill();

        // Highlight
        ctx.fillStyle = 'rgba(255, 255, 255, 0.3)';
        ctx.beginPath();
        ctx.ellipse(22, 22, 8, 12, -0.3, 0, Math.PI * 2);
        ctx.fill();

        // Inner white circle
        ctx.fillStyle = '#ffffff';
        ctx.beginPath();
        ctx.arc(32, 28, 10, 0, Math.PI * 2);
        ctx.fill();

        // Inner red dot
        ctx.fillStyle = '#c0392b';
        ctx.beginPath();
        ctx.arc(32, 28, 4, 0, Math.PI * 2);
        ctx.fill();

        // Create sprite texture
        const texture = new THREE.CanvasTexture(canvas);
        texture.needsUpdate = true;

        const spriteMaterial = new THREE.SpriteMaterial({
            map: texture,
            transparent: true,
            opacity: opacity,
            depthTest: true,
            depthWrite: false,
            sizeAttenuation: true
        });

        const sprite = new THREE.Sprite(spriteMaterial);

        // Sprite dimensions (world units at scale 1)
        const spriteWidth = 1.5;
        const spriteHeight = 2.25;
        sprite.scale.set(spriteWidth, spriteHeight, 1);

        // CRITICAL FIX: Position sprite so its BOTTOM (pin tip) is at z=0
        // Sprite anchor is at center by default, so offset by half height
        // The pin tip is at the bottom of the canvas (y=96 in canvas = bottom of sprite)
        sprite.position.set(0, 0, spriteHeight / 2);

        sprite.userData.isPin = true;
        sprite.userData.baseWidth = spriteWidth;
        sprite.userData.baseHeight = spriteHeight;
        pinGroup.add(sprite);

        // Small ground marker at exact pin tip location (z=0 relative to group)
        const groundMarkerGeom = new THREE.RingGeometry(0.05, 0.12, 16);
        const groundMarkerMat = new THREE.MeshBasicMaterial({
            color: 0xe74c3c,
            side: THREE.DoubleSide,
            transparent: true,
            opacity: 0.9,
            depthTest: false  // Always visible
        });
        const groundMarker = new THREE.Mesh(groundMarkerGeom, groundMarkerMat);
        groundMarker.position.z = 0.02;  // Just above ground
        groundMarker.userData.isGroundMarker = true;
        pinGroup.add(groundMarker);

        // Shadow - centered at pin tip
        const shadowGeom = new THREE.CircleGeometry(0.4, 32);
        const shadowMat = new THREE.MeshBasicMaterial({
            color: 0x000000,
            transparent: true,
            opacity: 0.25,
            side: THREE.DoubleSide
        });
        const shadow = new THREE.Mesh(shadowGeom, shadowMat);
        shadow.position.z = 0.01;
        shadow.userData.isShadow = true;
        pinGroup.add(shadow);

        // TAG LABEL - Create as sprite, positioned to the side, always on top
        const labelSprite = createTagLabelSprite(tag);
        // Position label to the RIGHT of the pin, at mid-height
        labelSprite.position.set(spriteWidth * 0.8 + 0.5, 0, spriteHeight * 0.6);
        labelSprite.userData.isLabel = true;
        labelSprite.userData.baseOffsetX = spriteWidth * 0.8 + 0.5;
        labelSprite.userData.baseOffsetZ = spriteHeight * 0.6;
        pinGroup.add(labelSprite);

        // Set group position at the exact click point
        // Pin tip will be at this location
        pinGroup.position.set(point.x, point.y, point.z || 0);

        pinGroup.userData = {
            tag: tag,
            type: 'spritePinMarker',
            worldX: point.x,
            worldY: point.y,
            worldZ: point.z || 0,
            baseWidth: spriteWidth,
            baseHeight: spriteHeight
        };
        pinGroup.name = 'pinMarker_' + tag;

    } catch (e) {
        console.error('drawSpritePinMarker Error:', e);
    }

    return pinGroup;
}


/**
 * Create tag label as a sprite (always faces camera, always on top)
 */
function createTagLabelSprite(tag) {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');

    // Measure text
    ctx.font = 'bold 36px Arial';
    const textMetrics = ctx.measureText(tag);
    const textWidth = textMetrics.width;

    // Size canvas
    const padding = 20;
    canvas.width = Math.max(100, textWidth + padding * 2);
    canvas.height = 56;

    // Draw rounded background
    ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
    roundRect(ctx, 0, 0, canvas.width, canvas.height, 10);
    ctx.fill();

    // Draw border
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 3;
    roundRect(ctx, 2, 2, canvas.width - 4, canvas.height - 4, 8);
    ctx.stroke();

    // Draw text
    ctx.font = 'bold 32px Arial';
    ctx.fillStyle = '#ffffff';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(tag, canvas.width / 2, canvas.height / 2);

    // Create sprite
    const texture = new THREE.CanvasTexture(canvas);
    texture.needsUpdate = true;

    const spriteMat = new THREE.SpriteMaterial({
        map: texture,
        transparent: true,
        depthTest: false,   // CRITICAL: Render on top of everything
        depthWrite: false
    });

    const sprite = new THREE.Sprite(spriteMat);
    const aspect = canvas.width / canvas.height;
    const baseScale = 1.0;
    sprite.scale.set(aspect * baseScale, baseScale, 1);
    sprite.userData.baseScaleX = aspect * baseScale;
    sprite.userData.baseScaleY = baseScale;

    return sprite;
}

/**
 * Helper: Draw rounded rectangle on canvas
 */
function roundRect(ctx, x, y, w, h, r) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();
}


// ============ 3D Pin Marker (Alternative) ============

function drawPinMarker(tag, point, opacity = 1, showLabel = true) {
    const pinGroup = new THREE.Group();

    try {
        const height = PIN_CONFIG.pinHeight;
        const radius = PIN_CONFIG.pinRadius;

        // Pin Head (Sphere)
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
        pinGroup.add(head);

        // White dot
        const dotGeometry = new THREE.SphereGeometry(radius * 0.35, 16, 16);
        const dotMaterial = new THREE.MeshBasicMaterial({ color: 0xffffff });
        const dot = new THREE.Mesh(dotGeometry, dotMaterial);
        dot.position.z = height + radius * 0.3;
        pinGroup.add(dot);

        // Cone stem - TIP at z=0
        const stemGeometry = new THREE.ConeGeometry(radius * 0.4, height, 16);
        const stemMaterial = new THREE.MeshPhongMaterial({
            color: PIN_CONFIG.pinColor,
            shininess: 80,
            transparent: opacity < 1,
            opacity: opacity
        });
        const stem = new THREE.Mesh(stemGeometry, stemMaterial);
        stem.position.z = height / 2;  // Center of cone
        stem.rotation.x = Math.PI;      // Flip so tip points down to z=0
        stem.userData.isPin = true;
        pinGroup.add(stem);

        // Ground marker
        const contactGeom = new THREE.RingGeometry(0.05, 0.1, 16);
        const contactMat = new THREE.MeshBasicMaterial({
            color: PIN_CONFIG.pinColor,
            side: THREE.DoubleSide,
            depthTest: false
        });
        const contact = new THREE.Mesh(contactGeom, contactMat);
        contact.position.z = 0.02;
        pinGroup.add(contact);

        // Shadow
        const shadowGeom = new THREE.CircleGeometry(radius * 1.2, 32);
        const shadowMat = new THREE.MeshBasicMaterial({
            color: 0x000000,
            transparent: true,
            opacity: 0.3,
            side: THREE.DoubleSide
        });
        const shadow = new THREE.Mesh(shadowGeom, shadowMat);
        shadow.position.z = 0.01;
        pinGroup.add(shadow);

        // Label
        if (showLabel && tag) {
            const labelSprite = createTagLabelSprite(tag);
            labelSprite.position.set(radius + 1.2, 0, height);
            labelSprite.userData.isLabel = true;
            pinGroup.add(labelSprite);
        }

        pinGroup.position.set(point.x, point.y, point.z || 0);

        pinGroup.userData = {
            tag: tag,
            type: 'pinMarker',
            worldX: point.x,
            worldY: point.y,
            worldZ: point.z || 0
        };
        pinGroup.name = 'pinMarker_' + tag;

    } catch (e) {
        console.error('drawPinMarker Error:', e);
    }

    return pinGroup;
}


// ============ Pin Scale Functions (Constant Screen Size) ============

/**
 * Update all pin scales to maintain constant screen size
 * MUST be called in animation loop
 */
export function updatePinScales(camera, renderer) {
    if (!pinStore.pins || pinStore.pins.size === 0) return;

    const screenHeight = renderer.domElement.clientHeight;
    if (screenHeight === 0) return;

    pinStore.pins.forEach((pin) => {
        const pinPos = new THREE.Vector3();
        pin.getWorldPosition(pinPos);

        let scale;

        if (camera.isPerspectiveCamera) {
            const dist = camera.position.distanceTo(pinPos);
            const vFOV = camera.fov * Math.PI / 180;
            const frustumH = 2 * dist * Math.tan(vFOV / 2);
            scale = (PIN_SCREEN_CONFIG.targetHeightPixels / screenHeight) * frustumH;
        } else if (camera.isOrthographicCamera) {
            const frustumH = camera.top - camera.bottom;
            scale = (PIN_SCREEN_CONFIG.targetHeightPixels / screenHeight) * frustumH;
        } else {
            return;
        }

        scale = Math.max(PIN_SCREEN_CONFIG.minScale, Math.min(PIN_SCREEN_CONFIG.maxScale, scale));

        // Apply scale to children
        pin.traverse((child) => {
            if (child.isSprite && child.userData.isPin) {
                // Pin sprite
                const bw = child.userData.baseWidth || 1.5;
                const bh = child.userData.baseHeight || 2.25;
                child.scale.set(bw * scale, bh * scale, 1);
                // IMPORTANT: Keep pin tip at z=0 by adjusting position
                child.position.z = (bh * scale) / 2;
            } else if (child.isSprite && child.userData.isLabel) {
                // Label sprite
                const bsx = child.userData.baseScaleX || 1;
                const bsy = child.userData.baseScaleY || 1;
                child.scale.set(bsx * scale, bsy * scale, 1);
                // Adjust label position
                const offX = child.userData.baseOffsetX || 1.5;
                const offZ = child.userData.baseOffsetZ || 1.5;
                child.position.x = offX * scale;
                child.position.z = offZ * scale;
            } else if (child.isMesh && (child.userData.isGroundMarker || child.userData.isShadow)) {
                // Ground marker and shadow stay at ground level, just scale
                child.scale.set(scale, scale, 1);
            }
        });
    });
}

/**
 * Update helper scales
 */
export function updateHelperScales(camera, renderer) {
    if (!pinStore.helpers) return;

    const screenHeight = renderer.domElement.clientHeight;
    if (screenHeight === 0) return;

    const pos = new THREE.Vector3();
    pinStore.helpers.getWorldPosition(pos);

    let scale;

    if (camera.isPerspectiveCamera) {
        const dist = camera.position.distanceTo(pos);
        const vFOV = camera.fov * Math.PI / 180;
        const frustumH = 2 * dist * Math.tan(vFOV / 2);
        scale = (80 / screenHeight) * frustumH;
    } else if (camera.isOrthographicCamera) {
        const frustumH = camera.top - camera.bottom;
        scale = (80 / screenHeight) * frustumH;
    }

    if (scale) {
        scale = Math.max(0.5, Math.min(50, scale));
        pinStore.helpers.scale.setScalar(scale);
    }
}

export function configurePinScreenSize(config) {
    Object.assign(PIN_SCREEN_CONFIG, config);
}

export function setPinScreenSize(heightPixels) {
    PIN_SCREEN_CONFIG.targetHeightPixels = heightPixels;
}


// ============ Helper Functions (Cross Lines & Circles) ============

/**
 * Create cross lines and circles at a point
 * FIXED: All elements at ground level with depthTest=false to show through
 */
function createPinHelpers(point, helperType = 'both') {
    const helperGroup = new THREE.Group();
    helperGroup.userData.type = 'pinHelpers';

    const groundZ = 0.03;  // Slightly above z=0 to prevent z-fighting

    // === Cross Lines ===
    if (helperType === 'cross' || helperType === 'both') {
        const len = PIN_CONFIG.crossLineLength;

        const lineMat = new THREE.LineBasicMaterial({
            color: PIN_CONFIG.helperColor,
            transparent: true,
            opacity: PIN_CONFIG.helperOpacity,
            depthTest: false  // Always visible
        });

        // X-axis line
        const xGeom = new THREE.BufferGeometry().setFromPoints([
            new THREE.Vector3(-len, 0, groundZ),
            new THREE.Vector3(len, 0, groundZ)
        ]);
        helperGroup.add(new THREE.Line(xGeom, lineMat));

        // Y-axis line
        const yGeom = new THREE.BufferGeometry().setFromPoints([
            new THREE.Vector3(0, -len, groundZ),
            new THREE.Vector3(0, len, groundZ)
        ]);
        helperGroup.add(new THREE.Line(yGeom, lineMat));

        // Dashed extensions
        const dashMat = new THREE.LineDashedMaterial({
            color: PIN_CONFIG.helperColor,
            transparent: true,
            opacity: PIN_CONFIG.helperOpacity * 0.5,
            dashSize: 0.3,
            gapSize: 0.2,
            depthTest: false
        });

        const extLen = len * 3;

        const extXGeom = new THREE.BufferGeometry().setFromPoints([
            new THREE.Vector3(-extLen, 0, groundZ),
            new THREE.Vector3(extLen, 0, groundZ)
        ]);
        const extXLine = new THREE.Line(extXGeom, dashMat);
        extXLine.computeLineDistances();
        helperGroup.add(extXLine);

        const extYGeom = new THREE.BufferGeometry().setFromPoints([
            new THREE.Vector3(0, -extLen, groundZ),
            new THREE.Vector3(0, extLen, groundZ)
        ]);
        const extYLine = new THREE.Line(extYGeom, dashMat);
        extYLine.computeLineDistances();
        helperGroup.add(extYLine);

        // Tick marks
        const tickMat = new THREE.LineBasicMaterial({
            color: PIN_CONFIG.helperColor,
            transparent: true,
            opacity: PIN_CONFIG.helperOpacity * 0.7,
            depthTest: false
        });

        for (let i = 1; i <= len; i++) {
            const ts = 0.15;
            [i, -i].forEach(p => {
                const txGeom = new THREE.BufferGeometry().setFromPoints([
                    new THREE.Vector3(p, -ts, groundZ),
                    new THREE.Vector3(p, ts, groundZ)
                ]);
                helperGroup.add(new THREE.Line(txGeom, tickMat));

                const tyGeom = new THREE.BufferGeometry().setFromPoints([
                    new THREE.Vector3(-ts, p, groundZ),
                    new THREE.Vector3(ts, p, groundZ)
                ]);
                helperGroup.add(new THREE.Line(tyGeom, tickMat));
            });
        }
    }

    // === Concentric Circles ===
    if (helperType === 'circles' || helperType === 'both') {
        PIN_CONFIG.circleRadii.forEach((r, i) => {
            const ringGeom = new THREE.RingGeometry(r - 0.03, r + 0.03, 64);
            const ringMat = new THREE.MeshBasicMaterial({
                color: PIN_CONFIG.helperColor,
                transparent: true,
                opacity: PIN_CONFIG.helperOpacity * (1 - i * 0.15),
                side: THREE.DoubleSide,
                depthTest: false
            });
            const ring = new THREE.Mesh(ringGeom, ringMat);
            ring.position.z = groundZ;
            helperGroup.add(ring);
        });
    }

    // Position at the point
    helperGroup.position.set(point.x, point.y, point.z || 0);

    return helperGroup;
}


/**
 * Pulse effect at pin location
 */
function createPulseEffect(scene, point) {
    const pulseGroup = new THREE.Group();
    const z = (point.z || 0) + 0.05;

    const geom = new THREE.RingGeometry(0.1, 0.2, 32);
    const mat = new THREE.MeshBasicMaterial({
        color: PIN_CONFIG.pinColor,
        transparent: true,
        opacity: 0.8,
        side: THREE.DoubleSide,
        depthTest: false
    });

    const ring = new THREE.Mesh(geom, mat);
    pulseGroup.add(ring);
    pulseGroup.position.set(point.x, point.y, z);
    scene.add(pulseGroup);

    const startTime = Date.now();
    const duration = 800;
    const maxScale = 6;

    function animate() {
        const progress = (Date.now() - startTime) / duration;
        if (progress >= 1) {
            scene.remove(pulseGroup);
            geom.dispose();
            mat.dispose();
            return;
        }
        ring.scale.setScalar(1 + progress * maxScale);
        mat.opacity = 0.8 * (1 - progress);
        requestAnimationFrame(animate);
    }
    animate();
}


// ============ Pin Manager Functions ============

function initPinManager(scene) {
    pinStore.scene = scene;
    pinStore.pins = new Map();
    pinStore.helpers = null;
    console.log('Pin manager initialized');
}

function addPin(scene, tag, point, useSprite = true, showHelpers = true) {
    if (!pinStore.scene) pinStore.scene = scene;

    const pinTag = tag || `PIN_${Date.now()}`;
    removePin(scene, pinTag);

    const pin = useSprite
        ? drawSpritePinMarker(pinTag, point, 1)
        : drawPinMarker(pinTag, point, 1, true);

    scene.add(pin);
    pinStore.pins.set(pinTag, pin);

    if (showHelpers) {
        showPinHelpers(scene, point, PIN_CONFIG.helperType);
    }

    createPulseEffect(scene, point);

    console.log(`Pin added: ${pinTag} at (${point.x.toFixed(2)}, ${point.y.toFixed(2)}, ${(point.z || 0).toFixed(2)})`);
    return pinTag;
}

function removePin(scene, tag) {
    const pin = pinStore.pins.get(tag);
    if (pin) {
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
        pinStore.pins.delete(tag);
        console.log(`Pin removed: ${tag}`);
    }
}

function clearAllPins(scene) {
    pinStore.pins.forEach((pin) => {
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

function showPinHelpers(scene, point, helperType = 'both') {
    hidePinHelpers(scene);
    pinStore.helpers = createPinHelpers(point, helperType);
    scene.add(pinStore.helpers);
}

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

function setPinState(tag, state) {
    const pin = pinStore.pins.get(tag);
    if (!pin) return;

    let color;
    switch (state) {
        case 'hover': color = PIN_CONFIG.pinHoverColor; break;
        case 'selected': color = PIN_CONFIG.pinSelectedColor; break;
        default: color = PIN_CONFIG.pinColor;
    }

    pin.traverse((child) => {
        if (child.userData.isPin && child.material?.color) {
            child.material.color.setHex(color);
        }
    });
}

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

function configurePins(config) {
    Object.assign(PIN_CONFIG, config);
}


// ============ Exports ============

export {
    drawRefPointMesh,
    drawPinMarker,
    drawSpritePinMarker,
    createTagLabelSprite,
    createPinHelpers,
    createPulseEffect,
    initPinManager,
    addPin,
    removePin,
    clearAllPins,
    showPinHelpers,
    hidePinHelpers,
    setPinState,
    getAllPins,
    configurePins,
    PIN_CONFIG,
    PIN_SCREEN_CONFIG
};