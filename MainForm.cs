using System;
using System.Drawing;
using System.Windows.Forms;
using Gigasoft.ProEssentials;
using Gigasoft.ProEssentials.Enums;

namespace MouseInteractionHotspots
{
    /// <summary>
    /// ProEssentials WinForms — Mouse Interaction: Coordinate Tracking &amp; Hotspots  (.NET 8)
    ///
    /// Code-built WinForms port of the WPF Mouse Interaction example. No
    /// .Designer.cs / no .resx — the UI is constructed in code so the WinForms
    /// designer is never invoked.
    ///
    /// Combines two complementary mouse-interaction techniques in a single
    /// Pego dual-Y-axis chart:
    ///
    /// TECHNIQUE 1 — ConvPixelToGraph tooltip (Example 007 technique):
    ///   PeCustomTrackingDataText converts mouse pixel position to graph
    ///   coordinates via ConvPixelToGraph — called twice (left Y, right Y) to
    ///   show both axis values at the cursor, interpolated between points.
    ///
    /// TECHNIQUE 2 — GetHotSpot status bar (Example 014 technique):
    ///   MouseMove calls GetHotSpot() to identify the named element under the
    ///   cursor (data point, series legend, point label); otherwise
    ///   SearchSubsetPointIndex reports the nearest data point.
    ///
    /// The ProEssentials chart configuration (MainForm_Load) is IDENTICAL to the
    /// WPF version. Only the host shell and the WPF geometry types in the mouse
    /// handlers changed — see PORT NOTES below.
    /// </summary>
    public class MainForm : Form
    {
        // ── ProEssentials chart control (WPF PegoWpf -> WinForms Pego) ────────
        private Pego Pego1;

        // ── Status bar (WPF Border + TextBlock -> Panel + Label) ──────────────
        private Label StatusText;

        private const string DefaultStatus =
            "Move mouse over the chart — data points, series legends, and axis labels are all hot-spot enabled";

        // ── Colors — ProEssentials chart palette (UNCHANGED ARGB from WPF) ────
        // WinForms PE API uses System.Drawing.Color instead of Media.Color.
        static readonly Color CyanColor  = Color.FromArgb(255,   0, 229, 229); // #00E5E5
        static readonly Color GreenColor = Color.FromArgb(255,   0, 255,   0); // #00FF00
        static readonly Color RedColor   = Color.FromArgb(255, 255,  48,  48); // #FF3030
        static readonly Color GoldColor  = Color.FromArgb(255, 255, 210,   0); // #FFD200

        // UI theme colors (WPF Brushes -> Drawing.Color)
        static readonly Color UiDarkBg    = Color.FromArgb(0x00, 0x1A, 0x20);
        static readonly Color UiDarkPanel = Color.FromArgb(0x00, 0x2B, 0x35);
        static readonly Color UiBorder    = Color.FromArgb(0x00, 0x3D, 0x4D);
        static readonly Color UiAccent    = Color.FromArgb(0x00, 0xE5, 0xE5);

        public MainForm()
        {
            // Window props (WPF Window attrs -> Form properties)
            Text = "ProEssentials — Mouse Interaction: Coordinate Tracking & Hotspots";
            ClientSize = new Size(1100, 750);
            MinimumSize = new Size(700, 500);
            BackColor = UiDarkBg;
            StartPosition = FormStartPosition.CenterScreen;

            BuildLayout();

            // WinForms: initialize the chart in Form.Load — by then the form
            // and all child control handles exist and the native PE control is
            // ready. (This is the WinForms convention; WPF must instead use the
            // chart control's own Loaded event because the HwndHost'd native
            // window doesn't exist until the control itself loads.)
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
        }

        // =====================================================================
        // BuildLayout — replaces MainWindow.xaml
        //
        // WPF Grid (Row Auto status bar + Row * chart) ->
        //   chart docked Fill (added first), status-bar panel docked Top.
        // The status bar is a Panel with a bottom border line + a Label.
        // =====================================================================
        private void BuildLayout()
        {
            // Chart (WPF Gigasoft:PegoWpf Grid.Row=1) — wires same events as XAML
            Pego1 = new Pego();
            Pego1.Dock = DockStyle.Fill;
            Pego1.MouseMove += Pego1_MouseMove;
            Pego1.PeCustomTrackingDataText += Pego1_PeCustomTrackingDataText;
            Controls.Add(Pego1);

            // Status bar panel (WPF Border Grid.Row=0, bottom 1px border)
            var statusBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = UiDarkPanel,
                Padding = new Padding(12, 7, 12, 7)
            };
            // 1px bottom border line (WPF BorderThickness 0,0,0,1)
            statusBar.Paint += (s, e) =>
            {
                using (var pen = new Pen(UiBorder))
                    e.Graphics.DrawLine(pen, 0, statusBar.Height - 1,
                                        statusBar.Width, statusBar.Height - 1);
            };

