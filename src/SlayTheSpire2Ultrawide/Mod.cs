using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace SlayTheSpire2Ultrawide;

[ModInitializer(nameof(Initialize))]
public static class Mod
{
    public const string ModId = "sts2-ultrawide";

    private static Config _config = Config.Defaults();
    private static List<AnchorRule> _rules = new();
    private static ResolutionUnlocker? _resolution;
    private static ViewportAdjuster? _viewport;
    private static HudReanchorer? _hud;
    private static BackgroundSwapper? _background;
    private static DebugOverlay? _debug;

    public static void Initialize()
    {
        try
        {
            var configPath = GetConfigPath();
            _config = Config.LoadOrCreate(configPath);
            Log($"loaded config from {configPath}");

            if (!_config.Enable)
            {
                Log("disabled via config; exiting initializer");
                return;
            }

            _rules = LoadAnchorRules();
            _resolution = new ResolutionUnlocker(_config);
            _resolution.ApplyPatches();
            _viewport = new ViewportAdjuster(_config);
            _hud = new HudReanchorer(_config, _rules);
            _background = new BackgroundSwapper(_config);
            if (_config.DebugOverlay) _debug = new DebugOverlay();

            if (Engine.GetMainLoop() is SceneTree tree)
            {
                tree.NodeAdded += OnNodeAdded;
                if (tree.Root is not null)
                {
                    tree.Root.SizeChanged += OnViewportSizeChanged;
                    OnViewportSizeChanged();
                    // Scan existing scene tree (HUD nodes may already exist).
                    _hud?.ApplyToSubtree(tree.Root);
                }
            }
            else
            {
                Log("main loop is not a SceneTree; cannot wire scene hooks", error: true);
            }

            Log($"initialization complete (verbose={_config.VerboseLog}, debug_overlay={_config.DebugOverlay})");
            Log($"primary screen size: {DisplayServer.ScreenGetSize()}, screen count: {DisplayServer.GetScreenCount()}");
            for (int i = 0; i < DisplayServer.GetScreenCount(); i++)
            {
                Log($"  screen {i}: size={DisplayServer.ScreenGetSize(i)}, position={DisplayServer.ScreenGetPosition(i)}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{ModId}] init failed: {ex}");
            WriteToFile($"[{ModId}] init failed: {ex}", error: true);
        }
    }

    private static void OnViewportSizeChanged()
    {
        if (Engine.GetMainLoop() is not SceneTree tree || tree.Root is null) return;
        var size = tree.Root.GetVisibleRect().Size;
        Log($"viewport size: {size.X:0}x{size.Y:0}");
        _viewport?.Apply((int)size.X, (int)size.Y);
        _background?.Apply((int)size.X, (int)size.Y, tree.CurrentScene);
        _hud?.Apply(tree.CurrentScene);
        if (_debug is not null) _debug.Refresh(tree.Root);
    }

    private static void OnNodeAdded(Node node)
    {
        try
        {
            _hud?.ApplyToSubtree(node);
            if (Engine.GetMainLoop() is SceneTree tree && tree.Root is not null)
            {
                var size = tree.Root.GetVisibleRect().Size;
                _background?.ApplyToSubtree(node, (int)size.X, (int)size.Y);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{ModId}] OnNodeAdded failed: {ex.Message}");
        }
    }

    private static List<AnchorRule> LoadAnchorRules()
    {
        const string resPath = "res://assets/anchor_rules.json";
        if (!Godot.FileAccess.FileExists(resPath))
        {
            Log("anchor_rules.json missing from PCK; HUD reanchoring disabled");
            return new List<AnchorRule>();
        }
        using var f = Godot.FileAccess.Open(resPath, Godot.FileAccess.ModeFlags.Read);
        var text = f.GetAsText();
        var rules = AnchorRules.Parse(text);
        Log($"loaded {rules.Count} anchor rules");
        return rules;
    }

    private static string GetConfigPath()
    {
        var dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var dir = System.IO.Path.GetDirectoryName(dllPath);
        if (string.IsNullOrEmpty(dir))
        {
            dir = System.IO.Path.GetDirectoryName(OS.GetExecutablePath()) ?? System.Environment.CurrentDirectory;
        }
        return System.IO.Path.Combine(dir, "config.toml");
    }

    private static string? _logFilePath;
    private static readonly object _logLock = new();

    internal static void Log(string msg, bool error = false)
    {
        var line = $"[{ModId}] {msg}";
        if (error) GD.PrintErr(line);
        else if (_config.VerboseLog) GD.Print(line);
        else GD.PrintRich($"[color=cyan]{line}[/color]");
        WriteToFile(line, error);
    }

    private static void WriteToFile(string line, bool error)
    {
        try
        {
            if (_logFilePath is null)
            {
                var dir = System.IO.Path.GetDirectoryName(GetConfigPath())!;
                _logFilePath = System.IO.Path.Combine(dir, "sts2-ultrawide.log");
                lock (_logLock)
                {
                    System.IO.File.WriteAllText(_logFilePath,
                        $"--- sts2-ultrawide log start {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---\n");
                }
            }
            var stamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var prefix = error ? "ERR " : "    ";
            lock (_logLock)
            {
                System.IO.File.AppendAllText(_logFilePath, $"{stamp} {prefix}{line}\n");
            }
        }
        catch { /* never let logging crash the mod */ }
    }
}
