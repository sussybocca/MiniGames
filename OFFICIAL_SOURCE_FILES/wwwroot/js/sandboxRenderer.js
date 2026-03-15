window.drawSandboxFrame = (canvas, particles, currentElement, stats) => {
    const ctx = canvas.getContext('2d');
    const w = canvas.width, h = canvas.height;
    ctx.clearRect(0, 0, w, h);

    // Dynamic background based on average temperature / atmosphere
    const bgGrad = ctx.createLinearGradient(0, 0, 0, h);
    bgGrad.addColorStop(0, '#0a0f1e');
    bgGrad.addColorStop(0.5, '#1a2a3a');
    bgGrad.addColorStop(1, '#0a1a2a');
    ctx.fillStyle = bgGrad;
    ctx.fillRect(0, 0, w, h);

    // Draw subtle underwater caustics
    ctx.globalAlpha = 0.03;
    for (let i = 0; i < 8; i++) {
        ctx.fillStyle = '#aaccff';
        ctx.beginPath();
        ctx.arc(150 + i * 180, 400 + i * 20, 120, 0, Math.PI * 2);
        ctx.fill();
    }
    ctx.globalAlpha = 1;

    // Sort particles: gases on top, then liquids, then solids
    const sorted = [...particles].sort((a, b) => {
        const gasIds = [1,2,7,8,9,10,17,18,36,54,86]; // H, He, N, O, F, Ne, Cl, Ar, Kr, Xe, Rn
        const liquidIds = [101,103,109]; // Water, Oil, Acid (add more)
        const aIsGas = gasIds.includes(a.element) ? 2 : (liquidIds.includes(a.element) ? 1 : 0);
        const bIsGas = gasIds.includes(b.element) ? 2 : (liquidIds.includes(b.element) ? 1 : 0);
        return aIsGas - bIsGas; // higher = drawn later (on top)
    });

    sorted.forEach(p => {
        const x = p.position.x;
        const y = p.position.y;
        const element = p.element;
        const temp = p.temperature;
        const onFire = p.onFire;
        const health = p.health;
        const velocity = p.velocity || { x: 0, y: 0 };

        ctx.save();
        ctx.translate(x, y);
        ctx.globalAlpha = 0.95;

        // Dispatch to element-specific drawer
        drawElement(ctx, element, temp, onFire, health, velocity);

        ctx.restore();
    });

    // UI overlay
    ctx.fillStyle = 'white';
    ctx.font = '14px "Segoe UI", monospace';
    ctx.shadowColor = 'black';
    ctx.shadowBlur = 6;
    ctx.fillText(`Drawing: ${currentElement}`, 20, 30);
    if (stats) {
        ctx.fillText(`FPS: ${stats.fps}`, 20, 55);
        ctx.fillText(`Particles: ${stats.bodyCount}`, 20, 80);
        ctx.fillText(`Temp: ${stats.temp.toFixed(1)}°C`, 20, 105);
        ctx.fillText(`Wind: ${stats.wind.toFixed(1)} m/s`, 20, 130);
    }
    ctx.shadowBlur = 0;
};

