# Horse Adventures - Experiencia 3D Interactiva con Three.js


![Demostración de Horse Adventures](./assets/horseAdventures.gif)

[**Ver Demo en Vivo**](URL_DE_TU_DEMO_AQUI)
*_(Nota: Cuando despliegues el proyecto, podrás poner el enlace a la versión funcional aquí)_*

Este proyecto es una demostración técnica y artística que utiliza **Three.js** para construir una página de aterrizaje inmersiva. El objetivo es explorar y combinar diversas técnicas de animación, interacción y efectos visuales para crear una experiencia de usuario memorable.

## ✨ Desglose de Características y Técnicas Implementadas

### Escena y Renderizado 3D
-   **Core:** Se establece una arquitectura 3D básica con `Scene`, `PerspectiveCamera` y `WebGLRenderer`, optimizada para el rendimiento con `setPixelRatio` para adaptarse a pantallas de alta densidad.
-   **Atmósfera:** El fondo `scene.background` se establece en un color oscuro y sólido para evocar una sensación de espacio profundo, haciendo que los elementos iluminados resalten.

### Objeto Central: Cubo Interactivo
-   **Geometría y Material:** Se utiliza una `BoxGeometry` con un `MeshBasicMaterial` que mapea una textura de imagen. `MeshBasicMaterial` no se ve afectado por las luces, lo que garantiza que la textura sea siempre visible y clara.
-   **Animación Compuesta:**
    -   **Rotación base:** Una rotación constante en el eje Y (`elapsedTime * 0.3`) proporciona un movimiento perpetuo.
    -   **Efecto de Flotación (Hovering):** Se aplica una función sinusoidal (`Math.sin`) a la rotación en el eje X, lo que produce un suave vaivén vertical que simula la flotación en el espacio.
-   **Interactividad (Mouse Tracking):**
    -   Se normalizan las coordenadas del ratón (de -1 a 1) para mapearlas al espacio de la escena.
    -   La rotación del cubo no sigue directamente al cursor, sino que se interpola suavemente hacia la posición del objetivo (`targetX`, `targetY`). Esto crea un efecto de seguimiento orgánico y fluido, en lugar de un movimiento brusco.

### Efectos Visuales y Ambientación
-   **Sistema de Partículas (Particle System):**
    -   **Impacto Visual:** Simula un campo de estrellas o polvo cósmico, añadiendo una enorme profundidad y escala a la escena.
    -   **Técnica:** Se genera una `BufferGeometry` con miles de vértices posicionados aleatoriamente. Se renderizan como puntos (`THREE.Points`) utilizando un `PointsMaterial` con transparencia para un efecto más etéreo.
-   **Efecto de Fragmentación (Deconstructed Effect):**
    -   **Impacto Visual:** Crea una sensación de deconstrucción o de un reflejo roto en el espacio, añadiendo complejidad y dinamismo.
    -   **Técnica:** Múltiples `PlaneGeometry` con la misma textura del cubo se instancian en posiciones y rotaciones aleatorias. Cada fragmento tiene su propia animación de rotación y traslación sinusoidal, lo que resulta en un movimiento caótico pero armonioso.

### Interfaz y Experiencia de Usuario (UI/UX)
-   **Cursor Personalizado con CSS:**
    -   **Impacto Visual:** Mejora la inmersión al reemplazar el cursor del sistema operativo por uno que encaja con la estética del sitio.
    -   **Técnica:** Se utiliza `mix-blend-mode: difference`, que invierte el color de los elementos que tiene debajo. Esto asegura que el cursor sea siempre visible, sin importar si está sobre un fondo claro u oscuro.
-   **Animaciones CSS de la Interfaz:**
    -   **Título Flotante:** La animación `@keyframes float` no solo traslada el texto, sino que utiliza `rotate3d`. Esta función aplica una rotación en un vector 3D (1, 1, 1), dando la ilusión de que el texto está realmente flotando y girando en un espacio tridimensional.

## 🛠️ Stack Tecnológico

-   **Librería 3D:** [Three.js](https://threejs.org/)
-   **Entorno de Desarrollo:** [Vite](https://vitejs.dev/) (con HMR para desarrollo rápido)
-   **Lenguajes:** HTML5, CSS3, JavaScript (ES6+)
-   **Gestor de Paquetes:** [NPM](https://www.npmjs.com/)

## 🚀 Ejecución Local

1.  Clona el repositorio: `git clone https://github.com/tu-usuario/tu-repositorio.git`
2.  Entra al directorio: `cd tu-repositorio`
3.  Instala dependencias: `npm install`
4.  Inicia el servidor: `npm run dev`
5.  Abre `http://localhost:5173` en tu navegador.
