using UnityEngine;
using Bones.Data;

namespace Bones.Presentation
{
    /// <summary>Builds placeholder dice (tinted cubes + blob shadow) at runtime so the project is
    /// playable before authored meshes exist. Swap to DieDefinition.meshOverride/materialOverride later.</summary>
    public static class DieFactory
    {
        public static DieView Create(Transform parent, DieDefinition def, Vector3 localPos)
        {
            var root = new GameObject(def != null ? $"Die_{def.id}" : "Die");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            // Body: authored mesh if present, else a primitive cube.
            GameObject bodyGo;
            if (def != null && def.meshOverride != null)
            {
                bodyGo = new GameObject("Body");
                var mf = bodyGo.AddComponent<MeshFilter>();
                mf.sharedMesh = def.meshOverride;
                var mr = bodyGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = def.materialOverride != null
                    ? def.materialOverride
                    : DefaultMaterial(def != null ? def.tier : DieTier.Common);
            }
            else
            {
                bodyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bodyGo.name = "Body";
                Object.Destroy(bodyGo.GetComponent<Collider>());
                var mr = bodyGo.GetComponent<MeshRenderer>();
                mr.sharedMaterial = def != null && def.materialOverride != null
                    ? def.materialOverride
                    : DefaultMaterial(def != null ? def.tier : DieTier.Common);
            }
            bodyGo.transform.SetParent(root.transform, false);
            bodyGo.transform.localScale = Vector3.one * 0.7f;

            // Blob shadow: a flat quad just under the die.
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "Shadow";
            Object.Destroy(shadow.GetComponent<Collider>());
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localPosition = new Vector3(0f, -0.36f, 0f);
            shadow.transform.localScale = Vector3.one * 0.9f;
            var sr = shadow.GetComponent<MeshRenderer>();
            sr.sharedMaterial = ShadowMaterial();

            var view = root.AddComponent<DieView>();
            view.Init(bodyGo.transform, shadow.transform);
            Color bodyColor = DieTierPalette.ColorFor(def != null ? def.tier : DieTier.Common);
            view.SetTint(bodyColor);

            // Pips contrast with the body so they read on both bone-white and ink-black dice.
            float lum = bodyColor.r * 0.299f + bodyColor.g * 0.587f + bodyColor.b * 0.114f;
            Color pipColor = lum > 0.5f ? new Color(0.10f, 0.09f, 0.10f) : new Color(0.92f, 0.90f, 0.82f);
            view.BuildPips(pipColor);
            return view;
        }

        private static Material _shadowMat;
        private static Material ShadowMaterial()
        {
            if (_shadowMat == null) _shadowMat = MaterialUtil.Unlit(new Color(0f, 0f, 0f, 0.35f));
            return _shadowMat;
        }

        private static Material DefaultMaterial(DieTier tier) => MaterialUtil.Opaque(DieTierPalette.ColorFor(tier));
    }
}