function drawElement(ctx, element, temp, onFire, health, vel) {
    // Atomic numbers 1-118 for elements, plus custom IDs > 100 for compounds
    const isMetal = (element >= 3 && element <= 92) && 
        ![6,7,8,9,10,15,16,17,18,35,36,53,54,86].includes(element);
    
    if (isMetal) {
        drawMetal(ctx, element, temp);
        return;
    }

    // Non-metals and compounds
    switch (element) {
        // Noble gases
        case 2:  drawGas(ctx, '#d4b8b8', temp, 'He'); break;
        case 10: drawGas(ctx, '#ff6b6b', temp, 'Ne'); break;
        case 18: drawGas(ctx, '#80c0ff', temp, 'Ar'); break;
        case 36: drawGas(ctx, '#c0a0c0', temp, 'Kr'); break;
        case 54: drawGas(ctx, '#a0c0ff', temp, 'Xe'); break;
        case 86: drawGas(ctx, '#d0a0a0', temp, 'Rn'); break;

        // Other gases
        case 1:  drawGas(ctx, '#d9d9d9', temp, 'H'); break;
        case 7:  drawGas(ctx, '#80c0ff', temp, 'N'); break;
        case 8:  drawGas(ctx, '#ff8080', temp, 'O'); break;
        case 9:  drawGas(ctx, '#90e090', temp, 'F'); break;
        case 17: drawGas(ctx, '#c0ff40', temp, 'Cl'); break;

        // Solids
        case 6:  drawCarbon(ctx, temp); break;
        case 11: drawAlkaliMetal(ctx, '#c0c0c0', temp); break;
        case 12: drawMetal(ctx, 12, temp); break; // Mg
        case 13: drawMetal(ctx, 13, temp); break; // Al
        case 14: drawCrystalline(ctx, '#a0a0c0', temp); break; // Si
        case 15: drawWaxy(ctx, '#ffa0a0', temp, onFire); break; // P
        case 16: drawSulfur(ctx, temp); break;
        case 19: drawAlkaliMetal(ctx, '#a0a0c0', temp); break; // K
        case 20: drawMetal(ctx, 20, temp); break; // Ca
        case 26: drawMetal(ctx, 26, temp); break; // Fe
        case 29: drawMetal(ctx, 29, temp); break; // Cu
        case 30: drawMetal(ctx, 30, temp); break; // Zn
        case 47: drawMetal(ctx, 47, temp); break; // Ag
        case 50: drawMetal(ctx, 50, temp); break; // Sn
        case 79: drawMetal(ctx, 79, temp); break; // Au
        case 80: drawMetal(ctx, 80, temp); break; // Hg (liquid at room temp)
        case 82: drawMetal(ctx, 82, temp); break; // Pb

        // Compounds (IDs 101+)
        case 101: drawWater(ctx, temp); break;
        case 102: drawWood(ctx, temp, onFire, health); break;
        case 103: drawOil(ctx, temp, onFire); break;
        case 104: drawSteam(ctx, temp); break;
        case 105: drawIce(ctx, temp); break;
        case 106: drawSand(ctx, temp); break;
        case 107: drawCrystalline(ctx, '#f0f0f0', temp); break; // Salt
        case 108: drawGunpowder(ctx, temp, onFire); break;
        case 109: drawAcid(ctx, temp); break;
        case 110: drawLava(ctx, temp); break;
        case 111: drawGlass(ctx, temp); break;
        case 112: drawRubber(ctx, temp); break;
        case 113: drawPlastic(ctx, temp); break;

        default:
            // Fallback – unknown element
            ctx.fillStyle = '#888';
            ctx.beginPath();
            ctx.arc(0, 0, 5, 0, 2*Math.PI);
            ctx.fill();
    }
}

// ---------- Core drawing functions ----------
function drawGas(ctx, color, temp, symbol) {
    const size = 8 + (temp-20)/50;
    ctx.globalAlpha = 0.5 + Math.random()*0.2;
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, 2*Math.PI);
    ctx.fill();
    ctx.globalAlpha = 1;
    ctx.fillStyle = 'white';
    ctx.font = '8px monospace';
    ctx.fillText(symbol, -3, 3);
}

function drawMetal(ctx, element, temp) {
    const size = 5 + (temp-20)/100;
    let baseColor;
    switch (element) {
        case 12: baseColor = '#c0c0c0'; break; // Mg
        case 13: baseColor = '#d0d0e0'; break; // Al
        case 20: baseColor = '#d0d0d0'; break; // Ca
        case 26: baseColor = '#b0b0b0'; break; // Fe
        case 29: baseColor = '#b87333'; break; // Cu
        case 30: baseColor = '#c0c0c0'; break; // Zn
        case 47: baseColor = '#c0c0c0'; break; // Ag
        case 50: baseColor = '#b0b0b0'; break; // Sn
        case 79: baseColor = '#ffd700'; break; // Au
        case 80: baseColor = '#a0a0a0'; break; // Hg
        case 82: baseColor = '#808080'; break; // Pb
        default: baseColor = '#a0a0a0';
    }
    const grad = ctx.createRadialGradient(-2, -2, 1, 0, 0, size);
    grad.addColorStop(0, temp > 500 ? '#ffaa33' : baseColor);
    grad.addColorStop(0.7, temp > 500 ? '#aa5500' : '#505050');
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, 2*Math.PI);
    ctx.fill();
    // Highlight
    ctx.fillStyle = 'rgba(255,255,255,0.4)';
    ctx.beginPath();
    ctx.arc(-2, -2, 2, 0, 2*Math.PI);
    ctx.fill();
}

function drawAlkaliMetal(ctx, color, temp) {
    const size = 5;
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, 2*Math.PI);
    ctx.fill();
    if (Math.random() > 0.5) {
        ctx.fillStyle = 'rgba(255,255,255,0.8)';
        ctx.beginPath();
        ctx.arc(-1, -1, 2, 0, 2*Math.PI);
        ctx.fill();
    }
}