            StatusText = new Label
            {
                Text = DefaultStatus,
                Dock = DockStyle.Fill,
                ForeColor = UiAccent,
                Font = new Font("Consolas", 9.75f),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            statusBar.Controls.Add(StatusText);

            Controls.Add(statusBar); // docked Top, above the Fill chart
        }

        // =====================================================================
        // MainForm_Load — chart initialization (WinForms Form.Load)
        // BELOW: 100% IDENTICAL to the WPF code-behind. Framework-agnostic.
        // =====================================================================
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Step 1 — Data: 4 subsets × 24 monthly points
            Pego1.PeData.Subsets = 4;
            Pego1.PeData.Points  = 24;

            var rand = new Random(42);

            for (int p = 0; p < 24; p++)
            {
                Pego1.PeData.Y[0, p] = 500f
                    + (float)(Math.Sin((p + 3) * Math.PI / 6.0) * 220f)
                    + (float)(rand.NextDouble() * 60 - 30);

                Pego1.PeData.Y[1, p] = 420f
                    + (float)(Math.Sin((p + 9) * Math.PI / 6.0) * 160f)
                    + (float)(rand.NextDouble() * 60 - 30);

                Pego1.PeData.Y[2, p] = 280f + p * 8f
                    + (float)(Math.Sin(p * Math.PI / 4.0) * 80f)
                    + (float)(rand.NextDouble() * 50 - 25);

                Pego1.PeData.Y[3, p] = 48f
                    + (float)(Math.Sin((p + 1) * Math.PI / 5.0) * 18f)
                    + (float)(rand.NextDouble() * 8 - 4);
            }

            // Step 2 — Point labels (month names)
            string[] months = {
                "Jan 23","Feb 23","Mar 23","Apr 23","May 23","Jun 23",
                "Jul 23","Aug 23","Sep 23","Oct 23","Nov 23","Dec 23",
                "Jan 24","Feb 24","Mar 24","Apr 24","May 24","Jun 24",
                "Jul 24","Aug 24","Sep 24","Oct 24","Nov 24","Dec 24"
            };
            for (int p = 0; p < 24; p++)
                Pego1.PeString.PointLabels[p] = months[p];

            // Step 3 — Subset labels
            Pego1.PeString.SubsetLabels[0] = "North";
            Pego1.PeString.SubsetLabels[1] = "South";
            Pego1.PeString.SubsetLabels[2] = "West";
            Pego1.PeString.SubsetLabels[3] = "Market Share";

            // Step 4 — Dual Y-axis
            Pego1.PePlot.RYAxisComparisonSubsets = 1;
            Pego1.PePlot.Method                  = GraphPlottingMethod.PointsPlusSpline;
            Pego1.PePlot.MethodII                = GraphPlottingMethodII.Line;

            Pego1.PeString.YAxisLabel  = "Units Sold";
            Pego1.PeString.RYAxisLabel = "Market Share (%)";
            Pego1.PeString.XAxisLabel  = "Month";

            // Step 5 — Colors
            Pego1.PeColor.SubsetColors[0] = CyanColor;
            Pego1.PeColor.SubsetColors[1] = GreenColor;
            Pego1.PeColor.SubsetColors[2] = RedColor;
            Pego1.PeColor.SubsetColors[3] = GoldColor;

            Pego1.PeColor.RYAxis = GoldColor;

            // Step 6 — Hotspot configuration (Example 014 technique)
            Pego1.PeUserInterface.HotSpot.Data   = true;
            Pego1.PeUserInterface.HotSpot.Subset = true;
            Pego1.PeUserInterface.HotSpot.Point  = true;
            Pego1.PeUserInterface.HotSpot.Size   = HotSpotSize.Large;

