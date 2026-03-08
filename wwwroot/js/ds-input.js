window.dsEmulator = {
    sendKeyEvent: (iframe, key, type) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;

        // Focus the iframe to ensure it receives events
        iframe.focus();

        const keyMap = {
            'ArrowUp': { key: 'ArrowUp', code: 'ArrowUp', keyCode: 38 },
            'ArrowDown': { key: 'ArrowDown', code: 'ArrowDown', keyCode: 40 },
            'ArrowLeft': { key: 'ArrowLeft', code: 'ArrowLeft', keyCode: 37 },
            'ArrowRight': { key: 'ArrowRight', code: 'ArrowRight', keyCode: 39 },
            'a': { key: 'a', code: 'KeyA', keyCode: 65 },
            'b': { key: 'b', code: 'KeyB', keyCode: 66 },
            'Start': { key: 'Enter', code: 'Enter', keyCode: 13 },
            'Select': { key: ' ', code: 'Space', keyCode: 32 }
        };

        const props = keyMap[key] || { key: key, code: key, keyCode: key.charCodeAt(0) };

        const dispatchEvent = (eventType) => {
            const event = new KeyboardEvent(eventType, {
                key: props.key,
                code: props.code,
                keyCode: props.keyCode,
                which: props.keyCode,
                bubbles: true,
                cancelable: true,
                composed: true
            });
            // Dispatch to the active element (or document) and the window
            const target = iframeWindow.document.activeElement || iframeWindow.document;
            target.dispatchEvent(event);
            iframeWindow.dispatchEvent(event);
        };

        if (type === 'down') {
            dispatchEvent('keydown');
            // Some games also listen for keypress
            if (props.key.length === 1 && props.key !== ' ') {
                const pressEvent = new KeyboardEvent('keypress', {
                    key: props.key,
                    code: props.code,
                    keyCode: props.keyCode,
                    which: props.keyCode,
                    bubbles: true,
                    cancelable: true
                });
                const target = iframeWindow.document.activeElement || iframeWindow.document;
                target.dispatchEvent(pressEvent);
                iframeWindow.dispatchEvent(pressEvent);
            }
        } else if (type === 'up') {
            dispatchEvent('keyup');
        }
    },

    // Legacy support (full press with timeout)
    sendKey: (iframe, key) => {
        dsEmulator.sendKeyEvent(iframe, key, 'down');
        setTimeout(() => {
            dsEmulator.sendKeyEvent(iframe, key, 'up');
        }, 100);
    }
};