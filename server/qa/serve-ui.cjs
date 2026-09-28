// Preview production frontend with a local-only, deterministic game-state fixture.
const http = require("node:http");
const fs = require("node:fs");
const path = require("node:path");
const root = path.resolve(__dirname, "../public");
const types = { ".html": "text/html; charset=utf-8", ".js": "text/javascript; charset=utf-8", ".css": "text/css; charset=utf-8", ".json": "application/json; charset=utf-8", ".jpg": "image/jpeg", ".png": "image/png", ".woff2": "font/woff2" };
http.createServer((req, res) => {
  const url = new URL(req.url, "http://localhost");
  res.setHeader("Cache-Control", "no-store");
  if (url.pathname === "/__qa__/grave") {
    res.setHeader("Content-Type", types[".html"]);
    const page = fs.readFileSync(path.join(root, "index.html"), "utf8")
      .replace("<head>", '<head><base href="/">')
      .replace("</body>", '<script src="/__qa__/fixture.js"></script></body>');
    return res.end(page);
  }
  let filename;
  if (url.pathname === "/__qa__/fixture.js") filename = path.join(__dirname, "grave-fixture.js");
  else {
    filename = path.resolve(root, "." + decodeURIComponent(url.pathname === "/" ? "/index.html" : url.pathname));
    if (!filename.startsWith(root + path.sep)) { res.writeHead(403); return res.end(); }
  }
  fs.readFile(filename, (error, data) => {
    if (error) { res.writeHead(404); return res.end("Not found"); }
    res.setHeader("Content-Type", types[path.extname(filename)] || "application/octet-stream");
    res.end(data);
  });
}).listen(8789, "127.0.0.1", () => console.log("UI preview http://127.0.0.1:8789/__qa__/grave"));
