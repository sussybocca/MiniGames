window.dsEmulator = {
    sendKey: (iframe, key) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;
        
        // Map DS buttons to keyboard events
        const keyMap = {
            'ArrowUp': 'ArrowUp',
            'ArrowDown': 'ArrowDown',
            'ArrowLeft': 'ArrowLeft',
            'ArrowRight': 'ArrowRight',
            'a': 'a',
            'b': 'b',
            'Start': 'Enter',
            'Select': ' '
        };
        const mappedKey = keyMap[key] || key;
        
        iframeWindow.dispatchEvent(new KeyboardEvent('keydown', { key: mappedKey }));
        setTimeout(() => {
            iframeWindow.dispatchEvent(new KeyboardEvent('keyup', { key: mappedKey }));
        }, 100);
    }
};