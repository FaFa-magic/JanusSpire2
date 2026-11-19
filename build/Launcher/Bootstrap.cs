using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace JanusSpire2.Launcher;

[ModInitializer(nameof(Initialize))]
public static class Bootstrap
{
    private const string ModId = "JanusSpire2";
    private const string ImplementationName = ModId + ".Impl";
    private const string BaseVariant = "g0.111.0";
    private static int _started;

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
            throw new InvalidOperationException($"{ModId} was already initialized.");

        Assembly launcher = typeof(Bootstrap).Assembly;
        string root = Path.GetDirectoryName(launcher.Location)
            ?? throw new InvalidOperationException($"Cannot resolve the {ModId} launcher directory.");
        Version? gameVersion = ReadGameVersion(OS.GetExecutablePath());
        string variant = SelectVariant(Path.Combine(root, "bin"), gameVersion);
        string implementationPath = Path.Combine(variant, ImplementationName + ".dll");
        string[] packs = GetPackPaths(variant);
        AssemblyName identity = AssemblyName.GetAssemblyName(implementationPath);
        if (identity.Name != ImplementationName || identity.Version != launcher.GetName().Version)
            throw new InvalidOperationException($"[{ModId}] Unexpected implementation version at {implementationPath}: {identity.FullName}.");

        Log.Info($"[{ModId}] Game {gameVersion?.ToString() ?? "unknown"}; loading {Path.GetFileName(variant)}.");
        var loadContext = AssemblyLoadContext.GetLoadContext(launcher) ?? AssemblyLoadContext.Default;
        Assembly implementation = loadContext.LoadFromAssemblyPath(implementationPath);
        MethodInfo entryPoint = implementation.GetType($"{ModId}.{ModId}Code.MainFile", throwOnError: true)!
            .GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)
            ?? throw new MissingMethodException(implementation.FullName, "MainFile.Initialize");
        if (entryPoint.ReturnType != typeof(void))
            throw new InvalidOperationException($"[{ModId}] MainFile.Initialize must return void.");

        foreach (string pack in packs)
        {
            Log.Info($"[{ModId}] Mounting {pack}.");
            if (!ProjectSettings.LoadResourcePack(pack, replaceFiles: true))
                throw new InvalidOperationException($"[{ModId}] Godot failed to mount {pack}.");
        }
        RegisterResourceUids();

        ModManager.AssociateAssemblyWithMod(ModId, implementation);
        if (!ModManager.Mods.Any(mod => mod.manifest?.id == ModId && mod.assemblies.Contains(implementation)))
            throw new InvalidOperationException($"[{ModId}] The game did not register {ImplementationName}.");

        try
        {
            entryPoint.Invoke(null, null);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    internal static void RegisterResourceUids()
    {
        // Read the selected pack's UID cache without replacing Godot's global cache file.
        using var cache = Godot.FileAccess.Open($"res://{ModId}/resource_uid_cache.bin", Godot.FileAccess.ModeFlags.Read);
        if (cache == null)
            return;

        uint count = cache.Get32();
        for (uint index = 0; index < count; index++)
        {
            long id = (long)cache.Get64();
            string path = cache.GetPascalString();
            bool belongsToMod = path.StartsWith($"res://{ModId}/", StringComparison.Ordinal)
                || path.StartsWith($"res://{ModId}Code/", StringComparison.Ordinal)
                || path.StartsWith("res://materials/transitions/janus_spire2_", StringComparison.Ordinal);
            if (belongsToMod && !ResourceUid.HasId(id) && ResourceLoader.Exists(path))
                ResourceUid.AddId(id, path);
        }
    }

    internal static Version? ReadGameVersion(string executablePath)
    {
        string directory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory;
        string appResources = Path.GetFullPath(Path.Combine(directory, "..", "Resources"));
        foreach (string root in new[] { directory, appResources, AppContext.BaseDirectory }.Distinct())
        {
            string path = Path.Combine(root, "release_info.json");
            if (!File.Exists(path))
                continue;

            try
            {
                using var release = JsonDocument.Parse(File.ReadAllText(path));
                if (release.RootElement.ValueKind == JsonValueKind.Object &&
                    release.RootElement.TryGetProperty("version", out var version) &&
                    version.ValueKind == JsonValueKind.String &&
                    Version.TryParse(version.GetString()?.Trim().TrimStart('v', 'V'), out var parsed))
                    return parsed;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                Log.Warn($"[{ModId}] Could not read {path}: {exception.Message}");
            }
        }
        return null;
    }

    internal static string SelectVariant(string bin, Version? gameVersion)
    {
        var candidates = Directory.Exists(bin)
            ? Directory.EnumerateDirectories(bin, "g*")
                .Select(path => (Path: path, Version: Version.TryParse(Path.GetFileName(path).AsSpan(1), out var version) ? version : null))
                .Where(candidate => candidate.Version != null)
                .OrderByDescending(candidate => candidate.Version)
                .ToArray()
            : [];

        if (gameVersion != null)
        {
            // Descending order puts an exact match first and otherwise picks the nearest lower release.
            var compatible = candidates.FirstOrDefault(candidate => candidate.Version! <= gameVersion);
            if (compatible.Path != null)
                return compatible.Path;
        }

        string latest = Path.Combine(bin, "latest");
        if (Directory.Exists(latest))
            return latest;
        if (gameVersion == null && candidates.Length > 0)
            return candidates[0].Path;

        throw new InvalidOperationException($"[{ModId}] No implementation for game {gameVersion?.ToString() ?? "unknown"} in {bin}.");
    }

    internal static string[] GetPackPaths(string directory)
    {
        string patch = Path.Combine(directory, ModId + ".patch.pck");
        if (File.Exists(patch))
        {
            string basePack = Path.GetFullPath(Path.Combine(directory, "..", BaseVariant, ModId + ".pck"));
            if (!File.Exists(basePack))
                throw new FileNotFoundException($"[{ModId}] Missing base pack for {patch}.", basePack);
            return [basePack, patch];
        }

        string fullPack = Path.Combine(directory, ModId + ".pck");
        if (File.Exists(fullPack))
            return [fullPack];

        throw new FileNotFoundException($"[{ModId}] Missing complete PCK or PCK patch in {directory}.");
    }
}
