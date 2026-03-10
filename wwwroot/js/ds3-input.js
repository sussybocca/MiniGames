window.ds3Emulator = {
    sendKey: (iframe, key, action) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;
        iframe.focus();
        const keyMap = {
            'ArrowUp': { key: 'ArrowUp', code: 'ArrowUp', keyCode: 38 },
            'ArrowDown': { key: 'ArrowDown', code: 'ArrowDown', keyCode: 40 },
            'ArrowLeft': { key: 'ArrowLeft', code: 'ArrowLeft', keyCode: 37 },
            'ArrowRight': { key: 'ArrowRight', code: 'ArrowRight', keyCode: 39 },
            'a': { key: 'a', code: 'KeyA', keyCode: 65 },
            'b': { key: 'b', code: 'KeyB', keyCode: 66 },
            'x': { key: 'x', code: 'KeyX', keyCode: 88 },
            'y': { key: 'y', code: 'KeyY', keyCode: 89 },
            'l': { key: 'l', code: 'KeyL', keyCode: 76 },
            'r': { key: 'r', code: 'KeyR', keyCode: 82 },
            'Enter': { key: 'Enter', code: 'Enter', keyCode: 13 },
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
        iframeWindow.dispatchEvent(event);
        iframeWindow.document.activeElement?.dispatchEvent(event);
    },
    sendTouch: (iframe, x, y, type) => {
        const iframeWindow = iframe.contentWindow;
        if (!iframeWindow) return;
        iframe.focus();
        const touch = new Touch({
            identifier: Date.now(),
            target: iframeWindow.document.body,
            clientX: x,
            clientY: y,
            radiusX: 2.5,
            radiusY: 2.5,
            rotationAngle: 0,
            force: 1
        });
        const touchEvent = new TouchEvent('touch' + type, {
            touches: [touch],
            targetTouches: [touch],
            changedTouches: [touch],
            bubbles: true,
            cancelable: true
        });
        iframeWindow.dispatchEvent(touchEvent);
        iframeWindow.document.body.dispatchEvent(touchEvent);
    }
};