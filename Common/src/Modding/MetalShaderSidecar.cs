using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Common {
    /// <summary>
    /// Moves an embedded bundle's materials onto Metal shaders when the game renders through Metal (macOS).
    ///
    /// The bundles are built for StandaloneWindows, so their shaders carry D3D11, OpenGLCore and Vulkan programs
    /// and none for Metal, the only API Unity 6 renders with on macOS: every bundled material rendered pink there,
    /// after "Desired shader compiler platform 14 is not available in shader blob" once per shader. Everything
    /// else in a Windows bundle loads fine on a Mac, so the editor builds "&lt;bundle&gt;_metal" for StandaloneOSX
    /// with only those shaders in it (ValheimUnity's MetalShaderSidecars.cs), and a mod embeds it beside the
    /// bundle. The shaders ride on empty carrier materials, which is how the build gets the built-in ones and the
    /// right keyword variants in. The platform 14 errors still appear when the Windows shaders load; they are
    /// harmless once the materials have been moved off them.
    /// </summary>
    internal static class MetalShaderSidecar {
        private const string Suffix = "_metal";

        // Test hook: on any other API the moved materials render pink, which shows the swap reached them.
        private const string ForceArgument = "-force-metal-sidecar";

        // Jotunn swaps a JVLmock_ shader for the game's own later, when the prefab is registered.
        private const string MockPrefix = "JVLmock_";

        // The moved materials use these bundles' shaders, so they stay loaded for the life of the process.
        private static readonly List<AssetBundle> _loadedSidecars = new List<AssetBundle>();

        // Held so Resources.UnloadUnusedAssets cannot drop one: the bundle would then load it afresh, with its
        // Windows shader, the next time a prefab that uses it loads.
        private static readonly List<Material> _movedMaterials = new List<Material>();

        /// <summary>
        /// On Metal, loads "&lt;assembly name&gt;.&lt;filename&gt;_metal" and gives every material in the bundle
        /// the Metal shader of the same name. Does nothing elsewhere, or when the mod embeds no sidecar.
        /// Call it straight after the bundle loads, before anything loads or copies one of its assets.
        /// </summary>
        internal static void Apply(AssetBundle bundle, string filename, Assembly assembly) {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal && !IsForced()) { return; }

            string resourceName = $"{assembly.GetName().Name}.{filename}{Suffix}";
            try {
                Stopwatch timer = Stopwatch.StartNew();
                byte[] bytes;
                using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
                    if (stream == null) {
                        ModLogger.LogDebug($"No Metal shaders are embedded for asset bundle '{filename}'.");
                        return;
                    }
                    using (var memory = new MemoryStream()) {
                        stream.CopyTo(memory);
                        bytes = memory.ToArray();
                    }
                }

                AssetBundle sidecar = AssetBundle.LoadFromMemory(bytes);
                if (sidecar == null) {
                    ModLogger.LogError($"Embedded Metal shaders '{resourceName}' failed to load; '{filename}' will render pink.");
                    return;
                }
                _loadedSidecars.Add(sidecar);

                var metalShaders = new Dictionary<string, Shader>();
                foreach (Material carrier in sidecar.LoadAllAssets<Material>()) {
                    if (carrier.shader != null) { metalShaders[carrier.shader.name] = carrier.shader; }
                }

                // A bundle's materials are rarely assets of their own in it: they come with the prefabs that use
                // them, and LoadAllAssets<Material>() returns only the bundle's own (none, in any bundle here). So
                // the whole bundle is loaded, and the materials that appear meanwhile are its materials. This runs
                // on the main thread before the mod uses the bundle, so nothing else loads one in between.
                var loadedBefore = new HashSet<Material>(Resources.FindObjectsOfTypeAll<Material>());
                bundle.LoadAllAssets();

                int moved = 0;
                var uncovered = new SortedSet<string>(StringComparer.Ordinal);
                foreach (Material material in Resources.FindObjectsOfTypeAll<Material>()) {
                    Shader shader = material.shader;
                    if (shader == null || loadedBefore.Contains(material)) { continue; }

                    if (metalShaders.TryGetValue(shader.name, out Shader metal)) {
                        if (shader == metal) { continue; }
                        // Assigning a shader can reset a custom render queue to the shader's own.
                        int renderQueue = material.renderQueue;
                        material.shader = metal;
                        material.renderQueue = renderQueue;
                        _movedMaterials.Add(material);
                        moved++;
                    } else if (!shader.isSupported && !shader.name.StartsWith(MockPrefix, StringComparison.Ordinal)) {
                        uncovered.Add(shader.name);
                    }
                }

                ModLogger.LogInfo($"Moved {moved} materials in asset bundle '{filename}' onto {metalShaders.Count} Metal shaders " +
                    $"in {timer.Elapsed.TotalMilliseconds:0} ms.");
                if (uncovered.Count > 0) {
                    ModLogger.LogWarning($"Asset bundle '{filename}' has materials using {string.Join(", ", uncovered)}, " +
                        $"which '{resourceName}' has no Metal copy of, so they render pink. Rebuild the asset bundles.");
                }
            } catch (Exception e) {
                ModLogger.LogError($"Failed to apply Metal shaders '{resourceName}': {e.Message}");
            }
        }

        private static bool IsForced() {
            return Environment.GetCommandLineArgs().Any(arg => string.Equals(arg, ForceArgument, StringComparison.OrdinalIgnoreCase));
        }
    }
}
