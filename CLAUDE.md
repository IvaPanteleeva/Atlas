# Атлас ERP

Малко ERP за Атлас, нашата фирма подизпълнител: ремонтни обекти, работни дни с позиции и цени, плащания, екип, задачи, ценоразпис.
ЖЕНИСТРОЙ ЕООД е друга фирма, която ни дава офертите (възложител). Атлас и Женистрой са различни фирми.
Референтен прототип (само опорна точка, не спецификация): https://claude.ai/artifact/4JEeA216cX2Z85gWN81aU3

## Stack
- Backend: .NET 10, Minimal APIs по feature (Sites, WorkDays, Payments, People, Tasks, RateCard), EF Core, PostgreSQL (SQLite локално в началото)
- Frontend: Angular 22 (създаден с `npx @angular/cli@22 new`, глобалното CLI е 20 и не се ползва), zoneless, signals, strict smart/dumb компоненти, `httpResource()` за четене
- Мобилният изглед е основен (попълва се на обекта)
- UI на български, валута EUR, locale `bg` ("1 157,89 €")

## Структура на repo-то
Един API проект с папки по feature, без отделни слоеве като в Gemsoft.
```
Атлас/
  src/Atlas.Api/            Features/Sites, Features/WorkDays, ... + Data/ (DbContext)
  tests/Atlas.Api.Tests/
  web/                      Angular
  docs/                     ai-log.md, screenshots/
```
- Отделно git repo в тази папка, branch `main`. GitHub: https://github.com/IvaPanteleeva/Atlas — private до предаването на курсовата, после public (линкът е за курсовата).
- Хостингът се решава след стъпка 5a. Дотогава CI прави само build и тестове.
- Solution е `Atlas.slnx`. Тестове с xUnit (+ `WebApplicationFactory` за интеграционни). Angular без SSR, със SCSS и routing.

## Стъпка 0: подготовка на repo-то
- 0.1 `git init`, `.gitignore`, `docs/ai-log.md`, `docs/screenshots/`
- 0.2 `Atlas.slnx`, празен `Atlas.Api` със static files + fallback + Scalar (само Development), `Atlas.Api.Tests`
- 0.3 Angular в `web/`, `outputPath` към `wwwroot`, `proxy.conf.json`
- 0.4 първи commit, GitHub repo (създава го Ива в github.com), push
- 0.5 AI настройки на проекта (виж „AI инструменти в repo-то“)
Всяка подстъпка завършва с проверка (build минава, страницата се отваря).

## AI инструменти в repo-то
- Скилове в `.claude/skills/`: `grill-me`, `grilling`, `caveman`; `angular-developer` (Google, MIT); `dotnet-webapi`, `create-datadriven-aspnetcore`, `run-tests` (github.com/dotnet/skills, MIT). При конфликт между скил и CLAUDE.md, CLAUDE.md печели.
- Subagents в `.claude/agents/`: `be-expert` (src/, tests/) и `fe-expert` (web/). `portal-reviewer` се добавя в стъпка 5a.
- Hooks в `.claude/settings.json` (скриптове в `.claude/hooks/`): след промяна на `.cs` тръгва `dotnet build`, след промяна в `web/` тръгва prettier; в края на отговора тръгват тестовете, ако има промени в кода след последния commit.
- MCP: Angular CLI (`.mcp.json`).
- Команда `/step-done <подстъпка>`: build, тестове, обобщение и чернова за `docs/ai-log.md`.
- CI (build + тестове) идва преди модул 6, ревюто с `@claude` в GitHub Actions — в модул 6. Cursor/Copilot — в модули 3 и 5.
- `docs/` (ai-log и screenshot-и) е само локално, в `.gitignore`. Финалната версия отива в Google документа.

## Деплой (едно repo)
- Както в Gemsoft: Angular build-ът отива в `wwwroot` на API-то, деплойва се само API-то.
- `wwwroot/` е в `.gitignore`. Готовите файлове не се commit-ват.
- GitHub Actions прави build: `npm ci`, `ng build`, `dotnet publish`.
- Локално: `proxy.conf.json` пренасочва `/api` от `ng serve` към API-то. Без CORS, същият origin като в production.
- `outputPath` е `{ "base": "<път>/wwwroot", "browser": "" }`, файловете отиват директно в `wwwroot`. Стандартни `UseStaticFiles()` + `MapFallbackToFile("index.html")`.

## Правила за домейна
- Пари винаги `decimal`, никога `double`.
- `WorkItem` пази собствена цена (копие от ценоразписа), не референция. Промяна в ценоразписа не пипа историята.
- `Rate` е nullable: `null` = "цена?", не влиза в сумите, но се брои като "за уточняване". Има и флаг `NeedsReview`.
- Сумите (изработено, платено, остатък) не се записват, а се смятат на сървъра (`GET /sites/{id}/summary`).
- `WorkDay` е за една дата; работниците са many-to-many към `Person` (нужно за заплащане по човек).
- Оферта ≠ свършена работа. По-късно: отделен `Offer`, който се изпълнява в работни дни.

## Достъп за възложителя
- Роля `ClientViewer` с claim `CompanyId`. Само четене.
- Филтърът по `CompanyId` се прави на сървъра при всяка заявка, никога в Angular.
- Отделни endpoints и DTO-та под `/portal/...`, не същите като вътрешните. Възложителят НЕ вижда: бележки, контакти, екипа, позиции без цена или с NeedsReview, бъдещи WorkerPayouts.
- `WorkDay` има `IsSharedWithClient` (или статус Draft/Sent): възложителят вижда само изпратените дни, за да не вижда чернови.
- Тест: потребител на една фирма не може да достъпи обект на друга фирма (403/404).

