
import * as THREE from 'three';
import { TextGeometry } from 'three/examples/jsm/geometries/TextGeometry.js';
import fontData from '../fonts/helvetiker_regular.typeface.json';
import {FontLoader} from "three/examples/jsm/loaders/FontLoader";

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
        const fontHeight = fontSize / 5; // Thickness to extrude text
        const loader = new FontLoader();
         loader.load('https://unpkg.com/three@0.77.0/examples/fonts/helvetiker_regular.typeface.json', (font) => {

             // Create the text geometry
             const textGeometry = new TextGeometry(tag, {
                 font: font,
                 depth: fontHeight,
                 size: fontSize,
                 color: '#0000ff',
                 //, curveSegments: 32,
                 bevelEnabled: false,
                 //bevelThickness: 0.5,
                 //bevelSize: 0.5,
                 //bevelSegments: 8,
             });
             // Geometries are attached to meshes so that they get rendered
             const textMesh = new THREE.Mesh(textGeometry, textMaterial);
             textMesh.geometry.center();
             // Update positioning of the text
             textMesh.position.set( .5,  .5, 0);
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

export { drawRefPointMesh };