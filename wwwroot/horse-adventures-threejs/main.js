import * as THREE from 'three';

// Create scene
const scene = new THREE.Scene();
scene.background = new THREE.Color(0x050505);

// Create camera
const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
camera.position.z = 5;
camera.position.y = -1.46; // Adjusted from -1.62 to -1.46 to move cube down another 10%

// Create renderer
const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
document.body.appendChild(renderer.domElement);

// Load horse image texture - Updated filename to match your image
const textureLoader = new THREE.TextureLoader();
const horseTexture = textureLoader.load('./assets/horses.jpeg');

// Create cube with dimensions increased by 15%
const cubeGeometry = new THREE.BoxGeometry(2.318, 2.318, 2.318); // Increased from 2.016 to 2.318
const cubeMaterial = new THREE.MeshBasicMaterial({
  map: horseTexture,
  side: THREE.DoubleSide,
});
const cube = new THREE.Mesh(cubeGeometry, cubeMaterial);
scene.add(cube);

// Create particles
const particlesGeometry = new THREE.BufferGeometry();
const particlesCount = 5000;
const posArray = new Float32Array(particlesCount * 3);

for (let i = 0; i < particlesCount * 3; i++) {
  posArray[i] = (Math.random() - 0.5) * 10;
}

particlesGeometry.setAttribute('position', new THREE.BufferAttribute(posArray, 3));
const particlesMaterial = new THREE.PointsMaterial({
  size: 0.02,
  color: 0xffffff,
  transparent: true,
  opacity: 0.8,
});
const particlesMesh = new THREE.Points(particlesGeometry, particlesMaterial);
scene.add(particlesMesh);

// Create fragment planes
const fragmentPlanes = [];
const planeCount = 10;
const planeSize = 0.5;

for (let i = 0; i < planeCount; i++) {
  const smallPlaneGeometry = new THREE.PlaneGeometry(planeSize, planeSize);
  const smallPlaneMaterial = new THREE.MeshBasicMaterial({
    map: horseTexture,
    transparent: true,
    opacity: 0.7,
    side: THREE.DoubleSide,
  });
  
  const smallPlane = new THREE.Mesh(smallPlaneGeometry, smallPlaneMaterial);
  smallPlane.position.set(
    (Math.random() - 0.5) * 5,
    (Math.random() - 0.5) * 5,
    (Math.random() - 0.5) * 2
  );
  smallPlane.rotation.set(
    Math.random() * Math.PI,
    Math.random() * Math.PI,
    Math.random() * Math.PI
  );
  fragmentPlanes.push(smallPlane);
  scene.add(smallPlane);
}

// Add lights
const ambientLight = new THREE.AmbientLight(0xffffff, 0.5);
scene.add(ambientLight);

const pointLight = new THREE.PointLight(0xff0000, 1);
pointLight.position.set(2, 3, 4);
scene.add(pointLight);

// Create custom cursor with 3D effect
const cursor = document.createElement('div');
cursor.className = 'cursor3d';
document.body.appendChild(cursor);

// Mouse movement effect
let mouseX = 0;
let mouseY = 0;
let targetX = 0;
let targetY = 0;

document.addEventListener('mousemove', (event) => {
  mouseX = (event.clientX / window.innerWidth) * 2 - 1;
  mouseY = -(event.clientY / window.innerHeight) * 2 + 1;
  
  // Update cursor position
  cursor.style.left = event.clientX + 'px';
  cursor.style.top = event.clientY + 'px';
});

// Animation loop
const clock = new THREE.Clock();

function animate() {
  const elapsedTime = clock.getElapsedTime();
  
  // Rotate cube
  cube.rotation.y = elapsedTime * 0.3;
  cube.rotation.x = Math.sin(elapsedTime * 0.2) * 0.2;
  
  // Update particles
  particlesMesh.rotation.y = elapsedTime * 0.05;
  
  // Update fragment planes
  fragmentPlanes.forEach((plane, i) => {
    plane.rotation.x += 0.003 * (i % 3 + 1);
    plane.rotation.y += 0.002 * (i % 2 + 1);
    plane.position.y += Math.sin(elapsedTime * 0.5 + i) * 0.003;
  });
  
  // Mouse interaction with cube
  targetX = mouseX * 0.5;
  targetY = mouseY * 0.5;
  cube.rotation.y += 0.05 * (targetX - cube.rotation.y);
  cube.rotation.x += 0.05 * (targetY - cube.rotation.x);
  
  renderer.render(scene, camera);
  requestAnimationFrame(animate);
}

// Handle window resize
window.addEventListener('resize', () => {
  // Update camera
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  
  // Update renderer
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
});

// Start animation
animate();