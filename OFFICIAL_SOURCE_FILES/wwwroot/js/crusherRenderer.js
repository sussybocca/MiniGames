window.crusherRenderer = {
    draw: (canvas, objects, terrain, stats) => {
        const ctx = canvas.getContext('2d');
        const w = canvas.width, h = canvas.height;
        ctx.clearRect(0, 0, w, h);

        // ========== SKY ==========
        const skyGrad = ctx.createLinearGradient(0, 0, 0, h);
        skyGrad.addColorStop(0, '#0a1f3a');   // deep blue
        skyGrad.addColorStop(0.5, '#4a7db5');
        skyGrad.addColorStop(0.8, '#f0c45a'); // sunset
        ctx.fillStyle = skyGrad;
        ctx.fillRect(0, 0, w, h);

        // Sun
        ctx.shadowColor = '#ffd966';
        ctx.shadowBlur = 100;
        ctx.beginPath();
        ctx.arc(1000, 150, 80, 0, 2*Math.PI);
        ctx.fillStyle = '#ffaa33';
        ctx.fill();
        ctx.shadowBlur = 0;

        // Clouds (volumetric)
        ctx.fillStyle = 'rgba(255,255,255,0.3)';
        for (let i=0; i<4; i++) {
            ctx.beginPath();
            ctx.ellipse(200 + i*250, 60 + i*10, 80, 30, 0, 0, 2*Math.PI);
            ctx.fill();
        }

        // ========== TERRAIN ==========
        if (terrain && terrain.length >= 2) {
            // Ground with 3D-ish perspective lines
            const groundY = terrain[0].y;
            const gradGround = ctx.createLinearGradient(0, groundY, 0, h);
            gradGround.addColorStop(0, '#6b4f3c');
            gradGround.addColorStop(1, '#2a1e14');
            ctx.fillStyle = gradGround;
            ctx.beginPath();
            ctx.moveTo(0, groundY);
            ctx.lineTo(w, groundY);
            ctx.lineTo(w, h);
            ctx.lineTo(0, h);
            ctx.closePath();
            ctx.fill();

            // 3D ground lines (perspective)
            ctx.strokeStyle = '#a87b5a';
            ctx.lineWidth = 1;
            for (let y = groundY+10; y < h; y += 30) {
                ctx.beginPath();
                ctx.moveTo(0, y);
                ctx.lineTo(w, y + 20);
                ctx.strokeStyle = '#8b6b4a';
                ctx.stroke();
            }

            // Grass blades
            for (let x=0; x<w; x+=8) {
                let hgt = 5 + Math.sin(x*0.05)*3;
                ctx.beginPath();
                ctx.moveTo(x, groundY);
                ctx.lineTo(x-3, groundY - hgt);
                ctx.strokeStyle = '#4caf50';
                ctx.lineWidth = 1;
                ctx.stroke();
            }
        }

        // ========== OBJECTS with next‑gen shading ==========
        objects.sort((a,b) => a.position.y - b.position.y);

        objects.forEach(obj => {
            const x = obj.position.x;
            const y = obj.position.y;
            const rot = obj.rotation;
            const type = obj.type;
            const color = obj.color;
            const health = obj.health || 100;
            const damageFactor = 1 - (100 - health) / 200; // slight shrink when damaged
            const isDamaged = health < 70;

            ctx.save();
            ctx.translate(x, y);
            ctx.rotate(rot);

            // Dynamic lighting: light from top-left
            const lightDir = -0.3;
            const shadowOffset = 5;

            // Drop shadow (longer when damaged)
            ctx.shadowColor = 'rgba(0,0,0,0.7)';
            ctx.shadowBlur = isDamaged ? 20 : 15;
            ctx.shadowOffsetX = -shadowOffset;
            ctx.shadowOffsetY = shadowOffset;

            // Draw with extreme detail
            switch (type) {
                case 'car':
                    drawCar(ctx, color, damageFactor, isDamaged);
                    break;
                case 'tree':
                    drawTree(ctx, damageFactor, isDamaged);
                    break;
                case 'house':
                    drawHouse(ctx, color, damageFactor, isDamaged);
                    break;
                case 'boulder':
                    drawBoulder(ctx, obj.size || 15, damageFactor, isDamaged);
                    break;
                case 'wall':
                    drawWall(ctx, obj.width || 40, obj.height || 30, color, damageFactor, isDamaged);
                    break;
                default:
                    ctx.fillStyle = color;
                    ctx.fillRect(-20, -15, 40, 30);
            }

            // Add damage effects (cracks, smoke) if health low
            if (isDamaged) {
                ctx.shadowBlur = 0;
                ctx.globalAlpha = 0.3 + Math.random()*0.2;
                ctx.fillStyle = '#444';
                for (let i=0; i<3; i++) {
                    ctx.beginPath();
                    ctx.arc(Math.random()*20-10, Math.random()*20-10, 3, 0, 2*Math.PI);
                    ctx.fill();
                }
                ctx.globalAlpha = 1;
            }

            ctx.restore();
        });

        // ========== STATS ==========
        ctx.shadowBlur = 0;
        ctx.fillStyle = '#fff';
        ctx.font = 'bold 20px "Segoe UI", monospace';
        ctx.fillText(`FPS: ${stats.fps}`, 20, 40);
        ctx.fillText(`Objects: ${objects.length}`, 20, 70);
        ctx.fillText(`Wind: ${stats.wind} m/s`, 20, 100);
    }
};

