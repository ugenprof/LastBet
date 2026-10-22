# Игра «Последняя ставка» — Unity-версия

Порт браузерной версии (`C:\AI\unity1`, её `CLAUDE.md` описывает весь сюжет и правила) на Unity 6.
Сюжет, правила, баланс и концовки — те же; графика своя, вся рисуется кодом.

## Стек
- Unity 6, C# 9, uGUI (legacy `Text`, системный шрифт Segoe UI), Newtonsoft JSON. Без префабов и UXML:
  сцена `Assets/Scenes/Main.unity` пустая (её создаёт `Editor/ProjectSetup.cs`), всё строит `Boot.cs`.
- Сборка .exe — меню «Последняя ставка → Собрать для Windows».

## Архитектура (повторяет браузерную, имена те же)
- `Resources/Data/*.json` — данные, выгружены из JS скриптом `node Tools/export_story.js C:\AI\unity1`
  (story, texts, catalog, characters + `Config/Balance.cs`, класс `B`). Свои данные Unity-версии — `texts_unity.json`
  (строки, которых не было: кнопки из index.html, HUD, сохранение), `passers.json` (внешность прохожих) и
  `heroes.json` (другие варианты внешности героя: ключи `hero_m_1`…, выбор — `G.S.look`, ключ героя — только через `Data.HeroKey()`).
  В `passers.json` у прохожего `model` (3D-модель) и `props` (реквизит для генератора: bike, scooter, stroller, cane…).
  Сюжет правится в JS и выгружается заново; JSON руками не править (кроме своих файлов).
- `Core/` — `Models.cs` (SceneDef, Choice, Effect, каталоги), `GameData.cs` (`Data.story`, `Data.T("путь")`),
  `State.cs` (`G.S` — единственный источник правды; новая игра — `new GameState()`), `Templates.cs` (`Tpl.Apply`, `Tpl.Fill`),
  `Txt.cs` (убирает эмодзи — шрифт их не рисует), `Run.cs` (таймеры вместо setTimeout), `SaveGame.cs` (сохранение каждое утро).
- `Engine/` — `Game.Render(id)` и реестры `Game.Routes / Screens / Texts / Builders` (как SCENE_ROUTES и т.д.),
  `Rules.cs`, `Effects.cs` (`ApplyChoice`), `Choices.cs` (фильтры кнопок).
- `Scenes/`, `Games/` — логика, файл в файл с `js/scenes`, `js/games`. Регистрация — в `Register()`, вызов — `Game.Init()`.
- `UI/` — только отображение: `UI.cs` (раскладка: фон, слева сцена с персонажами или игровое поле `UI.ShowBoard()`,
  справа панель текста и кнопок, сверху HUD; окна, эффекты), `Kit.cs` (конструкторы элементов), `Widgets.cs`, `Stage.cs`.
- `Art/` — `Painter` (сглаженные фигуры в текстуру), `Backgrounds` (22 фона по полю `bg`), `CharacterArt` (персонажи по
  `CharacterDef`, эмоции normal/happy/sad/angry, варианты blink/talk), `GameArt` (рулетка, слоты, ракета, вещи, товары, значки).

## Правила
- Текст и логику не смешивать: строки — в JSON (`texts_unity.json` для новых), логика — в `Scenes`/`Games`/`Engine`.
- Числа баланса — только `B.*` (из `balance.js`); свои числа Unity-версии — `BU.*` (`Config/BalanceUnity.cs`, `B` перезаписывается выгрузкой).
- Текст сцены, который не влезает в панель, сам уменьшается до `Theme.BodyMinSize` (`UI.FitText`); абзацы ужаты (`ParagraphGap`).
- HUD: шесть плашек (день, деньги + бонусный счёт золотом, зависимость, питание, совесть, рюкзак); значки рюкзака
  уменьшаются по числу вещей (`UI.UpdateHud`). Бонусы, совесть и достижения — как в браузерной версии (её CLAUDE.md).
