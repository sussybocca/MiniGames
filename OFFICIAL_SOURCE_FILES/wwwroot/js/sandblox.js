import * as THREE from '../three/build/three.module.js';

let scene, camera, renderer, raycaster, worldGroup;
let keys = {}, mouseDeltaX = 0, mouseDeltaY = 0;
let dotNetRef;
let canvas;
let selectedSlot = 0;
let debug = false;
let clock = new THREE.Clock();
let player = { x: 0, y: 80, z: 0, yaw: 0, pitch: 0 };
const moveSpeed = 5;
const mouseSensitivity = 0.002;

// Block color map
const blockColors = {
    1: 0x7ec850, // grass
    2: 0x8b5a2b, // dirt
    3: 0x808080, // stone
    4: 0x696969, // cobblestone
    5: 0x000000, // bedrock
    6: 0x3366cc, // water
    7: 0xf4e542, // sand
    8: 0x808080, // gravel
    9: 0x8b4513, // wood
    10: 0x228b22, // leaves
    11: 0x99ccff, // glass
    12: 0x993333, // brick
    13: 0xdeb887, // planks
    14: 0x333333, // coal ore
    15: 0xcd853f, // iron ore
    16: 0xdaa520, // gold ore
    17: 0x00ffff, // diamond ore
    18: 0xc19a6b, // crafting table
    19: 0xffaa00  // torch
};

export async function initialize(canvasElement, dotNetObjRef) {
    dotNetRef = dotNetObjRef;
    canvas = canvasElement;

    scene = new THREE.Scene();
    scene.background = new THREE.Color(0x111122);
    scene.fog = new THREE.Fog(0x111122, 50, 200);

    camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
    camera.position.copy(player);

    renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;

    // Lighting
    const ambientLight = new THREE.AmbientLight(0x404060);
    scene.add(ambientLight);

    const sunLight = new THREE.DirectionalLight(0xffeedd, 1);
    sunLight.position.set(50, 100, 50);
    sunLight.castShadow = true;
    sunLight.shadow.mapSize.width = 2048;
    sunLight.shadow.mapSize.height = 2048;
    sunLight.shadow.camera.near = 0.5;
    sunLight.shadow.camera.far = 200;
    sunLight.shadow.camera.left = -100;
    sunLight.shadow.camera.right = 100;
    sunLight.shadow.camera.top = 100;
    sunLight.shadow.camera.bottom = -100;
    scene.add(sunLight);

    // Sky gradient (using a large sphere)
    const skyGeometry = new THREE.SphereGeometry(500, 60, 40);
    const skyMaterial = new THREE.ShaderMaterial({
        uniforms: {},
        vertexShader: `
            varying vec3 vWorldPosition;
            void main() {
                vec4 worldPosition = modelMatrix * vec4(position, 1.0);
                vWorldPosition = worldPosition.xyz;
                gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
            }
        `,
        fragmentShader: `
            uniform vec3 topColor;
            uniform vec3 bottomColor;
            varying vec3 vWorldPosition;
            void main() {
                float h = normalize(vWorldPosition).y;
                gl_FragColor = vec4(mix(bottomColor, topColor, max(h, 0.0)), 1.0);
            }
        `,
        side: THREE.BackSide
    });
    skyMaterial.uniforms.topColor = { value: new THREE.Color(0x0077ff) };
    skyMaterial.uniforms.bottomColor = { value: new THREE.Color(0xffffff) };
    const sky = new THREE.Mesh(skyGeometry, skyMaterial);
    scene.add(sky);

    worldGroup = new THREE.Group();
    scene.add(worldGroup);

    raycaster = new THREE.Raycaster();
    raycaster.far = 6; // reach

    window.addEventListener('resize', onWindowResize, false);
    canvas.requestPointerLock();
}

function onWindowResize() {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
}

export function startGameLoop() {
    function animate() {
        requestAnimationFrame(animate);
        const delta = clock.getDelta();
        updatePlayer(delta);
        updateSun();
        renderer.render(scene, camera);
    }
    animate();
}

