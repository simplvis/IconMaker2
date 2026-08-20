#!/usr/bin/env node
/**
 * IconMaker2 MCP — 떠 있는 앱의 로컬 HTTP(http://127.0.0.1:17890)를 감싼다.
 * IconMaker2를 먼저 실행한 뒤 이 서버를 쓰면 된다.
 */
import { readFile } from "node:fs/promises";
import { homedir } from "node:os";
import { join } from "node:path";
import { createInterface } from "node:readline";

const DEFAULT_URL = "http://127.0.0.1:17890";

async function resolveBaseUrl() {
  if (process.env.ICONMAKER2_URL) return process.env.ICONMAKER2_URL.replace(/\/$/, "");
  const candidates = [
    process.env.LOCALAPPDATA
      ? join(process.env.LOCALAPPDATA, "IconMaker2", "agent-url.txt")
      : "",
    join(homedir(), "AppData", "Local", "IconMaker2", "agent-url.txt"),
  ].filter(Boolean);
  for (const file of candidates) {
    try {
      const text = (await readFile(file, "utf8")).trim();
      if (text.startsWith("http")) return text.replace(/\/$/, "");
    } catch {
      /* skip */
    }
  }
  return DEFAULT_URL;
}

function send(msg) {
  process.stdout.write(JSON.stringify(msg) + "\n");
}

function okText(obj) {
  return {
    content: [{ type: "text", text: typeof obj === "string" ? obj : JSON.stringify(obj, null, 2) }],
  };
}

function errText(message) {
  return {
    content: [{ type: "text", text: message }],
    isError: true,
  };
}

async function api(method, path, body) {
  const base = await resolveBaseUrl();
  const url = base + path;
  const res = await fetch(url, {
    method,
    headers: body === undefined ? undefined : { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = { raw: text };
  }
  if (!res.ok) {
    const msg = json.error || text || res.statusText;
    throw new Error(`IconMaker2 ${res.status} ${path}: ${msg}`);
  }
  return json;
}

const TOOLS = [
  {
    name: "get_canvas",
    description:
      "캔버스 상태를 본다. 기본은 전체 요약+PNG. x,y,w,h를 주면 그 구역만 PNG와 픽셀 목록을 준다. 수정 전에 구역을 먼저 볼 것.",
    inputSchema: {
      type: "object",
      properties: {
        include_png: { type: "boolean", description: "기본 true" },
        include_pixels: { type: "boolean", description: "전체 조회 시 기본 false. 구역 조회 시 기본 true" },
        x: { type: "integer" },
        y: { type: "integer" },
        w: { type: "integer" },
        h: { type: "integer" },
      },
    },
  },
  {
    name: "import_png",
    description:
      "그림을 64x64 격자로 내린다(초안). path 또는 image_base64. max_colors로 팔레트를 줄이고 knockout_corners로 밝은 네 모서리를 투명으로 뚫는다.",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string" },
        image_base64: { type: "string" },
        max_colors: { type: "integer", description: "예: 16. 0이면 줄이지 않음" },
        knockout_corners: { type: "boolean", description: "네 모서리 flood 투명" },
      },
    },
  },
  {
    name: "set_pixels",
    description:
      "도트를 직접 찍거나 지운다. mode=partial(기본)은 해당 칸만, full은 캔버스 교체. 지울 때는 color를 빈 문자열.",
    inputSchema: {
      type: "object",
      properties: {
        mode: { type: "string", enum: ["partial", "full"] },
        pixels: {
          type: "array",
          items: {
            type: "object",
            properties: {
              x: { type: "integer" },
              y: { type: "integer" },
              color: { type: "string", description: "#RRGGBB 또는 빈 문자열(지우기)" },
            },
            required: ["x", "y", "color"],
          },
        },
      },
      required: ["pixels"],
    },
  },
  {
    name: "export_icon",
    description: "현재 캔버스를 PNG 또는 ICO로 저장한다.",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "저장 경로" },
        format: { type: "string", enum: ["png", "ico"] },
      },
      required: ["path"],
    },
  },
  {
    name: "undo",
    description: "마지막 캔버스 변경을 되돌린다.",
    inputSchema: { type: "object", properties: {} },
  },
  {
    name: "flood_erase",
    description: "(x,y)에서 비슷한 색을 4방향으로 지워 투명으로 만든다. 모서리·얼룩 제거용.",
    inputSchema: {
      type: "object",
      properties: {
        x: { type: "integer" },
        y: { type: "integer" },
        tolerance: { type: "integer", description: "RGB 합 거리. 기본 32" },
      },
      required: ["x", "y"],
    },
  },
  {
    name: "recolor",
    description: "from 색에 가까운 칸을 to로 바꾼다. to가 빈 문자열이면 투명. 점 하나하나가 아니라 색 단위 수정.",
    inputSchema: {
      type: "object",
      properties: {
        from: { type: "string", description: "#RRGGBB" },
        to: { type: "string", description: "#RRGGBB 또는 빈 문자열" },
        tolerance: { type: "integer", description: "기본 16" },
      },
      required: ["from", "to"],
    },
  },
  {
    name: "fill_rect",
    description: "사각형을 한 색으로 채운다. color가 빈 문자열이면 지운다.",
    inputSchema: {
      type: "object",
      properties: {
        x: { type: "integer" },
        y: { type: "integer" },
        w: { type: "integer" },
        h: { type: "integer" },
        color: { type: "string" },
      },
      required: ["x", "y", "w", "h", "color"],
    },
  },
  {
    name: "draw_line",
    description: "두 점 사이 1픽셀 직선.",
    inputSchema: {
      type: "object",
      properties: {
        x0: { type: "integer" },
        y0: { type: "integer" },
        x1: { type: "integer" },
        y1: { type: "integer" },
        color: { type: "string" },
      },
      required: ["x0", "y0", "x1", "y1", "color"],
    },
  },
];