## Модел (първа версия)
- `Company`: Name (напр. Женистрой) — кой ни възлага работата и ни плаща
- `Site`: Name, CompanyId?, EndCustomer? (напр. "Професорът"), Address?, MapUrl?, Note?, Status (Lead | Active | Done)
- `WorkDay`: SiteId, Date (DateOnly), Title?, Note?, IsSharedWithClient, Workers[], Items[]
- `WorkItem`: Description, Quantity, Unit (LinearMeter | SquareMeter | Day | Piece), Rate?, NeedsReview; Amount = Quantity × Rate
- `RateCardItem`: Description, Unit, Rate
- `ClientPayment`: SiteId, Date, Amount, Note? (плащане от възложителя към Атлас)
- `Person`: Name, Phone?, Note?, IsCrew
- `SiteTask`: SiteId?, Text, Done
- По-късно: `WorkerPayout` (какво дължим на хората), `Offer`

### Решения спрямо прототипа
- Един `WorkDay` = една дата. Няма `DateEnd`; работа за няколко дни се записва като няколко дни (точно броене на дни по човек).
- `Unit` е enum, не свободен текст. В UI: м.л., м², ден, бр.
- Без оферти засега: няма `kind`/planned на `WorkDay`, той е само свършена работа. Офертите идват с `Offer` (стъпка 7).
- `Site.CompanyId` е nullable (директен клиент без фирма), `EndCustomer` е свободен текст. Прототипното поле `client` се разделя на двете.
- Работниците са many-to-many `WorkDay` ↔ `Person` по Id, не по име (преименуване не чупи историята).
- Флагът `flag` от прототипа е `NeedsReview`.
- `Note` на `Site` и `Person` е вътрешно поле и не излиза в `/portal/...`.

## Ред на работа
1. Sites + WorkDays + WorkItems + RateCard
2. ClientPayments + summary
3. People + кой е работил
4. Tasks
5. Login и роли (ASP.NET Core Identity): Owner, Crew, ClientViewer
5a. Портал за възложителя: ClientViewer от Женистрой вижда само обектите на своята Company и колко дължи
6. WorkerPayouts
7. Offers
8. Seed с реалните данни от бележката

## Курсов проект
Проектът е и курсова работа: документ (3–6 стр.), който описва системата по модули и как AI е помогнал за всеки.
За всеки модул трябват: подход, стъпки (какво е поискано от AI и какво е променено), тестване, избор на AI инструмент и 2–3 ключови prompt-а.
Модули: 1) Database layer, 2) API и бизнес логика, 3) Authentication и роли, 4) Портал за възложителя, 5) Angular UI, 6) Testing layer.
- Курсовият обхват е стъпки 1–5a от реда на работа. Останалото е след предаването.

### Инструменти по модули
Целта е реално сравнение на инструментите в документа: всеки се ползва там, където е най-силен.
1. Database layer — Claude Code: CLAUDE.md като памет на проекта, Plan Mode. Доказателство: entities и миграция по правилата тук.
2. API и бизнес логика — Claude Code: agentic TDD (първо тестове, които падат, после кодът). Доказателство: тестове за `GET /sites/{id}/summary` (`null` цени, `NeedsReview`).
3. Authentication и роли — Cursor (или Copilot, ако няма Cursor): prompt за целия проект „покажи план преди промени“, отделен worktree. Доказателство: Identity с роли, въведено с малки стъпки.
4. Портал за възложителя — Claude Code: собствен subagent в `.claude/agents/` за ревю срещу изтичане на данни. Доказателство: тест, че друга фирма получава 403/404.
5. Angular UI — GitHub Copilot (или Cursor): код по коментар, промени в избрания код, компонент от screenshot на прототипа. Доказателство: екрани на телефон.
6. Testing layer — Claude Code: hooks (тестовете тръгват след промяна), GitHub Actions с `@claude` за ревю на PR. Доказателство: CI, който минава, и ревю в PR.
- Правилата живеят в CLAUDE.md. Инструкциите за Copilot и правилата за Cursor само препращат към него, без копие.
- Prompt-овете следват шаблона: цел, ограничения, засегнати файлове, „първо план, после чакай одобрение“.
- След всяка завършена стъпка помогни на Ива да добави запис в `docs/ai-log.md`: модул, prompt (дословно), какво генерира AI, какво е променено ръчно и защо, как е тествано.
- Пази screenshot-и в `docs/screenshots/` (UI, Swagger/Postman, тестове).

## Как работим
По условието на курсовата целият код се пише от AI. Ива не пипа код: тя взема решенията, дава prompt-ове, ревюира и тества. Claude пише кода, пуска командите и проверява резултата сам. Не прави повече от договорената подстъпка; обяснявай избора, давай малки стъпки.
- Всяка стъпка от реда на работа се разбива на подстъпки (напр. 1.1, 1.2). Започваме подстъпка само след като решенията за нея са изяснени.
- Преди подстъпка, в която има решения, ползвай скила `grilling` (`/grill-me`): въпроси на рундове, номерирани, всеки с препоръка, формулирани така, че „да“ приема препоръката. Факти от кода и средата търси сам, не питай за тях.
- Отговорите са в стила на скила `caveman`: първо отговорът, без излишни думи, всеки технически факт остава. Код, документи, commit-и и `docs/ai-log.md` са с нормален език. Изключва се с „normal mode“.
- Скиловете са в `.claude/skills/`, копирани от github.com/mattpocock/skills (`grill-me`, `grilling`, MIT) и github.com/JuliusBrussee/caveman (`caveman`, Apache-2.0). Използването им се описва в курсовата работа.