function drawCarbon(ctx, temp) {
    const size = 5;
    ctx.fillStyle = '#3a3a3a';
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, 2*Math.PI);
    ctx.fill();
    ctx.strokeStyle = '#888';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(-size, -size);
    ctx.lineTo(size, size);
    ctx.stroke();
}

function drawSulfur(ctx, temp) {
    const size = 5;
    ctx.fillStyle = '#f0e68c';
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, 2*Math.PI);
    ctx.fill();
    ctx.fillStyle = '#d4b84c';
    for (let i=0; i<3; i++) {
        ctx.beginPath();
        ctx.arc(Math.random()*2-1, Math.random()*2-1, 1, 0, 2*Math.PI);
        ctx.fill();
    }
}

function drawCrystalline(ctx, color, temp) {
    const size = 5;
    ctx.fillStyle = color;
    ctx.beginPath();
    for (let i=0; i<6; i++) {
        const angle = (i/6)*Math.PI*2;
        const x = Math.cos(angle)*size;
        const y = Math.sin(angle)*size;
        if (i===0) ctx.moveTo(x,y);
        else ctx.lineTo(x,y);
    }
    ctx.closePath();
    ctx.fill();
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 0.5;
    ctx.stroke();
}

function drawWaxy(ctx, color, temp, onFire) {
    if (onFire) { drawFire(ctx, 500); return; }
    const size = 5;
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.ellipse(0, 0, size, size*0.7, 0, 0, 2*Math.PI);
    ctx.fill();
}

function drawGunpowder(ctx, temp, onFire) {
    if (onFire) {
        ctx.shadowColor = 'orange';
        ctx.shadowBlur = 20;
        drawFire(ctx, 1000);
        ctx.shadowBlur = 0;
        return;
    }
    ctx.fillStyle = '#2a2a2a';
    ctx.beginPath();
    ctx.arc(0, 0, 4, 0, 2*Math.PI);
    ctx.fill();
    ctx.fillStyle = '#555';
    ctx.beginPath();
    ctx.arc(1, -1, 1, 0, 2*Math.PI);
    ctx.fill();
}

function drawAcid(ctx, temp) {
    const size = 6;
    ctx.globalAlpha = 0.7;
    ctx.fillStyle = '#b0e57c';
    ctx.beginPath();
    ctx.ellipse(0, 0, size, size*0.6, 0, 0, 2*Math.PI);
    ctx.fill();
    ctx.fillStyle = '#90d050';
    ctx.beginPath();
    ctx.arc(-2, -1, 2, 0, 2*Math.PI);
    ctx.fill();
    ctx.globalAlpha = 1;
}

function drawLava(ctx, temp) {
    const size = 6;
    const grad = ctx.createRadialGradient(-2, -2, 1, 0, 0, size);
    grad.addColorStop(0, '#ffaa33');
    grad.addColorStop(0.7, '#cc4400');
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, 2*Math.PI);
    ctx.fill();
    // Glow
    ctx.shadowColor = '#ff6600';
    ctx.shadowBlur = 15;
    ctx.fill();
    ctx.shadowBlur = 0;
}

function drawGlass(ctx, temp) {
    const size = 5;
    ctx.globalAlpha = 0.5;
    ctx.fillStyle = '#a0d0ff';
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, 2*Math.PI);
    ctx.fill();
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 1;
    ctx.stroke();
    ctx.globalAlpha = 1;
}

function drawRubber(ctx, temp) {
    const size = 5;
    ctx.fillStyle = '#2a2a2a';
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, 2*Math.PI);
    ctx.fill();
    ctx.fillStyle = '#555';
    ctx.beginPath();
    ctx.arc(-1, -1, 1, 0, 2*Math.PI);
    ctx.fill();
}

function drawPlastic(ctx, temp) {
    const size = 5;
    ctx.fillStyle = '#a0a0ff';
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, 2*Math.PI);
    ctx.fill();
    ctx.fillStyle = '#c0c0ff';
    ctx.beginPath();
    ctx.arc(-1, -1, 1, 0, 2*Math.PI);
    ctx.fill();
}

// ---------- Existing material drawing functions (unchanged) ----------
function drawSand(ctx, temp) {
    const size = 4 + (temp - 20) / 50;
    ctx.beginPath();
    for (let i = 0; i < 6; i++) {
        const angle = (i / 6) * Math.PI * 2 + Math.random() * 0.2;
        const rad = size * (0.8 + Math.random() * 0.4);
        const x = Math.cos(angle) * rad;
        const y = Math.sin(angle) * rad;
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
    }
    ctx.closePath();
    const grad = ctx.createRadialGradient(-2, -2, 1, 0, 0, size);
    grad.addColorStop(0, temp > 100 ? '#ffaa33' : '#d2b48c');
    grad.addColorStop(1, temp > 100 ? '#cc8800' : '#8b5a2b');
    ctx.fillStyle = grad;
    ctx.fill();
    ctx.fillStyle = 'rgba(255,255,200,0.3)';
    ctx.beginPath();
    ctx.arc(-1, -1, 1, 0, 2*Math.PI);
    ctx.fill();
}

