using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ParallelKosaraju.Core;

namespace ParallelKosaraju.UI
{
    /// <summary>
    /// Custom panel that renders a directed graph with SCC colouring.
    /// Supports up to ~500 vertices in the visual mode; larger graphs
    /// display a condensation/stats view.
    /// </summary>
    public sealed class GraphCanvas : Panel
    {
        private DirectedGraph? _graph;
        private int[]?         _comp;          // SCC labels per vertex
        private PointF[]?      _positions;     // layout positions
        private int            _hoveredVertex  = -1;
        private int            _highlightedScc = -1;

        // Highlight state for step-by-step
        private readonly HashSet<int> _visitedV   = new();
        private readonly HashSet<int> _finishedV  = new();
        private int                    _activeV    = -1;

        private static readonly Color[] SccPalette = GeneratePalette(32);
        private const int  VertexRadius     = 10;
        private const int  MaxVisualVertices = 500;

        public bool ShowLabels { get; set; } = true;

        public GraphCanvas()
        {
            DoubleBuffered = true;
            ResizeRedraw   = true;
            BackColor      = Color.FromArgb(18, 18, 28);
            MouseMove     += OnMouseMove;
            MouseLeave    += (_, _) => { _hoveredVertex = -1; Invalidate(); };
        }

        public void SetGraph(DirectedGraph g, int[]? comp = null)
        {
            _graph = g;
            _comp  = comp;
            _visitedV.Clear();
            _finishedV.Clear();
            _activeV = -1;
            _highlightedScc = -1;

            if (g.VertexCount <= MaxVisualVertices)
                _positions = ComputeLayout(g);
            else
                _positions = null;

            Invalidate();
        }

        public void UpdateScc(int[]? comp)
        {
            _comp = comp;
            Invalidate();
        }

        public void MarkVisited(int v)   { _visitedV.Add(v);  _activeV = v; Invalidate(); }
        public void MarkFinished(int v)  { _finishedV.Add(v); _activeV = v; Invalidate(); }
        public void SetActive(int v)     { _activeV = v;       Invalidate(); }

        public void HighlightScc(int sccId)
        {
            _highlightedScc = sccId;
            Invalidate();
        }

        public void ResetHighlight()
        {
            _visitedV.Clear();
            _finishedV.Clear();
            _activeV = -1;
            _highlightedScc = -1;
            Invalidate();
        }

        // ── Layout ──────────────────────────────────────────────────────────
        private static PointF[] ComputeLayout(DirectedGraph g)
        {
            int n = g.VertexCount;
            var pos = new PointF[n];

            if (n == 0) return pos;

            // For small graphs: use Fruchterman–Reingold force-directed
            // For medium: use circular layout (faster, clear)
            if (n <= 30)
                return ForceLayout(g);

            // Circular
            double angleStep = 2 * Math.PI / n;
            double r = Math.Min(400, 50 + n * 5);
            float cx = 500, cy = 500;
            for (int i = 0; i < n; i++)
            {
                double a = i * angleStep - Math.PI / 2;
                pos[i] = new PointF(cx + (float)(r * Math.Cos(a)),
                                    cy + (float)(r * Math.Sin(a)));
            }
            return pos;
        }

