window.connectEmulator = function(iframe) {
    const device = iframe.dataset.device;
    const iframeWindow = iframe.contentWindow;

    if (device === 'ds') {
        // Map DS button clicks to keyboard events inside iframe
        document.querySelectorAll('[data-key]').forEach(btn => {
            btn.addEventListener('click', () => {
                const key = btn.dataset.key;
                iframeWindow.dispatchEvent(new KeyboardEvent('keydown', { key }));
                setTimeout(() => {
                    iframeWindow.dispatchEvent(new KeyboardEvent('keyup', { key }));
                }, 100);
            });
        });
    } else if (device === 'mobile') {
        // Forward touch events to iframe
        const touchArea = document.querySelector('.touch-area');
        touchArea.addEventListener('touchstart', (e) => {
            const touch = e.touches[0];
            const target = iframeWindow.document.elementFromPoint(touch.clientX, touch.clientY);
            if (target) {
                target.dispatchEvent(new TouchEvent('touchstart', { touches: e.touches }));
            }
        });
    }
    // Computer devices use native keyboard; no mapping needed.
};