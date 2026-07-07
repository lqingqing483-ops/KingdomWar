using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomWar.UI
{
    /// <summary>
    /// Simple vertex gradient for UI Images/Text.
    /// Replaces the need for UIExtensions UILinearGradient.
    /// Adds a second color that fades horizontally or vertically across the element.
    /// </summary>
    [AddComponentMenu("UI/Effects/UIGradient")]
    public class UIGradient : BaseMeshEffect
    {
        public Color color2 = Color.white;
        public bool horizontal = false;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            var verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);

            int count = verts.Count;
            if (count == 0) return;

            // Find bounds
            float minX = verts[0].position.x, maxX = verts[0].position.x;
            float minY = verts[0].position.y, maxY = verts[0].position.y;
            for (int i = 1; i < count; i++)
            {
                float x = verts[i].position.x, y = verts[i].position.y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            float width = maxX - minX;
            float height = maxY - minY;

            // Apply gradient to each vertex
            for (int i = 0; i < count; i++)
            {
                UIVertex v = verts[i];
                float t = horizontal
                    ? (width > 0f ? (v.position.x - minX) / width : 0f)
                    : (height > 0f ? (v.position.y - minY) / height : 0f);
                v.color = Color.Lerp(v.color, color2, t);
                verts[i] = v;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }
    }
}