// ========== ULTRA‑REALISTIC DRAWING FUNCTIONS ==========

function drawCar(ctx, color, scale, damaged) {
    // Body with metallic flake
    const grad = ctx.createLinearGradient(-30, -15, 30, 15);
    grad.addColorStop(0, damaged ? darken(color, 50) : color);
    grad.addColorStop(0.5, lighten(color, 40));
    grad.addColorStop(1, darken(color, 30));
    ctx.fillStyle = grad;
    ctx.fillRect(-30 * scale, -12 * scale, 60 * scale, 24 * scale);

    // Windshield
    ctx.fillStyle = '#334455';
    ctx.fillRect(-8 * scale, -25 * scale, 25 * scale, 12 * scale);

    // Windows
    ctx.fillStyle = '#88aacc';
    ctx.fillRect(-5 * scale, -22 * scale, 8 * scale, 5 * scale);
    ctx.fillRect(7 * scale, -22 * scale, 8 * scale, 5 * scale);

    // Wheels with tread
    ctx.fillStyle = '#111';
    ctx.shadowBlur = 20;
    ctx.beginPath();
    ctx.arc(-18 * scale, 12 * scale, 8 * scale, 0, 2*Math.PI);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(18 * scale, 12 * scale, 8 * scale, 0, 2*Math.PI);
    ctx.fill();

    // Wheel rims
    ctx.fillStyle = '#aaa';
    ctx.beginPath();
    ctx.arc(-18 * scale, 12 * scale, 4 * scale, 0, 2*Math.PI);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(18 * scale, 12 * scale, 4 * scale, 0, 2*Math.PI);
    ctx.fill();

    // Headlights (glow)
    ctx.shadowColor = '#ffffaa';
    ctx.shadowBlur = 20;
    ctx.fillStyle = '#ffffaa';
    ctx.beginPath();
    ctx.arc(25 * scale, -5 * scale, 5 * scale, 0, 2*Math.PI);
    ctx.fill();
    ctx.fillStyle = '#ffaa00';
    ctx.beginPath();
    ctx.arc(-25 * scale, -5 * scale, 5 * scale, 0, 2*Math.PI);
    ctx.fill();
}

function drawTree(ctx, scale, damaged) {
    // Trunk with bark
    ctx.fillStyle = damaged ? '#5a3a1a' : '#8b5a2b';
    ctx.fillRect(-6 * scale, 0, 12 * scale, 50 * scale);
    for (let i=0; i<5; i++) {
        ctx.fillStyle = '#4a2a0a';
        ctx.fillRect(-4 * scale, i*10 * scale, 8 * scale, 3 * scale);
    }

    // Leaves – realistic clumps
    ctx.fillStyle = damaged ? '#3a6b2a' : '#2e8b57';
    ctx.shadowColor = '#1a4d2a';
    ctx.shadowBlur = 30;
    ctx.beginPath();
    ctx.ellipse(0, -30 * scale, 30 * scale, 18 * scale, 0, 0, 2*Math.PI);
    ctx.fill();

    ctx.fillStyle = damaged ? '#4a7d3a' : '#3cb371';
    ctx.beginPath();
    ctx.ellipse(-15 * scale, -45 * scale, 22 * scale, 14 * scale, 0, 0, 2*Math.PI);
    ctx.fill();

    ctx.beginPath();
    ctx.ellipse(15 * scale, -45 * scale, 22 * scale, 14 * scale, 0, 0, 2*Math.PI);
    ctx.fill();

    // Fallen leaves if damaged
    if (damaged) {
        ctx.fillStyle = '#8b5a2b';
        for (let i=0; i<5; i++) {
            ctx.beginPath();
            ctx.ellipse(Math.random()*20-10, 20 + i*5, 2, 1, 0, 0, 2*Math.PI);
            ctx.fill();
        }
    }
}

