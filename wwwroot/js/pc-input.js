window.pcEmulator = {
    sendKey: (iframe, key) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;
        iframe.focus();

        // Map special keys
        const keyMap = {
            'Backspace': 'Backspace',
            'Tab': 'Tab',
            'Enter': 'Enter',
            'Shift': 'Shift',
            'Control': 'Control',
            'Alt': 'Alt',
            'CapsLock': 'CapsLock',
            'Escape': 'Escape',
            ' ': ' ',
            'ArrowUp': 'ArrowUp',
            'ArrowDown': 'ArrowDown',
            'ArrowLeft': 'ArrowLeft',
            'ArrowRight': 'ArrowRight'
        };

        let eventKey = keyMap[key] || key;
        let eventCode = key.length === 1 ? 'Key' + key.toUpperCase() : key;

        // Dispatch keydown and keyup
        const downEvent = new KeyboardEvent('keydown', {
            key: eventKey,
            code: eventCode,
            bubbles: true,
            cancelable: true
        });
        iframeWindow.dispatchEvent(downEvent);
        iframeWindow.document.activeElement?.dispatchEvent(downEvent);

        setTimeout(() => {
            const upEvent = new KeyboardEvent('keyup', {
                key: eventKey,
                code: eventCode,
                bubbles: true,
                cancelable: true
            });
            iframeWindow.dispatchEvent(upEvent);
            iframeWindow.document.activeElement?.dispatchEvent(upEvent);
        }, 50);
    }
};