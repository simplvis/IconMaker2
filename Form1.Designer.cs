using System.Drawing;
using System.Windows.Forms;

namespace IconMaker2
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            canvasPanel = new Panel();
            pnlMain = new Panel();
            pnlLeft = new Panel();
            flowLayoutPanel1 = new FlowLayoutPanel();
            chkCoords = new CheckBox();
            pnlPalette = new FlowLayoutPanel();
            lblUserPaletteHint = new Label();
            pnlUserPalette = new FlowLayoutPanel();
            pnlLeftBottom = new Panel();
            tableLayoutPanel2 = new TableLayoutPanel();
            btnLoadIcon = new Button();
            btnSaveIco = new Button();
            btnSavePng = new Button();
            btnTextInput = new Button();
            btnSystemIcons = new Button();
            btnCaptureImport = new Button();
            btnEmojiImport = new Button();
            lblStatus = new Label();
            lblBlogLink = new LinkLabel();
            pnlMain.SuspendLayout();
            pnlLeft.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            pnlLeftBottom.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // canvasPanel
            // 
            canvasPanel.BorderStyle = BorderStyle.FixedSingle;
            canvasPanel.Dock = DockStyle.Top;
            canvasPanel.Location = new Point(0, 0);
            canvasPanel.Name = "canvasPanel";
            canvasPanel.Size = new Size(642, 642);
            canvasPanel.TabIndex = 0;
            // 
            // pnlMain
            // 
            pnlMain.Controls.Add(pnlLeft);
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.Location = new Point(0, 0);
            pnlMain.Name = "pnlMain";
            pnlMain.Padding = new Padding(10);
            pnlMain.Size = new Size(662, 892);
            pnlMain.TabIndex = 0;
            // 
            // pnlLeft
            // 
            pnlLeft.Controls.Add(flowLayoutPanel1);
            pnlLeft.Controls.Add(pnlLeftBottom);
            pnlLeft.Controls.Add(canvasPanel);
            pnlLeft.Dock = DockStyle.Fill;
            pnlLeft.Location = new Point(10, 10);
            pnlLeft.Name = "pnlLeft";
            pnlLeft.Size = new Size(642, 872);
            pnlLeft.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(chkCoords);
            flowLayoutPanel1.Controls.Add(pnlPalette);
            flowLayoutPanel1.Controls.Add(lblUserPaletteHint);
            flowLayoutPanel1.Controls.Add(pnlUserPalette);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.Location = new Point(0, 642);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(642, 128);
            flowLayoutPanel1.TabIndex = 4;
            // 
            // chkCoords
            // 
            chkCoords.AutoSize = true;
            chkCoords.Checked = false;
            chkCoords.CheckState = CheckState.Unchecked;
            chkCoords.ForeColor = Color.Silver;
            chkCoords.Location = new Point(4, 5);
            chkCoords.Margin = new Padding(4, 5, 2, 0);
            chkCoords.Name = "chkCoords";
            chkCoords.Size = new Size(106, 24);
            chkCoords.TabIndex = 0;
            chkCoords.Text = "좌표 호버";
            chkCoords.UseVisualStyleBackColor = true;
            // 
            // pnlPalette
            // 
            pnlPalette.Location = new Point(3, 32);
            pnlPalette.Name = "pnlPalette";
            pnlPalette.Padding = new Padding(2);
            pnlPalette.Size = new Size(636, 45);
            pnlPalette.TabIndex = 13;
            // 
            // lblUserPaletteHint
            // 
            lblUserPaletteHint.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblUserPaletteHint.ForeColor = Color.DarkGray;
            lblUserPaletteHint.Location = new Point(3, 80);
            lblUserPaletteHint.Name = "lblUserPaletteHint";
            lblUserPaletteHint.Size = new Size(100, 18);
            lblUserPaletteHint.TabIndex = 15;
            lblUserPaletteHint.Text = "My Palette:";
            // 
            // pnlUserPalette
            // 
            pnlUserPalette.Location = new Point(3, 101);
            pnlUserPalette.Name = "pnlUserPalette";
            pnlUserPalette.Size = new Size(638, 42);
            pnlUserPalette.TabIndex = 14;
            // 
            // pnlLeftBottom
            // 
            pnlLeftBottom.Controls.Add(tableLayoutPanel2);
            pnlLeftBottom.Controls.Add(lblStatus);
            pnlLeftBottom.Controls.Add(lblBlogLink);
            pnlLeftBottom.Dock = DockStyle.Bottom;
            pnlLeftBottom.Location = new Point(0, 770);
            pnlLeftBottom.Name = "pnlLeftBottom";
            pnlLeftBottom.Padding = new Padding(0, 10, 0, 0);
            pnlLeftBottom.Size = new Size(642, 102);
            pnlLeftBottom.TabIndex = 3;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 7;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.2857141F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.2857141F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.2857141F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.2857141F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.2857141F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.2857141F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.2857141F));
            tableLayoutPanel2.Controls.Add(btnLoadIcon, 2, 0);
            tableLayoutPanel2.Controls.Add(btnSaveIco, 0, 0);
            tableLayoutPanel2.Controls.Add(btnSavePng, 1, 0);
            tableLayoutPanel2.Controls.Add(btnTextInput, 6, 0);
            tableLayoutPanel2.Controls.Add(btnSystemIcons, 4, 0);
            tableLayoutPanel2.Controls.Add(btnCaptureImport, 3, 0);
            tableLayoutPanel2.Controls.Add(btnEmojiImport, 5, 0);
            tableLayoutPanel2.Dock = DockStyle.Top;
            tableLayoutPanel2.Location = new Point(0, 10);
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(642, 38);
            tableLayoutPanel2.TabIndex = 14;
            // 
            // btnLoadIcon
            // 
            btnLoadIcon.Dock = DockStyle.Fill;
            btnLoadIcon.Font = new Font("맑은 고딕", 8F);
            btnLoadIcon.Location = new Point(182, 0);
            btnLoadIcon.Margin = new Padding(0);
            btnLoadIcon.Name = "btnLoadIcon";
            btnLoadIcon.Size = new Size(91, 38);
            btnLoadIcon.TabIndex = 1;
            btnLoadIcon.Text = "이미지소스";
            btnLoadIcon.UseVisualStyleBackColor = true;
            // 
            // btnSaveIco
            // 
            btnSaveIco.Dock = DockStyle.Fill;
            btnSaveIco.Location = new Point(0, 0);
            btnSaveIco.Margin = new Padding(0);
            btnSaveIco.Name = "btnSaveIco";
            btnSaveIco.Size = new Size(91, 38);
            btnSaveIco.TabIndex = 13;
            btnSaveIco.Text = "ICO 저장";
            btnSaveIco.UseVisualStyleBackColor = true;
            // 
            // btnSavePng
            // 
            btnSavePng.Dock = DockStyle.Fill;
            btnSavePng.Location = new Point(91, 0);
            btnSavePng.Margin = new Padding(0);
            btnSavePng.Name = "btnSavePng";
            btnSavePng.Size = new Size(91, 38);
            btnSavePng.TabIndex = 9;
            btnSavePng.Text = "PNG 저장";
            btnSavePng.UseVisualStyleBackColor = true;
            // 
            // btnTextInput
            // 
            btnTextInput.Dock = DockStyle.Fill;
            btnTextInput.Font = new Font("맑은 고딕", 8F);
            btnTextInput.Location = new Point(546, 0);
            btnTextInput.Margin = new Padding(0);
            btnTextInput.Name = "btnTextInput";
            btnTextInput.Size = new Size(96, 38);
            btnTextInput.TabIndex = 0;
            btnTextInput.Text = "텍스트입력";
            btnTextInput.UseVisualStyleBackColor = true;
            // 
            // btnSystemIcons
            // 
            btnSystemIcons.Dock = DockStyle.Fill;
            btnSystemIcons.Font = new Font("맑은 고딕", 8F);
            btnSystemIcons.Location = new Point(364, 0);
            btnSystemIcons.Margin = new Padding(0);
            btnSystemIcons.Name = "btnSystemIcons";
            btnSystemIcons.Size = new Size(91, 38);
            btnSystemIcons.TabIndex = 0;
            btnSystemIcons.Text = "아이콘";
            btnSystemIcons.UseVisualStyleBackColor = true;
            // 
            // btnCaptureImport
            // 
            btnCaptureImport.Dock = DockStyle.Fill;
            btnCaptureImport.Location = new Point(273, 0);
            btnCaptureImport.Margin = new Padding(0);
            btnCaptureImport.Name = "btnCaptureImport";
            btnCaptureImport.Size = new Size(91, 38);
            btnCaptureImport.TabIndex = 1;
            btnCaptureImport.Text = "클립보드";
            btnCaptureImport.UseVisualStyleBackColor = true;
            // 
            // btnEmojiImport
            // 
            btnEmojiImport.Dock = DockStyle.Fill;
            btnEmojiImport.Location = new Point(455, 0);
            btnEmojiImport.Margin = new Padding(0);
            btnEmojiImport.Name = "btnEmojiImport";
            btnEmojiImport.Size = new Size(91, 38);
            btnEmojiImport.TabIndex = 0;
            btnEmojiImport.Text = "이모지";
            btnEmojiImport.UseVisualStyleBackColor = true;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.ForeColor = Color.Silver;
            lblStatus.Location = new Point(5, 52);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(632, 22);
            lblStatus.TabIndex = 15;
            lblStatus.Text = "";
            lblStatus.AutoEllipsis = true;
            // 
            // lblBlogLink
            // 
            lblBlogLink.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblBlogLink.AutoSize = true;
            lblBlogLink.LinkColor = Color.LightSkyBlue;
            lblBlogLink.Location = new Point(5, 76);
            lblBlogLink.Name = "lblBlogLink";
            lblBlogLink.Size = new Size(166, 20);
            lblBlogLink.TabIndex = 13;
            lblBlogLink.TabStop = true;
            lblBlogLink.Text = "IconMaker2 사용법 보기";
            lblBlogLink.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(662, 892);
            Controls.Add(pnlMain);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "IconMaker2";
            pnlMain.ResumeLayout(false);
            pnlLeft.ResumeLayout(false);
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            pnlLeftBottom.ResumeLayout(false);
            pnlLeftBottom.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel canvasPanel;
        private Panel pnlMain;
        private Panel pnlLeft;
        private Panel pnlLeftBottom;
        private LinkLabel lblBlogLink;
        private Label lblStatus;
        private Button btnSystemIcons;
        private Button btnLoadIcon;
        private Button btnSaveIco;
        private Button btnSavePng;
        private FlowLayoutPanel pnlUserPalette;
        private Label lblUserPaletteHint;
        private FlowLayoutPanel pnlPalette;
        private CheckBox chkCoords;
        private FlowLayoutPanel flowLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private Button btnTextInput;
        private Button btnCaptureImport;
        private Button btnEmojiImport;
    }
}
