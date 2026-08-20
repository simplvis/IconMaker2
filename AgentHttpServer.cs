using System.Net;
using System.Text;
using System.Text.Json;

namespace IconMaker2
{
    internal sealed class AgentHttpServer : IDisposable
    {
        public const int DefaultPort = 17890;
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private readonly Form1 _form;
        private HttpListener? _listener;
        private CancellationTokenSource? _cts;

        public int Port { get; private set; }

        public AgentHttpServer(Form1 form) => _form = form;

        public int Start()
        {
            Exception? last = null;
            for (int port = DefaultPort; port < DefaultPort + 20; port++)
            {
                var listener = new HttpListener();
                string prefix = $"http://127.0.0.1:{port}/";
                listener.Prefixes.Add(prefix);
                try
                {
                    listener.Start();
                    _listener = listener;
                    Port = port;
                    WriteUrlFile(prefix);
                    _cts = new CancellationTokenSource();
                    _ = Task.Run(() => ListenLoop(_cts.Token));
                    return port;
                }
                catch (Exception ex)
                {
                    last = ex;
                    listener.Close();
                }
            }
            throw last ?? new InvalidOperationException("에이전트 포트를 열 수 없다.");
        }

        private async Task ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener is { IsListening: true })
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener.GetContextAsync();
                }
                catch (ObjectDisposedException) { break; }
                catch (HttpListenerException) { break; }

                _ = Task.Run(() => Handle(ctx));
            }
        }

        private async Task Handle(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var res = ctx.Response;
            res.Headers["Access-Control-Allow-Origin"] = "*";
            res.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS";
            res.Headers["Access-Control-Allow-Headers"] = "Content-Type";

            try
            {
                if (req.HttpMethod == "OPTIONS")
                {
                    res.StatusCode = 204;
                    res.Close();
                    return;
                }

                string path = (req.Url?.AbsolutePath ?? "/").TrimEnd('/').ToLowerInvariant();
                if (path.Length == 0) path = "/";

                if (req.HttpMethod == "GET" && path == "/health")
                {
                    await WriteJson(res, 200, new { ok = true, port = Port, grid = Form1.GridSize });
                    return;
                }
                if (req.HttpMethod == "GET" && path == "/canvas.png")
                {
                    byte[] png = _form.AgentGetCanvasPng();
                    res.StatusCode = 200;
                    res.ContentType = "image/png";
                    res.ContentLength64 = png.Length;
                    await res.OutputStream.WriteAsync(png);
                    res.Close();
                    return;
                }
                if (req.HttpMethod == "GET" && path == "/canvas")
                {
                    bool png = QueryFlag(req, "png", defaultValue: true);
                    bool pixels = QueryFlag(req, "pixels", defaultValue: false);
                    int? rx = QueryInt(req, "x");
                    int? ry = QueryInt(req, "y");
                    int? rw = QueryInt(req, "w");
                    int? rh = QueryInt(req, "h");
                    await WriteJson(res, 200, _form.AgentGetCanvas(png, pixels, rx, ry, rw, rh));
                    return;
                }
                if (req.HttpMethod == "POST" && path == "/import")
                {
                    var imp = await ReadImportRequest(req);
                    await WriteJson(res, 200, _form.AgentImport(imp.Bytes, imp.MaxColors, imp.KnockoutCorners));
                    return;
                }
                if (req.HttpMethod == "POST" && path == "/pixels")
                {
                    using var doc = JsonDocument.Parse(await ReadBody(req));
                    string mode = "partial";
                    if (doc.RootElement.TryGetProperty("mode", out var modeProp))
                        mode = modeProp.GetString() ?? "partial";
                    if (!doc.RootElement.TryGetProperty("pixels", out var pixelsProp))
                    {
                        await WriteJson(res, 400, new { ok = false, error = "pixels 배열이 필요하다." });
                        return;
                    }
                    var list = JsonSerializer.Deserialize<List<PixelInfo>>(pixelsProp.GetRawText(), JsonOpts) ?? new();
                    await WriteJson(res, 200, _form.AgentSetPixels(mode, list));
                    return;
                }
                if (req.HttpMethod == "POST" && path == "/export")
                {
                    using var doc = JsonDocument.Parse(await ReadBody(req));
                    string pathOut = doc.RootElement.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
                    string format = doc.RootElement.TryGetProperty("format", out var f) ? f.GetString() ?? "png" : "png";
                    if (string.IsNullOrWhiteSpace(pathOut))
                    {
                        await WriteJson(res, 400, new { ok = false, error = "path가 필요하다." });
                        return;
                    }
                    await WriteJson(res, 200, _form.AgentExport(pathOut, format));
                    return;
                }
                if (req.HttpMethod == "POST" && path == "/undo")
                {
                    await WriteJson(res, 200, _form.AgentUndo());
                    return;
                }
                if (req.HttpMethod == "POST" && path == "/flood_erase")
                {
                    using var doc = JsonDocument.Parse(await ReadBody(req));
                    var el = doc.RootElement;
                    int fx = GetInt(el, "x", -1);
                    int fy = GetInt(el, "y", -1);
                    int tol = GetInt(el, "tolerance", 32);
                    ReadClip(el, out int? cx, out int? cy, out int? cw, out int? ch);
                    await WriteJson(res, 200, _form.AgentFloodErase(fx, fy, tol, cx, cy, cw, ch));
                    return;
                }
                if (req.HttpMethod == "POST" && path == "/recolor")
                {
                    using var doc = JsonDocument.Parse(await ReadBody(req));
                    var el = doc.RootElement;
                    string from = GetStr(el, "from") ?? "";
                    string to = GetStr(el, "to") ?? "";
                    int tol = GetInt(el, "tolerance", 16);
                    ReadClip(el, out int? cx, out int? cy, out int? cw, out int? ch);
                    await WriteJson(res, 200, _form.AgentRecolor(from, to, tol, cx, cy, cw, ch));
                    return;
                }
                if (req.HttpMethod == "POST" && path == "/fill_rect")
                {
                    using var doc = JsonDocument.Parse(await ReadBody(req));
                    await WriteJson(res, 200, _form.AgentFillRect(
                        GetInt(doc.RootElement, "x", 0),
                        GetInt(doc.RootElement, "y", 0),
                        GetInt(doc.RootElement, "w", 1),
                        GetInt(doc.RootElement, "h", 1),
                        GetStr(doc.RootElement, "color") ?? ""));
                    return;
                }
                if (req.HttpMethod == "POST" && path == "/draw_line")
                {
                    using var doc = JsonDocument.Parse(await ReadBody(req));
                    await WriteJson(res, 200, _form.AgentDrawLine(
                        GetInt(doc.RootElement, "x0", 0),
                        GetInt(doc.RootElement, "y0", 0),
                        GetInt(doc.RootElement, "x1", 0),
                        GetInt(doc.RootElement, "y1", 0),
                        GetStr(doc.RootElement, "color") ?? ""));
                    return;
                }

                await WriteJson(res, 404, new
                {
                    ok = false,
                    error = "unknown path",
                    endpoints = new[]
                    {
                        "GET /health", "GET /canvas", "GET /canvas.png",
                        "POST /import", "POST /pixels", "POST /export", "POST /undo",
                        "POST /flood_erase", "POST /recolor", "POST /fill_rect", "POST /draw_line"
                    }
                });
            }
            catch (Exception ex)
            {
                try { await WriteJson(res, 500, new { ok = false, error = ex.Message }); }
                catch { res.Abort(); }
            }
        }

        private static bool QueryFlag(HttpListenerRequest req, string name, bool defaultValue)
        {
            string? v = req.QueryString[name];
            if (string.IsNullOrEmpty(v)) return defaultValue;
            return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static int? QueryInt(HttpListenerRequest req, string name)
        {
            string? v = req.QueryString[name];
            if (int.TryParse(v, out int n)) return n;
            return null;
        }

        private static int GetInt(JsonElement el, string name, int fallback)
        {
            if (!el.TryGetProperty(name, out var p)) return fallback;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out int n)) return n;
            if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out n)) return n;
            return fallback;
        }

        private static string? GetStr(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p)) return null;
            return p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
        }

        private static void ReadClip(JsonElement el, out int? x, out int? y, out int? w, out int? h)
        {
            int cw = GetInt(el, "clip_w", GetInt(el, "w", 0));
            int ch = GetInt(el, "clip_h", GetInt(el, "h", 0));
            if (cw <= 0 || ch <= 0)
            {
                x = y = w = h = null;
                return;
            }
            int cx = GetInt(el, "clip_x", int.MinValue);
            int cy = GetInt(el, "clip_y", int.MinValue);
            if (cx == int.MinValue) cx = GetInt(el, "x", 0);
            if (cy == int.MinValue) cy = GetInt(el, "y", 0);
            x = cx;
            y = cy;
            w = cw;
            h = ch;
        }

        private static async Task<string> ReadBody(HttpListenerRequest req)
        {
            using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        private readonly record struct ImportRequest(byte[] Bytes, int MaxColors, bool KnockoutCorners);

        private static async Task<ImportRequest> ReadImportRequest(HttpListenerRequest req)
        {
            string? ctype = req.ContentType ?? "";
            if (ctype.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                using var ms = new MemoryStream();
                await req.InputStream.CopyToAsync(ms);
                return new ImportRequest(ms.ToArray(), 0, false);
            }

            string body = await ReadBody(req);
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException("import 본문이 비어 있다.");

            using var doc = JsonDocument.Parse(body);
            int maxColors = GetInt(doc.RootElement, "max_colors", GetInt(doc.RootElement, "maxColors", 0));
            bool knockout = false;
            if (doc.RootElement.TryGetProperty("knockout_corners", out var kc) ||
                doc.RootElement.TryGetProperty("knockoutCorners", out kc))
            {
                knockout = kc.ValueKind == JsonValueKind.True ||
                           (kc.ValueKind == JsonValueKind.String && kc.GetString() == "true");
            }

            byte[] bytes;
            if (doc.RootElement.TryGetProperty("image_base64", out var b64) ||
                doc.RootElement.TryGetProperty("imageBase64", out b64))
            {
                string? s = b64.GetString();
                if (string.IsNullOrEmpty(s)) throw new InvalidOperationException("image_base64가 비어 있다.");
                int comma = s.IndexOf(',');
                if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                    s = s[(comma + 1)..];
                bytes = Convert.FromBase64String(s);
            }
            else if (doc.RootElement.TryGetProperty("path", out var pathProp))
            {
                string? path = pathProp.GetString();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    throw new FileNotFoundException("이미지 경로를 찾을 수 없다.", path);
                bytes = await File.ReadAllBytesAsync(path);
            }
            else
                throw new InvalidOperationException("image_base64 또는 path가 필요하다.");

            return new ImportRequest(bytes, maxColors, knockout);
        }

        private static async Task WriteJson(HttpListenerResponse res, int status, object payload)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOpts);
            res.StatusCode = status;
            res.ContentType = "application/json; charset=utf-8";
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
            res.Close();
        }

        private static void WriteUrlFile(string url)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IconMaker2");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "agent-url.txt"), url.TrimEnd('/') + "\n");
            }
            catch { }
        }

        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
            _cts?.Dispose();
        }
    }
}
