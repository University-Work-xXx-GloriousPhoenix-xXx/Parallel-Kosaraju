using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ParallelKosaraju.Algorithms;
using ParallelKosaraju.Benchmark;
using ParallelKosaraju.Core;
using ParallelKosaraju.Utils;

namespace ParallelKosaraju.UI
{
    public sealed class MainForm : Form
    {
        // ── Controls ────────────────────────────────────────────────────────
        private TabControl        _tabs        = null!;
        private TabPage           _tabGraph    = null!;
        private TabPage           _tabStep     = null!;
        private TabPage           _tabBench    = null!;

        // Graph tab
        private GraphCanvas       _canvas      = null!;
        private ComboBox          _cmbModel    = null!;
        private NumericUpDown     _nudN        = null!;
        private NumericUpDown     _nudParam    = null!;
        private Button            _btnGen      = null!;
        private Button            _btnRunSeq   = null!;
        private Button            _btnRunPar   = null!;
        private Label             _lblStatus   = null!;
        private RichTextBox       _rtbInfo     = null!;
        private CheckBox          _chkLabels   = null!;

        // Step tab
        private GraphCanvas       _stepCanvas  = null!;
        private RichTextBox       _rtbStepLog  = null!;
        private Button            _btnStepGen  = null!;
        private Button            _btnStepRun  = null!;
        private Button            _btnStepNext = null!;
        private Button            _btnStepAll  = null!;
        private Label             _lblStep     = null!;
        private NumericUpDown     _nudStepN    = null!;
        private NumericUpDown     _nudStepDelay= null!;
        private List<StepEvent>   _stepEvents  = new();
        private int               _stepIdx     = 0;
        private int[]?            _stepComp    = null;
        private DirectedGraph?    _stepGraph   = null;
        private CancellationTokenSource? _stepCts = null;

        // Bench tab
        private DataGridView      _dgvBench    = null!;
        private Button            _btnBench    = null!;
        private RichTextBox       _rtbBenchLog = null!;
        private ProgressBar       _pbBench     = null!;

        private DirectedGraph?    _graph;

        public MainForm()
        {
            InitializeComponent();
        }

        // ── UI Construction ─────────────────────────────────────────────────
        private void InitializeComponent()
        {
            Text            = "Parallel Kosaraju — SCC Finder";
            Size            = new Size(1280, 820);
            MinimumSize     = new Size(960, 640);
            BackColor       = Color.FromArgb(22, 22, 35);
            ForeColor       = Color.FromArgb(210, 215, 235);
            Font            = new Font("Segoe UI", 9f);
            StartPosition   = FormStartPosition.CenterScreen;
            Icon            = SystemIcons.Application;

            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                ItemSize = new Size(130, 32),
                Padding  = new Point(12, 6),
            };
            _tabs.DrawItem += DrawTabItem;

            _tabGraph = new TabPage("  Graph & Run");
            _tabStep  = new TabPage("  Step-by-Step");
            _tabBench = new TabPage("  Benchmark");

            _tabs.TabPages.AddRange(new[] { _tabGraph, _tabStep, _tabBench });
            Controls.Add(_tabs);

            BuildGraphTab();
            BuildStepTab();
            BuildBenchTab();

            StyleTabs();
        }

        private void StyleTabs()
        {
            foreach (TabPage tp in _tabs.TabPages)
            {
                tp.BackColor = Color.FromArgb(22, 22, 35);
                tp.ForeColor = Color.FromArgb(210, 215, 235);
            }
        }

        private void DrawTabItem(object? sender, DrawItemEventArgs e)
        {
            var tab  = (TabControl)sender!;
            bool sel = e.Index == tab.SelectedIndex;
            var bg   = sel ? Color.FromArgb(40, 40, 60) : Color.FromArgb(28, 28, 44);
            var fg   = sel ? Color.FromArgb(130, 200, 255) : Color.FromArgb(160, 170, 190);
            using var bb = new SolidBrush(bg);
            e.Graphics.FillRectangle(bb, e.Bounds);
            using var fb = new SolidBrush(fg);
            e.Graphics.DrawString(tab.TabPages[e.Index].Text,
                new Font("Segoe UI", 9f, sel ? FontStyle.Bold : FontStyle.Regular),
                fb, e.Bounds.X + 8, e.Bounds.Y + 8);
        }