- Подпись кнопки выбора поднята `TextLift` (Widgets.cs): строка Segoe UI сидит ниже середины, номер выровнен по цифрам.
- Состояние — только через `G.S`; новое поле — в `State.cs`.
- Таймеры сцены — `Run.After / Every / Tween` (отменяются при смене сцены); эффекты интерфейса — `Run.UiAfter / UiTween`.
- Внутри `partial class UI` тип картинки писать как `UnityEngine.UI.Image` (`Image` там — метод).
- В строках `{мужской|женский}` — через `Tpl.Apply`.
- В редакторских скриптах атрибут писать как `[UnityEditor.MenuItem(...)]`: в игре есть свой класс `MenuItem` (меню кафе).
- `Packages/manifest.json` — минимальный набор; в Unity 6.6 нет `com.unity.modules.textrendering` (он в ядре).
  Правя манифест, удалять `packages-lock.json`, иначе Unity вернёт пакеты по умолчанию (purchasing кладёт файл в Resources).
- Проверено в Unity 6000.6.7f1: компиляция, импорт моделей, снимок, Play со сценой из трёх 3D-персонажей.
  Batch-режим: `Unity.exe -batchmode -projectPath . -executeMethod ModelPreview.SnapshotBatch -logFile <файл>`.

- Большие картинки (фоны, персонажи) рисуются в фоновом потоке (`Art/ArtQueue.cs`): `Get` возвращает null, пока не готово.
  Поэтому код рисования (`Painter`, `Backgrounds.Draw`, `CharacterArt.Pixels`) не должен трогать API Unity, кроме структур.

## 3D-модели (Blender)
`Tools/Blender/` — генератор моделей кодом, как и остальной арт: `lb_core.py` (примитивы, материалы, палитра),
`lb_characters.py` (персонажи по `characters.json` + прохожие из `passers.json`), `lb_props.py` (вещи `ITEMS`, казино, окружение),
`build_models.py` — сборка всего → FBX: персонажи в `Assets/Resources/Models/Characters` (их грузит игра),
остальное в `Assets/Models/{Items,Casino,World}` (пока не используется, в сборку не попадает); превью в `Docs/models_*.png`,
`lastbet_models.blend`. Запуск: `blender --background --factory-startup --python Tools/Blender/build_models.py`.
- Стиль «игрушечная диорама»: мягкие формы, большие головы; обычный мир матовый, всё про ставки — глянец, золото, неон (emission).
- FBX руками не править — менять генератор и пересобирать. Модель стоит на z = 0, лицом к -Y Blender (в Unity по настройкам осей экспорта — вдоль +Z, не проверено); 1 единица = 1 м.
- В игре: `UI/Actor3D.cs` — у персонажа своя камера рендерит модель в RenderTexture; кадр `Bust` (сцена, `StageActor`
  вместо 2D `CharacterArt`) или `Full` (прохожие в листовках, примерка в рюкзаке). Моделей нет — остаётся 2D.
  Эмоции — позой (`Actor3D.Animate`), моргания нет.
- Вещи на герое: модель героя — части-дети `…__base`, `…__top_default`, `…__top_hoodie`, `…__acc_watch`…;
  что надето — `G.S.worn` через `Engine/Outfit.cs` (слот — одна вещь), `Actor3D.SetOutfit` включает части. Надеть — в рюкзаке.
- Прохожие в листовках ходят: в модели ноги, руки, колёса, шатун — части `…__anim_leg_l`, `__anim_arm_r`, `__anim_pedal_l`,
  `__anim_kick_r`, `__anim_wheel_f`, `__anim_crank` с началом координат в суставе / на оси (`build_passer` в `lb_characters.py`).
  `Actor3D.Walk` качает их вокруг оси X (направление «вперёд» определяется само при загрузке, скорость колеса — по радиусу).
- Иконки вещей и продуктов лавки — рендеры моделей `Resources/Icons/item_<id>.png`, `product_<номер>.png`
  (`GameArt.Item/Product` берут их, иначе рисуют кодом). Причёски только для 3D — `HAIR_3D` в `lb_characters.py`.
  Материалы собирает `Editor/ModelImport.cs` по имени (`LB_<hex>_r_m_e_c`); проверка — меню «Снимок 3D-персонажей».

## Проверка без Unity
`Tools/Harness` (нужен .NET 8 SDK): заглушки Unity (`FakeUnity.cs`) + весь код игры.
- `dotnet run -- sim 500 1` — 500 случайных прохождений: исключения, зависания, неподставленные `{шаблоны}`, концовки, непосещённые сцены.
- `dotnet run -- art <папка>` — весь арт в PNG.
Заглушки проверяют только связность кода; поведение настоящего Unity (раскладка, отрисовка) они не проверяют.
