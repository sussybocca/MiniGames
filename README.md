MiniGames – The Ultimate Blazor WebAssembly Gaming Hub

Welcome to MiniGames – the most ambitious, feature‑packed, and immersive gaming platform ever built with Blazor WebAssembly. This project is a testament to what's possible when you refuse to give up, pushing the boundaries of browser‑based emulation, interactive 3D, and cutting‑edge UI/UX.

⚠️ Important: This repository contains the complete source code for the MiniGames platform. It is provided as a learning resource and foundation for your own projects.

✨ What Makes MiniGames Special?

MiniGames is not just another collection of emulators. It's a fully integrated, cyberpunk‑themed gaming portal that brings together:

· High‑fidelity emulation – GameBoy, GBA, PS1, and Flash games running directly in your browser via WebAssembly.
· Immersive 3D environments – Three.js‑powered backgrounds that make every page feel alive.
· Multimodal control – Hand tracking, eye gaze, voice commands, Bluetooth headphone buttons, and gamepad support – all working together.
· Procedural sandboxes – Real‑time physics, destruction, and elemental simulations (Elemental Sandbox, Crusher Sandbox, Physics Sandbox).
· Live code modification – The Universal Modder lets you tweak any game with Lua scripts.
· Chaos Mode – A site‑wide toggle that simulates glitches, error messages, and visual mayhem for a truly retro‑futuristic feel.

🚀 Current Features (What Works Right Now)

Feature Status Details
GameBoy 3 Emulator ✅ Fully functional Powered by binjgb. Upload your own .gb or .gbc ROMs. Includes file management, save states, and controller support.
Universal Frontend Emulator (UFE) ✅ Works with uploads Flash (via Ruffle) and GBA/PS1 (via EmulatorJS). Upload your own ROMs – no server‑side hosting required.
Three.js Example Gallery ✅ Live Hundreds of Three.js demos, filterable and embeddable.
Elemental Sandbox ✅ Interactive Physics playground with sand, water, fire, and more.
Crusher Sandbox ✅ Destruction physics Crush cars, trees, houses with realistic damage.
Physics Sandbox ✅ Stress test Adjust gravity, friction, wind, spawn thousands of bodies.
Hand Tracking ✅ MediaPipe Navigate menus with your index finger; pinch to click.
Bluetooth Headphone Controls ✅ Media keys Volume up/down moves focus, play/pause clicks, next/previous scrolls.
Voice Control ✅ Web Speech API Say "home", "about", "gameboy", "three", etc. to navigate.
Eye Tracking ✅ WebGazer.js Highlight menu items by looking at them.
Gamepad Support ✅ USB/Bluetooth controllers D‑pad moves focus, A button clicks.
Chaos Mode ✅ Global toggle Random glitches, console errors, and visual distortions.

🔮 Planned Features (What's Coming)

· Universal Modder enhancements – More Lua APIs, real‑time game variable editing.
· Multiplayer sandboxes – Physics collaboration and destruction with friends.
· More emulator cores – SNES, N64, MAME via EmulatorJS.
· Cloud saves – IndexedDB sync across devices.
· Community game uploads – Let users share their own ROMs (with moderation).
· Advanced accessibility – Improved screen reader support, keyboard‑only navigation.

🛠️ Tech Stack

· Framework: Blazor WebAssembly (.NET 8)
· Emulation: binjgb (GameBoy), Ruffle (Flash), EmulatorJS (GBA/PS1)
· 3D Graphics: Three.js (via static examples and custom integration)
· Hand Tracking: MediaPipe
· Eye Tracking: WebGazer.js
· Voice Recognition: Web Speech API
· Gamepad: Toolbelt.Blazor.Gamepad
· Styling: Pure CSS with cyberpunk neon theme
· Hosting: Cloudflare Tunnel / Automatic BAT file with Task Scheduler

📦 Repository Structure

```
MiniGames/
├── wwwroot/                 # Static files (CSS, JS, emulators, three.js)
│   ├── binjgb/              # GameBoy emulator core
│   ├── ufe/                 # Universal Frontend Emulator (Ruffle + EmulatorJS)
│   ├── three/               # Three.js library and examples
│   └── css/                 # Global styles
├── Pages/                   # Blazor components (Home, About, etc.)
├── Emulators/               # Razor components for each emulator
├── BlazorGames/             # Custom sandbox games (Elemental, Crusher, Physics)
├── Services/                # Game service, Chaos service, etc.
├── CsLists/                 # Central game list for the homepage
└── Program.cs               # App entry point and service registration
```

🏗️ Building and Running Locally

Prerequisites

· .NET 8 SDK
· Git
· (Optional) Node.js for Three.js examples

Steps

1. Clone the repository
   ```bash
   git clone https://github.com/yourusername/MiniGames.git
   cd MiniGames
   ```
2. Restore and build
   ```bash
   dotnet restore
   dotnet build
   ```
3. Run the development server
   ```bash
   dotnet run
   ```
   The site will be available at http://localhost:5000.
4. Publish for deployment
   ```bash
   dotnet publish -c Release
   ```
   The static files will be in bin/Release/net8.0/publish/wwwroot. Deploy these to any static host.

🌐 Live Demo Availability

Today's Schedule: 6:00 AM – 3:00 PM (your local time)

The server is automatically managed by an automatic BAT file with Task Scheduler that handles startup and shutdown according to the schedule above.

Active URLs:

Platform URL
MiniGames Hub https://newspapers-societies-distributors-interface.trycloudflare.com
Emulation Platform https://researchers-lasting-baptist-healing.trycloudflare.com

⚠️ Note: If you try to access the sites outside the scheduled hours, you may see a 502 error or timeout. The automatic BAT file ensures the servers are only running during the specified window.

📥 Forking and Building Your Own Site

You are strongly encouraged to fork this repository and build your own version of MiniGames! Because the project is under active development, some source files may be newer than others, and you might need to update paths or dependencies to get everything working.

Common Pitfalls When Forking

· Missing submodules: Some emulators (like binjgb) are included as submodules. Run git submodule update --init --recursive after cloning.
· Three.js path: The Three.js examples expect the library at /three/build/three.module.js. If your structure differs, update the import map in the HTML files.
· EmulatorJS dependencies: The UFE page loads nipple.js and gamepad.js from the /data/ folder. Make sure these files are present.

If you encounter issues, please check the console for 404s – they usually point to missing files.

📞 Contact & Support

Need help? Found a bug? Have an idea for a new feature? Feel free to reach out:

· Email: babyyodacutefry@gmail.com
· GitHub Issues: Use the Issues tab for bug reports and feature requests.
· Discussions: Start a Discussion for general questions.

We're a small team (okay, it's mostly just one person grinding away), but we do our best to respond within a few days. If you're emailing, please include "MiniGames" in the subject line so I don't miss it.

📜 License

This project is licensed under the MIT License – see the LICENSE file for details. You are free to use, modify, and distribute this code for personal or commercial projects, but please give credit where it's due.

🙏 Acknowledgements

This project would not exist without the incredible open‑source work of:

· binjgb by binji
· Ruffle by the Ruffle project
· EmulatorJS by the EmulatorJS team
· MediaPipe by Google
· Three.js by the Three.js community
· VelcroPhysics by geniiii
· MoonSharp by moonsharp‑devs
· Toolbelt.Blazor.Gamepad by jsakamoto

And countless others who make the web a magical place to build.

---

Now go forth, fork, and build something amazing. The future of browser‑based gaming is in your hands. 💥
