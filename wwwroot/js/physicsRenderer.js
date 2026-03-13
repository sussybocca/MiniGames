window.physicsRenderer = {
    draw: (canvas, bodies, fps) => {
        const ctx = canvas.getContext('2d');
        const w = canvas.width, h = canvas.height;
        ctx.clearRect(0, 0, w, h);

        // Background gradient
        const grad = ctx.createLinearGradient(0, 0, 0, h);
        grad.addColorStop(0, '#0a0f1e');
        grad.addColorStop(1, '#1a2a3a');
        ctx.fillStyle = grad;
        ctx.fillRect(0, 0, w, h);

        // Draw bodies (circles with glow if many)
        const count = bodies.length;
        ctx.shadowBlur = count > 5000 ? 0 : 5; // disable shadow for performance
        ctx.shadowColor = '#00ffff';

        for (let b of bodies) {
            ctx.save();
            ctx.translate(b.position.x, b.position.y);
            ctx.rotate(b.rotation);
            ctx.fillStyle = b.color;
            ctx.beginPath();
            ctx.arc(0, 0, b.radius, 0, 2*Math.PI);
            ctx.fill();
            // Add highlight
            ctx.fillStyle = '#ffffff';
            ctx.beginPath();
            ctx.arc(-2, -2, 2, 0, 2*Math.PI);
            ctx.fill();
            ctx.restore();
        }

        // FPS counter
        ctx.shadowBlur = 0;
        ctx.fillStyle = 'white';
        ctx.font = '16px monospace';
        ctx.fillText(`FPS: ${fps}`, 10, 30);
        ctx.fillText(`Bodies: ${count}`, 10, 55);
    }
};