using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Diagnostics;

namespace IconMaker2
{
    public partial class Form1 : Form
    {
        internal const int GridSize = 64;
        private const int PixelSize = 10;
        private readonly object _pixelLock = new();
        private Color?[,] _pixels = NewGrid();
        private Color?[,] _previewPixels = NewGrid();
        private string _selectedColor = "#FF004D";
        private bool _isEraserMode = false;
        private bool _showCoordinates = false;
        private readonly List<Color?[,]> _undoStack = new();
        private const int MaxUndoSteps = 50;
        private int _hoverX = -1;
        private int _hoverY = -1;
        private AgentHttpServer? _agentServer;

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public Form1()
        {
            InitializeComponent();
            SetupCustomUI();
            InitializePalette();
        }

        private void SetupCustomUI()
        {
            this.Text = "IconMaker2";
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;
            this.Shown += (_, _) => StartAgentServer();
            this.FormClosed += (_, _) =>
            {
                _agentServer?.Dispose();
                _agentServer = null;
            };

            canvasPanel.BackColor = Color.FromArgb(35, 35, 35);
            canvasPanel.Paint += CanvasPanel_Paint;

            foreach (var btn in new[] { btnSavePng, btnSaveIco })
            {
                StyleButton(btn,
                    Color.FromArgb(40, 75, 60),
                    Color.FromArgb(60, 115, 90),
                    Color.FromArgb(52, 98, 78),
                    Color.FromArgb(30, 58, 46));
            }

            foreach (var btn in new[] { btnLoadIcon, btnSystemIcons, btnTextInput, btnCaptureImport, btnEmojiImport })
            {
                StyleButton(btn,
                    Color.FromArgb(50, 50, 56),
                    Color.FromArgb(75, 75, 82),
                    Color.FromArgb(68, 68, 76),
                    Color.FromArgb(38, 38, 44));
            }

            btnSavePng.Click += btnSavePng_Click;
            btnSaveIco.Click += btnSaveIco_Click;
            btnLoadIcon.Click += btnLoad_Click;
            btnSystemIcons.Click += btnSystemIcons_Click;
            btnTextInput.Click += btnTextInput_Click;
            btnCaptureImport.Click += btnCaptureImport_Click;
            btnEmojiImport.Click += btnEmojiImport_Click;
            lblBlogLink.LinkClicked += lblBlogLink_LinkClicked;

            chkCoords.CheckedChanged += (s, e) =>
            {
                _showCoordinates = chkCoords.Checked;
                canvasPanel.Invalidate();
            };

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Z) PerformUndo();
            };

            canvasPanel.MouseDown += (s, e) =>
            {
                SaveUndoState();
                HandleCanvasMouse(e);
            };

            canvasPanel.MouseMove += (s, e) =>
            {
                UpdateHover(e.X, e.Y);
                if (e.Button != MouseButtons.None) HandleCanvasMouse(e);
            };