function updatePlayer(delta) {
    // Mouse look
    if (mouseDeltaX !== 0 || mouseDeltaY !== 0) {
        player.yaw -= mouseDeltaX * mouseSensitivity;
        player.pitch -= mouseDeltaY * mouseSensitivity;
        player.pitch = Math.max(-Math.PI/2, Math.min(Math.PI/2, player.pitch));
        camera.rotation.set(player.pitch, player.yaw, 0, 'YXZ');
        mouseDeltaX = 0;
        mouseDeltaY = 0;
    }

    // Movement
    const forward = new THREE.Vector3(0, 0, -1).applyEuler(new THREE.Euler(0, player.yaw, 0));
    const right = new THREE.Vector3(1, 0, 0).applyEuler(new THREE.Euler(0, player.yaw, 0));
    let move = new THREE.Vector3(0, 0, 0);
    if (keys['w']) move.add(forward);
    if (keys['s']) move.sub(forward);
    if (keys['a']) move.sub(right);
    if (keys['d']) move.add(right);
    if (keys['Shift']) move.y -= 1; // sneak/descend
    if (keys[' '] && !keys['Shift']) move.y += 1; // jump/ascend

    if (move.lengthSq() > 0) {
        move.normalize().multiplyScalar(moveSpeed * delta);
        player.x += move.x;
        player.y += move.y;
        player.z += move.z;
    }

    camera.position.set(player.x, player.y, player.z);
    dotNetRef.invokeMethodAsync('UpdatePlayerPosition', player.x, player.y, player.z);
}

function updateSun() {
    // Simple day/night rotation
    const time = Date.now() * 0.0001;
    const sun = scene.children.find(c => c.type === 'DirectionalLight');
    if (sun) {
        sun.position.x = Math.sin(time) * 100;
        sun.position.y = Math.cos(time) * 100 + 50;
        sun.position.z = 50;
    }
}

export function handleKeyDown(key, shiftKey, ctrlKey) {
    keys[key] = true;
    keys['Shift'] = shiftKey;
    keys['Ctrl'] = ctrlKey;
}

export function handleKeyUp(key) {
    keys[key] = false;
    if (key === 'Shift') keys['Shift'] = false;
    if (key === 'Ctrl') keys['Ctrl'] = false;
}

export function handleMouseMove(deltaX, deltaY) {
    mouseDeltaX += deltaX;
    mouseDeltaY += deltaY;
}

export function handleClick(button) {
    if (button === 0) { // left click
        castRay(true);
    } else if (button === 2) { // right click
        castRay(false);
    }
}

function castRay(breakMode) {
    raycaster.setFromCamera(new THREE.Vector2(0, 0), camera);
    const intersects = raycaster.intersectObjects(worldGroup.children, true);
    if (intersects.length > 0) {
        const hit = intersects[0];
        const blockPos = hit.object.userData.blockPos;
        const face = hit.face.normal;
        if (breakMode) {
            dotNetRef.invokeMethodAsync('OnBlockHit', blockPos.x, blockPos.y, blockPos.z, face, hit.point.x, hit.point.y, hit.point.z);
        } else {
            const placePos = {
                x: blockPos.x + face.x,
                y: blockPos.y + face.y,
                z: blockPos.z + face.z
            };
            dotNetRef.invokeMethodAsync('OnBlockPlace', blockPos.x, blockPos.y, blockPos.z, face);
        }
    }
}

export function updateChunk(chunkKey, blocks) {
    // Remove old chunk if exists
    const old = worldGroup.getObjectByName(chunkKey);
    if (old) worldGroup.remove(old);

    const chunkGroup = new THREE.Group();
    chunkGroup.name = chunkKey;

    for (const block of blocks) {
        if (block.type === 0) continue; // air

        // Simple box geometry for blocks
        const geometry = new THREE.BoxGeometry(1, 1, 1);
        const material = new THREE.MeshStandardMaterial({ color: blockColors[block.type] || 0xff00ff });
        const cube = new THREE.Mesh(geometry, material);
        cube.castShadow = true;
        cube.receiveShadow = true;
        cube.position.set(block.x + 0.5, block.y + 0.5, block.z + 0.5);
        cube.userData.blockPos = { x: block.x, y: block.y, z: block.z };
        chunkGroup.add(cube);
    }
    worldGroup.add(chunkGroup);
}

export function removeChunk(chunkKey) {
    const chunk = worldGroup.getObjectByName(chunkKey);
    if (chunk) worldGroup.remove(chunk);
}

export function setSelectedSlot(slot) {
    selectedSlot = slot;
}

export function toggleDebug() {
    debug = !debug;
    // Show chunk boundaries, etc.
}