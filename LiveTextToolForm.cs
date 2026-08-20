using System;
using System.Drawing;
using System.Windows.Forms;

namespace IconMaker2
{
    // 실시간 텍스트 도구용 비실행형(Modeless) 폼
    public class LiveTextToolForm : Form
    {
        private Form1 _mainForm;
        private TextBox _txtInput;
        private ComboBox _cmbFonts;
        private Button _btnApply;
        private Font _currentFont;
        private string _currentColor;

        public LiveTextToolForm(Form1 mainForm, string hexColor, string initialText = "")
        {
            _mainForm = mainForm;
            _currentColor = hexColor;
            _currentFont = new Font("Arial", 12, FontStyle.Bold);

            this.Text = "글자 입력 도구 (Live)";
            this.Size = new Size(300, 180);
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            
            // 메인 폼 우측 또는 중앙 근처에 배치
            this.Location = new Point(mainForm.Location.X + 100, mainForm.Location.Y + 150);
            this.BackColor = Color.FromArgb(45, 45, 45);
            this.ForeColor = Color.White;

            Label lbl = new Label { Text = "내용을 입력하세요:", Left = 15, Top = 15, AutoSize = true };
            _txtInput = new TextBox { Left = 15, Top = 40, Width = 255, Text = initialText };
            _txtInput.TextChanged += (s, e) => RequestUpdate();

            Label lblFont = new Label { Text = "글꼴 선택:", Left = 15, Top = 75, AutoSize = true };
            _cmbFonts = new ComboBox { 
                Left = 15, 
                Top = 95, 
                Width = 255, 
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            // 시스템 글꼴 목록 채우기
            foreach (var family in FontFamily.Families)
            {
                _cmbFonts.Items.Add(family.Name);
            }

            // 기본 폰트 설정 (Segoe UI 또는 Arial 권장)
            int defaultIdx = _cmbFonts.Items.IndexOf("Arial");
            if (defaultIdx == -1) defaultIdx = _cmbFonts.Items.IndexOf("Segoe UI");
            _cmbFonts.SelectedIndex = defaultIdx >= 0 ? defaultIdx : 0;

            _cmbFonts.SelectedIndexChanged += (s, e) => {
                _currentFont = new Font(_cmbFonts.SelectedItem.ToString(), 12, FontStyle.Bold);
                RequestUpdate();
            };

            _btnApply = new Button { 
                Text = "캔버스에 적용 (Commit)", 
                Left = 15, 
                Top = 135, 
                Width = 255, 
                Height = 35,
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(30, 80, 30),
                Cursor = Cursors.Hand
            };
            _btnApply.Click += (s, e) => {
                _mainForm.CommitTextPreview();
                this.Close();
            };

            this.Controls.AddRange(new Control[] { lbl, _txtInput, lblFont, _cmbFonts, _btnApply });
            
            this.Size = new Size(300, 230); // 콤보박스 추가로 인한 높이 조절
            
            // 초기 텍스트가 있으면 즉시 프리뷰 업데이트
            if (!string.IsNullOrEmpty(initialText))
            {
                RequestUpdate();
            }

            // 창을 닫으면 프리뷰 데이터 초기화
            this.FormClosing += (s, e) => _mainForm.ClearTextPreview();
        }

        private void RequestUpdate()
        {
            // 이모지 여부 판단 (Surrogate Pair 또는 특정 유니코드 범위)
            bool isEmoji = false;
            string text = _txtInput.Text;
            for (int i = 0; i < text.Length; i++) {
                if (char.IsSurrogatePair(text, i)) { isEmoji = true; break; }
                int codePoint = char.ConvertToUtf32(text, i);
                if (codePoint >= 0x2000) { isEmoji = true; break; }
            }

            // 메인 폼에 실시간 업데이트 요청 (이모지라면 keepColor: true)
            _mainForm.UpdateTextPreview(text, _currentFont, _currentColor, isEmoji);
        }
    }
}
