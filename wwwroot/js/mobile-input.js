window.mobileEmulator = {
    handleTouchStart: (iframe, x, y, identifier) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;

        // Focus the iframe to ensure it receives events
        iframe.focus();

        const touch = new Touch({
            identifier: identifier || Date.now(),
            target: iframeWindow.document.body,
            clientX: x,
            clientY: y,
            radiusX: 2.5,
            radiusY: 2.5,
            rotationAngle: 0,
            force: 1
        });

        const touchEvent = new TouchEvent('touchstart', {
            touches: [touch],
            targetTouches: [touch],
            changedTouches: [touch],
            bubbles: true,
            cancelable: true
        });

        // Dispatch to the active element or document
        const target = iframeWindow.document.activeElement || iframeWindow.document.body;
        target.dispatchEvent(touchEvent);
        iframeWindow.dispatchEvent(touchEvent);
    },

    handleTouchMove: (iframe, x, y, identifier) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;

        const touch = new Touch({
            identifier: identifier || Date.now(),
            target: iframeWindow.document.body,
            clientX: x,
            clientY: y,
            radiusX: 2.5,
            radiusY: 2.5,
            rotationAngle: 0,
            force: 1
        });

        const touchEvent = new TouchEvent('touchmove', {
            touches: [touch],
            targetTouches: [touch],
            changedTouches: [touch],
            bubbles: true,
            cancelable: true
        });

        const target = iframeWindow.document.activeElement || iframeWindow.document.body;
        target.dispatchEvent(touchEvent);
        iframeWindow.dispatchEvent(touchEvent);
    },

    handleTouchEnd: (iframe) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;

        const touchEvent = new TouchEvent('touchend', {
            touches: [],
            targetTouches: [],
            changedTouches: [],
            bubbles: true,
            cancelable: true
        });

        const target = iframeWindow.document.activeElement || iframeWindow.document.body;
        target.dispatchEvent(touchEvent);
        iframeWindow.dispatchEvent(touchEvent);
    },

    sendHome: (iframe) => {
        // Simulate home button: could navigate to a blank page or emit a custom event
        console.log('Home button pressed (simulated)');
        // Optionally, set iframe.src to 'about:blank' or something
        // iframe.src = 'about:blank';
    },

    sendBack: (iframe) => {
        const iframeWindow = iframe.contentWindow;
        if (iframeWindow) {
            iframeWindow.history.back();
        }
    }
};