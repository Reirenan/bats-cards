using UnityEngine;
using UnityEngine.UI;

namespace BatsSolitaire.Presentation
{
    // Small native vector set: identical 24-unit grid and 2-unit strokes, no font glyph dependency.
    public sealed class UiIcon : MaskableGraphic
    {
        public string Kind;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            switch (Kind)
            {
                case "play": Path(mesh, 7,4, 19,12, 7,20, 7,4); break;
                case "undo": Path(mesh, 8,5, 3,10, 8,15); Path(mesh, 3,10, 15,10, 20,14, 20,19); break;
                case "hint": Path(mesh, 8,17, 5,11, 6,6, 12,3, 18,6, 19,11, 16,17, 8,17); Path(mesh, 9,21, 15,21); break;
                case "calendar": Path(mesh, 3,6, 21,6, 21,21, 3,21, 3,6); Path(mesh, 3,11, 21,11); Path(mesh, 8,3, 8,8); Path(mesh,16,3,16,8); break;
                case "settings": Path(mesh,3,6,21,6); Path(mesh,3,12,21,12); Path(mesh,3,18,21,18); Path(mesh,8,3,8,9); Path(mesh,16,9,16,15); Path(mesh,10,15,10,21); break;
                case "gallery": Path(mesh,3,3,21,3,21,21,3,21,3,3); Path(mesh,3,12,21,12); Path(mesh,12,3,12,21); break;
                case "shuffle": Path(mesh,3,5,7,5,17,19,21,19); Path(mesh,3,19,7,19,17,5,21,5); Path(mesh,18,2,21,5,18,8); Path(mesh,18,16,21,19,18,22); break;
                case "trophy": Path(mesh,7,3,17,3,17,11,12,15,7,11,7,3); Path(mesh,7,5,3,5,3,10,7,12); Path(mesh,17,5,21,5,21,10,17,12); Path(mesh,12,15,12,21); Path(mesh,7,21,17,21); break;
                case "diamonds": Polygon(mesh, 12,2, 21,12, 12,22, 3,12); break;
                case "hearts": Disc(mesh,8,9,5); Disc(mesh,16,9,5); Polygon(mesh,3,9, 21,9, 12,22); break;
                case "clubs": Disc(mesh,12,7,5); Disc(mesh,7,13,5); Disc(mesh,17,13,5); Polygon(mesh,10,12, 14,12, 15,22, 9,22); break;
                case "spades": Polygon(mesh,12,2, 3,14, 8,18, 12,14, 16,18, 21,14); Polygon(mesh,10,13, 14,13, 15,22, 9,22); break;
                default: Path(mesh,3,5,21,5); Path(mesh,3,12,21,12); Path(mesh,3,19,21,19); break;
            }
        }

        private void Polygon(VertexHelper mesh, params float[] points)
        {
            var rect = rectTransform.rect;
            var start = mesh.currentVertCount;
            for (var i = 0; i < points.Length; i += 2)
                mesh.AddVert(new Vector2(rect.xMin + points[i] / 24 * rect.width,
                    rect.yMax - points[i + 1] / 24 * rect.height), color, Vector2.zero);
            for (var i = 1; i < points.Length / 2 - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
        }

        private void Disc(VertexHelper mesh, float x, float y, float radius)
        {
            const int segments = 16;
            var rect = rectTransform.rect;
            var center = new Vector2(rect.xMin + x / 24 * rect.width, rect.yMax - y / 24 * rect.height);
            var start = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2 / segments;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle) * radius / 24 * rect.width,
                    Mathf.Sin(angle) * radius / 24 * rect.height), color, Vector2.zero);
            }
            for (var i = 0; i < segments; i++) mesh.AddTriangle(start, start + i + 1, start + i + 2);
        }
        private void Path(VertexHelper mesh, params float[] points)
        {
            var rect = rectTransform.rect;
            for (var i = 0; i < points.Length - 2; i += 2)
            {
                var a = new Vector2(rect.xMin + points[i] / 24 * rect.width, rect.yMax - points[i+1] / 24 * rect.height);
                var b = new Vector2(rect.xMin + points[i+2] / 24 * rect.width, rect.yMax - points[i+3] / 24 * rect.height);
                var normal = new Vector2(-(b-a).y, (b-a).x).normalized * rect.width / 24;
                var start = mesh.currentVertCount;
                mesh.AddVert(a-normal, color, Vector2.zero); mesh.AddVert(a+normal,color,Vector2.zero);
                mesh.AddVert(b+normal,color,Vector2.zero); mesh.AddVert(b-normal,color,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
