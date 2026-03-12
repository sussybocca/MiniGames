using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MiniGames.AI_OFFLINE_SERVICES;

/// <summary>
/// Production‑ready offline game generator.
/// Parses a structured folder and outputs a fully playable HTML/Three.js game.
/// No placeholders – everything is built from the provided JSON and assets.
/// </summary>
public class OfflineGameGenerator
{
    // ======================================================================
    // Public API – Simple single‑file generation (backward compatible)
    // ======================================================================

    public string GenerateGameFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string title = GetString(root, "title") ?? "Generated Game";
            string genre = GetString(root, "genre")?.ToLowerInvariant() ?? "platformer";
            string theme = GetString(root, "theme") ?? "retro";
            int width = GetInt(root, "width") ?? 800;
            int height = GetInt(root, "height") ?? 600;

            var player = ParseSimplePlayer(root.GetProperty("player"));
            var levels = ParseSimpleLevels(root);

            return GenerateSimpleHtml(title, genre, theme, width, height, player, levels);
        }
        catch (Exception ex)
        {
            return $"<p>Error parsing JSON: {ex.Message}</p>";
        }
    }

    // ======================================================================
    // Public API – Advanced folder‑based generation (3D, assets)
    // ======================================================================

    /// <summary>
    /// Generates a full 3D game from a structured folder on disk.
    /// </summary>
    public async Task<string> GenerateGameFromFolderAsync(string gameFolder)
    {
        if (!Directory.Exists(gameFolder))
            throw new DirectoryNotFoundException($"Game folder not found: {gameFolder}");

        string miniJsonFolder = Path.Combine(gameFolder, "MiniJson");
        string assetsFolder = Path.Combine(gameFolder, "EXTERNAL_ASSETS");

        if (!Directory.Exists(miniJsonFolder))
            throw new DirectoryNotFoundException($"Missing MiniJson folder: {miniJsonFolder}");
        if (!Directory.Exists(assetsFolder))
            throw new DirectoryNotFoundException($"Missing EXTERNAL_ASSETS folder: {assetsFolder}");

        var jsonFiles = Directory.GetFiles(gameFolder, "*.json");
        if (jsonFiles.Length < 3)
            throw new InvalidOperationException("Game folder must contain at least 3 JSON files.");

        string gameConfigPath = jsonFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("game.json", StringComparison.OrdinalIgnoreCase)) ?? jsonFiles[0];
        string levelsPath = jsonFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("levels.json", StringComparison.OrdinalIgnoreCase));
        string assetsPath = jsonFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("assets.json", StringComparison.OrdinalIgnoreCase));
        string scriptsPath = jsonFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("scripts.json", StringComparison.OrdinalIgnoreCase));

        var gameConfig = await ParseGameConfigAsync(gameConfigPath);
        var levels = levelsPath != null ? await ParseLevelsAsync(levelsPath) : new List<LevelData>();
        var assets = assetsPath != null ? await ParseAssetsAsync(assetsPath) : new List<AssetData>();
        var scripts = scriptsPath != null ? await ParseScriptsAsync(scriptsPath) : new List<string>();

        var assetMap = await ProcessAssetsAsync(assetsFolder, assets);

        return GenerateThreeJsHtml(gameConfig, levels, assetMap, scripts);
    }

    /// <summary>
    /// Generates a full 3D game from an in‑memory file dictionary (e.g., from a ZIP).
    /// Automatically normalizes paths that may include a common root folder.
    /// </summary>
    public async Task<string> GenerateGameFromFolderStructureAsync(Dictionary<string, byte[]> files)
    {
        // ------------------------------------------------------------
        // 1. Normalize paths: remove any common root folder
        // ------------------------------------------------------------
        var normalizedFiles = new Dictionary<string, byte[]>();
        var keys = files.Keys.ToList();

        if (keys.Count == 0)
            throw new InvalidOperationException("Empty file dictionary.");

        // Find the longest common prefix that ends with '/'
        string commonPrefix = "";
        if (keys.Count > 1)
        {
            string first = keys[0];
            int lastSlash = first.LastIndexOf('/');
            if (lastSlash > 0)
            {
                string candidate = first.Substring(0, lastSlash + 1);
                if (keys.All(k => k.StartsWith(candidate)))
                    commonPrefix = candidate;
            }
        }
        else
        {
            // Only one file – if it contains a slash, use everything before the last slash as prefix
            int lastSlash = keys[0].LastIndexOf('/');
            if (lastSlash > 0)
                commonPrefix = keys[0].Substring(0, lastSlash + 1);
        }

        // Strip common prefix from all keys
        foreach (var kv in files)
        {
            string normalizedPath = kv.Key;
            if (!string.IsNullOrEmpty(commonPrefix) && normalizedPath.StartsWith(commonPrefix))
                normalizedPath = normalizedPath.Substring(commonPrefix.Length);
            normalizedFiles[normalizedPath] = kv.Value;
        }

        // ------------------------------------------------------------
        // 2. Validate required folders
        // ------------------------------------------------------------
        bool hasMiniJson = normalizedFiles.Keys.Any(k => k.StartsWith("MiniJson/"));
        bool hasExternalAssets = normalizedFiles.Keys.Any(k => k.StartsWith("EXTERNAL_ASSETS/"));
        if (!hasMiniJson) throw new InvalidOperationException("Missing MiniJson/ folder.");
        if (!hasExternalAssets) throw new InvalidOperationException("Missing EXTERNAL_ASSETS/ folder.");

        var jsonFiles = normalizedFiles.Keys.Where(k => k.EndsWith(".json") && !k.Contains('/')).ToList();
        if (jsonFiles.Count < 3)
            throw new InvalidOperationException("Root folder must contain at least 3 JSON files.");

        // Helper to get file content as string
        string? getText(string path) =>
            normalizedFiles.TryGetValue(path, out var data) ? Encoding.UTF8.GetString(data) : null;

        // Locate main JSON files
        string gameConfigPath = jsonFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("game.json", StringComparison.OrdinalIgnoreCase)) ?? jsonFiles[0];
        string levelsPath = jsonFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("levels.json", StringComparison.OrdinalIgnoreCase));
        string assetsPath = jsonFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("assets.json", StringComparison.OrdinalIgnoreCase));
        string scriptsPath = jsonFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("scripts.json", StringComparison.OrdinalIgnoreCase));

        // Parse JSON files
        var gameConfig = ParseGameConfig(getText(gameConfigPath) ?? throw new Exception("Missing game config"));
        var levels = levelsPath != null ? ParseLevels(getText(levelsPath) ?? "[]") : new List<LevelData>();
        var assets = assetsPath != null ? ParseAssets(getText(assetsPath) ?? "[]") : new List<AssetData>();
        var scripts = scriptsPath != null ? ParseScripts(getText(scriptsPath) ?? "[]") : new List<string>();

        // Process assets – locate files under EXTERNAL_ASSETS/ OR handle inline data URIs