            Pego1.PeUserInterface.Cursor.Mode                         = CursorMode.DataCross;
            Pego1.PeUserInterface.Cursor.MouseCursorControl             = true;
            Pego1.PeUserInterface.Cursor.MouseCursorControlClosestPoint = true;
            Pego1.PeUserInterface.Cursor.Hand = (int)System.Windows.Forms.Cursors.Arrow.Handle;

            // Step 7 — Tooltip configuration (Example 007 technique)
            Pego1.PeUserInterface.Cursor.PromptTracking          = true;
            Pego1.PeUserInterface.Cursor.PromptLocation          = CursorPromptLocation.ToolTip;
            Pego1.PeUserInterface.Cursor.PromptStyle             = CursorPromptStyle.XYValues;
            Pego1.PeUserInterface.Cursor.TrackingCustomDataText  = true;
            Pego1.PeUserInterface.Cursor.TrackingTooltipMaxWidth = 260;

            Pego1.PePlot.MarkDataPoints = true;

            // Step 8 — Zoom
            Pego1.PeUserInterface.Allow.Zooming   = AllowZooming.HorzAndVert;
            Pego1.PeUserInterface.Allow.ZoomStyle = ZoomStyle.Ro2Not;

            Pego1.PePlot.Option.MinimumPointSize = MinimumPointSize.Small;
            Pego1.PePlot.Option.MaximumPointSize = MinimumPointSize.Large;

            // Step 9 — Style
            Pego1.PeColor.BitmapGradientMode = true;
            Pego1.PeColor.QuickStyle         = QuickStyle.DarkNoBorder;
            Pego1.PeConfigure.BorderTypes    = TABorder.DropShadow;

            Pego1.PeGrid.InFront     = true;
            Pego1.PeGrid.LineControl = GridLineControl.Both;
            Pego1.PeGrid.Style       = GridStyle.Dot;
            Pego1.PePlot.DataShadows = DataShadows.Shadows;

            // Step 10 — Titles
            Pego1.PeString.MainTitle = "Regional Sales Performance";
            Pego1.PeString.SubTitle  = "Tooltip: both Y values at cursor   \u00b7   Status bar: named element under cursor";

            Pego1.PeFont.FontSize       = Gigasoft.ProEssentials.Enums.FontSize.Large;
            Pego1.PeFont.Fixed          = true;
            Pego1.PeFont.MainTitle.Bold = true;

            // Step 11 — Rendering quality
            Pego1.PeConfigure.AntiAliasGraphics = true;
            Pego1.PeConfigure.RenderEngine      = RenderEngine.Direct2D;
            Pego1.PeConfigure.ImageAdjustLeft   = 25;

            // Step 12 — ReinitializeResetImage (final step)
            Pego1.PeFunction.ReinitializeResetImage();
            Pego1.Invalidate();
        }

