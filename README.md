# IconMaker2

Windows에서 64×64 픽셀 아이콘을 만들고, CLI 에이전트(Grok, Claude, Cursor, Codex 등)가 **같은 캔버스**를 MCP로 조종할 수 있게 한 편집기다.

| 사용자 | 필요한 것 |
|---|---|
| 직접 그리기만 | 앱 설치·실행 |
| 에이전트로 그리기 | 앱 실행 + 쓰는 CLI에 MCP 등록 |

> **Windows x64 전용**이다. Linux/macOS에서는 이 창을 네이티브로 띄울 수 없다.

---

## 설치

### 1) 소스에서 (개발)

1. [Git](https://git-scm.com/)과 [.NET 10 SDK](https://dotnet.microsoft.com/download)를 설치한다.
2. 저장소를 받는다.

```bat
git clone https://github.com/simplvis/IconMaker2.git
cd IconMaker2
```

3. 복원한 뒤 빌드가 되는지만 확인한다.

```bat
dotnet build
```

에이전트를 붙일 계획이면 [Node.js](https://nodejs.org/) **Windows 설치본**도 넣는다. (`node -v`가 `C:\Program Files\nodejs\node.exe`에서 나와야 한다.)

### 2) 단일 EXE로 (배포)

소스 폴더에서:

```bat
dotnet publish -c Release -o publish
```

`publish\IconMaker2.exe`를 아무 폴더에 복사해 실행하면 된다. .NET 런타임을 따로 설치할 필요는 없다(self-contained).

---

## 실행

소스에서:

```bat
cd IconMaker2
dotnet run
```

EXE로:

```bat
IconMaker2.exe
```

정상 기동이면 창 제목에 **`[agent :17890]`** 이 붙고, 아래 상태 줄에 주소가 나온다. 17890이 이미 쓰이면 17891부터 순차로 연다. **항상 제목의 포트**를 보면 된다.

브라우저나 터미널에서 확인:

```bat
curl http://127.0.0.1:17890/health
```

`{"ok":true,"port":17890,"grid":64}` 가 오면 준비된 것이다.

앱을 끄면 에이전트도 캔버스에 붙을 수 없다. MCP 작업을 할 때는 **창을 켜 둔 채**로 둔다.

채팅창이나 API 키 입력은 없다. AI는 쓰는 CLI 쪽에서 붙인다.

---

## 화면과 기본 조작

창은 위부터 **64×64 캔버스 → 팔레트 → 버튼** 순이다. 배경은 투명이다. 저장할 때 빈 칸은 알파로 나간다.

### 캔버스

| 동작 | 하는 일 |
|---|---|
| 왼쪽 클릭 / 드래그 | 선택한 색으로 점을 찍는다 |
| 오른쪽 클릭 / 드래그 | 그 칸을 지운다 (투명) |
| `Alt` + 클릭 | 스포이드. 그 색을 고르고, `My Palette` 빈칸에 없으면 순서대로 넣는다 |
| `Ctrl+Z` | 되돌리기 (최대 50단계) |
| 좌표 호버 | 켜면 마우스 칸에 `x,y`를 보여 준다 |

격자는 64×64, 화면 칸은 10px라서 패널은 640px이다.

### 팔레트

| 컨트롤 | 하는 일 |
|---|---|
| `X` | 지우개 모드. 이후 왼쪽 클릭도 지운다. 색을 다시 고르면 해제 |
| `↶` | 되돌리기 (`Ctrl+Z`와 같음) |
| Pico-8 색 버튼 | 그 색으로 그리기 |
| `My Palette` | 왼쪽 클릭: 저장된 색 선택. `Alt` + 클릭: **지금 고른 색**을 그 칸에 저장 |

---

## 버튼

아래 줄은 왼쪽부터 이 순서다.

| 버튼 | 하는 일 |
|---|---|
| **ICO 저장** | 64격자를 256×256으로 키운 뒤 32비트 PNG-in-ICO로 저장한다. 투명 유지 |
| **PNG 저장** | 같은 격자를 512×512 PNG로 저장한다 (nearest-neighbor) |
| **이미지소스** | PNG/JPG/ICO, 또는 EXE/DLL에서 아이콘을 읽어 64격자로 내린다. 기존 그림을 덮는다 |
| **클립보드** | 클립보드 이미지를 즉시 64격자로 붙여 넣는다. 확인 창 없음 |
| **아이콘** | `shell32.dll`, `imageres.dll` 등 윈도우 시스템 아이콘을 고른다. 고르는 즉시 캔버스에 반영 |
| **이모지** | 컬러 이모지를 고르면 64격자로 도트화한다 |
| **텍스트입력** | 글자·글꼴을 올려 미리 보고, 적용하면 캔버스에 찍힌다 |

하단 상태 줄에는 방금 한 일(에이전트 포트, 불러오기 완료 등)이 한 줄로 나온다. `IconMaker2 사용법 보기`는 제작 노트로 연결된다.

에이전트가 `import_png` / `set_pixels`를 쓰면 같은 캔버스가 바로 바뀐다. 버튼으로 손본 것과 섞어 써도 된다.

---

## MCP 등록

MCP는 앱 안의 기능이 아니라, **Grok/Claude/Cursor 같은 CLI가 IconMaker2를 도구로 부르게 하는 설정**이다. 클라이언트마다 등록 파일과 명령이 다르다. 스크립트는 이 저장소의 [`mcp/iconmaker2-mcp.mjs`](mcp/iconmaker2-mcp.mjs) 하나다.

### 공통 준비

1. IconMaker2를 실행해 `[agent :포트]`를 확인한다.
2. 아래 예시에서 경로 두 곳을 **자기 PC 경로**로 바꾼다.
   - Node: 보통 `C:\Program Files\nodejs\node.exe`
   - 스크립트: 클론한 폴더의 `mcp\iconmaker2-mcp.mjs`
3. 포트가 17890이 아니면 `ICONMAKER2_URL`도 맞춘다.

복붙용 뼈대는 [`mcp.json.example`](mcp.json.example)에도 있다.

제공하는 도구:

| 도구 | 역할 |
|---|---|
| `get_canvas` | 현재 64×64 캔버스 보기 |
| `import_png` | 그림 파일을 격자로 내리기 (초안) |
| `set_pixels` | 점 찍기 / 지우기 (다듬기) |
| `export_icon` | PNG 또는 ICO 저장 |
| `undo` | 마지막 변경 되돌리기 |

### Grok Build

프로젝트 루트 `.mcp.json`:

```json
{
  "mcpServers": {
    "iconmaker2": {
      "command": "C:\\Program Files\\nodejs\\node.exe",
      "args": ["C:\\Source\\IconMaker2\\mcp\\iconmaker2-mcp.mjs"],
      "env": {
        "ICONMAKER2_URL": "http://127.0.0.1:17890"
      }
    }
  }
}
```

Grok을 **WSL**에서 쓰면 `command`만 WSL 경로로 두고, **실행되는 node는 Windows node**여야 한다. WSL의 `127.0.0.1`은 Windows 앱과 다른 머신이다.

```json
"command": "/mnt/c/Program Files/nodejs/node.exe",
"args": ["C:\\Source\\IconMaker2\\mcp\\iconmaker2-mcp.mjs"]
```

설정을 바꾼 뒤에는 Grok 세션을 다시 연다. 도구 목록에 `get_canvas`가 보이면 등록된 것이다.

### Claude Code

```bash
claude mcp add iconmaker2 --env ICONMAKER2_URL=http://127.0.0.1:17890 -- "C:/Program Files/nodejs/node.exe" "C:/Source/IconMaker2/mcp/iconmaker2-mcp.mjs"
```

### Cursor

프로젝트 또는 사용자 `.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "iconmaker2": {
      "command": "C:\\Program Files\\nodejs\\node.exe",
      "args": ["C:\\Source\\IconMaker2\\mcp\\iconmaker2-mcp.mjs"],
      "env": {
        "ICONMAKER2_URL": "http://127.0.0.1:17890"
      }
    }
  }
}
```

### Codex CLI

`~/.codex/config.toml` (Windows는 `%USERPROFILE%\.codex\config.toml`):

```toml
[mcp_servers.iconmaker2]
command = "C:\\Program Files\\nodejs\\node.exe"
args = ["C:\\Source\\IconMaker2\\mcp\\iconmaker2-mcp.mjs"]

[mcp_servers.iconmaker2.env]
ICONMAKER2_URL = "http://127.0.0.1:17890"
```

### Claude Desktop

`%APPDATA%\Claude\claude_desktop_config.json`에 Cursor와 같은 `mcpServers.iconmaker2` 블록을 넣고 Desktop을 재시작한다.

### 연결이 안 될 때

- 앱이 꺼져 있다 → 실행 후 제목의 포트를 확인한다.
- `fetch failed` / `ECONNREFUSED` → CLI의 node가 **Windows node**인지, URL 포트가 제목과 같은지 본다. WSL node로 `127.0.0.1:17890`을 치면 실패하는 것이 정상이다.
- 도구가 목록에 없다 → MCP 설정 변경 후 해당 CLI를 재시작한다.

---

## HTTP (참고)

MCP는 아래 API의 얇은 껍질이다. 앱이 켜진 동안만 동작한다.

| 호출 | 역할 |
|---|---|
| `GET /health` | 생존 확인 |
| `GET /canvas` | 요약 + PNG (`?png=1&pixels=0`) |
| `GET /canvas.png` | 64×64 PNG |
| `POST /import` | `{ "path" }` 또는 `{ "image_base64" }` |
| `POST /pixels` | `{ "mode": "partial"\|"full", "pixels": [{ "x", "y", "color" }] }` — 지울 때 `color`는 `""` |
| `POST /export` | `{ "path", "format": "png"\|"ico" }` |
| `POST /undo` | 되돌리기 |

---

## 라이선스

MIT. [IconMaker](https://github.com/simplvis/IconMaker)에서 갈라져 나왔다.