function drawWater(ctx, temp) {
    const size = 5;
    ctx.beginPath();
    ctx.ellipse(0, 0, size, size * 0.8, 0, 0, Math.PI*2);
    const grad = ctx.createRadialGradient(-2, -2, 1, 0, 0, size);
    const blue = temp > 50 ? 100 + (temp-50)*2 : 150;
    grad.addColorStop(0, `rgba(100, 150, 255, 0.8)`);
    grad.addColorStop(0.7, `rgba(30, 80, 200, 0.6)`);
    ctx.fillStyle = grad;
    ctx.fill();
    ctx.fillStyle = 'rgba(255,255,255,0.4)';
    ctx.beginPath();
    ctx.ellipse(-2, -2, 2, 1.5, 0, 0, Math.PI*2);
    ctx.fill();
}

function drawWood(ctx, temp, onFire, health) {
    if (onFire) {
        drawFire(ctx, 500);
        return;
    }
    const size = 5;
    ctx.fillStyle = '#8b5a2b';
    ctx.fillRect(-size, -size, size*2, size*2);
    ctx.strokeStyle = '#5a3a1a';
    ctx.lineWidth = 1;
    for (let i = -size; i < size; i+=3) {
        ctx.beginPath();
        ctx.moveTo(i, -size);
        ctx.lineTo(i + 2, size);
        ctx.stroke();
    }
    ctx.fillStyle = '#4a2a0a';
    ctx.beginPath();
    ctx.arc(0, 0, 2, 0, 2*Math.PI);
    ctx.fill();
}

function drawFire(ctx, temp) {
    const size = 6 + Math.random()*2;
    const grad = ctx.createRadialGradient(-2, -2, 1, 0, 0, size);
    grad.addColorStop(0, '#ffffa0');
    grad.addColorStop(0.4, '#ffaa00');
    grad.addColorStop(1, '#ff4500');
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.moveTo(0, -size);
    ctx.quadraticCurveTo(size, -size/2, size/2, 0);
    ctx.quadraticCurveTo(size, size/2, 0, size/2);
    ctx.quadraticCurveTo(-size, size/2, -size/2, 0);
    ctx.quadraticCurveTo(-size, -size/2, 0, -size);
    ctx.fill();
    ctx.shadowColor = '#ff6600';
    ctx.shadowBlur = 15;
    ctx.fill();
    ctx.shadowBlur = 0;
}

function drawOil(ctx, temp, onFire) {
    if (onFire) {
        drawFire(ctx, 500);
        return;
    }
    const size = 5;
    ctx.fillStyle = '#2a2a2a';
    ctx.beginPath();
    ctx.ellipse(0, 0, size, size*0.8, 0, 0, Math.PI*2);
    ctx.fill();
    ctx.fillStyle = 'rgba(200,200,200,0.3)';
    ctx.beginPath();
    ctx.ellipse(-2, -2, 2, 1.5, 0, 0, Math.PI*2);
    ctx.fill();
    ctx.globalAlpha = 0.2;
    ctx.fillStyle = '#88aaff';
    ctx.beginPath();
    ctx.ellipse(0, -1, 2, 1, 0, 0, Math.PI*2);
    ctx.fill();
    ctx.globalAlpha = 1;
}

function drawIce(ctx, temp) {
    const size = 5;
    ctx.fillStyle = 'rgba(200,240,255,0.7)';
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 1;
    ctx.beginPath();
    for (let i = 0; i < 6; i++) {
        const angle = (i / 6) * Math.PI*2;
        const x = Math.cos(angle) * size;
        const y = Math.sin(angle) * size;
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
    }
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    ctx.fillStyle = 'rgba(255,255,255,0.3)';
    ctx.beginPath();
    ctx.arc(-2, -2, 2, 0, 2*Math.PI);
    ctx.fill();
}

function drawSteam(ctx, temp) {
    const size = 8 + Math.random()*4;
    ctx.globalAlpha = 0.4 + Math.random()*0.2;
    ctx.fillStyle = 'rgba(220,240,255,0.5)';
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, 2*Math.PI);
    ctx.fill();
    ctx.globalAlpha = 1;
}