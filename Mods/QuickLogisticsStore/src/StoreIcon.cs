using UnityEngine;
using UnityEngine.UI;

namespace SanCun.QuickLogisticsStore
{
    // 收纳托盘和向下箭头直接用 UI 几何绘制，随游戏界面缩放，不引入低分辨率位图。
    public sealed class StoreIcon : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect area = GetPixelAdjustedRect();
            AddQuad(vertices, area, 1f, 1f, 15f, 2.4f);
            AddQuad(vertices, area, 1f, 1f, 2.4f, 6.8f);
            AddQuad(vertices, area, 13.6f, 1f, 15f, 6.8f);
            AddQuad(vertices, area, 7.2f, 7.5f, 8.8f, 15f);
            int start = vertices.currentVertCount;
            AddVertex(vertices, area, 4.1f, 9.2f);
            AddVertex(vertices, area, 8f, 5.2f);
            AddVertex(vertices, area, 11.9f, 9.2f);
            vertices.AddTriangle(start, start + 1, start + 2);
        }

        private void AddQuad(VertexHelper vertices, Rect area, float x0, float y0, float x1, float y1)
        {
            int start = vertices.currentVertCount;
            AddVertex(vertices, area, x0, y0);
            AddVertex(vertices, area, x0, y1);
            AddVertex(vertices, area, x1, y1);
            AddVertex(vertices, area, x1, y0);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start, start + 2, start + 3);
        }

        private void AddVertex(VertexHelper vertices, Rect area, float x, float y)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(area.xMin + x / 16f * area.width, area.yMin + y / 16f * area.height, 0f);
            vertex.color = color;
            vertices.AddVert(vertex);
        }
    }
}
