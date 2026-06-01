using UnityEngine;
using UnityEngine.Rendering;

namespace Bones.Presentation
{
    /// <summary>
    /// Creates placeholder materials that render under WHICHEVER pipeline is active — URP if a
    /// pipeline asset is assigned, otherwise Built-in. Avoids the magenta "missing shader" look
    /// when URP is installed but not active. Real authored materials replace these later.
    /// </summary>
    public static class MaterialUtil
    {
        private static bool UrpActive => GraphicsSettings.currentRenderPipeline != null;

        public static Material Opaque(Color color)
        {
            var shader = UrpActive
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // last-ditch fallback
            return new Material(shader) { color = color };
        }

        public static Material Unlit(Color color)
        {
            var shader = UrpActive
                ? Shader.Find("Universal Render Pipeline/Unlit")
                : Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { color = color };
            return mat;
        }
    }
}