        // =====================================================================
        // Pego1_MouseMove — GetHotSpot status bar (Example 014 technique)
        //
        // PORT NOTE: WPF MouseEventArgs (System.Windows.Input) ->
        //   WinForms MouseEventArgs (System.Windows.Forms). Same handler name;
        //   resolves to the WinForms type because we don't import Windows.Input.
        //
        // PORT NOTE: WPF used System.Windows.Point for LastMouseMove. On the
        //   WinForms PE build, Cursor.LastMouseMove returns System.Drawing.Point
        //   (integer X/Y), so the (int) casts the WPF code needed are gone.
        //   GetHotSpot / SearchSubsetPointIndex / Closest* are chart-object
        //   members — UNCHANGED.
        // =====================================================================
        private void Pego1_MouseMove(object sender, MouseEventArgs e)
        {
            Gigasoft.ProEssentials.Structs.HotSpotData ds = Pego1.PeFunction.GetHotSpot();

            if (ds.Type == HotSpotType.DataPoint)
            {
                float val    = Pego1.PeData.Y[ds.Data1, ds.Data2];
                string label = Pego1.PeString.SubsetLabels[ds.Data1];
                string month = Pego1.PeString.PointLabels[ds.Data2];
                string axis  = ds.Data1 == 3 ? "RY" : "LY";
                StatusText.Text = $"Data point  \u00b7  {label}  \u00b7  {month}  \u00b7  {axis}: {val:0.0}";
            }
            else if (ds.Type == HotSpotType.Subset)
            {
                StatusText.Text = $"Series legend  \u00b7  {Pego1.PeString.SubsetLabels[ds.Data1]}";
            }
            else if (ds.Type == HotSpotType.Point)
            {
                StatusText.Text = $"Point label  \u00b7  {Pego1.PeString.PointLabels[ds.Data1]}";
            }
            else
            {
                // Not over a named element — find the nearest data point.
                // WinForms: LastMouseMove is already integer-valued, and
                // SearchSubsetPointIndex returns System.Drawing.Point on the
                // WinForms build (WPF returned Gigasoft.ProEssentials.Structs.Point).
                System.Drawing.Point nResult =
                    Pego1.PeFunction.SearchSubsetPointIndex(
                        Pego1.PeUserInterface.Cursor.LastMouseMove.X,
                        Pego1.PeUserInterface.Cursor.LastMouseMove.Y);

                if (nResult.X >= 0)
                {
                    int s     = Pego1.PeFunction.ClosestSubsetIndex;
                    int p     = Pego1.PeFunction.ClosestPointIndex;
                    float val = Pego1.PeData.Y[s, p];
                    StatusText.Text = $"Nearest  \u00b7  {Pego1.PeString.SubsetLabels[s]}"
                                    + $"  \u00b7  {Pego1.PeString.PointLabels[p]}  \u00b7  {val:0.0}";
                }
                else
                {
                    StatusText.Text = DefaultStatus;
                }
            }
        }

        // =====================================================================
        // Pego1_PeCustomTrackingDataText — ConvPixelToGraph tooltip (Ex. 007)
        //
        // PORT NOTE: this is a ProEssentials chart event — the event args type
        //   (CustomTrackingDataTextEventArgs) and e.TrackingText are UNCHANGED.
        //
        // PORT NOTE: WPF used System.Windows.Point (LastMouseMove) and
        //   System.Windows.Rect (GetRectGraph) with r.Contains(pt). On the
        //   WinForms PE build these return System.Drawing.Point and
        //   System.Drawing.Rectangle; Rectangle.Contains(Point) exists, and the
        //   coordinates are already integers (no (int) casts needed).
        //   ConvPixelToGraph signature and the ref-parameter pattern are
        //   UNCHANGED.
        // =====================================================================
        private void Pego1_PeCustomTrackingDataText(object sender,
            Gigasoft.ProEssentials.EventArg.CustomTrackingDataTextEventArgs e)
        {
            if (Pego1.PeUserInterface.Cursor.TrackingPromptTrigger == TrackingTrigger.MouseMove)
            {
                System.Drawing.Point     pt = Pego1.PeUserInterface.Cursor.LastMouseMove;
                System.Drawing.Rectangle r  = Pego1.PeFunction.GetRectGraph();

                if (!r.Contains(pt))
                    return;

                int    nA = 0;
                int    nX = pt.X;
                int    nY = pt.Y;
                double fX = 0, fLY = 0, fRY = 0;

                // Left Y axis value at cursor position
                Pego1.PeFunction.ConvPixelToGraph(ref nA, ref nX, ref nY, ref fX, ref fLY,
                                                   false, false, false);

                // Right Y axis value — reset pixel coords, call again rightAxis=true
                nX = pt.X;
                nY = pt.Y;
                nA = 0;
                Pego1.PeFunction.ConvPixelToGraph(ref nA, ref nX, ref nY, ref fX, ref fRY,
                                                   true, false, false);

                e.TrackingText = $"Left Y   \u2190  {fLY:0.0}  (units)\n"
                               + $"{fRY:0.0}  (%)  \u2192  Right Y";
            }
            else
            {
                int s   = Pego1.PeUserInterface.Cursor.Subset;
                int p   = Pego1.PeUserInterface.Cursor.Point;
                float v = Pego1.PeData.Y[s, p];
                string axis = s == 3 ? "Right Y" : "Left Y";
                e.TrackingText = $"{Pego1.PeString.SubsetLabels[s]}\n"
                               + $"{Pego1.PeString.PointLabels[p]}  \u00b7  {axis}: {v:0.0}";
            }
        }

        // WinForms Form.FormClosing (WPF Window.Closing -> CancelEventArgs)
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
        }
    }
}
