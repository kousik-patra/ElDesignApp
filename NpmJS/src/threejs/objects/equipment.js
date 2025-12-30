import * as THREE from 'three';

export default function drawEquipmentMesh(tag, x, y, z, w, d, h, a, material, color, opacity, colortext) {
    try {
        var clr = JSON.parse(color);
        var setColor = new THREE.Color(clr[0] / 255, clr[1] / 255, clr[2] / 255);
        material.color = setColor;

        var clrT = JSON.parse(colortext);
        var textColor = new THREE.Color(clrT[0] / 255, clrT[1] / 255, clrT[2] / 255);

        var geometry = new THREE.BoxGeometry(w, d, h);  // l along x-axis, w along y axis, d along z axis
        var equipmentMesh = new THREE.Mesh(geometry, material);
        //
        var fontSize = 0.3 * Math.min(w, d, 1);
        if (tag.length * fontSize > Math.max(w, d)) {
            fontSize = Math.max(w, d) / tag.length;
        }
        const fontHeight = fontSize / 5; // Thickness to extrude text
        //if (fontHeight > 0.3 * Math.min(w, d, 1)) { fontHeight = 0.3 * Math.min(w, d, 1); fontSize = 4 * fontHeight; }
        //if (fontHeight > 0.3 * Math.min(w, d, 1)) { fontHeight = 0.3 * Math.min(w, d, 1); fontSize = 4 * fontHeight; }
        loader.load('https://unpkg.com/three@0.77.0/examples/fonts/helvetiker_regular.typeface.json', (font) => {

            // Create the text geometry
            const textGeometry = new TextGeometry(tag, {
                font: font,
                height: fontHeight,
                size: fontSize,
                color: '#5C4033'
                //, curveSegments: 32,
                //bevelEnabled: true,
                //bevelThickness: 0.5,
                //bevelSize: 0.5,
                //bevelSegments: 8,
            });
            // Geometries are attached to meshes so that they get rendered
            const textMesh = new THREE.Mesh(textGeometry, textMaterial);
            textMesh.geometry.center();
            // Update positioning of the text
            textMesh.position.set(0, 0, h / 2);
            textMesh.rotateOnAxis(new THREE.Vector3(0, 0, 1), Math.PI * (-1 + w < d ? 1 / 2 : 0));
            equipmentMesh.add(textMesh);
        });
        equipmentMesh.rotateOnAxis(new THREE.Vector3(0, 0, 1), a);

        equipmentMesh.position.set(x, y, z);
        equipmentMesh.updateMatrixWorld();

        equipmentMesh.Tag = tag;
        equipmentMesh.Type = "equipment";
        equipmentMesh.Clicked = false;
        equipmentMesh.OriginalColor = equipmentMesh.material.color;
        equipmentMesh.material.opacity = opacity;

        geometry.dispose();
        //
    } catch (e) {
        console.log(tag + e);
    }
    return equipmentMesh;
}

export {drawEquipmentMesh}