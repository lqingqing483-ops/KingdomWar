using UnityEngine;
using System.Collections;

namespace KingdomWar.Game.Battle
{
    /// <summary>
    /// Shows spell targeting visual (radius ring + impact flash).
    /// Created by Spell.cs when a spell is cast.
    /// </summary>
    public class SpellVisualEffect : MonoBehaviour
    {
        public static void ShowRadiusIndicator(Vector3 position, float radius, Color color, float duration = 0.5f)
        {
            GameObject go = new GameObject("SpellRadius");
            go.transform.position = position;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0f);
            lr.startWidth = 0.1f;
            lr.endWidth = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.positionCount = 36;

            // Draw circle
            Vector3[] points = new Vector3[36];
            for (int i = 0; i < 36; i++)
            {
                float angle = (float)i / 36f * Mathf.PI * 2f;
                points[i] = new Vector3(Mathf.Sin(angle) * radius, 0.1f, Mathf.Cos(angle) * radius);
            }
            lr.SetPositions(points);

            // Auto-destroy after duration
            GameObject.Destroy(go, duration);
        }

        public static void ShowImpactFlash(Vector3 position, float radius, Color color, float duration = 0.3f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SpellImpact";
            go.transform.position = position + Vector3.up * 0.1f;
            go.transform.localScale = Vector3.one * radius * 0.5f;

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = color;
            renderer.material.SetFloat("_Glossiness", 0f);

            GameObject.Destroy(go, duration);
        }
    }
}