        private static PointF[] ForceLayout(DirectedGraph g, int iter = 200)
        {
            int n = g.VertexCount;
            var pos = new PointF[n];
            var vel = new PointF[n];
            var rng = new Random(42);

            for (int i = 0; i < n; i++)
                pos[i] = new PointF((float)(rng.NextDouble() * 800),
                                    (float)(rng.NextDouble() * 800));

            float area = 800 * 800;
            float k    = (float)Math.Sqrt(area / Math.Max(n, 1));

            for (int it = 0; it < iter; it++)
            {
                float temp = 100f * (1f - (float)it / iter);
                var disp = new PointF[n];

                // Repulsion
                for (int u = 0; u < n; u++)
                    for (int v = 0; v < n; v++)
                    {
                        if (u == v) continue;
                        float dx = pos[u].X - pos[v].X;
                        float dy = pos[u].Y - pos[v].Y;
                        float d  = MathF.Sqrt(dx * dx + dy * dy);
                        if (d < 0.01f) d = 0.01f;
                        float f  = (k * k) / d;
                        disp[u].X += dx / d * f;
                        disp[u].Y += dy / d * f;
                    }

                // Attraction
                foreach (var (from, to) in g.Edges())
                {
                    float dx = pos[from].X - pos[to].X;
                    float dy = pos[from].Y - pos[to].Y;
                    float d  = MathF.Sqrt(dx * dx + dy * dy);
                    if (d < 0.01f) d = 0.01f;
                    float f  = (d * d) / k;
                    disp[from].X -= dx / d * f;
                    disp[from].Y -= dy / d * f;
                    disp[to].X   += dx / d * f;
                    disp[to].Y   += dy / d * f;
                }

                for (int u = 0; u < n; u++)
                {
                    float dn = MathF.Sqrt(disp[u].X * disp[u].X + disp[u].Y * disp[u].Y);
                    if (dn < 0.01f) continue;
                    float scale = Math.Min(dn, temp) / dn;
                    pos[u].X = Math.Clamp(pos[u].X + disp[u].X * scale, 50, 750);
                    pos[u].Y = Math.Clamp(pos[u].Y + disp[u].Y * scale, 50, 750);
                }
            }
            return pos;
        }

        // ── Painting ────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g   = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (_graph == null)
            {
                DrawPlaceholder(g);
                return;
            }

            if (_positions == null || _graph.VertexCount > MaxVisualVertices)
            {
                DrawLargeGraphStats(g, _graph);
                return;
            }

            // Scale layout to current panel size
            float margin   = 40;
            float scaleX   = (Width  - margin * 2) / 1000f;
            float scaleY   = (Height - margin * 2) / 1000f;
            float scale    = Math.Min(scaleX, scaleY);

            PointF Transform(PointF p) => new(margin + p.X * scale, margin + p.Y * scale);

            var pos = _positions;
            int n   = _graph.VertexCount;

            // Draw edges
            using var edgePen = new Pen(Color.FromArgb(80, 180, 180, 220), 1f)
            {
                CustomEndCap = new AdjustableArrowCap(4, 4)
            };
            using var sccEdgePen = new Pen(Color.FromArgb(160, 80, 220, 140), 1.5f)
            {
                CustomEndCap = new AdjustableArrowCap(4, 4)
            };

            foreach (var (from, to) in _graph.Edges())
            {
                bool sameScc = _comp != null && _comp[from] == _comp[to];
                var pen = sameScc ? sccEdgePen : edgePen;
                DrawArrow(g, pen, Transform(pos[from]), Transform(pos[to]), VertexRadius);
            }

            // Draw vertices
            for (int v = 0; v < n; v++)
            {
                var tp = Transform(pos[v]);
                float r = VertexRadius;

                Color fill = GetVertexColor(v);
                float glow = (v == _activeV) ? 2.5f : 0;
                if (glow > 0)
                {
                    using var glowBrush = new SolidBrush(Color.FromArgb(60, fill));
                    g.FillEllipse(glowBrush,
                        tp.X - r - 6, tp.Y - r - 6, (r + 6) * 2, (r + 6) * 2);
                }

                using var fillBrush = new SolidBrush(fill);
                g.FillEllipse(fillBrush, tp.X - r, tp.Y - r, r * 2, r * 2);

                var borderColor = (v == _hoveredVertex) ? Color.White :
                                  _finishedV.Contains(v) ? Color.Gold  : Color.FromArgb(100, 200, 200, 255);
                using var borderPen = new Pen(borderColor, v == _activeV ? 2f : 1f);
                g.DrawEllipse(borderPen, tp.X - r, tp.Y - r, r * 2, r * 2);

                if (ShowLabels && n <= 60)
                {
                    using var lbl = new SolidBrush(Color.White);
                    g.DrawString(v.ToString(), Font, lbl,
                        tp.X - 4, tp.Y - 6, StringFormat.GenericDefault);
                }
            }

