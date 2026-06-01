using System.Collections;
using UnityEngine;

namespace Bones.Presentation
{
    /// <summary>
    /// One physical die in the scene. Placeholder is a tinted cube; Erin's authored mesh/material
    /// drops in via DieFactory. The roll is a controlled tumble that settles on a logic-chosen face
    /// (outcome-first) — never a free physics sim.
    /// </summary>
    public class DieView : MonoBehaviour
    {
        public int Face { get; private set; } = 1;

        [SerializeField] private Transform body;     // the visible cube/mesh
        [SerializeField] private Transform shadow;    // blob contact shadow sprite

        private static readonly Vector3[] FaceUpEuler =
        {
            new(  0, 0,   0), // 1 up
            new(-90, 0,   0), // 2
            new(  0, 0,  90), // 3
            new(  0, 0, -90), // 4
            new( 90, 0,   0), // 5
            new(180, 0,   0), // 6
        };

        public void Init(Transform bodyTransform, Transform shadowTransform)
        {
            body = bodyTransform;
            shadow = shadowTransform;
        }

        /// <summary>
        /// Place readable pips on all six faces. Self-consistent with the face-up rotation table:
        /// value v's pips go on the face whose local normal rotates to +Y under FaceUpEuler[v-1],
        /// so when the die settles to show v, that face points at the camera (no manual pairing).
        /// </summary>
        public void BuildPips(Color pipColor)
        {
            if (body == null) return;
            var pipMat = MaterialUtil.Unlit(pipColor);
            for (int v = 1; v <= 6; v++)
            {
                Vector3 n = (Quaternion.Inverse(Quaternion.Euler(FaceUpEuler[v - 1])) * Vector3.up).normalized;
                Vector3 right = Vector3.Cross(n, Vector3.up);
                if (right.sqrMagnitude < 0.001f) right = Vector3.Cross(n, Vector3.forward);
                right.Normalize();
                Vector3 up = Vector3.Cross(right, n).normalized;
                Vector3 faceCenter = n * 0.5f;

                foreach (var p in PipPattern(v))
                {
                    var pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    var col = pip.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    pip.transform.SetParent(body, false);
                    pip.transform.localPosition = faceCenter + (right * p.x + up * p.y) * 0.26f + n * 0.04f;
                    pip.transform.localScale = Vector3.one * 0.16f;
                    var r = pip.GetComponent<Renderer>();
                    if (r != null) r.sharedMaterial = pipMat;
                }
            }
        }

        // 3x3 grid offsets (-1/0/1) per pip count — standard die faces.
        private static Vector2[] PipPattern(int v) => v switch
        {
            1 => new[] { new Vector2(0, 0) },
            2 => new[] { new Vector2(-1, 1), new Vector2(1, -1) },
            3 => new[] { new Vector2(-1, 1), new Vector2(0, 0), new Vector2(1, -1) },
            4 => new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) },
            5 => new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-1, -1), new Vector2(1, -1) },
            6 => new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, 0), new Vector2(1, 0), new Vector2(-1, -1), new Vector2(1, -1) },
            _ => new[] { new Vector2(0, 0) },
        };

        /// <summary>
        /// Roll the die IN from an off-screen start to its resting slot, tumbling on the way, then
        /// settle showing <paramref name="face"/>. The die root translates (start → end) while the
        /// body spins; <paramref name="linger"/> stretches the final settle for tension.
        /// Positions are in the parent anchor's local space.
        /// </summary>
        public IEnumerator RollInto(Vector3 startLocal, Vector3 endLocal, int face, float travel, bool linger)
        {
            Face = Mathf.Clamp(face, 1, 6);
            transform.localPosition = startLocal;
            var spinAxis = Random.onUnitSphere;
            float spinSpeed = Random.Range(900f, 1500f);

            float t = 0f;
            while (t < travel)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / travel);
                float ease = 1f - Mathf.Pow(1f - k, 3f); // ease-out: fast in, decelerates into place
                Vector3 pos = Vector3.Lerp(startLocal, endLocal, ease);
                pos.y += Mathf.Sin(k * Mathf.PI) * 0.5f; // bounce arc as it skitters in
                transform.localPosition = pos;
                if (body != null) body.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);
                yield return null;
            }
            transform.localPosition = endLocal;

            // Settle to the target face.
            Quaternion target = Quaternion.Euler(FaceUpEuler[Face - 1]);
            float settle = linger ? 0.5f : 0.2f;
            float s = 0f;
            var from = body != null ? body.localRotation : Quaternion.identity;
            while (s < settle)
            {
                s += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, s / settle);
                if (body != null) body.localRotation = Quaternion.Slerp(from, target, k);
                yield return null;
            }
            if (body != null) body.localRotation = target;
        }

        public void SetTint(Color color)
        {
            if (body == null) return;
            var rend = body.GetComponent<Renderer>(); // body only — never the child pips
            if (rend != null) rend.material.color = color;
        }
    }
}
