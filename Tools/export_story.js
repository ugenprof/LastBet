// Экспорт сюжета и настроек из браузерной версии в JSON для Unity.
// Запуск: node Tools/export_story.js <папка браузерной версии>
// Результат: Assets/Resources/Data/*.json и Assets/Scripts/Config/Balance.cs
const fs = require("fs");
const path = require("path");
const vm = require("vm");

const src = process.argv[2] || "C:/AI/unity1";
const root = path.resolve(__dirname, "..");
const outData = path.join(root, "Assets/Resources/Data");
const outBalance = path.join(root, "Assets/Scripts/Config/Balance.cs");
fs.mkdirSync(outData, { recursive: true });
fs.mkdirSync(path.dirname(outBalance), { recursive: true });

const files = [
  "js/config/balance.js", "js/config/catalog.js", "js/config/characters.js", "js/config/music.js",
  "js/story/story.js", "js/story/intro.js", "js/story/chapter1.js", "js/story/chapter2.js", "js/story/daily.js",
  "js/story/family.js", "js/story/town.js", "js/story/jobs.js", "js/story/theft.js", "js/story/casino.js",
  "js/story/recovery.js", "js/story/sunday.js", "js/story/endings.js", "js/story/texts.js"
];
const ctx = vm.createContext({ console });
for (const f of files) vm.runInContext(fs.readFileSync(path.join(src, f), "utf8"), ctx, { filename: f });
const get = (expr) => vm.runInContext(expr, ctx);

// Сюжет: нормализуем поля с двумя типами, чтобы C# читал их одним типом
const story = get("story");
for (const scene of Object.values(story)) {
  for (const c of scene.choices || []) {
    if (c.lie === true) c.lie = "always";
    if (c.effect && c.effect.dadTalk === true) c.effect.dadTalk = "mom";
  }
}
const write = (name, data) => fs.writeFileSync(path.join(outData, name), JSON.stringify(data, null, 1), "utf8");
write("story.json", story);
// HTML-разметка браузерной версии → rich text Unity (картинки вещей рисует сам интерфейс)
const RICH = [
  [/<img[^>]*>\s*/g, ""],
  [/<span class="result-loss">(.*?)<\/span>/g, "<color=#f87171><b>$1</b></color>"],
  [/<span class="result-win">(.*?)<\/span>/g, "<color=#4ade80><b>$1</b></color>"],
  [/<span class="result-jackpot">(.*?)<\/span>/g, "<color=#facc15><b>$1</b></color>"],
  [/<span class="match-odds">(.*?)<\/span>/g, "  <color=#facc15>$1</color>"],
  [/<span style="[^"]*">(.*?)<\/span>/g, "  <color=#f87171>$1</color>"],
  [/<span>(.*?)<\/span>/g, "$1"]
];
const toRich = (v) => {
  if (typeof v === "string") return RICH.reduce((s, [re, to]) => s.replace(re, to), v);
  if (Array.isArray(v)) return v.map(toRich);
  if (v && typeof v === "object") return Object.fromEntries(Object.entries(v).map(([k, x]) => [k, toRich(x)]));
  return v;
};
write("texts.json", toRich(get("TEXTS")));
write("catalog.json", get(`({ ITEMS, CLOTHES_ITEMS, TECH_ITEMS, LUNCH_DISHES, MATCH_TEAMS, FLYER_PASSERS, GROCERY_PRODUCTS,
  COURIER_DISTRICTS, CAFE_MENU, CAFE_BILLS, SLOT_SYMBOLS, SUNDAY_ACTIVITIES, JOB_RAISE, BONUS_OFFERS, MUSIC_BY_BG, MUSIC_TRACKS })`));
write("characters.json", get("CHARACTERS"));

// Числа баланса → C#-константы (объекты — в catalog.json)
const balanceSrc = fs.readFileSync(path.join(src, "js/config/balance.js"), "utf8");
const lines = ["// Сгенерировано Tools/export_story.js из js/config/balance.js — не править руками.", "public static class B", "{"];
for (const line of balanceSrc.split("\n")) {
  const m = line.match(/^const (\w+)\s*=.*?;\s*(\/\/.*)?$/) || line.match(/^const (\w+)\s*=/);
  if (!m) continue;
  const name = m[1];
  const v = get(name);
  const comment = (line.match(/;\s*(\/\/.*)$/) || [])[1];
  let decl;
  if (typeof v === "number") decl = Number.isInteger(v) ? `public const int ${name} = ${v};` : `public const float ${name} = ${v}f;`;
  else if (typeof v === "string") decl = `public const string ${name} = ${JSON.stringify(v)};`;
  else if (Array.isArray(v) && v.every(Number.isInteger)) decl = `public static readonly int[] ${name} = { ${v.join(", ")} };`;
  else continue;
  lines.push(`    ${decl}${comment ? " " + comment : ""}`);
}
lines.push("}");
fs.writeFileSync(outBalance, lines.join("\n") + "\n", "utf8");
console.log("Сцен:", Object.keys(story).length);
