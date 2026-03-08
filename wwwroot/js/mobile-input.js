window.mobileEmulator = {
    handleTouchStart: (iframe, x, y) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;
        const touch = new Touch({ identifier: Date.now(), target: iframeWindow.document.body, clientX: x, clientY: y });
        const touchEvent = new TouchEvent('touchstart', { touches: [touch], changedTouches: [touch] });
        iframeWindow.document.body.dispatchEvent(touchEvent);
    },
    handleTouchEnd: (iframe) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;
        const touchEvent = new TouchEvent('touchend', { touches: [], changedTouches: [] });
        iframeWindow.document.body.dispatchEvent(touchEvent);
    },
    sendHome: (iframe) => {
        // Could simulate home button by navigating to a blank page or emitting an event
        console.log('Home button pressed (simulated)');
    },
    sendBack: (iframe) => {
        // Could simulate back button by history.back() inside iframe
        const iframeWindow = iframe.contentWindow;
        if (iframeWindow) iframeWindow.history.back();
    }
};