using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace IconMaker2
{
    // 아이콘 탐색을 위한 별도 폼 클래스
    public class IconPickerForm : Form
    {
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public Icon? SelectedIcon { get; private set; }

        public IconPickerForm(string filePath, int count)
        {
            this.Text = $"{Path.GetFileName(filePath)} - 아이콘 선택하기 ({count}개)";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(35, 35, 35);
            this.ForeColor = Color.White;

            FlowLayoutPanel flp = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            for (int i = 0; i < count; i++)
            {
                IntPtr[] largeIcons = new IntPtr[1];
                if (ExtractIconEx(filePath, i, largeIcons, null!, 1) > 0 && largeIcons[0] != IntPtr.Zero)
                {
                    Icon ico = Icon.FromHandle(largeIcons[0]);
                    PictureBox pb = new PictureBox
                    {
                        Image = ico.ToBitmap(),
                        Size = new Size(32, 32),
                        SizeMode = PictureBoxSizeMode.CenterImage,
                        Margin = new Padding(5),
                        Cursor = Cursors.Hand,
                        BorderStyle = BorderStyle.FixedSingle,
                        Tag = i
                    };
                    
                    pb.Click += (s, e) => {
                        this.SelectedIcon = Icon.FromHandle(largeIcons[0]);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    };
                    flp.Controls.Add(pb);
                }
            }

            this.Controls.Add(flp);
        }
    }
}
