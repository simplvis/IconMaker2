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
      "실행 중인 IconMaker2 캔버스 상태를 본다. 64x64 격자 요약과 PNG(base64). include_pixels=true면 점유 픽셀 목록도 준다.",
    inputSchema: {
      type: "object",
      properties: {
        include_png: { type: "boolean", description: "기본 true. 64x64 PNG base64" },
        include_pixels: { type: "boolean", description: "기본 false. 색 있는 칸 목록" },
      },
    },
  },
  {
    name: "import_png",
    description:
      "그림 파일을 64x64 격자로 내린다(초안). path 또는 image_base64 중 하나.",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "로컬 PNG/JPG 경로" },
        image_base64: { type: "string", description: "PNG/JPEG base64 (data: URL 가능)" },
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
];

async function callTool(name, args) {
  args = args || {};
  switch (name) {
    case "get_canvas": {
      const png = args.include_png !== false;
      const pixels = args.include_pixels === true;
      const q = `?png=${png ? "1" : "0"}&pixels=${pixels ? "1" : "0"}`;
      return okText(await api("GET", "/canvas" + q));
    }
    case "import_png": {
      if (!args.path && !args.image_base64) {
        return errText("path 또는 image_base64가 필요하다.");
      }
      return okText(await api("POST", "/import", {
        path: args.path,
        image_base64: args.image_base64,
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
