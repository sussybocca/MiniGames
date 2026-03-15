window.computerEmulator = {
    ctrlAltDel: (iframe) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;
        // Dispatch Ctrl+Alt+Del inside iframe (though most games ignore it)
        iframeWindow.dispatchEvent(new KeyboardEvent('keydown', { key: 'Control', ctrlKey: true }));
        iframeWindow.dispatchEvent(new KeyboardEvent('keydown', { key: 'Alt', altKey: true }));
        iframeWindow.dispatchEvent(new KeyboardEvent('keydown', { key: 'Delete' }));
        setTimeout(() => {
            iframeWindow.dispatchEvent(new KeyboardEvent('keyup', { key: 'Delete' }));
            iframeWindow.dispatchEvent(new KeyboardEvent('keyup', { key: 'Alt', altKey: true }));
            iframeWindow.dispatchEvent(new KeyboardEvent('keyup', { key: 'Control', ctrlKey: true }));
        }, 100);
    }
};