            // Legend
            DrawLegend(g);
        }

        private Color GetVertexColor(int v)
        {
            if (v == _activeV) return Color.FromArgb(255, 220, 50);
            if (_comp != null)
            {
                int scc = _comp[v];
                if (_highlightedScc >= 0 && scc != _highlightedScc)
                    return Color.FromArgb(40, 40, 55);
                return SccPalette[scc % SccPalette.Length];
            }
            if (_finishedV.Contains(v)) return Color.FromArgb(80, 200, 120);
            if (_visitedV.Contains(v))  return Color.FromArgb(80, 120, 220);
            return Color.FromArgb(60, 80, 140);
        }

        private static void DrawArrow(Graphics g, Pen pen, PointF from, PointF to, float radius)
        {
            float dx = to.X - from.X, dy = to.Y - from.Y;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 2) return;
            float ux = dx / len, uy = dy / len;
            var start = new PointF(from.X + ux * radius, from.Y + uy * radius);
            var end   = new PointF(to.X   - ux * radius, to.Y   - uy * radius);
            g.DrawLine(pen, start, end);
        }

        private void DrawLegend(Graphics g)
        {
            if (_comp == null) return;
            int n = _graph!.VertexCount;
            int sccCount = _comp.Max() + 1;
            string info = $"Vertices: {n}  |  Edges: {_graph.EdgeCount}  |  SCCs: {sccCount}";
            using var brush = new SolidBrush(Color.FromArgb(180, 200, 200, 220));
            g.DrawString(info, Font, brush, 10, Height - 22);
        }

        private static void DrawLargeGraphStats(Graphics g, DirectedGraph graph)
        {
            using var b = new SolidBrush(Color.FromArgb(140, 160, 200));
            g.DrawString(
                $"Graph too large for visual rendering ({graph.VertexCount:N0} vertices).\n" +
                $"Edges: {graph.EdgeCount:N0}\n\n" +
                "Run the algorithm or benchmark to see results.",
                new Font("Segoe UI", 11), b, 30, 30);
        }

        private static void DrawPlaceholder(Graphics g)
        {
            using var b = new SolidBrush(Color.FromArgb(80, 100, 140));
            g.DrawString(
                "No graph loaded.\nGenerate or load a graph to begin.",
                new Font("Segoe UI", 12), b, 30, 30);
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (_positions == null) return;
            float margin = 40;
            float scaleX = (Width  - margin * 2) / 1000f;
            float scaleY = (Height - margin * 2) / 1000f;
            float scale  = Math.Min(scaleX, scaleY);

            int prev = _hoveredVertex;
            _hoveredVertex = -1;
            for (int v = 0; v < _graph!.VertexCount; v++)
            {
                float px = margin + _positions[v].X * scale;
                float py = margin + _positions[v].Y * scale;
                float dx = e.X - px, dy = e.Y - py;
                if (dx * dx + dy * dy <= (VertexRadius + 3) * (VertexRadius + 3))
                { _hoveredVertex = v; break; }
            }
            if (_hoveredVertex != prev) Invalidate();
        }

        private static Color[] GeneratePalette(int count)
        {
            var pal = new Color[count];
            for (int i = 0; i < count; i++)
            {
                double h = (double)i / count;
                pal[i] = HsvToRgb(h, 0.65, 0.85);
            }
            return pal;
        }

        private static Color HsvToRgb(double h, double s, double v)
        {
            double r, gr, b;
            int hi = (int)(h * 6) % 6;
            double f = h * 6 - Math.Floor(h * 6);
            double p = v * (1 - s), q = v * (1 - f * s), t = v * (1 - (1 - f) * s);
            (r, gr, b) = hi switch
            {
                0 => (v, t, p), 1 => (q, v, p), 2 => (p, v, t),
                3 => (p, q, v), 4 => (t, p, v), _ => (v, p, q)
            };
            return Color.FromArgb((int)(r * 255), (int)(gr * 255), (int)(b * 255));
        }
    }
}
