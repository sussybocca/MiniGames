window.chaos = {
    logRandomError: () => {
        const errors = [
            "Uncaught TypeError: Cannot read property 'x' of undefined",
            "Failed to load resource: net::ERR_CONNECTION_REFUSED",
            "Uncaught ReferenceError: game is not defined",
            "WebGL: CONTEXT_LOST_WEBGL"
        ];
        const error = errors[Math.floor(Math.random() * errors.length)];
        console.error(error);
    }
};

// Automatically spam console on page load (if you want)
setInterval(() => {
    if (Math.random() < 0.3) chaos.logRandomError();
}, 5000);