        // ── Graph Tab ───────────────────────────────────────────────────────
        private void BuildGraphTab()
        {
            var split = new SplitContainer
            {
                Dock        = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 820,
                Panel1MinSize    = 400,
                Panel2MinSize    = 280,
                BackColor   = Color.FromArgb(22, 22, 35),
            };
            _tabGraph.Controls.Add(split);

            // Left: canvas
            _canvas = new GraphCanvas { Dock = DockStyle.Fill };
            _chkLabels = MakeCheckBox("Show labels", 0, 0);
            _chkLabels.Checked = true;
            _chkLabels.CheckedChanged += (_, _) =>
            {
                _canvas.ShowLabels = _chkLabels.Checked;
                _canvas.Invalidate();
            };
            var canvasHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 18, 28) };
            canvasHost.Controls.Add(_canvas);
            split.Panel1.Controls.Add(canvasHost);

            // Right: controls panel
            var right = new FlowLayoutPanel
            {
                Dock      = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding   = new Padding(12, 10, 8, 8),
                AutoScroll= true,
                BackColor = Color.FromArgb(26, 26, 40),
            };
            split.Panel2.Controls.Add(right);

            right.Controls.Add(MakeLabel("Graph Generation", bold: true, large: true));
            right.Controls.Add(MakeLabel("Model:"));
            _cmbModel = MakeCombo(Enum.GetNames<GraphModel>());
            right.Controls.Add(_cmbModel);

            right.Controls.Add(MakeLabel("Vertices (n):"));
            _nudN = MakeNud(100, 1, 1_000_000, 100);
            right.Controls.Add(_nudN);

            right.Controls.Add(MakeLabel("Param (p / k / m):"));
            _nudParam = MakeNud(10, 1, 100000, 10, 2);
            right.Controls.Add(_nudParam);

            right.Controls.Add(MakeLabel(" "));
            _btnGen = MakeButton("⚡  Generate Graph", Color.FromArgb(50, 100, 200));
            _btnGen.Click += OnGenerate;
            right.Controls.Add(_btnGen);
            right.Controls.Add(_chkLabels);

            right.Controls.Add(MakeLabel(" "));
            right.Controls.Add(MakeLabel("Run Algorithm", bold: true, large: true));
            _btnRunSeq = MakeButton("▶  Sequential Kosaraju", Color.FromArgb(30, 140, 80));
            _btnRunSeq.Click += OnRunSequential;
            right.Controls.Add(_btnRunSeq);

            _btnRunPar = MakeButton("▶▶  Parallel Kosaraju", Color.FromArgb(140, 60, 160));
            _btnRunPar.Click += OnRunParallel;
            right.Controls.Add(_btnRunPar);

            right.Controls.Add(MakeLabel(" "));
            _lblStatus = MakeLabel("Ready.");
            right.Controls.Add(_lblStatus);

            _rtbInfo = new RichTextBox
            {
                Dock       = DockStyle.None,
                Width      = 260,
                Height     = 220,
                ReadOnly   = true,
                BackColor  = Color.FromArgb(16, 16, 26),
                ForeColor  = Color.FromArgb(180, 220, 200),
                Font       = new Font("Consolas", 8.5f),
                BorderStyle= BorderStyle.None,
                Margin     = new Padding(0, 4, 0, 0),
            };
            right.Controls.Add(_rtbInfo);
        }

        // ── Step Tab ─────────────────────────────────────────────────────────
        private void BuildStepTab()
        {
            var split = new SplitContainer
            {
                Dock        = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 760,
                Panel1MinSize    = 400,
                Panel2MinSize    = 260,
                BackColor   = Color.FromArgb(22, 22, 35),
            };
            _tabStep.Controls.Add(split);

            _stepCanvas = new GraphCanvas { Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(_stepCanvas);

            var right = new FlowLayoutPanel
            {
                Dock      = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding   = new Padding(12, 10, 8, 8),
                AutoScroll= true,
                BackColor = Color.FromArgb(26, 26, 40),
            };
            split.Panel2.Controls.Add(right);

            right.Controls.Add(MakeLabel("Step-by-Step Demo", bold: true, large: true));
            right.Controls.Add(MakeLabel("Vertices (n ≤ 30 recommended):"));
            _nudStepN = MakeNud(12, 3, 80, 1);
            right.Controls.Add(_nudStepN);

            right.Controls.Add(MakeLabel("Animation delay (ms):"));
            _nudStepDelay = MakeNud(400, 50, 5000, 50);
            right.Controls.Add(_nudStepDelay);

            _btnStepGen = MakeButton("⚡  Generate Small Graph", Color.FromArgb(50, 100, 200));
            _btnStepGen.Click += OnStepGenerate;
            right.Controls.Add(_btnStepGen);

            _btnStepRun = MakeButton("▶  Start Step Demo", Color.FromArgb(30, 140, 80));
            _btnStepRun.Click += OnStepStart;
            right.Controls.Add(_btnStepRun);

            var btnRow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
            };
            _btnStepNext = MakeButton("→ Next", Color.FromArgb(80, 80, 130), 90);
            _btnStepNext.Click += OnStepNext;
            _btnStepAll  = MakeButton("▶▶ Auto", Color.FromArgb(110, 80, 30), 90);
            _btnStepAll.Click  += OnStepAuto;
            btnRow.Controls.Add(_btnStepNext);
            btnRow.Controls.Add(_btnStepAll);
            right.Controls.Add(btnRow);

            _lblStep = MakeLabel("—");
            _lblStep.ForeColor = Color.FromArgb(130, 200, 255);
            _lblStep.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            right.Controls.Add(_lblStep);

            _rtbStepLog = new RichTextBox
            {
                Dock       = DockStyle.None,
                Width      = 250,
                Height     = 300,
                ReadOnly   = true,
                BackColor  = Color.FromArgb(14, 14, 22),
                ForeColor  = Color.FromArgb(170, 200, 180),
                Font       = new Font("Consolas", 8f),
                BorderStyle= BorderStyle.None,
                Margin     = new Padding(0, 4, 0, 0),
            };
            right.Controls.Add(_rtbStepLog);

            _btnStepNext.Enabled = false;
            _btnStepAll.Enabled  = false;
        }

        // ── Benchmark Tab ────────────────────────────────────────────────────
        private void BuildBenchTab()
        {
            var outer = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                BackColor   = Color.FromArgb(22, 22, 35),
                Padding     = new Padding(12),
            };
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _tabBench.Controls.Add(outer);

            // Top row: button + progress
            var topRow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
            };
            _btnBench = MakeButton("🚀  Run Full Benchmark Suite", Color.FromArgb(160, 60, 50), 240);
            _btnBench.Click += OnBenchmark;
            topRow.Controls.Add(_btnBench);
            _pbBench = new ProgressBar
            {
                Width  = 300,
                Height = 28,
                Style  = ProgressBarStyle.Marquee,
                Visible= false,
                Margin = new Padding(10, 2, 0, 0),
            };
            topRow.Controls.Add(_pbBench);
            outer.Controls.Add(topRow, 0, 0);

            // DataGrid
            _dgvBench = new DataGridView
            {
                Dock          = DockStyle.Fill,
                ReadOnly      = true,
                AllowUserToAddRows = false,
                BackgroundColor    = Color.FromArgb(16, 16, 28),
                GridColor         = Color.FromArgb(40, 40, 60),
                DefaultCellStyle  = { BackColor = Color.FromArgb(20, 20, 34),
                                      ForeColor = Color.FromArgb(210, 215, 230),
                                      SelectionBackColor = Color.FromArgb(50, 80, 150),
                                      Font = new Font("Consolas", 8.5f) },
                ColumnHeadersDefaultCellStyle = {
                    BackColor = Color.FromArgb(30, 30, 50),
                    ForeColor = Color.FromArgb(130, 200, 255),
                    Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                BorderStyle   = BorderStyle.None,
                Margin        = new Padding(0, 4, 0, 4),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Height        = 280,
                ColumnHeadersHeight = 28,
                RowHeadersVisible   = false,
            };
            outer.Controls.Add(_dgvBench, 0, 1);

            _rtbBenchLog = new RichTextBox
            {
                Dock       = DockStyle.Fill,
                ReadOnly   = true,
                BackColor  = Color.FromArgb(14, 14, 22),
                ForeColor  = Color.FromArgb(160, 190, 160),
                Font       = new Font("Consolas", 8.5f),
                BorderStyle= BorderStyle.None,
            };
            outer.Controls.Add(_rtbBenchLog, 0, 2);
        }

        // ── Event Handlers ───────────────────────────────────────────────────
        private void OnGenerate(object? sender, EventArgs e)
        {
            var model = (GraphModel)_cmbModel.SelectedIndex;
            int n     = (int)_nudN.Value;
            double p  = (double)_nudParam.Value;

            SetStatus($"Generating {model} graph, n={n:N0}...");
            _graph = null;
            Task.Run(() => GraphGenerator.Generate(model, n, p))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted) { SetStatus("Error: " + t.Exception!.Message); return; }
                    _graph = t.Result;
                    Invoke(() =>
                    {
                        _canvas.SetGraph(_graph);
                        SetStatus($"Graph ready: {_graph.VertexCount:N0} vertices, {_graph.EdgeCount:N0} edges.");
                        AppendInfo($"Generated: {model}, n={n:N0}, edges={_graph.EdgeCount:N0}\n");
                    });
                });
        }

        private void OnRunSequential(object? sender, EventArgs e)
        {
            if (_graph == null) { SetStatus("Generate a graph first."); return; }
            var g = _graph;
            SetStatus("Running Sequential Kosaraju...");
            Task.Run(() =>
            {
                var alg  = new SequentialKosaraju(g);
                var comp = alg.Compute(out var elapsed);
                return (comp, elapsed);
            }).ContinueWith(t =>
            {
                var (comp, elapsed) = t.Result;
                Invoke(() =>
                {
                    _canvas.UpdateScc(comp);
                    int scc = comp.Max() + 1;
                    string msg = $"Sequential: {elapsed.TotalMilliseconds:F2}ms | SCCs={scc}";
                    SetStatus(msg);
                    AppendInfo(msg + "\n");
                    AppendSccStats(comp);
                });
            });
        }

        private void OnRunParallel(object? sender, EventArgs e)
        {
            if (_graph == null) { SetStatus("Generate a graph first."); return; }
            var g = _graph;
            SetStatus("Running Parallel Kosaraju...");
            Task.Run(() =>
            {
                var alg  = new Algorithms.ParallelKosaraju(g);
                var comp = alg.Compute(out var elapsed);
                return (comp, elapsed);
            }).ContinueWith(t =>
            {
                var (comp, elapsed) = t.Result;
                Invoke(() =>
                {
                    _canvas.UpdateScc(comp);
                    int scc = comp.Max() + 1;
                    string msg = $"Parallel ({Environment.ProcessorCount} threads): " +
                                 $"{elapsed.TotalMilliseconds:F2}ms | SCCs={scc}";
                    SetStatus(msg);
                    AppendInfo(msg + "\n");
                    AppendSccStats(comp);
                });
            });
        }

        // ── Step-by-step ────────────────────────────────────────────────────
        private void OnStepGenerate(object? sender, EventArgs e)
        {
            int n = (int)_nudStepN.Value;
            _stepGraph  = GraphGenerator.Generate(GraphModel.RandomClusters, n, Math.Max(2, n / 5));
            _stepCanvas.SetGraph(_stepGraph);
            _stepComp   = null;
            _stepEvents.Clear();
            _stepIdx    = 0;
            _rtbStepLog.Clear();
            _lblStep.Text = "Graph generated. Click 'Start Step Demo'.";
            _btnStepNext.Enabled = false;
            _btnStepAll.Enabled  = false;
        }

        private void OnStepStart(object? sender, EventArgs e)
        {
            if (_stepGraph == null) { OnStepGenerate(null, EventArgs.Empty); }
            var g = _stepGraph!;

            _stepEvents.Clear();
            _stepIdx  = 0;
            _stepComp = null;
            _stepCanvas.ResetHighlight();
            _rtbStepLog.Clear();

            // Collect ALL step events synchronously in background
            var captured = new List<StepEvent>();
            Task.Run(() =>
            {
                var alg = new SequentialKosaraju(g);
                var progress = new Progress<StepEvent>(captured.Add);
                int[] comp = alg.Compute(out _, progress);
                return (comp, captured);
            }).ContinueWith(t =>
            {
                var (comp, events) = t.Result;
                _stepComp   = comp;
                _stepEvents = events;
                _stepIdx    = 0;
                Invoke(() =>
                {
                    _btnStepNext.Enabled = true;
                    _btnStepAll.Enabled  = true;
                    _lblStep.Text = $"Step 0 / {events.Count}  — Press → to advance";
                });
            });
        }

        private void OnStepNext(object? sender, EventArgs e) => AdvanceStep();

        private async void OnStepAuto(object? sender, EventArgs e)
        {
            _stepCts?.Cancel();
            _stepCts = new CancellationTokenSource();
            var token = _stepCts.Token;
            _btnStepAll.Enabled  = false;
            _btnStepNext.Enabled = false;

            int delay = (int)_nudStepDelay.Value;
            while (_stepIdx < _stepEvents.Count && !token.IsCancellationRequested)
            {
                AdvanceStep();
                try { await Task.Delay(delay, token); } catch { break; }
            }
            _btnStepAll.Enabled  = true;
            _btnStepNext.Enabled = true;
        }

        private void AdvanceStep()
        {
            if (_stepIdx >= _stepEvents.Count) return;
            var ev = _stepEvents[_stepIdx++];
            ApplyStepEvent(ev);
            _lblStep.Text = $"Step {_stepIdx} / {_stepEvents.Count}: {ev.Phase}";
            AppendStepLog(ev.ToString());
        }

        private void ApplyStepEvent(StepEvent ev)
        {
            switch (ev.Phase)
            {
                case AlgoPhase.Pass1Visit:
                    _stepCanvas.MarkVisited(ev.Vertex);
                    break;
                case AlgoPhase.Pass1Finish:
                    _stepCanvas.MarkFinished(ev.Vertex);
                    break;
                case AlgoPhase.Pass2Assign:
                    if (_stepIdx == _stepEvents.Count)
                        _stepCanvas.UpdateScc(_stepComp);
                    else
                        _stepCanvas.SetActive(ev.Vertex);
                    break;
                case AlgoPhase.Done:
                    _stepCanvas.UpdateScc(_stepComp);
                    break;
            }
        }

        // ── Benchmark ────────────────────────────────────────────────────────
        private async void OnBenchmark(object? sender, EventArgs e)
        {
            _btnBench.Enabled = false;
            _pbBench.Visible  = true;
            _rtbBenchLog.Clear();
            _dgvBench.Rows.Clear();
            _dgvBench.Columns.Clear();
            SetupBenchGrid();

            var progress = new Progress<string>(msg =>
            {
                _rtbBenchLog.AppendText(msg + "\n");
                _rtbBenchLog.ScrollToCaret();
            });

            var results = await Task.Run(() => BenchmarkEngine.Run(progress, warmup: true));

            foreach (var r in results)
            {
                Color rowColor = r.Speedup >= 1.5 ? Color.FromArgb(20, 80, 30) :
                                 r.Speedup >= 1.2 ? Color.FromArgb(30, 60, 20) :
                                                    Color.FromArgb(70, 20, 20);
                int rowIdx = _dgvBench.Rows.Add(
                    r.Model.ToString(),
                    $"{r.Vertices:N0}",
                    $"{r.Edges:N0}",
                    $"{r.SeqTime.TotalMilliseconds:F1}",
                    $"{r.ParTime.TotalMilliseconds:F1}",
                    $"{r.Speedup:F2}×",
                    r.SccCountSeq.ToString(),
                    r.ResultsMatch ? "✓" : "✗"
                );
                _dgvBench.Rows[rowIdx].DefaultCellStyle.BackColor = rowColor;
            }

            _pbBench.Visible  = false;
            _btnBench.Enabled = true;

            double avgSpeedup = results.Count > 0 ? results.Average(r => r.Speedup) : 0;
            _rtbBenchLog.AppendText($"\n=== Average speedup: {avgSpeedup:F2}× " +
                                    $"(target ≥1.20×) ===\n" +
                                    $"Logical CPUs: {Environment.ProcessorCount}\n");
        }

        private void SetupBenchGrid()
        {
            string[] cols = { "Model", "Vertices", "Edges", "Seq (ms)", "Par (ms)", "Speedup", "SCCs", "Match" };
            foreach (var c in cols)
                _dgvBench.Columns.Add(c.Replace(" ", "_"), c);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void SetStatus(string msg) =>
            Invoke(() => _lblStatus.Text = msg);

        private void AppendInfo(string msg) =>
            _rtbInfo.AppendText(msg);

        private void AppendSccStats(int[] comp)
        {
            var sizes = comp.GroupBy(x => x).Select(g => g.Count()).OrderByDescending(x => x).ToList();
            _rtbInfo.AppendText($"  Top SCCs: {string.Join(", ", sizes.Take(5))}" +
                                (sizes.Count > 5 ? $"... ({sizes.Count} total)" : "") + "\n");
        }

        private void AppendStepLog(string msg)
        {
            _rtbStepLog.AppendText(msg + "\n");
            _rtbStepLog.ScrollToCaret();
        }

        // ── Control factories ────────────────────────────────────────────────
        private static Label MakeLabel(string text, bool bold = false, bool large = false) =>
            new Label
            {
                Text      = text,
                AutoSize  = true,
                ForeColor = large ? Color.FromArgb(160, 200, 255) : Color.FromArgb(190, 195, 215),
                Font      = new Font("Segoe UI", large ? 10f : 8.5f,
                                     bold ? FontStyle.Bold : FontStyle.Regular),
                Margin    = new Padding(0, large ? 6 : 1, 0, 1),
            };

        private static CheckBox MakeCheckBox(string text, int x, int y) =>
            new CheckBox
            {
                Text      = text,
                AutoSize  = true,
                ForeColor = Color.FromArgb(180, 190, 210),
                BackColor = Color.Transparent,
            };

        private static ComboBox MakeCombo(string[] items)
        {
            var c = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 200,
                BackColor     = Color.FromArgb(32, 32, 52),
                ForeColor     = Color.FromArgb(200, 210, 230),
                FlatStyle     = FlatStyle.Flat,
                Margin        = new Padding(0, 2, 0, 4),
            };
            c.Items.AddRange(items);
            c.SelectedIndex = 0;
            return c;
        }

        private static NumericUpDown MakeNud(decimal val, decimal min, decimal max,
                                              decimal inc, int decimals = 0) =>
            new NumericUpDown
            {
                Value         = val,
                Minimum       = min,
                Maximum       = max,
                Increment     = inc,
                DecimalPlaces = decimals,
                Width         = 140,
                BackColor     = Color.FromArgb(32, 32, 52),
                ForeColor     = Color.FromArgb(200, 210, 230),
                Margin        = new Padding(0, 2, 0, 4),
            };

        private static Button MakeButton(string text, Color accent, int width = 200) =>
            new Button
            {
                Text      = text,
                Width     = width,
                Height    = 30,
                BackColor = accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 2, 0, 4),
                FlatAppearance = { BorderColor = Color.FromArgb(255, 255, 255, 40), BorderSize = 1 },
            };
    }
}
