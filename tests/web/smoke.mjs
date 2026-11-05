// Browser smoke test for web/: serves the folder locally and plays it in headless Chromium at desktop and
// iPhone 13 sizes. Fails on console errors, page errors, failed or third-party requests, or if the game does not start.
// Run from the repo root: node tests/web/smoke.mjs   (needs Playwright with Chromium)
import { createServer } from "node:http";
import { readFile, stat } from "node:fs/promises";
import { join, extname, normalize } from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
const { chromium, devices } = createRequire(import.meta.url)("playwright");

const root = fileURLToPath(new URL("../../web/", import.meta.url));
const TYPES = { ".html": "text/html; charset=utf-8", ".js": "text/javascript", ".json": "application/json", ".mp3": "audio/mpeg", ".woff2": "font/woff2", ".txt": "text/plain" };
const server = createServer(async (req, res) => {
  const path = normalize(decodeURIComponent(new URL(req.url, "http://x").pathname)).replace(/^([/\\])+/, "") || "index.html";
  try { const f = join(root, path); if (!f.startsWith(root) || !(await stat(f)).isFile()) throw 0;
    res.writeHead(200, { "content-type": TYPES[extname(f)] || "application/octet-stream" }); res.end(await readFile(f)); }
  catch { res.writeHead(404); res.end("not found"); }
}).listen(0);
const base = `http://127.0.0.1:${server.address().port}/`;

const browser = await chromium.launch({ args: ["--autoplay-policy=no-user-gesture-required", "--use-gl=swiftshader", "--enable-unsafe-swiftshader"] });
const failures = [];
async function run(name, ctxOpts) {
  const ctx = await browser.newContext(ctxOpts), page = await ctx.newPage(), problems = [];
  let bytes = 0; const firstLoad = new Set();
  page.on("console", m => { if (m.type() === "error") problems.push("console: " + m.text()); });
  page.on("pageerror", e => problems.push("pageerror: " + e.message));
  page.on("requestfailed", r => problems.push("request failed: " + r.url()));
  page.on("request", r => { if (!r.url().startsWith(base) && !r.url().startsWith("data:") && !r.url().startsWith("blob:")) problems.push("third-party request: " + r.url()); });
  page.on("response", async r => { try { const b = (await r.body()).length; bytes += b; if (!r.url().includes("/audio/")) firstLoad.add(r.url()); } catch { } });
  const t0 = Date.now(); await page.goto(base, { waitUntil: "load" });
  await page.waitForSelector("#loading", { state: "hidden", timeout: 30000 }); const bootMs = Date.now() - t0;
  const shellBytes = bytes, sizeCheck = () => page.evaluate(() => { if (innerWidth > 760) return [];
    const out = []; for (const e of document.querySelectorAll("button, input")) { const r = e.getBoundingClientRect(); if (r.width && r.height && r.height < 44) out.push(`${e.id || e.className || e.tagName} ${Math.round(r.height)}px tall`); }
    for (const e of document.querySelectorAll("body *")) { const r = e.getBoundingClientRect(); if (!r.width || ![...e.childNodes].some(n => n.nodeType === 3 && n.textContent.trim())) continue;
      const fs = parseFloat(getComputedStyle(e).fontSize); if (fs < 12) out.push(`${e.id || e.className || e.tagName} text ${fs}px`); } return out; });
  const small = await sizeCheck();
  // Start a new game (dismiss the tutorial), then play the suggested piece
  await page.click("#m-new"); await page.click("#tut-close");
  await page.waitForFunction(() => window.__orchestra.state && !document.querySelector("#topbar").hidden);
  await page.waitForFunction(() => window.__orchestra.actx, null, { timeout: 10000 });
  await page.click('[data-tab="music"]'); await page.click("#a-listen");       // Music & Concerts: Listen
  await page.waitForFunction(() => !!window.__orchestra.perf, null, { timeout: 60000 });
  await page.waitForTimeout(1500);
  const playing = await page.evaluate(() => ({ t: window.__orchestra.actx.currentTime, state: window.__orchestra.actx.state }));
  // Tab hidden: loop and audio pause; visible again: both resume
  await page.evaluate(() => { Object.defineProperty(document, "hidden", { value: true, configurable: true }); Object.defineProperty(document, "visibilityState", { value: "hidden", configurable: true }); document.dispatchEvent(new Event("visibilitychange")); });
  await page.waitForTimeout(400); const away = await page.evaluate(() => window.__orchestra.actx.state);
  await page.evaluate(() => { Object.defineProperty(document, "hidden", { value: false, configurable: true }); Object.defineProperty(document, "visibilityState", { value: "visible", configurable: true }); document.dispatchEvent(new Event("visibilitychange")); });
  await page.waitForTimeout(400); const back = await page.evaluate(() => window.__orchestra.actx.state);
  small.push(...await sizeCheck());   // in game, a panel open
  await page.screenshot({ path: fileURLToPath(new URL(`./${name}.png`, import.meta.url)) });
  const ok = playing.state === "running" && away === "suspended" && back === "running";
  if (!ok) problems.push(`audio states: playing ${playing.state}, hidden ${away}, visible ${back}`);
  for (const s of small) problems.push("phone size: " + s);
  console.log(`${name}: boot ${bootMs} ms, page shell ${(shellBytes / 1024).toFixed(0)} KB, total with audio ${(bytes / 1024).toFixed(0)} KB, ${problems.length} problem(s)`);
  for (const p of problems) console.log("  - " + p);
  if (problems.length) failures.push(name);
  await ctx.close();
}
await run("desktop", { viewport: { width: 1366, height: 800 } });
await run("iphone13", { ...devices["iPhone 13"] });
await browser.close(); server.close();
if (failures.length) { console.error("FAILED: " + failures.join(", ")); process.exit(1); }
console.log("PASS");