async function callTool(name, args) {
  args = args || {};
  switch (name) {
    case "get_canvas": {
      const png = args.include_png !== false;
      const hasRegion = [args.x, args.y, args.w, args.h].every((v) => v !== undefined && v !== null);
      const pixels = args.include_pixels === true || (hasRegion && args.include_pixels !== false);
      const q = new URLSearchParams({
        png: png ? "1" : "0",
        pixels: pixels ? "1" : "0",
      });
      if (hasRegion) {
        q.set("x", String(args.x));
        q.set("y", String(args.y));
        q.set("w", String(args.w));
        q.set("h", String(args.h));
      }
      return okText(await api("GET", "/canvas?" + q.toString()));
    }
    case "import_png": {
      if (!args.path && !args.image_base64) {
        return errText("path 또는 image_base64가 필요하다.");
      }
      return okText(await api("POST", "/import", {
        path: args.path,
        image_base64: args.image_base64,
        max_colors: args.max_colors ?? 0,
        knockout_corners: args.knockout_corners === true,
      }));
    }
    case "set_pixels":
      return okText(await api("POST", "/pixels", {
        mode: args.mode || "partial",
        pixels: args.pixels || [],
      }));
    case "export_icon":
      return okText(await api("POST", "/export", {
        path: args.path,
        format: args.format || "png",
      }));
    case "undo":
      return okText(await api("POST", "/undo", {}));
    case "flood_erase":
      return okText(await api("POST", "/flood_erase", {
        x: args.x,
        y: args.y,
        tolerance: args.tolerance ?? 32,
      }));
    case "recolor":
      return okText(await api("POST", "/recolor", {
        from: args.from,
        to: args.to,
        tolerance: args.tolerance ?? 16,
      }));
    case "fill_rect":
      return okText(await api("POST", "/fill_rect", {
        x: args.x, y: args.y, w: args.w, h: args.h, color: args.color,
      }));
    case "draw_line":
      return okText(await api("POST", "/draw_line", {
        x0: args.x0, y0: args.y0, x1: args.x1, y1: args.y1, color: args.color,
      }));
    default:
      return errText(`unknown tool: ${name}`);
  }
}

async function onMessage(msg) {
  if (msg.method === "initialize") {
    send({
      jsonrpc: "2.0",
      id: msg.id,
      result: {
        protocolVersion: msg.params?.protocolVersion || "2024-11-05",
        serverInfo: { name: "iconmaker2", version: "1.0.0" },
        capabilities: { tools: {} },
      },
    });
    return;
  }
  if (msg.method === "notifications/initialized" || msg.method === "notifications/cancelled") {
    return;
  }
  if (msg.method === "ping") {
    send({ jsonrpc: "2.0", id: msg.id, result: {} });
    return;
  }
  if (msg.method === "tools/list") {
    send({ jsonrpc: "2.0", id: msg.id, result: { tools: TOOLS } });
    return;
  }
  if (msg.method === "tools/call") {
    try {
      const result = await callTool(msg.params?.name, msg.params?.arguments);
      send({ jsonrpc: "2.0", id: msg.id, result });
    } catch (e) {
      send({
        jsonrpc: "2.0",
        id: msg.id,
        result: errText(e instanceof Error ? e.message : String(e)),
      });
    }
    return;
  }
  if (msg.id !== undefined) {
    send({
      jsonrpc: "2.0",
      id: msg.id,
      error: { code: -32601, message: `Method not found: ${msg.method}` },
    });
  }
}

const rl = createInterface({ input: process.stdin });
rl.on("line", (line) => {
  const trimmed = line.trim();
  if (!trimmed) return;
  let msg;
  try {
    msg = JSON.parse(trimmed);
  } catch {
    return;
  }
  onMessage(msg).catch((e) => {
    if (msg?.id !== undefined) {
      send({
        jsonrpc: "2.0",
        id: msg.id,
        error: { code: -32000, message: String(e) },
      });
    }
  });
});
