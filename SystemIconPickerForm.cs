using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

namespace IconMaker2
{
    // 수백 개의 윈도우 시스템 아이콘을 한눈에 펼쳐 보여주는 폼
    public class SystemIconPickerForm : Form
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public Bitmap? SelectedIconBitmap { get; private set; }

        public SystemIconPickerForm()
        {
            this.Text = "윈도우 대규모 아이콘 보물창고";
            this.Size = new Size(650, 750);
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(25, 25, 25);
            this.ForeColor = Color.White;

            FlowLayoutPanel flp = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            // 아이콘이 가득 담긴 윈도우 핵심 라이브러리들
            string[] iconFiles = { 
                "imageres.dll", "shell32.dll", "ddores.dll", 
                "accessibility.dll", "moricons.dll", "mmcndmgr.dll", 
                "netshell.dll", "setupapi.dll", "wmploc.dll", "pifmgr.dll" 
            };

            foreach (string file in iconFiles)
            {
                string fullPath = Path.Combine(Environment.SystemDirectory, file);
                if (!File.Exists(fullPath)) continue;

                // 아이콘 개수 확인
                int iconCount = ExtractIconEx(fullPath, -1, null, null, 0);
                if (iconCount <= 0) continue;

                // 구분 라벨 추가
                Label lbl = new Label { 
                    Text = $"📦 {file} ({iconCount}개)", 
                    Width = 580, 
                    ForeColor = Color.FromArgb(0, 120, 215), 
                    Height = 40,
                    TextAlign = ContentAlignment.BottomLeft,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Padding = new Padding(0, 0, 0, 5)
                };
                flp.Controls.Add(lbl);

                // 라이브러리당 너무 많으면 로딩이 느려지므로 주요 범위(최대 160개) 추출
                int loadLimit = Math.Min(iconCount, 160);

                for (int i = 0; i < loadLimit; i++)
                {
                    IntPtr[] largeIcons = new IntPtr[1];
                    ExtractIconEx(fullPath, i, largeIcons, null!, 1);

                    if (largeIcons[0] != IntPtr.Zero)
                    {
                        using (Icon icon = Icon.FromHandle(largeIcons[0]))
                        {
                            Bitmap rawBmp = icon.ToBitmap();
                            Button btn = new Button
                            {
                                Size = new Size(56, 56),
                                Image = new Bitmap(rawBmp, new Size(32, 32)),
                                FlatStyle = FlatStyle.Flat,
                                Margin = new Padding(4),
                                Cursor = Cursors.Hand,
                                Tag = rawBmp, // 고해상도 비트맵 보관
                                BackColor = Color.FromArgb(35, 35, 35)
                            };
                            btn.FlatAppearance.BorderSize = 0;
                            
                            btn.Click += (s, e) => {
                                this.SelectedIconBitmap = (Bitmap)btn.Tag;
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            };

                            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(60, 60, 60);
                            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(35, 35, 35);

                            flp.Controls.Add(btn);
                        }
                        DestroyIcon(largeIcons[0]);
                    }
                }
            }

            this.Controls.Add(flp);
        }
    }
}