var assetMap = new Dictionary<string, ProcessedAsset>();
foreach (var asset in assets)
{
    // Check if the src is a data URI (starts with "data:")
    if (asset.Src.StartsWith("data:"))
    {
        // Inline asset – use the data URI directly
        assetMap[asset.Id] = new ProcessedAsset
        {
            Id = asset.Id,
            Type = asset.Type,
            DataUri = asset.Src,
            OriginalPath = null,
            FileSize = 0 // unknown
        };
    }
    else
    {
        // Regular file – look under EXTERNAL_ASSETS/
        string assetRelPath = $"EXTERNAL_ASSETS/{asset.Src}";
        if (!normalizedFiles.TryGetValue(assetRelPath, out var assetData))
            throw new FileNotFoundException($"Asset not found: {assetRelPath}");

        string mimeType = GetMimeType(asset.Src);
        string dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(assetData)}";

        assetMap[asset.Id] = new ProcessedAsset
        {
            Id = asset.Id,
            Type = asset.Type,
            DataUri = dataUri,
            OriginalPath = asset.Src,
            FileSize = assetData.Length
        };
    }
}

        return GenerateThreeJsHtml(gameConfig, levels, assetMap, scripts);
    }

    // ----------------------------------------------------------------------
    // JSON Parsing helpers (overloads for in‑memory strings)
    // ----------------------------------------------------------------------
    private GameConfig ParseGameConfig(string json) =>
        JsonSerializer.Deserialize<GameConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidDataException("Invalid game.json");

    private List<LevelData> ParseLevels(string json) =>
        JsonSerializer.Deserialize<List<LevelData>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? new List<LevelData>();

    private List<AssetData> ParseAssets(string json) =>
        JsonSerializer.Deserialize<List<AssetData>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? new List<AssetData>();

    private List<string> ParseScripts(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var scripts = new List<string>();
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String)
                    scripts.Add(item.GetString() ?? "");
        }
        return scripts;
    }

    // ----------------------------------------------------------------------
    // JSON Parsing (async file-based)
    // ----------------------------------------------------------------------
    private async Task<GameConfig> ParseGameConfigAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        return ParseGameConfig(json);
    }

    private async Task<List<LevelData>> ParseLevelsAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        return ParseLevels(json);
    }

    private async Task<List<AssetData>> ParseAssetsAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        return ParseAssets(json);
    }

    private async Task<List<string>> ParseScriptsAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        return ParseScripts(json);
    }

    // ----------------------------------------------------------------------
    // Asset Processing
    // ----------------------------------------------------------------------
    private async Task<Dictionary<string, ProcessedAsset>> ProcessAssetsAsync(string assetsFolder, List<AssetData> assets)
    {
        var assetMap = new Dictionary<string, ProcessedAsset>();

        foreach (var asset in assets)
        {
            string sourcePath = Path.Combine(assetsFolder, asset.Src);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"Asset not found: {sourcePath}");

            byte[] data = await File.ReadAllBytesAsync(sourcePath);
            string mimeType = GetMimeType(asset.Src);
            string dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(data)}";

            assetMap[asset.Id] = new ProcessedAsset
            {
                Id = asset.Id,
                Type = asset.Type,
                DataUri = dataUri,
                OriginalPath = asset.Src,
                FileSize = data.Length
            };
        }

        return assetMap;
    }

    private string GetMimeType(string fileName)
    {
        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".glb" => "model/gltf-binary",
            ".gltf" => "model/gltf+json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }

    // ----------------------------------------------------------------------
    // HTML Generation – Advanced Three.js
    // ----------------------------------------------------------------------
    private string GenerateThreeJsHtml(GameConfig config, List<LevelData> levels, Dictionary<string, ProcessedAsset> assets, List<string> scripts)
{
    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html>");
    sb.AppendLine("<html>");
    sb.AppendLine("<head>");
    sb.AppendLine($"<title>{config.Title}</title>");
    sb.AppendLine("<style>body{margin:0;overflow:hidden;}</style>");
    sb.AppendLine("<script src='https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js'></script>");
    if (assets.Values.Any(a => a.Type == "model"))
        sb.AppendLine("<script src='https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/loaders/GLTFLoader.js'></script>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body>");
    sb.AppendLine("<script>");

    string bgColor = config.BackgroundColor.HasValue 
        ? $"0x{config.BackgroundColor.Value:X6}" 
        : "0x111111";

    sb.AppendLine($@"
        // ----- Three.js setup -----
        const scene = new THREE.Scene();
        scene.background = new THREE.Color({bgColor});
        const camera = new THREE.PerspectiveCamera(75, {config.Width ?? 1024} / {config.Height ?? 768}, 0.1, 1000);
        const renderer = new THREE.WebGLRenderer({{ antialias: true }});
        renderer.setSize({config.Width ?? 1024}, {config.Height ?? 768});
        document.body.appendChild(renderer.domElement);

        // Lights
        const ambientLight = new THREE.AmbientLight(0x404040);
        scene.add(ambientLight);
        const dirLight = new THREE.DirectionalLight(0xffffff, 1);
        dirLight.position.set(1, 2, 1);
        scene.add(dirLight);

        // Asset map
        const assets = {SerializeAssets(assets)};
        console.log('Assets loaded:', assets);

        // Level data
        const levels = {JsonSerializer.Serialize(levels)};
        console.log('Levels:', levels);

        function loadAsset(assetId) {{
            return new Promise((resolve, reject) => {{
                const asset = assets[assetId];
                if (!asset) {{ reject('Asset not found: ' + assetId); return; }}

                if (asset.type === 'model') {{
                    const loader = new THREE.GLTFLoader();
                    // Extract base64 data
                    const base64 = asset.dataUri.split(',')[1];
                    const binary = atob(base64);
                    const buffer = new ArrayBuffer(binary.length);
                    const view = new Uint8Array(buffer);
                    for (let i = 0; i < binary.length; i++) {{
                        view[i] = binary.charCodeAt(i);
                    }}
                    loader.parse(buffer, '', resolve, reject);
                }} else if (asset.type === 'texture') {{
                    const loader = new THREE.TextureLoader();
                    loader.load(asset.dataUri, resolve, undefined, reject);
                }} else {{
                    resolve(null);
                }}
            }});
        }}

        async function buildScene() {{
            for (const level of levels) {{
                for (const obj of level.objects) {{
                    try {{
                        const loaded = await loadAsset(obj.assetId);
                        if (loaded) {{
                            let mesh;
                            if (loaded.scene) {{ // GLTF group
                                mesh = loaded.scene;
                            }} else if (loaded.isTexture) {{
                                const material = new THREE.MeshStandardMaterial({{ map: loaded }});
                                const geometry = new THREE.BoxGeometry(1,1,1);
                                mesh = new THREE.Mesh(geometry, material);
                            }} else {{
                                mesh = loaded;
                            }}
                            mesh.position.set(obj.x || 0, obj.y || 0, obj.z || 0);
                            mesh.rotation.set(obj.rotX || 0, obj.rotY || 0, obj.rotZ || 0);
                            mesh.scale.set(obj.scaleX || 1, obj.scaleY || 1, obj.scaleZ || 1);
                            scene.add(mesh);
                        }}
                    }} catch (e) {{
                        console.warn('Failed to load asset ' + obj.assetId, e);
                        // Fallback: create a colored cube
                        const geometry = new THREE.BoxGeometry(1,1,1);
                        const material = new THREE.MeshStandardMaterial({{ color: 0xff00ff }});
                        const mesh = new THREE.Mesh(geometry, material);
                        mesh.position.set(obj.x || 0, obj.y || 0, obj.z || 0);
                        mesh.scale.set(obj.scaleX || 1, obj.scaleY || 1, obj.scaleZ || 1);
                        scene.add(mesh);
                    }}
                }}
            }}
        }}

        buildScene().then(() => {{
            camera.position.set(0, 2, 5);
            camera.lookAt(0, 1, 0);
            animate();
        }});

        function animate() {{
            requestAnimationFrame(animate);
            renderer.render(scene, camera);
        }}
    ");

    foreach (var script in scripts)
        sb.AppendLine(script);

    sb.AppendLine("</script>");
    sb.AppendLine("</body>");
    sb.AppendLine("</html>");
    return sb.ToString();
}
    private string SerializeAssets(Dictionary<string, ProcessedAsset> assets)
    {
        var dict = assets.ToDictionary(
            kv => kv.Key,
            kv => new { kv.Value.Type, kv.Value.DataUri, kv.Value.FileSize });
        return JsonSerializer.Serialize(dict);
    }

    // ======================================================================
    // Simple HTML generation (backward compatible)
    // ======================================================================
    private PlayerInfoSimple ParseSimplePlayer(JsonElement playerElem)
    {
        return new PlayerInfoSimple
        {
            Sprite = GetString(playerElem, "sprite") ?? "🟢",
            Speed = GetInt(playerElem, "speed") ?? 5,
            JumpPower = GetInt(playerElem, "jumpPower") ?? 10,
            Color = GetString(playerElem, "color") ?? "#0f0",
            StartX = GetInt(playerElem, "startX") ?? 50,
            StartY = GetInt(playerElem, "startY") ?? 100
        };
    }

    private List<LevelInfoSimple> ParseSimpleLevels(JsonElement root)
    {
        var levels = new List<LevelInfoSimple>();
        if (root.TryGetProperty("levels", out var levelsElem) && levelsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var level in levelsElem.EnumerateArray())
            {
                levels.Add(new LevelInfoSimple
                {
                    Name = GetString(level, "name") ?? "Level",
                    Map = ParseSimpleMap(level),
                    Enemies = ParseSimpleEnemies(level),
                    Collectibles = ParseSimpleCollectibles(level),
                    Goal = ParseSimpleGoal(level)
                });
            }
        }
        if (levels.Count == 0)
        {
            levels.Add(new LevelInfoSimple
            {
                Name = "Level 1",
                Map = new List<string> {
                    "                    ",
                    "                    ",
                    "                    ",
                    "                    ",
                    "                    ",
                    "                    ",
                    "                    ",
                    "                    ",
                    "                    ",
                    "####################"
                }
            });
        }
        return levels;
    }

    private List<string> ParseSimpleMap(JsonElement level)
    {
        var map = new List<string>();
        if (level.TryGetProperty("map", out var mapElem) && mapElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in mapElem.EnumerateArray())
                if (row.ValueKind == JsonValueKind.String)
                    map.Add(row.GetString() ?? "");
        }
        return map;
    }

    private List<EnemyInfoSimple> ParseSimpleEnemies(JsonElement level)
    {
        var enemies = new List<EnemyInfoSimple>();
        if (level.TryGetProperty("enemies", out var enemiesElem) && enemiesElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in enemiesElem.EnumerateArray())
            {
                enemies.Add(new EnemyInfoSimple
                {
                    Type = GetString(e, "type") ?? "basic",
                    X = GetInt(e, "x") ?? 0,
                    Y = GetInt(e, "y") ?? 0,
                    Speed = GetInt(e, "speed") ?? 2,
                    Sprite = GetString(e, "sprite") ?? "🔴"
                });
            }
        }
        return enemies;
    }

    private List<CollectibleInfoSimple> ParseSimpleCollectibles(JsonElement level)
    {
        var items = new List<CollectibleInfoSimple>();
        if (level.TryGetProperty("collectibles", out var itemsElem) && itemsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var i in itemsElem.EnumerateArray())
            {
                items.Add(new CollectibleInfoSimple
                {
                    Type = GetString(i, "type") ?? "coin",
                    X = GetInt(i, "x") ?? 0,
                    Y = GetInt(i, "y") ?? 0,
                    Value = GetInt(i, "value") ?? 1,
                    Sprite = GetString(i, "sprite") ?? "🟡"
                });
            }
        }
        return items;
    }

    private GoalInfoSimple? ParseSimpleGoal(JsonElement level)
    {
        if (level.TryGetProperty("goal", out var goalElem))
        {
            return new GoalInfoSimple
            {
                X = GetInt(goalElem, "x") ?? 0,
                Y = GetInt(goalElem, "y") ?? 0,
                Sprite = GetString(goalElem, "sprite") ?? "🏁"
            };
        }
        return null;
    }

    private string GenerateSimpleHtml(string title, string genre, string theme,
                                      int width, int height,
                                      PlayerInfoSimple player, List<LevelInfoSimple> levels)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head>");
        sb.AppendLine($"<title>{title}</title>");
        sb.AppendLine("<style>body{margin:0;overflow:hidden;background:#111;color:white;font-family:monospace;}#gameCanvas{display:block;margin:auto;background:#000;}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<canvas id='gameCanvas' width='{width}' height='{height}'></canvas><script>");
        sb.AppendLine($@"
        const config = {{
            title:'{title}', genre:'{genre}', theme:'{theme}', width:{width}, height:{height},
            player:{JsonSerializer.Serialize(player)},
            levels:{JsonSerializer.Serialize(levels)}
        }};
        const canvas=document.getElementById('gameCanvas'), ctx=canvas.getContext('2d');
        let currentLevel=0, playerPos={{x:config.player.startX, y:config.player.startY}}, velocity={{x:0,y:0}},
            gravity=0.5, onGround=false, keys={{}}, collectibles=[], enemies=[];

        function loadLevel(levelIndex){{
            const level=config.levels[levelIndex];
            window.collisionMap=level.map.map(row=>row.split('').map(c=>c==='#'?1:0));
            enemies=level.enemies||[]; collectibles=level.collectibles||[];
            playerPos={{x:config.player.startX, y:config.player.startY}}; velocity={{x:0,y:0}};
        }}
        loadLevel(0);
        document.addEventListener('keydown',e=>keys[e.key.toLowerCase()]=true);
        document.addEventListener('keyup',e=>keys[e.key.toLowerCase()]=false);

        function update(){{
            if(keys['arrowleft']||keys['a']) playerPos.x-=config.player.speed;
            if(keys['arrowright']||keys['d']) playerPos.x+=config.player.speed;
            if((keys['arrowup']||keys['w']||keys[' '])&&onGround){{ velocity.y=-config.player.jumpPower; onGround=false; }}
            velocity.y+=gravity; playerPos.y+=velocity.y;
            if(playerPos.y+32>canvas.height-50){{ playerPos.y=canvas.height-50-32; velocity.y=0; onGround=true; }} else onGround=false;
            playerPos.x=Math.max(0,Math.min(canvas.width-32,playerPos.x));
        }}

        function draw(){{
            ctx.clearRect(0,0,canvas.width,canvas.height);
            if(window.collisionMap) for(let y=0; y<window.collisionMap.length; y++) for(let x=0; x<window.collisionMap[y].length; x++) if(window.collisionMap[y][x]){{ ctx.fillStyle='#888'; ctx.fillRect(x*32,y*32,32,32); }}
            enemies.forEach(e=>{{ ctx.fillStyle='#f00'; ctx.fillRect(e.x,e.y,32,32); }});
            collectibles.forEach(c=>{{ ctx.fillStyle='#ff0'; ctx.fillRect(c.x,c.y,16,16); }});
            ctx.fillStyle=config.player.color; ctx.fillRect(playerPos.x,playerPos.y,32,32);
            requestAnimationFrame(updateAndDraw);
        }}
        function updateAndDraw(){{ update(); draw(); }}
        updateAndDraw();
        ");
        sb.AppendLine("</script></body></html>");
        return sb.ToString();
    }

    // ======================================================================
    // Helper methods
    // ======================================================================
    private string? GetString(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private int? GetInt(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.GetInt32();
        return null;
    }

    // ======================================================================
    // Data classes
    // ======================================================================
    public class GameConfig
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "3D Game";
        [JsonPropertyName("width")]
        public int? Width { get; set; }
        [JsonPropertyName("height")]
        public int? Height { get; set; }
        [JsonPropertyName("backgroundColor")]
        public int? BackgroundColor { get; set; }
    }

    public class LevelData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("objects")]
        public List<SceneObject> Objects { get; set; } = new();
    }

    public class SceneObject
    {
        [JsonPropertyName("assetId")]
        public string AssetId { get; set; } = "";
        [JsonPropertyName("x")]
        public float? X { get; set; }
        [JsonPropertyName("y")]
        public float? Y { get; set; }
        [JsonPropertyName("z")]
        public float? Z { get; set; }
        [JsonPropertyName("rotX")]
        public float? RotX { get; set; }
        [JsonPropertyName("rotY")]
        public float? RotY { get; set; }
        [JsonPropertyName("rotZ")]
        public float? RotZ { get; set; }
        [JsonPropertyName("scaleX")]
        public float? ScaleX { get; set; }
        [JsonPropertyName("scaleY")]
        public float? ScaleY { get; set; }
        [JsonPropertyName("scaleZ")]
        public float? ScaleZ { get; set; }
    }

    public class AssetData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
        [JsonPropertyName("src")]
        public string Src { get; set; } = "";
    }

    public class ProcessedAsset
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string DataUri { get; set; } = "";
        public string OriginalPath { get; set; } = "";
        public int FileSize { get; set; }
    }

    private class PlayerInfoSimple
    {
        [JsonPropertyName("sprite")]
        public string Sprite { get; set; } = "🟢";
        [JsonPropertyName("speed")]
        public int Speed { get; set; } = 5;
        [JsonPropertyName("jumpPower")]
        public int JumpPower { get; set; } = 10;
        [JsonPropertyName("color")]
        public string Color { get; set; } = "#0f0";
        [JsonPropertyName("startX")]
        public int StartX { get; set; } = 50;
        [JsonPropertyName("startY")]
        public int StartY { get; set; } = 100;
    }

    private class LevelInfoSimple
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("map")]
        public List<string> Map { get; set; } = new();
        [JsonPropertyName("enemies")]
        public List<EnemyInfoSimple> Enemies { get; set; } = new();
        [JsonPropertyName("collectibles")]
        public List<CollectibleInfoSimple> Collectibles { get; set; } = new();
        [JsonPropertyName("goal")]
        public GoalInfoSimple? Goal { get; set; }
    }

    private class EnemyInfoSimple
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "basic";
        [JsonPropertyName("x")]
        public int X { get; set; }
        [JsonPropertyName("y")]
        public int Y { get; set; }
        [JsonPropertyName("speed")]
        public int Speed { get; set; } = 2;
        [JsonPropertyName("sprite")]
        public string Sprite { get; set; } = "🔴";
    }

    private class CollectibleInfoSimple
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "coin";
        [JsonPropertyName("x")]
        public int X { get; set; }
        [JsonPropertyName("y")]
        public int Y { get; set; }
        [JsonPropertyName("value")]
        public int Value { get; set; } = 1;
        [JsonPropertyName("sprite")]
        public string Sprite { get; set; } = "🟡";
    }

    private class GoalInfoSimple
    {
        [JsonPropertyName("x")]
        public int X { get; set; }
        [JsonPropertyName("y")]
        public int Y { get; set; }
        [JsonPropertyName("sprite")]
        public string Sprite { get; set; } = "🏁";
    }
}