            canvasPanel.MouseLeave += (s, e) =>
            {
                if (_hoverX == -1 && _hoverY == -1) return;
                _hoverX = -1;
                _hoverY = -1;
                canvasPanel.Invalidate();
            };
        }

        private void InitializePalette()
        {
            // 1. 지우개 버튼
            Button btnEraser = new Button { Size = new Size(31, 31), Text = "X", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.Red, Margin = new Padding(1), Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btnEraser.FlatAppearance.BorderSize = 1;
            btnEraser.FlatAppearance.BorderColor = Color.Red;
            btnEraser.Click += (s, e) => { _isEraserMode = true; LogSystemMessage("지우개 모드 활성"); };
            pnlPalette.Controls.Add(btnEraser);

            // 2. 되돌리기 버튼
            Button btnEditUndo = new Button { Size = new Size(31, 31), Text = "↶", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.Gold, Margin = new Padding(1), Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btnEditUndo.FlatAppearance.BorderSize = 1;
            btnEditUndo.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btnEditUndo.Click += (s, e) => PerformUndo();
            pnlPalette.Controls.Add(btnEditUndo);

            // 3. 기본 Pico-8 색상들
            string[] pico8Colors = {
                "#000000", "#1D2B53", "#7E2553", "#008751", "#AB5236", "#5F574F", "#C2C3C7", "#FFF1E8",
                "#FF004D", "#FFA300", "#FFEC27", "#00E436", "#29ADFF", "#83769C", "#FF77A8", "#FFCCAA"
            };

            foreach (var hex in pico8Colors)
            {
                Button b = new Button { Size = new Size(31, 31), BackColor = ColorTranslator.FromHtml(hex), FlatStyle = FlatStyle.Flat, Margin = new Padding(1), Cursor = Cursors.Hand };
                b.FlatAppearance.BorderSize = 0;
                b.Click += (s, e) => { _selectedColor = hex; _isEraserMode = false; };
                pnlPalette.Controls.Add(b);
            }

            // 4. 나의 팔레트 (User Palette) 19칸 생성 (요청사항 반영)
            for (int i = 0; i < 19; i++)
            {
                Button b = new Button { Size = new Size(31, 31), BackColor = Color.FromArgb(40, 40, 40), FlatStyle = FlatStyle.Flat, Margin = new Padding(1), Cursor = Cursors.Hand };
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Color.FromArgb(55, 55, 55);
                b.MouseDown += (s, e) =>
                {
                    var currentMods = Control.ModifierKeys;
                    // 알트 키 감지 (비트 연산으로 더 확실하게)
                    if ((currentMods & Keys.Alt) != 0)
                    {
                        b.BackColor = ColorTranslator.FromHtml(_selectedColor);
                        b.Tag = _selectedColor;
                        b.Refresh(); // UI 강제 갱신
                        LogSystemMessage($"[성공] 팔레트 저장: {_selectedColor} (감지된 키: {currentMods})");
                    }
                    else if (b.Tag != null && e.Button == MouseButtons.Left)
                    {
                        _selectedColor = b.Tag.ToString()!;
                        _isEraserMode = false;
                        LogSystemMessage($"팔레트 선택: {_selectedColor}");
                    }
                    else
                    {
                        // 아무 일도 안 일어날 때 로그를 남겨서 원인 분석
                        LogSystemMessage($"[정보] 팔레트 클릭됨 (감지된 키: {currentMods}, 태그존재: {b.Tag != null})");
                    }
                };
                pnlUserPalette.Controls.Add(b);
            }
        }

        private void UpdateHover(int mouseX, int mouseY)
        {
            int x = mouseX / PixelSize;
            int y = mouseY / PixelSize;
            if (x < 0 || x >= GridSize || y < 0 || y >= GridSize)
            {
                x = -1;
                y = -1;
            }
            if (x == _hoverX && y == _hoverY) return;
            _hoverX = x;
            _hoverY = y;
            canvasPanel.Invalidate();
        }

        private void HandleCanvasMouse(MouseEventArgs e)
        {
            int x = e.X / PixelSize;
            int y = e.Y / PixelSize;

            if (x >= 0 && x < GridSize && y >= 0 && y < GridSize)
            {
                if (Control.ModifierKeys.HasFlag(Keys.Alt)) // 스포이드 + 자동 팔레트 저장
                {
                    Color? sampled;
                    lock (_pixelLock) sampled = _pixels[x, y];
                    if (sampled is Color pix)
                    {
                        _selectedColor = ColorToHex(pix);
                        _isEraserMode = false;
                        LogSystemMessage($"색상 추출: {_selectedColor}");

                        bool alreadyExists = false;
                        foreach (Control ctrl in pnlUserPalette.Controls)
                        {
                            if (ctrl is Button btn && btn.Tag?.ToString() == _selectedColor)
                            {
                                alreadyExists = true;
                                break;
                            }
                        }

                        if (!alreadyExists)
                        {
                            foreach (Control ctrl in pnlUserPalette.Controls)
                            {
                                if (ctrl is Button btn && btn.Tag == null)
                                {
                                    btn.BackColor = pix;
                                    btn.Tag = _selectedColor;
                                    btn.Refresh();
                                    LogSystemMessage("나의 팔레트에 새 색상이 저장되었습니다.");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            LogSystemMessage("이미 팔레트에 존재하는 색상입니다.");
                        }
                    }
                    return;
                }

                lock (_pixelLock)
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        if (_isEraserMode)
                            _pixels[x, y] = null;
                        else
                            _pixels[x, y] = TryParseHex(_selectedColor);
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        _pixels[x, y] = null;
                    }
                }
                canvasPanel.Invalidate();
            }
        }

        private void CanvasPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.Clear(Color.FromArgb(45, 45, 45));

            lock (_pixelLock)
            {
                PaintGrid(g, _pixels);
                PaintGrid(g, _previewPixels);
            }

            using var pen = new Pen(Color.FromArgb(55, 55, 55));
            for (int i = 0; i <= GridSize; i++)
            {
                g.DrawLine(pen, i * PixelSize, 0, i * PixelSize, GridSize * PixelSize);
                g.DrawLine(pen, 0, i * PixelSize, GridSize * PixelSize, i * PixelSize);
            }

            if (_showCoordinates && _hoverX >= 0 && _hoverY >= 0)
            {
                int hx = _hoverX * PixelSize;
                int hy = _hoverY * PixelSize;
                using var hoverPen = new Pen(Color.FromArgb(220, 255, 200, 80));
                g.DrawRectangle(hoverPen, hx, hy, PixelSize - 1, PixelSize - 1);

                string label = $"{_hoverX},{_hoverY}";
                using var font = new Font("Tahoma", 8, FontStyle.Bold);
                SizeF sz = g.MeasureString(label, font);
                float tx = hx + PixelSize + 4;
                float ty = hy - 2;
                if (tx + sz.Width > GridSize * PixelSize) tx = hx - sz.Width - 2;
                if (ty < 0) ty = hy + PixelSize + 1;
                if (ty + sz.Height > GridSize * PixelSize) ty = hy - sz.Height - 1;
                using var bg = new SolidBrush(Color.FromArgb(200, 20, 20, 20));
                g.FillRectangle(bg, tx - 1, ty - 1, sz.Width + 2, sz.Height + 1);
                g.DrawString(label, font, Brushes.White, tx, ty);
            }
        }

        private static void PaintGrid(Graphics g, Color?[,] grid)
        {
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (grid[x, y] is not Color c) continue;
                    using var b = new SolidBrush(c);
                    g.FillRectangle(b, x * PixelSize, y * PixelSize, PixelSize, PixelSize);
                }
            }
        }

        private void btnSavePng_Click(object? sender, EventArgs e) => SaveIcon(false);
        private void btnSaveIco_Click(object? sender, EventArgs e) => SaveIcon(true);

        private void SaveIcon(bool isIco)
        {
            using SaveFileDialog sfd = new SaveFileDialog { Filter = isIco ? "Icon|*.ico" : "PNG|*.png" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                int s = isIco ? 256 : 512;
                using Bitmap bmp = new Bitmap(s, s);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.Clear(Color.Transparent);
                    int sc = s / GridSize;
                    lock (_pixelLock)
                    {
                        for (int y = 0; y < GridSize; y++)
                        {
                            for (int x = 0; x < GridSize; x++)
                            {
                                if (_pixels[x, y] is not Color c) continue;
                                using var b = new SolidBrush(c);
                                g.FillRectangle(b, x * sc, y * sc, sc, sc);
                            }
                        }
                    }
                }
                if (isIco) IconRenderer.SaveAsHighQualityIco(bmp, sfd.FileName);
                else bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void btnLoad_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog { Filter = "Images/Icons|*.png;*.jpg;*.ico;*.exe;*.dll" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Bitmap? bmp = null;
                if (Path.GetExtension(ofd.FileName).ToLower() is ".exe" or ".dll")
                {
                    IntPtr[] h = new IntPtr[1];
                    if (ExtractIconEx(ofd.FileName, 0, h, null!, 1) > 0 && h[0] != IntPtr.Zero)
                    {
                        bmp = Icon.FromHandle(h[0]).ToBitmap();
                        DestroyIcon(h[0]);
                    }
                }
                else bmp = new Bitmap(ofd.FileName);

                if (bmp != null)
                {
                    SaveUndoState();
                    ReplaceGridFromList(IconRenderer.BitmapToPixels(bmp, GridSize));
                    canvasPanel.Invalidate();
                }
            }
        }

        public void UpdateTextPreview(string t, Font f, string c, bool keepColor = false)
        {
            lock (_pixelLock)
            {
                ApplyList(_previewPixels, IconRenderer.GenerateTextPixels(t, f, c, GridSize, keepColor), replaceAll: true);
            }
            canvasPanel.Invalidate();
        }

        public void UpdateImagePreview(Bitmap b)
        {
            lock (_pixelLock)
            {
                ApplyList(_previewPixels, IconRenderer.BitmapToPixels(b, GridSize), replaceAll: true);
            }
            canvasPanel.Invalidate();
        }

        public void CommitTextPreview()
        {
            bool hasPreview = false;
            lock (_pixelLock)
            {
                hasPreview = HasAnyPixel(_previewPixels);
            }
            if (!hasPreview) return;

            SaveUndoState();
            lock (_pixelLock)
            {
                MergePreviewIntoPixels();
                Array.Clear(_previewPixels);
            }
            canvasPanel.Invalidate();
        }

        public void ClearTextPreview()
        {
            lock (_pixelLock) Array.Clear(_previewPixels);
            canvasPanel.Invalidate();
        }

        public void SaveUndoState()
        {
            lock (_pixelLock)
            {
                _undoStack.Add(CloneGrid(_pixels));
                if (_undoStack.Count > MaxUndoSteps)
                    _undoStack.RemoveAt(0);
            }
        }

        private void PerformUndo()
        {
            lock (_pixelLock)
            {
                if (_undoStack.Count == 0) return;
                _pixels = _undoStack[_undoStack.Count - 1];
                _undoStack.RemoveAt(_undoStack.Count - 1);
            }
            canvasPanel.Invalidate();
        }

        private void btnSystemIcons_Click(object? sender, EventArgs e)
        {
            using (var picker = new SystemIconPickerForm())
            {
                if (picker.ShowDialog() == DialogResult.OK && picker.SelectedIconBitmap != null)
                {
                    UpdateImagePreview(picker.SelectedIconBitmap);
                    CommitTextPreview(); // 즉시 확정
                    LogSystemMessage("시스템 아이콘 로드 및 반영 완료!");
                }
            }
        }

        private void btnTextInput_Click(object? sender, EventArgs e) => OpenLiveTextTool("");

        private void btnEmojiImport_Click(object? sender, EventArgs e)
        {
            using var p = new EmojiPickerForm();
            if (p.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(p.EmojiImageData))
            {
                try
                {
                    // 브라우저에서 보낸 Base64 컬러 이미지 데이터를 복원
                    byte[] bytes = Convert.FromBase64String(p.EmojiImageData);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        using (Bitmap rawEmojiBmp = new Bitmap(ms))
                        {
                            UpdateImagePreview(rawEmojiBmp);
                            CommitTextPreview(); // 즉시 확정
                            LogSystemMessage($"컬러 이모지 '{p.SelectedEmoji}' 반영 완료!");
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show($"이모지 변환 실패: {ex.Message}"); }
            }
        }

        private void OpenLiveTextTool(string t)
        {
            foreach (Form f in Application.OpenForms)
            {
                if (f is LiveTextToolForm) { f.Focus(); return; }
            }
            new LiveTextToolForm(this, _selectedColor, t).Show(this);
        }

        private void btnCaptureImport_Click(object? sender, EventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                using Bitmap b = new Bitmap(Clipboard.GetImage()!);
                UpdateImagePreview(b);
                CommitTextPreview(); // 즉시 확정하여 편집 가능한 상태로 레이어 통합
                LogSystemMessage("클립보드 이미지가 캔버스에 즉시 반영되었습니다.");
            }
        }

        private static void StyleButton(Button btn, Color bg, Color border, Color hoverBg, Color downBg, int radius = 6)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = bg;
            btn.ForeColor = Color.White;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = border;
            btn.FlatAppearance.MouseOverBackColor = hoverBg;
            btn.FlatAppearance.MouseDownBackColor = downBg;
            btn.Cursor = Cursors.Hand;

            // 둥근 모서리 곡면 적용
            btn.SizeChanged += (s, e) => ApplyButtonRegion(btn, radius);
            ApplyButtonRegion(btn, radius);
        }

        private static void ApplyButtonRegion(Button btn, int radius)
        {
            if (btn.Width <= 0 || btn.Height <= 0) return;
            try
            {
                using GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), radius);
                btn.Region = new Region(path);
            }
            catch { }
        }

        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2F;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ApplyPartialUpdate(List<PixelInfo> p)
        {
            lock (_pixelLock) ApplyList(_pixels, p, replaceAll: false);
        }

        private void ReplaceGridFromList(List<PixelInfo> list)
        {
            lock (_pixelLock) ApplyList(_pixels, list, replaceAll: true);
        }

        private void LogSystemMessage(string m)
        {
            if (lblStatus.IsDisposed) return;
            void set() => lblStatus.Text = m;
            if (lblStatus.InvokeRequired) lblStatus.Invoke(set);
            else set();
        }

        private void lblBlogLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("https://systemscalping.tistory.com/21") { UseShellExecute = true }); }
            catch { MessageBox.Show("링크를 열 수 없습니다."); }
        }

        private static Color?[,] NewGrid() => new Color?[GridSize, GridSize];

        private static Color?[,] CloneGrid(Color?[,] src)
        {
            var dst = NewGrid();
            Array.Copy(src, dst, src.Length);
            return dst;
        }

        private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static Color? TryParseHex(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            try { return ColorTranslator.FromHtml(hex); }
            catch { return null; }
        }

        private static bool HasAnyPixel(Color?[,] grid)
        {
            for (int y = 0; y < GridSize; y++)
                for (int x = 0; x < GridSize; x++)
                    if (grid[x, y] is Color) return true;
            return false;
        }

        private static void ApplyList(Color?[,] grid, IEnumerable<PixelInfo> list, bool replaceAll)
        {
            if (replaceAll) Array.Clear(grid);
            foreach (var p in list)
            {
                if (p.X < 0 || p.X >= GridSize || p.Y < 0 || p.Y >= GridSize) continue;
                grid[p.X, p.Y] = string.IsNullOrEmpty(p.Color) ? null : TryParseHex(p.Color);
            }
        }

        private void MergePreviewIntoPixels()
        {
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (_previewPixels[x, y] is Color c)
                        _pixels[x, y] = c;
                }
            }
        }

        private string BuildCanvasSummary()
        {
            int minX = GridSize, minY = GridSize, maxX = -1, maxY = -1, count = 0;
            var palette = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            lock (_pixelLock)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    for (int x = 0; x < GridSize; x++)
                    {
                        if (_pixels[x, y] is not Color c) continue;
                        count++;
                        palette.Add(ColorToHex(c));
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    grid = $"{GridSize}x{GridSize}",
                    occupied = 0,
                    note = "empty canvas, transparent background"
                });
            }

            return JsonSerializer.Serialize(new
            {
                grid = $"{GridSize}x{GridSize}",
                occupied = count,
                bounds = new { minX, minY, maxX, maxY },
                palette = palette.OrderBy(h => h).ToList(),
                note = "Full pixel list omitted. Prefer mode=partial. Use the attached canvas snapshot when present."
            });
        }

        private void StartAgentServer()
        {
            try
            {
                _agentServer = new AgentHttpServer(this);
                int port = _agentServer.Start();
                this.Text = $"IconMaker2 - AI Pixel Art Studio  [agent :{port}]";
                LogSystemMessage($"에이전트 HTTP: http://127.0.0.1:{port}/  (MCP가 이 주소를 씀)");
            }
            catch (Exception ex)
            {
                LogSystemMessage($"에이전트 서버 시작 실패: {ex.Message}");
            }
        }

        internal T OnUi<T>(Func<T> fn)
        {
            if (IsHandleCreated && InvokeRequired)
                return (T)Invoke(fn);
            return fn();
        }

        internal void OnUi(Action fn)
        {
            if (IsHandleCreated && InvokeRequired)
                Invoke(fn);
            else
                fn();
        }

        internal object AgentGetCanvas(bool includePng, bool includePixels, int? x, int? y, int? w, int? h)
        {
            return OnUi(() =>
            {
                bool region = x is int && y is int && w is int ww && ww > 0 && h is int hh && hh > 0;
                int rx = 0, ry = 0, rw = GridSize, rh = GridSize;
                if (region)
                {
                    rx = Math.Clamp(x!.Value, 0, GridSize - 1);
                    ry = Math.Clamp(y!.Value, 0, GridSize - 1);
                    rw = Math.Clamp(w!.Value, 1, GridSize - rx);
                    rh = Math.Clamp(h!.Value, 1, GridSize - ry);
                    if (!includePixels) includePixels = true;
                }

                var summary = JsonSerializer.Deserialize<JsonElement>(BuildCanvasSummary());
                string? png = includePng
                    ? Convert.ToBase64String(region ? RenderRegionPng(rx, ry, rw, rh) : RenderCanvasPng(GridSize))
                    : null;
                List<PixelInfo>? pixels = includePixels ? ListOccupiedPixels(rx, ry, rw, rh) : null;
                return new
                {
                    ok = true,
                    grid = GridSize,
                    region = region ? new { x = rx, y = ry, w = rw, h = rh } : null,
                    summary,
                    png_base64 = png,
                    pixels
                };
            });
        }

        internal byte[] AgentGetCanvasPng()
        {
            return OnUi(() => RenderCanvasPng(GridSize));
        }

        internal object AgentImport(byte[] imageBytes, int maxColors, bool knockoutCorners)
        {
            return OnUi(() =>
            {
                using var ms = new MemoryStream(imageBytes);
                using var bmp = new Bitmap(ms);
                SaveUndoState();
                var list = IconRenderer.BitmapToPixels(bmp, GridSize);
                if (maxColors > 0) list = IconRenderer.LimitColors(list, maxColors);
                ReplaceGridFromList(list);
                int knocked = 0;
                if (knockoutCorners) knocked = KnockoutCorners(32);
                canvasPanel.Invalidate();
                LogSystemMessage($"에이전트: 그림을 64x64로 내렸다. colors={maxColors}, corners={knocked}");
                return new { ok = true, grid = GridSize, maxColors, knockoutCorners, knocked };
            });
        }

        internal object AgentSetPixels(string mode, List<PixelInfo> pixels)
        {
            return OnUi(() =>
            {
                SaveUndoState();
                if (string.Equals(mode, "full", StringComparison.OrdinalIgnoreCase))
                    ReplaceGridFromList(pixels);
                else
                    ApplyPartialUpdate(pixels);
                canvasPanel.Invalidate();
                LogSystemMessage($"에이전트: {mode} 픽셀 {pixels.Count}개 반영.");
                return new { ok = true, mode, count = pixels.Count };
            });
        }

        internal object AgentExport(string path, string format)
        {
            return OnUi(() =>
            {
                bool isIco = format.Equals("ico", StringComparison.OrdinalIgnoreCase)
                    || Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase);
                int s = isIco ? 256 : 512;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
                using Bitmap bmp = new Bitmap(s, s);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.Clear(Color.Transparent);
                    int sc = s / GridSize;
                    lock (_pixelLock)
                    {
                        for (int y = 0; y < GridSize; y++)
                        {
                            for (int x = 0; x < GridSize; x++)
                            {
                                if (_pixels[x, y] is not Color c) continue;
                                using var b = new SolidBrush(c);
                                g.FillRectangle(b, x * sc, y * sc, sc, sc);
                            }
                        }
                    }
                }
                if (isIco) IconRenderer.SaveAsHighQualityIco(bmp, path);
                else bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                LogSystemMessage($"에이전트: 저장 {path}");
                return new { ok = true, path, format = isIco ? "ico" : "png" };
            });
        }

        internal object AgentUndo()
        {
            return OnUi(() =>
            {
                bool had = _undoStack.Count > 0;
                if (had) PerformUndo();
                return new { ok = true, undone = had };
            });
        }

        internal object AgentFloodErase(int x, int y, int tolerance)
        {
            return OnUi(() =>
            {
                if (x < 0 || y < 0 || x >= GridSize || y >= GridSize)
                    return (object)new { ok = false, error = "좌표가 격자 밖이다." };
                SaveUndoState();
                int n;
                lock (_pixelLock) n = FloodEraseLocked(x, y, tolerance, requireLight: false);
                canvasPanel.Invalidate();
                LogSystemMessage($"에이전트: flood_erase ({x},{y}) {n}칸");
                return new { ok = true, erased = n };
            });
        }

        internal object AgentRecolor(string fromHex, string toHex, int tolerance)
        {
            return OnUi(() =>
            {
                Color? from = IconRenderer.ParseHex(fromHex);
                if (from is not Color src)
                    return (object)new { ok = false, error = "from 색이 필요하다." };
                Color? dest = IconRenderer.ParseHex(toHex);
                SaveUndoState();
                int n = 0;
                lock (_pixelLock)
                {
                    for (int yy = 0; yy < GridSize; yy++)
                    {
                        for (int xx = 0; xx < GridSize; xx++)
                        {
                            if (_pixels[xx, yy] is not Color c) continue;
                            if (IconRenderer.ColorDistance(c, src) > tolerance) continue;
                            _pixels[xx, yy] = dest;
                            n++;
                        }
                    }
                }
                canvasPanel.Invalidate();
                LogSystemMessage($"에이전트: recolor {fromHex} → {toHex} {n}칸");
                return new { ok = true, count = n };
            });
        }

        internal object AgentFillRect(int x, int y, int w, int h, string color)
        {
            return OnUi(() =>
            {
                int x0 = Math.Clamp(x, 0, GridSize - 1);
                int y0 = Math.Clamp(y, 0, GridSize - 1);
                int x1 = Math.Clamp(x + Math.Max(1, w), 1, GridSize);
                int y1 = Math.Clamp(y + Math.Max(1, h), 1, GridSize);
                Color? c = IconRenderer.ParseHex(color);
                SaveUndoState();
                int n = 0;
                lock (_pixelLock)
                {
                    for (int yy = y0; yy < y1; yy++)
                    {
                        for (int xx = x0; xx < x1; xx++)
                        {
                            _pixels[xx, yy] = c;
                            n++;
                        }
                    }
                }
                canvasPanel.Invalidate();
                LogSystemMessage($"에이전트: fill_rect {n}칸");
                return new { ok = true, count = n };
            });
        }

        internal object AgentDrawLine(int x0, int y0, int x1, int y1, string color)
        {
            return OnUi(() =>
            {
                Color? c = IconRenderer.ParseHex(color);
                SaveUndoState();
                int n = 0;
                lock (_pixelLock)
                {
                    foreach (var (xx, yy) in Bresenham(x0, y0, x1, y1))
                    {
                        if (xx < 0 || yy < 0 || xx >= GridSize || yy >= GridSize) continue;
                        _pixels[xx, yy] = c;
                        n++;
                    }
                }
                canvasPanel.Invalidate();
                LogSystemMessage($"에이전트: draw_line {n}칸");
                return new { ok = true, count = n };
            });
        }

        private byte[] RenderCanvasPng(int outputSize)
        {
            using Bitmap bmp = new Bitmap(outputSize, outputSize);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.Clear(Color.Transparent);
                int sc = Math.Max(1, outputSize / GridSize);
                lock (_pixelLock)
                {
                    for (int y = 0; y < GridSize; y++)
                    {
                        for (int x = 0; x < GridSize; x++)
                        {
                            if (_pixels[x, y] is not Color c) continue;
                            using var br = new SolidBrush(c);
                            g.FillRectangle(br, x * sc, y * sc, sc, sc);
                        }
                    }
                }
            }
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private List<PixelInfo> ListOccupiedPixels(int x0, int y0, int w, int h)
        {
            var list = new List<PixelInfo>();
            int x1 = Math.Min(GridSize, x0 + w);
            int y1 = Math.Min(GridSize, y0 + h);
            lock (_pixelLock)
            {
                for (int y = Math.Max(0, y0); y < y1; y++)
                {
                    for (int x = Math.Max(0, x0); x < x1; x++)
                    {
                        if (_pixels[x, y] is not Color c) continue;
                        list.Add(new PixelInfo { X = x, Y = y, Color = ColorToHex(c) });
                    }
                }
            }
            return list;
        }

        private byte[] RenderRegionPng(int x0, int y0, int w, int h)
        {
            using Bitmap bmp = new Bitmap(Math.Max(1, w), Math.Max(1, h));
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                lock (_pixelLock)
                {
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int gx = x0 + x, gy = y0 + y;
                            if (gx < 0 || gy < 0 || gx >= GridSize || gy >= GridSize) continue;
                            if (_pixels[gx, gy] is not Color c) continue;
                            using var br = new SolidBrush(c);
                            g.FillRectangle(br, x, y, 1, 1);
                        }
                    }
                }
            }
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private int KnockoutCorners(int tolerance)
        {
            int n = 0;
            lock (_pixelLock)
            {
                n += FloodEraseLocked(0, 0, tolerance, requireLight: true);
                n += FloodEraseLocked(GridSize - 1, 0, tolerance, requireLight: true);
                n += FloodEraseLocked(0, GridSize - 1, tolerance, requireLight: true);
                n += FloodEraseLocked(GridSize - 1, GridSize - 1, tolerance, requireLight: true);
            }
            return n;
        }

        private int FloodEraseLocked(int sx, int sy, int tolerance, bool requireLight)
        {
            if (_pixels[sx, sy] is not Color seed) return 0;
            if (requireLight && (seed.R + seed.G + seed.B) / 3 < 160) return 0;

            var q = new Queue<(int x, int y)>();
            var seen = new bool[GridSize, GridSize];
            q.Enqueue((sx, sy));
            int n = 0;
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                if (x < 0 || y < 0 || x >= GridSize || y >= GridSize || seen[x, y]) continue;
                seen[x, y] = true;
                if (_pixels[x, y] is not Color c) continue;
                if (IconRenderer.ColorDistance(c, seed) > tolerance) continue;
                _pixels[x, y] = null;
                n++;
                q.Enqueue((x + 1, y));
                q.Enqueue((x - 1, y));
                q.Enqueue((x, y + 1));
                q.Enqueue((x, y - 1));
            }
            return n;
        }

        private static IEnumerable<(int x, int y)> Bresenham(int x0, int y0, int x1, int y1)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                yield return (x0, y0);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }
    }
}
