import * as THREE from 'three'

interface Sketch {
    sizes: {
        width: number;
        height: number;
    };
    scene: THREE.Scene;
}

interface Settings {
    [key: string]: any; // Adjust this according to the actual settings structure
}

class Camera {
    sketch: Sketch;
    settings: Settings;
    camera: THREE.PerspectiveCamera;

    constructor(sketch: Sketch, settings: Settings) {
        this.sketch = sketch;
        this.settings = {...settings};

        this.camera = new THREE.PerspectiveCamera(
            75,
            this.sketch.sizes.width / this.sketch.sizes.height,
            1,
            200
        );
        this.camera.position.x = 0;
        this.camera.position.y = 0;
        this.camera.position.z = 10;
        this.sketch.scene.add(this.camera);

        //return this.camera;
    }
}

export default Camera;
