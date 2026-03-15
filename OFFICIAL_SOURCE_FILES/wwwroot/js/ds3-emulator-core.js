// ds3-emulator-core.js
// This file should be loaded after jsGB library
window.ds3EmulatorCore = {
    emulator: null,
    canvas: null,
    start: (canvas, romBase64) => {
        if (!window.JSGB) {
            console.error('JSGB library not loaded');
            return;
        }
        ds3EmulatorCore.canvas = canvas;
        // Convert base64 to array buffer
        const binary = atob(romBase64);
        const len = binary.length;
        const bytes = new Uint8Array(len);
        for (let i = 0; i < len; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        // Initialize emulator
        ds3EmulatorCore.emulator = new JSGB(canvas, { rom: bytes });
        ds3EmulatorCore.emulator.start();
    },
    stop: () => {
        if (ds3EmulatorCore.emulator) {
            ds3EmulatorCore.emulator.stop();
            ds3EmulatorCore.emulator = null;
        }
    },
    input: (key, action) => {
        if (!ds3EmulatorCore.emulator) return;
        const buttonMap = {
            'up': 0,    // UP
            'down': 1,  // DOWN
            'left': 2,  // LEFT
            'right': 3, // RIGHT
            'a': 4,     // A
            'b': 5,     // B
            'select': 6,// SELECT
            'start': 7  // START
        };
        const btn = buttonMap[key];
        if (btn !== undefined) {
            if (action === 'down') {
                ds3EmulatorCore.emulator.press(btn);
            } else {
                ds3EmulatorCore.emulator.release(btn);
            }
        }
    }
};