using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;

namespace IconMaker2
{
    // 수백 개의 컬러 이모지를 제공하는 완성형 셀렉터
    public class EmojiPickerForm : Form
    {
        public string? SelectedEmoji { get; private set; }
        public string? EmojiImageData { get; private set; }
        private WebBrowser _wb;

        public EmojiPickerForm()
        {
            this.Text = "프리미엄 컬러 이모지 대기실";
            this.Size = new Size(550, 700);
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(25, 25, 25);

            _wb = new WebBrowser
            {
                Dock = DockStyle.Fill,
                AllowWebBrowserDrop = false,
                IsWebBrowserContextMenuEnabled = false,
                WebBrowserShortcutsEnabled = false,
                ScriptErrorsSuppressed = true,
                ScrollBarsEnabled = true
            };

            _wb.DocumentTitleChanged += (s, e) => {
                string title = _wb.DocumentTitle;
                if (!string.IsNullOrEmpty(title) && title.StartsWith("EMOJI_PICK:")) {
                    string data = title.Substring(11);
                    string[] parts = data.Split(new[] { "|||" }, StringSplitOptions.None);
                    if (parts.Length == 2) {
                        this.SelectedEmoji = parts[0];
                        this.EmojiImageData = parts[1];
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            };

            this.Controls.Add(_wb);

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head>");
            sb.Append("<meta http-equiv='X-UA-Compatible' content='IE=edge'>");
            sb.Append("<style>");
            sb.Append("body { background: #191919; color: #ccc; font-family: 'Segoe UI Emoji', sans-serif; padding: 15px; margin: 0; user-select: none; }");
            sb.Append("h3 { border-bottom: 1px solid #333; padding-bottom: 5px; color: #888; font-size: 14px; margin-top: 20px; }");
            sb.Append(".container { display: flex; flex-wrap: wrap; margin-bottom: 10px; }");
            sb.Append(".emoji { width: 55px; height: 55px; line-height: 55px; text-align: center; font-size: 34px; cursor: pointer; border-radius: 8px; transition: all 0.1s; display: inline-block; }");
            sb.Append(".emoji:hover { background: #333; transform: scale(1.15); color: white; }");
            sb.Append("canvas { display: none; }");
            sb.Append("</style>");
            sb.Append("<script>");
            sb.Append("function chooseEmoji(emoji) {");
            sb.Append("  try {");
            sb.Append("    var canvas = document.getElementById('rc');");
            sb.Append("    var ctx = canvas.getContext('2d');");
            sb.Append("    ctx.clearRect(0,0,64,64);");
            sb.Append("    ctx.font = '50px Segoe UI Emoji';");
            sb.Append("    ctx.textAlign = 'center';");
            sb.Append("    ctx.textBaseline = 'middle';");
            sb.Append("    ctx.fillText(emoji, 32, 32);");
            sb.Append("    var b64 = canvas.toDataURL('image/png').split(',')[1];");
            sb.Append("    document.title = 'EMOJI_PICK:' + emoji + '|||' + b64;");
            sb.Append("  } catch(e) { }");
            sb.Append("}");
            sb.Append("</script></head><body>");
            sb.Append("<canvas id='rc' width='64' height='64'></canvas>");

            // 카테고리별 데이터 (대폭 확장)
            AddCategory(sb, "😃 인기 표정 & 감정", "😀,😃,😄,😁,😆,😅,🤣,😂,🙂,🙃,😉,😊,😇,🥰,😍,🤩,😘,😗,😚,😙,😋,😛,😜,🤪,🤨,🧐,🤓,😎,🥳,🤠,😏,😒,😞,😔,😟,😕,🙁,☹️,😣,😖,😫,😩,🥺,😢,😭,😤,😠,😡,🤬,🤯,😳,🥵,🥶,😱,😨,😰,😥,😓,🤔,🤭,🤫,🤥,😶,😐,😑,😬,🙄,😯,😦,😧,😮,😲,🥱,😴,🤤,😪,😵,🤐,🥴,🤢,🤮,🤧,😷,🤒,🤕,🤑,😈,👿,👹,👺,🤡,💩,👻,💀,☠️,👽,👾,🤖,🎃");
            AddCategory(sb, "👍 손동작 & 신체", "👋,🤚,🖐️,✋,🖖,👌,🤏,✌️,🤞,🤟,🤘,🤙,👈,👉,👆,🖕,👇,☝️,👍,👎,✊,👊,🤛,🤜,👏,🙌,👐,🤲,🤝,🙏,✍️,💅,🤳,💪,🦾,👂,🦻,👃,🧠,🦷,👀,👁️,👅,👄");
            AddCategory(sb, "❤️ 하트 & 고유 기호", "❤️,🧡,💛,💚,💙,🟣,🤎,🖤,🤍,❤️‍🔥,❤️‍🩹,💖,💗,💓,💞,💕,💟,❣️,💔,☮️,✝️,☪️,🕉️,☸️,✡️,🔯,🕎,☯️,☦️,🛐,♈,♉,♊,♋,♌, Virgo, ♎, ♏, ♐, ♑, ♒, ♓, 🆔, ⚛️, ☢️, ☣️, 📴, 📳, 🈶, 🈚, 🆚, 💮, 🉐, ㊙️, ㊗️, 🅰️, 🅱️, 🅾️, 🆘, ❌, ⭕, 🛑, ⛔, 📛, 🚫");
            AddCategory(sb, "💻 IT & 사무용 도구", "💻,⌨️,🖱️,🖲️,🕹️,💽,💾,💿,📀,📼,📷,📸,📹,🎥,📽️,🎞️,📞,☎️,📟,📠,📺,📻,🎙️,🎚️,🎛️,⏱️,⏲️,⏰,🕰️,⏳,⌛,📡,🔋,🔌,💡,🔦,🕯️,🧯,💸,💵,💶,💷,💰,💳,💎,⚖️,🧰,🔧,🔨,⚒️,🛠️,⛏️,🔩,⚙️,🧱,⛓️,🧲,🛡️,🔑,🗝️,🚪,🛏️,🛋️,✉️,📩,📨,📧,💌,📥,📤,📦,🏷️,📪,📫,📬,📭,📮,📯,📜,📃,📄,📑,📊,📈,📉,🗒️,🗓️,📅,📆,🗑️,📇,🗃️,🗳️,🗄️,📋,📁,📂,🗂️,🗞️,📰,📓,📕,📗,📘,📙,📚,📖,🔖,🧷,🔗,📎,📐,📏,📌,📍,✂️,🖊️,🖋️,✒️,🖌️,🖍️,📝,✏️,🔍,🔎,🔏,🔐,🔒,🔓");
            AddCategory(sb, "🐶 동물 & 자연", "🐶,🐱,🐭,🐹,🐰,🦊,🐻,🐼,🐨,🐯,🦁,🐮,🐷,🐽,🐸,🐵,🐈,🐕,🐩,🐺,🦊,🦝,🐈‍⬛,🐅,🐆,🐎,🦄,🦓,🦌,🦬,🐃,🐄,🐖,🐗,🐏,🐑,🐐,🐪,🐫,🦙,🦒,🐘,🦏,🦛,🐁,🐀,🐿️,🦔,🦇,🐨,🐼,🐾,🦃,🐔,🐓,🐣,🐤,🐥,🐦,🐧,🕊️,🦅,🦆,🦢,🦉,🦩,🦜,🐸,🐊,🐢,🦎,🐍,🐲,🐉,🦕,🦖,🐳,🐋,🐬,🦭,🐟,🐠,🐡,🦈,🐙,🐚,🐌,🦋,🐛,🐜,🐝,🐞,🦗,🕷️,🕸️,🦂,🦟,💐,🌸,💮,🌹,🥀,🌺,🌻,🌼,🌷,🌱,🌲,🌳,🌴,🌵,🌾,🌿,🍀,🍁,🍂,🍃");
            AddCategory(sb, "⚽ 스포츠 & 취미", "⚽,🏀,🏈,⚾,🥎,🎾,🏐,🏉,🎱,🏓,🏸,🥅,🏒,🏑,🏏,⛳,🏹,🎣,📽️,🥊,🥋,⛸️,⛷️,🎿,🏂,🏋️,🤺,🤼,🤸,🧗,🚴,🚵,🎮,🕹️,🎰,🎲,🧩,🧸,♠️,♥️,♣️,♦️,♟️,🎭,🎨,🧵,🧶,🎹,🥁,🎷,🎺,🎸,🎻");
            AddCategory(sb, "🍎 음식 & 음료", "🍏,🍎,🍐,🍊,🍋,🍌,🍉,🍇,🍓,🫐,🍈,🍒,🍑,🥭, pineapple, 🥥, 🥝, 🍅, 🍆, 🥑, 🥦, 🥬, 🥒, 🌶️, 🌽, 🥕, 🫒, 🧄, 🧅, 🍄, 🥜, 🥐, 🍞, 🥨, 🥯, 🥞, 🧇, 🧀, 🍖, 🍗, 🥩, 🥓, 🍔, 🍟, 🍕, 🌭, 🥪, 🌮, 🌯, 🍳, 🍲, 🍿, 🍣, 🍦, 🍩, 🎂, 🍰, 🧁, 🥧, 🍫, 🍬, 🍭, 🍮, 🍯, ☕, 🍵, 🍶, 🍷, 🍸, 🍹, 🍺, 🍻, 🥂, 🥃");
            AddCategory(sb, "🚗 교통 & 건축물", "🌍,🌎,🌏,🗺️,🗾,🏔️,🌋,🗻,🏠,🏡,🏘️,🏚️,🏢,🏣,🏤,🏥,🏦,🏨,🏩,🏪,🏫,🏬,🏭,🏯,🏰,⛪,🕌,🕍,🕋,⛩️,⛲,⛺,🌁,🌃,🏙️,🌄,🌅,🌆,🌇,🌉,♨️,🎠,🎡,🎢,💈,🎪,🚂,🚃,🚄,🚅,🚆,🚇,🚈,🚉,🚊,🚝,🚞,🚋,🚌,🚍,🚎,🚐,🚑,🚒,🚓,🚔,🚕,🚖,🚗,🚘,🚙,🚚,🚛,🚜,🛵,🏍️,🚲,🛴,🛸,🚀,🚁,✈️,🛫,🛬,🛶,⛵,🚤,🛥️,⛴️,🛳️,🚢,⚓,🚧,⛵");
            AddCategory(sb, "⚠️ 정보 & 화살표", "🔴,🟠,🟡,🟢,🔵,🟣,🟤,⚫,⚪,🟥,🟧,🟨,🟩,🟦,🟪,🟫,⬛,⬜,🔸,🔹,🔶,🔷,🔺,🔻,💠,🔘,🔳,🔲,🏁,🚩,🎌,🏴,🏳️,🏳️‍🌈,🏳️‍⚧️,▶️,⏸️,⏯️,⏹️,⏺️,⏭️,⏮️,⏩,⏪,⏫,⏬,◀️,🔼,🔽,➡️,⬅️,⬆️,⬇️,↗️,↘️,↙️,↖️,↕️,↔️,↪️,↩️,⤴️,⤵️,🔀,🔁,🔂,🔄,🔃");

            sb.Append("</body></html>");
            _wb.DocumentText = sb.ToString();
        }

        private void AddCategory(StringBuilder sb, string title, string emojiList)
        {
            sb.Append($"<h3>{title}</h3>");
            sb.Append("<div class='container'>");
            var list = emojiList.Split(',');
            foreach (var em in list)
            {
                string emoji = em.Trim();
                if (string.IsNullOrEmpty(emoji)) continue;
                sb.Append($"<div class='emoji' onclick='chooseEmoji(\"{emoji}\")'>{emoji}</div>");
            }
            sb.Append("</div>");
        }
    }
}
