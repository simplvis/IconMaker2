# IconMaker2

Windows에서 64×64 픽셀 아이콘을 만들고, CLI 에이전트(Grok, Claude, Cursor, Codex 등)가 MCP로 같은 캔버스를 조종할 수 있게 한 편집기다.

일반 사용자는 앱만 실행하면 된다. 에이전트 연동은 **앱을 켠 뒤**, 쓰는 CLI에 MCP만 등록하면 된다.

> Windows x64 전용이다. Linux/macOS에서는 이 창을 네이티브로 띄울 수 없다.

---

## 요구 사항

- Windows 10/11 x64
- 빌드 시: [.NET 10 SDK](https://dotnet.microsoft.com/download)
- 에이전트 연동 시: [Node.js](https://nodejs.org/) (Windows 빌드)

---

## 실행

```bat
dotnet run --project IconMaker2.csproj
```

또는 게시된 `IconMaker2.exe`를 연다.

창 제목에 `[agent :17890]`이 보이면 로컬 HTTP가 열린 것이다.

```
GET  http://127.0.0.1:17890/health
GET  http://127.0.0.1:17890/canvas
POST http://127.0.0.1:17890/import
POST http://127.0.0.1:17890/pixels
POST http://127.0.0.1:17890/export
POST http://127.0.0.1:17890/undo
```

---

## 앱만 쓰는 경우

팔레트, 불러오기, 클립보드, 시스템 아이콘, 이모지, 텍스트, PNG/ICO 저장으로 직접 그린다. MCP는 필요 없다.

---

## 에이전트로 쓰는 경우

1. IconMaker2를 실행한다. (`[agent :17890]`)
2. 아래 MCP 설정을 **사용하는 CLI에만** 넣는다. 클라이언트마다 파일 위치가 다르다.
3. `node.exe`와 `iconmaker2-mcp.mjs` 경로는 설치 위치에 맞게 바꾼다.

MCP 스크립트: [`mcp/iconmaker2-mcp.mjs`](mcp/iconmaker2-mcp.mjs)

도구: `get_canvas`, `import_png`, `set_pixels`, `export_icon`, `undo`

### Grok Build (프로젝트 `.mcp.json`)

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

Grok이 WSL에서 돌면 `command`는 WSL 경로여도 된다. 다만 **스크립트를 실행하는 node가 Windows node**여야 `127.0.0.1:17890`으로 앱에 붙는다.

```json
"command": "/mnt/c/Program Files/nodejs/node.exe",
"args": ["C:\\Source\\IconMaker2\\mcp\\iconmaker2-mcp.mjs"]
```

### Claude Code

```bash
claude mcp add iconmaker2 --env ICONMAKER2_URL=http://127.0.0.1:17890 -- "C:/Program Files/nodejs/node.exe" "C:/Source/IconMaker2/mcp/iconmaker2-mcp.mjs"
```

### Cursor (`.cursor/mcp.json`)

```json
{
  "mcpServers": {
    "iconmaker2": {
      "command": "C:\\Program Files\\nodejs\\node.exe",
      "args": ["C:\\Source\\IconMaker2\\mcp\\iconmaker2-mcp.mjs"],
      "env": { "ICONMAKER2_URL": "http://127.0.0.1:17890" }
    }
  }
}
```

### Codex CLI (`~/.codex/config.toml`)

```toml
[mcp_servers.iconmaker2]
command = "C:\\Program Files\\nodejs\\node.exe"
args = ["C:\\Source\\IconMaker2\\mcp\\iconmaker2-mcp.mjs"]

[mcp_servers.iconmaker2.env]
ICONMAKER2_URL = "http://127.0.0.1:17890"
```

### Claude Desktop

`%APPDATA%\Claude\claude_desktop_config.json`에 Cursor와 같은 `mcpServers.iconmaker2` 블록을 넣는다.

---

## 라이선스

MIT. 원 프로젝트 [IconMaker](https://github.com/simplvis/IconMaker)에서 갈라져 나왔다.
