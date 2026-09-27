/**
 * Mixamo batch download (FBX for Unity, without skin).
 * Usage: node mixamo-download.mjs <access_token> <out_dir>
 */
import fs from "fs";
import path from "path";
import https from "https";
import http from "http";

const token = process.argv[2];
const outDir = process.argv[3];
if (!token || !outDir) {
  console.error("usage: node mixamo-download.mjs <token> <outDir>");
  process.exit(1);
}
fs.mkdirSync(outDir, { recursive: true });

const CHAR = "2dee24f8-3b49-48af-b735-c6377509eaac"; // X Bot
const API = "https://www.mixamo.com/api/v1";

const JOBS = [
  // Daha sert idle (Standing Idle fazla oynaktı)
  ["Idle", "Fighting Idle", "Fighting Idle", true],
  // Cast'ler snappier overdrive ile yeniden
  ["Melee_Slash", "Standing Melee Attack Horizontal", "Standing Melee Attack Horizontal", true],
  ["Melee_Thrust", "Standing Melee Attack Downward", "Standing Melee Attack Downward", true],
  ["Melee_Punch", "Hook Punch", "Hook Punch", true],
  ["Spell_Cast", "Standing 2H Magic Attack 01", "Standing 2H Magic Attack 01", true],
];

/** Cast kliplerinde overdrive — daha sert/hızlı silüet (0 = nötr, ~0.5–1 snappier). */
const OVERDRIVE_BOOST = 0.65;

function req(method, url, body) {
  return new Promise((resolve, reject) => {
    const u = new URL(url);
    const lib = u.protocol === "https:" ? https : http;
    const data = body ? JSON.stringify(body) : null;
    const opts = {
      method,
      hostname: u.hostname,
      path: u.pathname + u.search,
      headers: {
        Accept: "application/json",
        "X-Api-Key": "mixamo2",
        Authorization: "Bearer " + token,
        ...(data
          ? { "Content-Type": "application/json", "Content-Length": Buffer.byteLength(data) }
          : {}),
      },
    };
    const r = lib.request(opts, (res) => {
      const chunks = [];
      res.on("data", (c) => chunks.push(c));
      res.on("end", () => {
        const buf = Buffer.concat(chunks);
        const text = buf.toString("utf8");
        let json = null;
        try {
          json = JSON.parse(text);
        } catch {}
        resolve({ status: res.statusCode, json, text, buf, headers: res.headers });
      });
    });
    r.on("error", reject);
    if (data) r.write(data);
    r.end();
  });
}

function download(url, filePath) {
  return new Promise((resolve, reject) => {
    const u = new URL(url);
    const lib = u.protocol === "https:" ? https : http;
    const r = lib.get(u, (res) => {
      if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
        download(res.headers.location, filePath).then(resolve, reject);
        return;
      }
      if (res.statusCode !== 200) {
        reject(new Error("download http " + res.statusCode));
        return;
      }
      const ws = fs.createWriteStream(filePath);
      res.pipe(ws);
      ws.on("finish", () => resolve(filePath));
      ws.on("error", reject);
    });
    r.on("error", reject);
  });
}

function gmsify(gms, inplace, boostOverdrive) {
  // Mixamo gmsifySequence: params.map(p => p.value).join(",")
  const pairs = Array.isArray(gms.params) ? gms.params : [];
  const values = pairs.map((p) => (Array.isArray(p) ? p[1] : p));
  const overPair = pairs.find((p) => Array.isArray(p) && /overdrive/i.test(String(p[0])));
  let overdrive = overPair ? overPair[1] : values.length === 1 ? values[0] : 0;
  if (boostOverdrive) overdrive = Number(overdrive) + OVERDRIVE_BOOST;
  // params string: overdrive değeri son slotta ise güncelle
  let paramsStr = values.length ? values.join(",") : String(overdrive);
  if (boostOverdrive && overPair) {
    const idx = pairs.findIndex((p) => Array.isArray(p) && /overdrive/i.test(String(p[0])));
    if (idx >= 0) {
      const copy = values.slice();
      copy[idx] = overdrive;
      paramsStr = copy.join(",");
    }
  }
  return [
    {
      "model-id": gms["model-id"],
      mirror: !!gms.mirror,
      trim: gms.trim || [0, 100],
      overdrive,
      params: paramsStr,
      "arm-space": gms["arm-space"] || 0,
      inplace: !!inplace,
    },
  ];
}

async function findExact(query, exactName) {
  const url =
    API +
    "/products?page=1&limit=48&order=&type=Motion%2CMotionPack&query=" +
    encodeURIComponent(query);
  const { json } = await req("GET", url);
  const list = (json && json.results) || [];
  return list.find((x) => x.name === exactName) || null;
}

async function exportOne(query, exactName, inplace) {
  const product = await findExact(query, exactName);
  if (!product) throw new Error("not found: " + exactName);
  await sleep(2500);
  const full = (await req("GET", API + "/products/" + product.id + "?similar=0")).json;
  const boost = exactName !== "Fighting Idle";
  const body = {
    gms_hash: gmsify(full.details.gms_hash, inplace, boost),
    preferences: { format: "fbx7_unity", skin: "false", fps: "30", reducekf: "0" },
    character_id: CHAR,
    type: "Motion",
    product_name: full.name,
  };
  let exp = (await req("POST", API + "/animations/export", body)).json;
  for (let n = 0; n < 60; n++) {
    if (exp.status === "completed") return { name: full.name, url: exp.job_result };
    if (exp.status === "failed")
      throw new Error((exp.job_result && exp.job_result.message) || exp.message || "failed");
    await sleep(1500);
    exp = (await req("GET", API + "/characters/" + CHAR + "/monitor")).json;
  }
  throw new Error("timeout");
}

function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

const summary = [];
for (const [label, query, exact, inplace] of JOBS) {
  const dest = path.join(outDir, label + ".fbx");
  process.stdout.write(label + " ... ");
  try {
    const { name, url } = await exportOne(query, exact, inplace);
    await download(url, dest);
    const size = fs.statSync(dest).size;
    console.log("OK " + name + " (" + size + " bytes)");
    summary.push({ label, name, ok: true, size });
  } catch (e) {
    console.log("FAIL " + e.message);
    summary.push({ label, ok: false, error: e.message });
    await sleep(4000);
  }
}

fs.writeFileSync(path.join(outDir, "download-summary.json"), JSON.stringify(summary, null, 2));
const ok = summary.filter((s) => s.ok).length;
console.log("done " + ok + "/" + summary.length);
process.exit(ok > 0 ? 0 : 1);
