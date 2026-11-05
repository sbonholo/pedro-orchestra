// Builds the game's local copies of its libraries, so the browser game needs no CDN at run time.
//  web/vendor/three.js : Three.js 0.160 + OrbitControls, only the classes web/index.html uses (minified ES module)
//  web/fonts/*.woff2   : the three typefaces (Latin subset, only the weights the CSS uses)
// Run after adding a new THREE.Something to web/index.html: npm ci && npm run build
import { build } from "esbuild";
import { readFileSync, writeFileSync, mkdirSync, copyFileSync, rmSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const here = dirname(fileURLToPath(import.meta.url)), web = join(here, "../../web");
const html = readFileSync(join(web, "index.html"), "utf8");
const names = [...new Set([...html.matchAll(/\bTHREE\.([A-Za-z_]\w*)/g)].map(m => m[1]))].sort();
const entry = `export { ${names.join(", ")} } from "three";\nexport { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";\n`;
mkdirSync(join(web, "vendor"), { recursive: true });
await build({ stdin: { contents: entry, resolveDir: here }, bundle: true, format: "esm", minify: true, target: "es2020",
  legalComments: "eof", outfile: join(web, "vendor/three.js"), logLevel: "warning" });   // an unknown name fails the build
console.log(`vendor/three.js: ${names.length} Three.js exports + OrbitControls`);

const FONTS = { "cormorant-garamond": [500, 600, 700], "ibm-plex-sans": [400, 500, 600], "ibm-plex-mono": [400, 500] };
rmSync(join(web, "fonts"), { recursive: true, force: true }); mkdirSync(join(web, "fonts"));
for (const [fam, weights] of Object.entries(FONTS)) for (const w of weights) {
  const f = `${fam}-latin-${w}-normal.woff2`; copyFileSync(join(here, "node_modules/@fontsource", fam, "files", f), join(web, "fonts", f)); }
for (const fam of Object.keys(FONTS)) copyFileSync(join(here, "node_modules/@fontsource", fam, "LICENSE"), join(web, "fonts", `LICENSE-${fam}.txt`));
console.log("fonts: " + Object.entries(FONTS).map(([f, w]) => `${f} ${w.join("/")}`).join(", "));
