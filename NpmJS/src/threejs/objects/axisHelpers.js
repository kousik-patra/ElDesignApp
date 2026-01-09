// NpmJS/src/threejs/objects/axisHelpers.js

import * as THREE from 'three';

/**
 * Configuration for axis helpers
 */
const AXIS_CONFIG = {
    // Infinite axis lines
    axisLength: 100000,  // Very large number to appear infinite
    axisColors: {
        x: 0xff0000,  // Red
        y: 0x00ff00,  // Green
        z: 0x0000ff   // Blue
    },
    axisOpacity: 0.6,

    // 3D Arrow indicator (bottom-left corner)
    arrowSize: 300,           // Size in pixels (will be converted to viewport)
    arrowHeadLength: 0.3,    // Proportion of shaft
    arrowHeadWidth: 0.15,    // Proportion of shaft
    arrowShaftRadius: 0.04,
    arrowPosition: { x: 80, y: 80 },  // Pixels from bottom-left
    arrowLabels: true,       // Show X, Y, Z labels
    labelSize: 30            // Font size for labels
};

/**
 * Create infinite axis lines (extends in both directions)
 * @returns {THREE.Group} Group containing axis lines
 */
export function createInfiniteAxes() {
    const group = new THREE.Group();
    group.name = 'InfiniteAxes';

    const length = AXIS_CONFIG.axisLength;

    // X Axis (Red)
    const xGeometry = new THREE.BufferGeometry().setFromPoints([
        new THREE.Vector3(-length, 0, 0),
        new THREE.Vector3(length, 0, 0)
    ]);
    const xMaterial = new THREE.LineBasicMaterial({
        color: AXIS_CONFIG.axisColors.x,
        opacity: AXIS_CONFIG.axisOpacity,
        transparent: true
    });
    const xAxis = new THREE.Line(xGeometry, xMaterial);
    xAxis.name = 'X-Axis';
    group.add(xAxis);

    // Y Axis (Green)
    const yGeometry = new THREE.BufferGeometry().setFromPoints([
        new THREE.Vector3(0, -length, 0),
        new THREE.Vector3(0, length, 0)
    ]);
    const yMaterial = new THREE.LineBasicMaterial({
        color: AXIS_CONFIG.axisColors.y,
        opacity: AXIS_CONFIG.axisOpacity,
        transparent: true
    });
    const yAxis = new THREE.Line(yGeometry, yMaterial);
    yAxis.name = 'Y-Axis';
    group.add(yAxis);

    // Z Axis (Blue)
    const zGeometry = new THREE.BufferGeometry().setFromPoints([
        new THREE.Vector3(0, 0, -length),
        new THREE.Vector3(0, 0, length)
    ]);
    const zMaterial = new THREE.LineBasicMaterial({
        color: AXIS_CONFIG.axisColors.z,
        opacity: AXIS_CONFIG.axisOpacity,
        transparent: true
    });
    const zAxis = new THREE.Line(zGeometry, zMaterial);
    zAxis.name = 'Z-Axis';
    group.add(zAxis);

    return group;
}

/**
 * Create a single 3D arrow with shaft and head
 * @param {THREE.Vector3} direction - Direction of arrow
 * @param {number} color - Hex color
 * @param {number} length - Total length of arrow
 * @returns {THREE.Group} Arrow mesh group
 */
function createArrow(direction, color, length = 1) {
    const group = new THREE.Group();

    const shaftLength = length * (1 - AXIS_CONFIG.arrowHeadLength);
    const headLength = length * AXIS_CONFIG.arrowHeadLength;
    const shaftRadius = length * AXIS_CONFIG.arrowShaftRadius;
    const headRadius = length * AXIS_CONFIG.arrowHeadWidth;

    // Shaft (cylinder)
    const shaftGeometry = new THREE.CylinderGeometry(
        shaftRadius, shaftRadius, shaftLength, 16
    );
    const material = new THREE.MeshBasicMaterial({ color: color });
    const shaft = new THREE.Mesh(shaftGeometry, material);
    shaft.position.y = shaftLength / 2;
    group.add(shaft);

    // Head (cone)
    const headGeometry = new THREE.ConeGeometry(headRadius, headLength, 16);
    const head = new THREE.Mesh(headGeometry, material);
    head.position.y = shaftLength + headLength / 2;
    group.add(head);

    // Rotate group to point in the specified direction
    if (direction.x === 1) {
        group.rotation.z = -Math.PI / 2;
    } else if (direction.z === 1) {
        group.rotation.x = Math.PI / 2;
    }
    // Y direction is default (no rotation needed)

    return group;
}

/**
 * Create text sprite for axis label
 * @param {string} text - Label text
 * @param {number} color - Hex color
 * @returns {THREE.Sprite} Text sprite
 */
function createTextSprite(text, color) {
    const canvas = document.createElement('canvas');
    const size = 64;
    canvas.width = size;
    canvas.height = size;

    const context = canvas.getContext('2d');
    context.fillStyle = `#${color.toString(16).padStart(6, '0')}`;
    context.font = 'Bold 48px Arial';
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.fillText(text, size / 2, size / 2);

    const texture = new THREE.CanvasTexture(canvas);
    const spriteMaterial = new THREE.SpriteMaterial({
        map: texture,
        transparent: true
    });
    const sprite = new THREE.Sprite(spriteMaterial);
    sprite.scale.set(0.4, 0.4, 0.4);

    return sprite;
}

/**
 * Creates a 3D axis indicator (triad) for the corner of the screen
 * This creates its own scene and camera for overlay rendering
 * @returns {Object} Object containing scene, camera, and update method
 */
