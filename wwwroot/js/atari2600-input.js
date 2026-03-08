window.atari2600Emulator = {
    sendKey: (iframe, key, action) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;

        iframe.focus();

        const keyMap = {
            'ArrowUp': { key: 'ArrowUp', code: 'ArrowUp', keyCode: 38 },
            'ArrowDown': { key: 'ArrowDown', code: 'ArrowDown', keyCode: 40 },
            'ArrowLeft': { key: 'ArrowLeft', code: 'ArrowLeft', keyCode: 37 },
            'ArrowRight': { key: 'ArrowRight', code: 'ArrowRight', keyCode: 39 },
            ' ': { key: ' ', code: 'Space', keyCode: 32 }
        };

        const props = keyMap[key] || { key: key, code: key, keyCode: key.charCodeAt(0) };

        const eventType = action === 'down' ? 'keydown' : 'keyup';
        const event = new KeyboardEvent(eventType, {
            key: props.key,
            code: props.code,
            keyCode: props.keyCode,
            which: props.keyCode,
            bubbles: true,
            cancelable: true,
            composed: true
        });

        const target = iframeWindow.document.activeElement || iframeWindow.document;
        target.dispatchEvent(event);
        iframeWindow.dispatchEvent(event);
    }
};