function drawHouse(ctx, color, scale, damaged) {
    // Base
    ctx.fillStyle = damaged ? darken(color, 50) : color;
    ctx.fillRect(-25 * scale, -12 * scale, 50 * scale, 24 * scale);

    // Wood siding
    ctx.strokeStyle = '#543210';
    ctx.lineWidth = 2;
    for (let i=-10; i<20; i+=8) {
        ctx.beginPath();
        ctx.moveTo(-25 * scale, i * scale);
        ctx.lineTo(25 * scale, i * scale);
        ctx.stroke();
    }

    // Roof
    ctx.fillStyle = damaged ? '#5a0000' : '#8b0000';
    ctx.beginPath();
    ctx.moveTo(-30 * scale, -12 * scale);
    ctx.lineTo(0, -35 * scale);
    ctx.lineTo(30 * scale, -12 * scale);
    ctx.closePath();
    ctx.fill();

    // Roof shingles
    ctx.strokeStyle = '#5a0000';
    ctx.lineWidth = 1;
    for (let i=-8; i<8; i+=4) {
        ctx.beginPath();
        ctx.moveTo(-25 * scale + i*3, -20 * scale);
        ctx.lineTo(25 * scale - i*3, -20 * scale);
        ctx.stroke();
    }

    // Door
    ctx.fillStyle = '#654321';
    ctx.fillRect(-6 * scale, 0, 12 * scale, 15 * scale);

    // Windows
    ctx.fillStyle = '#add8e6';
    ctx.fillRect(-18 * scale, -6 * scale, 8 * scale, 8 * scale);
    ctx.fillRect(10 * scale, -6 * scale, 8 * scale, 8 * scale);

    // Damage cracks
    if (damaged) {
        ctx.strokeStyle = '#333';
        ctx.lineWidth = 3;
        ctx.beginPath();
        ctx.moveTo(-10 * scale, -5 * scale);
        ctx.lineTo(5 * scale, 5 * scale);
        ctx.stroke();
    }
}

function drawBoulder(ctx, size, scale, damaged) {
    ctx.fillStyle = damaged ? '#606060' : '#808080';
    ctx.shadowColor = '#404040';
    ctx.beginPath();
    ctx.ellipse(0, 0, size * scale, size * 0.6 * scale, 0, 0, 2*Math.PI);
    ctx.fill();

    // Highlights
    ctx.fillStyle = '#a0a0a0';
    ctx.beginPath();
    ctx.ellipse(-4 * scale, -4 * scale, size*0.2 * scale, size*0.15 * scale, 0, 0, 2*Math.PI);
    ctx.fill();

    // Cracks if damaged
    if (damaged) {
        ctx.strokeStyle = '#404040';
        ctx.lineWidth = 3;
        ctx.beginPath();
        ctx.moveTo(-5 * scale, 2 * scale);
        ctx.lineTo(5 * scale, -3 * scale);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(-2 * scale, 5 * scale);
        ctx.lineTo(3 * scale, -2 * scale);
        ctx.stroke();
    }
}

function drawWall(ctx, width, height, color, scale, damaged) {
    const w = width * scale;
    const h = height * scale;

    ctx.fillStyle = damaged ? darken(color, 50) : color;
    ctx.fillRect(-w/2, -h/2, w, h);

    // Brick pattern
    ctx.strokeStyle = '#555';
    ctx.lineWidth = 2;
    for (let y = -h/2 + 8; y < h/2; y += 16) {
        ctx.beginPath();
        ctx.moveTo(-w/2, y);
        ctx.lineTo(w/2, y);
        ctx.stroke();
    }
    for (let x = -w/2 + 12; x < w/2; x += 24) {
        ctx.beginPath();
        ctx.moveTo(x, -h/2);
        ctx.lineTo(x, h/2);
        ctx.stroke();
    }

    // Damage holes
    if (damaged) {
        ctx.fillStyle = '#333';
        ctx.beginPath();
        ctx.arc(-10 * scale, -5 * scale, 5 * scale, 0, 2*Math.PI);
        ctx.fill();
        ctx.beginPath();
        ctx.arc(8 * scale, 6 * scale, 4 * scale, 0, 2*Math.PI);
        ctx.fill();
    }
}

// Color helpers
function lighten(color, percent) {
    let r = parseInt(color.slice(1,3), 16);
    let g = parseInt(color.slice(3,5), 16);
    let b = parseInt(color.slice(5,7), 16);
    r = Math.min(255, r + percent);
    g = Math.min(255, g + percent);
    b = Math.min(255, b + percent);
    return `#${r.toString(16).padStart(2,'0')}${g.toString(16).padStart(2,'0')}${b.toString(16).padStart(2,'0')}`;
}

function darken(color, percent) {
    let r = parseInt(color.slice(1,3), 16);
    let g = parseInt(color.slice(3,5), 16);
    let b = parseInt(color.slice(5,7), 16);
    r = Math.max(0, r - percent);
    g = Math.max(0, g - percent);
    b = Math.max(0, b - percent);
    return `#${r.toString(16).padStart(2,'0')}${g.toString(16).padStart(2,'0')}${b.toString(16).padStart(2,'0')}`;
}