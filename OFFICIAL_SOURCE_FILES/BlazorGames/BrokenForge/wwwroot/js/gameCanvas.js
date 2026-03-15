export function drawWorld(world, width, height, playerX, playerY, canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const tileSize = 8; // pixels per tile
    canvas.width = width * tileSize;
    canvas.height = height * tileSize;

    for (let x = 0; x < width; x++) {
        for (let y = 0; y < height; y++) {
            const tile = world[x][y];
            ctx.fillStyle = getTileColor(tile);
            ctx.fillRect(x * tileSize, y * tileSize, tileSize, tileSize);
        }
    }

    // Draw player
    ctx.fillStyle = 'red';
    ctx.fillRect(playerX * tileSize, playerY * tileSize, tileSize, tileSize);
}

function getTileColor(tile) {
    switch (tile) {
        case 0: return '#000080'; // DeepWater
        case 1: return '#0000FF'; // Water
        case 2: return '#FFFF00'; // Sand
        case 3: return '#00FF00'; // Grass
        case 4: return '#008000'; // Forest
        case 5: return '#808080'; // Stone
        case 6: return '#A52A2A'; // Mountain
        default: return '#000000';
    }
}