export function createAxisIndicator() {
    // Create separate scene for the axis indicator
    const scene = new THREE.Scene();

    // Create orthographic camera for consistent size
    const camera = new THREE.PerspectiveCamera(50, 1, 0.1, 100);
    camera.position.set(2, 2, 2);
    camera.lookAt(0, 0, 0);

    // Create arrow group
    const arrowGroup = new THREE.Group();
    arrowGroup.name = 'AxisIndicator';

    const arrowLength = 1;

    // X Arrow (Red)
    const xArrow = createArrow(
        new THREE.Vector3(1, 0, 0),
        AXIS_CONFIG.axisColors.x,
        arrowLength
    );
    xArrow.name = 'X-Arrow';
    arrowGroup.add(xArrow);

    // Y Arrow (Green)
    const yArrow = createArrow(
        new THREE.Vector3(0, 1, 0),
        AXIS_CONFIG.axisColors.y,
        arrowLength
    );
    yArrow.name = 'Y-Arrow';
    arrowGroup.add(yArrow);

    // Z Arrow (Blue)
    const zArrow = createArrow(
        new THREE.Vector3(0, 0, 1),
        AXIS_CONFIG.axisColors.z,
        arrowLength
    );
    zArrow.name = 'Z-Arrow';
    arrowGroup.add(zArrow);

    // Add labels if enabled
    if (AXIS_CONFIG.arrowLabels) {
        const labelOffset = 1.3;

        const xLabel = createTextSprite('X', AXIS_CONFIG.axisColors.x);
        xLabel.position.set(labelOffset, 0, 0);
        arrowGroup.add(xLabel);

        const yLabel = createTextSprite('Y', AXIS_CONFIG.axisColors.y);
        yLabel.position.set(0, labelOffset, 0);
        arrowGroup.add(yLabel);

        const zLabel = createTextSprite('Z', AXIS_CONFIG.axisColors.z);
        zLabel.position.set(0, 0, labelOffset);
        arrowGroup.add(zLabel);
    }

    // Add small sphere at origin
    const originGeometry = new THREE.SphereGeometry(0.08, 16, 16);
    const originMaterial = new THREE.MeshBasicMaterial({ color: 0xffffff });
    const origin = new THREE.Mesh(originGeometry, originMaterial);
    origin.name = 'Origin';
    arrowGroup.add(origin);

    scene.add(arrowGroup);

    // Add subtle lighting
    scene.add(new THREE.AmbientLight(0xffffff, 0.8));

    return {
        scene: scene,
        camera: camera,
        arrowGroup: arrowGroup,
        config: AXIS_CONFIG,

        /**
         * Update the indicator to match main camera rotation
         * @param {THREE.Camera} mainCamera - The main scene camera
         */
        update: function(mainCamera) {
            if (!mainCamera) return;

            // Copy rotation from main camera to make indicator rotate with view
            this.camera.position.copy(mainCamera.position);
            this.camera.position.sub(mainCamera.position.clone().normalize().multiplyScalar(
                mainCamera.position.length() - 4
            ));
            this.camera.position.normalize().multiplyScalar(4);
            this.camera.lookAt(0, 0, 0);
            this.camera.quaternion.copy(mainCamera.quaternion);

            // Alternative: Match arrow group rotation to camera
            // This makes the arrows always show orientation relative to world
            const cameraDirection = new THREE.Vector3();
            mainCamera.getWorldDirection(cameraDirection);

            // Position camera based on main camera orientation
            const distance = 4;
            this.camera.position.set(distance, distance, distance);
            this.camera.position.applyQuaternion(mainCamera.quaternion);
            this.camera.lookAt(0, 0, 0);
        },

        /**
         * Render the indicator in the corner
         * @param {THREE.WebGLRenderer} renderer - The main renderer
         * @param {number} viewportWidth - Total viewport width
         * @param {number} viewportHeight - Total viewport height
         */
        render: function(renderer, viewportWidth, viewportHeight) {
            const size = AXIS_CONFIG.arrowSize;
            const padding = 10;

            // Save current state
            const currentAutoClear = renderer.autoClear;
            renderer.autoClear = false;

            // Set viewport to bottom-left corner
            renderer.setViewport(
                padding,                    // x
                padding,                    // y (from bottom)
                size,                       // width
                size                        // height
            );

            renderer.setScissor(
                padding,
                padding,
                size,
                size
            );
            renderer.setScissorTest(true);

            // Clear only the depth buffer for this viewport
            renderer.clearDepth();

            // Render the indicator scene
            renderer.render(this.scene, this.camera);

            // Restore state
            renderer.setScissorTest(false);
            renderer.setViewport(0, 0, viewportWidth, viewportHeight);
            renderer.autoClear = currentAutoClear;
        },

        /**
         * Dispose of all resources
         */
        dispose: function() {
            this.scene.traverse((child) => {
                if (child.geometry) child.geometry.dispose();
                if (child.material) {
                    if (child.material.map) child.material.map.dispose();
                    child.material.dispose();
                }
            });
        }
    };
}

/**
 * Create optional grid helper
 * @param {number} size - Grid size
 * @param {number} divisions - Number of divisions
 * @returns {THREE.GridHelper}
 */
export function createGridHelper(size = 10000, divisions = 100) {
    const grid = new THREE.GridHelper(size, divisions, 0x888888, 0x444444);
    grid.name = 'GridHelper';
    grid.material.opacity = 0.3;
    grid.material.transparent = true;
    return grid;
}

export { AXIS_CONFIG };