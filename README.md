# מערכת לניהול פניות – מבדק Full Stack

מערכת לניהול פניות המתקבלות מארגונים ומעסיקים: חיפוש, סינון, מיון ודפדוף בצד השרת על 100,000+ פניות, עדכון סטטוס עם Optimistic Concurrency, היסטוריית שינויים, עדכון מרובה (Bulk) ונתונים מסכמים עם Cache.

| מסמך | תוכן |
|---|---|
| [README.md](README.md) | הרצה, מבנה, החלטות, Concurrency, Bulk, Cache, מגבלות, שימוש ב-AI |
| [docs/performance.md](docs/performance.md) | מדידות לפני/אחרי, Execution plans, אינדקסים, Bottleneck |
| [docs/work-plan.md](docs/work-plan.md) | פירוק האפיון ל-Features/Tasks, תלויות, סדר והערכת מאמץ |

## טכנולוגיות וגרסאות

| שכבה | טכנולוגיה |
|---|---|
| Backend | .NET 8 (LTS), ASP.NET Core Web API (Controllers), EF Core 8 |
| בסיס נתונים | SQL Server 2022 (Docker) |
| Frontend | Angular 20 (Standalone components, Signals + RxJS), TypeScript 5.9 |
| בדיקות | xUnit, FluentAssertions, WebApplicationFactory, Testcontainers (SQL Server אמיתי) |
| Cache | `IDistributedCache`: בזיכרון כברירת מחדל, Redis לפי הגדרה |

## הרצה

דרישות: Docker, .NET 8 SDK, Node.js 20 ומעלה.

### אפשרות א: הכול ב-Docker

```bash
docker compose up --build
```

- ממשק: http://localhost:8080
- API + Swagger: http://localhost:5080/swagger

בעלייה הראשונה ה-API מריץ Migrations ויוצר 100,000 פניות (כ-10 שניות).

### אפשרות ב: פיתוח מקומי

```bash
# 1. בסיס נתונים
docker compose up -d sqlserver

# 2. API (Migrations + Seed של 100,000 פניות בעלייה הראשונה)
cd backend
dotnet run --project src/RequestsManager.Api
# http://localhost:5080/swagger

# 3. Angular (בחלון נוסף; כתובת ה-API מוגדרת ב-src/environments/environment.development.ts)
cd web
npm ci
npm start
# http://localhost:4200
```

### אפשרות ג: בלי Docker בכלל

צריך SQL Server מקומי: SQL Server Express / Developer, או LocalDB שמגיע עם Visual Studio. מגדירים את מחרוזת החיבור דרך משתנה סביבה, בלי לשנות קבצים:

```powershell
# PowerShell, עם LocalDB
cd backend
$env:ConnectionStrings__Default = "Server=(localdb)\MSSQLLocalDB;Database=RequestsManager;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project src/RequestsManager.Api
```

(עם SQL Server Express: `Server=.\SQLEXPRESS;...` באותו פורמט.) בעלייה הראשונה נוצרים מסד הנתונים, הטבלאות ו-100,000 הפניות. את Angular מריצים כמו באפשרות ב.

בדיקות בלי Docker: מפנים את בדיקות ה-Integration לשרת המקומי (נוצר מסד נתונים נפרד לכל הרצה):

```powershell
$env:TEST_SQL_CONNECTION = "Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test
```

### יצירה מחדש של נתוני הבדיקה

```bash
cd backend
dotnet run --project src/RequestsManager.Api -- seed 100000
```

מוחק את כל הפניות וההיסטוריה ויוצר אותן מחדש עם `SqlBulkCopy`. ה-Random מאותחל עם Seed קבוע, כך שכל הרצה מייצרת בדיוק אותו Dataset (אפשר להשוות מדידות בין הרצות).

### בדיקות

```bash
cd backend
dotnet test
```

בדיקות ה-Integration מרימות SQL Server זמני ב-Docker (Testcontainers). כדי להשתמש בשרת קיים במקום: `TEST_SQL_CONNECTION="Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True" dotnet test` (נוצר מסד נתונים נפרד לבדיקות).

## מבנה הפתרון

```
backend/
  src/
    RequestsManager.Domain          ישויות, Enum-ים, כללי מעבר סטטוס, חריגות דומיין. בלי תלויות.
    RequestsManager.Application     DTOs + Validation, שירותים (Query / Status / Stats), ממשקים (IAppDbContext, ICurrentUser)
    RequestsManager.Infrastructure  EF Core DbContext, קונפיגורציות ואינדקסים, Migrations, Seeder, רישום Cache
    RequestsManager.Api             Controllers, GlobalExceptionHandler (Problem Details), Program.cs
  tests/RequestsManager.Tests
    Unit/                           מעברי סטטוס, Validation
    Integration/                    API + SQL Server אמיתי
web/src/app/
  core/models                       טיפוסים ותוויות
  core/services                     RequestsApiService (HTTP בלבד), Interceptor למשתמש, מיפוי שגיאות
  features/requests/
    requests.store.ts               State של המסך: RxJS, debounce, switchMap, טיפול ב-409
    requests-page/                  Container: מחבר Store לקומפוננטות
    components/<name>/              Presentational: פילטרים, טבלה, דפדוף, Aggregations, היסטוריה, Bulk
                                    (כל קומפוננטה בתיקייה משלה: ts + html + scss)
docs/                               ביצועים ותוכנית עבודה
```

**הפרדת אחריות:** ה-Controllers דקים (קריאה לשירות בלבד). הלוגיקה העסקית (מי יכול לעבור לאיזה סטטוס, מה נרשם ב-Audit) יושבת ב-Domain. גישה לנתונים דרך EF Core, בלי שכבת Repository נוספת: `DbContext` כבר מממש Repository + Unit of Work, ושכבה נוספת הייתה מוסיפה קוד בלי ערך (Over-engineering).

**Lifetimes ב-DI:** `DbContext` והשירותים הם Scoped (אחד לכל בקשת HTTP). `TimeProvider` ו-`IDistributedCache` הם Singleton. `ICurrentUser` הוא Scoped (תלוי בבקשה).

## API

| Method | Route | תיאור | קודים |
|---|---|---|---|
| GET | `/api/requests` | חיפוש, סינון, מיון, דפדוף | 200, 400 |
| GET | `/api/requests/{id}` | פנייה אחת | 200, 404 |
| GET | `/api/requests/{id}/history` | היסטוריית שינויי סטטוס | 200, 404 |
| GET | `/api/requests/stats` | Aggregations (עם Cache) | 200 |
| PATCH | `/api/requests/{id}/status` | עדכון סטטוס `{ status, rowVersion }` | 200, 400, 404, 409, 422 |
| POST | `/api/requests/bulk-status` | עדכון עד 100 פניות `{ status, items: [{ id, rowVersion }] }` | 200, 400 |

פרמטרים של החיפוש: `page`, `pageSize` (1..100), `status` (אפשר כמה), `priority` (אפשר כמה), `organizationName` (מתחיל ב-), `assignedTo` (מדויק), `createdFrom`, `createdTo`, `search` (בכותרת ובשם הארגון, לפחות 2 תווים), `sortBy` (`createdAt`, `updatedAt`, `priority`, `status`, `title`, `organizationName`), `sortDir` (`asc`/`desc`).

כל השגיאות מוחזרות כ-Problem Details (RFC 7807). שגיאה לא צפויה מחזירה 500 בלי פרטים פנימיים, והפרטים נכתבים ללוג.

המשתמש המבצע נשלח ב-Header `X-User-Name` ונשמר ב-`ChangedBy`. אין Authentication במבדק; במערכת אמיתית הערך היה נלקח מה-Claims של JWT (רק המימוש של `ICurrentUser` היה משתנה).

### מעברי סטטוס מותרים

| מסטטוס | מותר לעבור ל- |
|---|---|
| New | InProgress, Waiting |
| InProgress | Waiting, Completed |
| Waiting | InProgress, Completed |
| Completed | (סופי) |

מעבר אסור מחזיר `422 Unprocessable Entity` עם הסטטוס הנוכחי והמעברים המותרים. סטטוס שלא קיים (`"Archived"`) מחזיר `400`.

## בסיס הנתונים

נבחר **SQL Server**: הנתונים טבלאיים עם סכמה קבועה, יש צורך בטרנזקציה שכוללת עדכון + Audit, ו-`rowversion` נותן Optimistic Concurrency מובנה ברמת המנוע. MongoDB הייתה מחייבת לממש שדה גרסה ידנית ולוותר על טרנזקציה פשוטה בין שני Collections.

- `Requests`: `Status` ו-`Priority` נשמרים כ-`tinyint` (אינדקסים צרים), תאריכים ב-UTC כ-`datetime2(3)`, `RowVersion` כ-`rowversion`.
- `RequestStatusHistory`: `RequestId`, `PreviousStatus`, `NewStatus`, `ChangedAt`, `ChangedBy`, עם FK לפנייה.
- עמודה מחושבת `SearchText` לחיפוש חופשי.

פירוט האינדקסים, הסיבה לכל אחד ומדידות לפני/אחרי: [docs/performance.md](docs/performance.md).

## Concurrency

- כל פנייה מגיעה ל-Client עם `rowVersion` (ה-`rowversion` של SQL Server ב-Base64). SQL Server משנה את הערך אוטומטית בכל UPDATE.
- עדכון חייב לשלוח את ה-`rowVersion` שה-Client ראה. השירות מגדיר אותו כ-Original value של ה-Concurrency token, ו-EF Core שולח:
  `UPDATE Requests SET ... WHERE Id = @id AND RowVersion = @clientVersion`
- אם מישהו עדכן בינתיים, ה-WHERE לא מוצא שורה, EF זורק `DbUpdateConcurrencyException`, וה-API מחזיר **409 Conflict**. ההגנה נאכפת בבסיס הנתונים, ולכן גם שני עדכונים שמגיעים באותה מילישנייה לשני מופעי שרת שונים לא יכולים לדרוס זה את זה (אין Lost Update). הבדיקה `UpdateStatus_ConcurrentUpdatesWithSameVersion_OnlyOneSucceeds` שולחת שני עדכונים במקביל ומוודאת שבדיוק אחד מצליח ושנרשמה שורת היסטוריה אחת.
- `UpdatedAt` ו-`RowVersion` מתעדכנים באותו UPDATE, ושורת ה-Audit נכתבת באותו `SaveChanges`, כלומר באותה טרנזקציה. אין מצב של סטטוס שהשתנה בלי היסטוריה, או היסטוריה בלי שינוי.
- ב-Angular: על 409 מוצגת הודעה שהפנייה עודכנה ע"י משתמש אחר, והרשימה, הנתונים המסכמים וההיסטוריה נטענים מחדש, כך שהמשתמש מחליט שוב על סמך המצב העדכני.

## Bulk update

**הבחירה: Partial Success.** כל פריט מעודכן בטרנזקציה קצרה משלו ומקבל תוצאה משלו: `Updated`, `NotFound`, `Conflict` או `InvalidTransition`. התשובה תמיד 200 עם סיכום (`requested`, `succeeded`, `failed`) ופירוט לכל פריט, כולל ה-`rowVersion` החדש לפריטים שעודכנו.

**הנימוק:** הפניות בלתי תלויות זו בזו. במערכת עם הרבה משתמשים במקביל, הסיכוי שאחת מ-100 פניות תשתנה בזמן שהמשתמש בוחר גבוה. ב-All-or-Nothing פנייה אחת שהשתנתה הייתה מבטלת את כל 99 האחרות, והמשתמש היה נאלץ לנסות שוב ושוב. ב-Partial Success המשתמש רואה בדיוק אילו פניות לא עודכנו ולמה (ב-UI: רשימה מתקפלת עם הסיבה לכל פנייה).

**פרטי מימוש:** כל 100 הפניות נטענות בשאילתה אחת. כל פריט נבדק בנפרד (קיים? הגרסה עדכנית? המעבר מותר?) ונשמר ב-`SaveChanges` משלו, כך שכל Conflict מבודד לפריט שלו. `Validation` מחזיר 400 על הבקשה כולה כשיש 0 או יותר מ-100 פריטים, מזהים כפולים או `rowVersion` לא תקין, כי אלה שגיאות של הבקשה ולא של פריט.

## Cache ו-Invalidation

**מה נשמר:** תוצאת `GET /api/requests/stats` (שלושת ה-Aggregations).

**למה דווקא זה:** ה-Aggregations סורקים את כל הטבלה (100,000+ שורות), מוצגים בכל טעינת מסך, והם זהים לכל המשתמשים. הם משתנים רק כשסטטוס משתנה. רשימת הפניות עצמה לא נשמרת ב-Cache: כל שילוב של פילטרים הוא מפתח אחר (Hit rate נמוך), והשאילתות כבר מהירות בזכות האינדקסים.

- **Expiration:** `AbsoluteExpirationRelativeToNow` של 60 שניות (`StatsCache:AbsoluteExpirationSeconds`). זו רשת ביטחון לשינויים שלא עוברים דרך ה-API.
- **Invalidation:** אחרי כל עדכון סטטוס מוצלח, בודד או Bulk (אם לפחות פריט אחד עודכן), המפתח נמחק. הקריאה הבאה מחשבת מחדש. הבדיקה `Stats_AreRecomputedAfterAStatusChange` מוודאת זאת.
- **מספר מופעי שרת:** הקוד עובד מול `IDistributedCache`. במופע יחיד זה Cache בזיכרון. עם כמה מופעים, `IMemoryCache` נפרד בכל מופע היה משאיר מידע מיושן במופעים שלא ביצעו את העדכון. לכן מגדירים `Redis:ConnectionString`, וכל המופעים חולקים Cache אחד: מחיקה במופע אחד תקפה לכולם, בלי שינוי קוד. חלופה, אם רוצים לשמור Cache מקומי מהיר: Redis Pub/Sub או Backplane שמודיע לכל המופעים למחוק את המפתח.
- **חלון מרוץ ידוע:** אם בקשה מחשבת Stats בדיוק בזמן עדכון, היא עלולה לשמור תוצאה מלפני העדכון אחרי שה-Invalidation כבר קרה. החשיפה מוגבלת ל-60 שניות לכל היותר בזכות ה-Expiration. זה מקובל לנתונים מסכמים. אם לא, אפשר להוסיף מספר גרסה למפתח.

## Angular

- **כל פעולה בצד השרת:** ה-Store מחזיק רק את ה-Query (פילטרים, מיון, עמוד) ושולח אותו ל-API. אין סינון, מיון או דפדוף בצד הלקוח.
- **Debounce:** חיפוש חופשי `debounceTime(350)` + `distinctUntilChanged`, ומתעלם ממונח של תו אחד. שדות טקסט בפילטרים (ארגון, מטפל) עם `debounceTime(300)`.
- **ביטול בקשות קודמות:** `switchMap` על זרם ה-Query. כשמגיעה Query חדשה, ה-Subscription הקודם מבוטל, ה-HTTP request מבוטל (Abort), ובשרת ה-`CancellationToken` מבטל את השאילתה ב-SQL. תשובה ישנה ואיטית לא יכולה לדרוס תשובה חדשה.
- **מצבים:** Loading (ספינר מעל הנתונים הקודמים, בלי הבהוב), Empty ("לא נמצאו פניות"), Error (הודעה + "נסה שוב"). כל אחד מהפאנלים (רשימה, Aggregations, היסטוריה) מנהל את המצב שלו.
- **409:** הודעה + רענון, כמתואר ב-Concurrency. 422 ו-400: מוצגת ההודעה מה-Problem Details.
- **הפרדת אחריות:** `RequestsApiService` מכיר רק HTTP. `RequestsStore` מכיר State ו-RxJS. `RequestsPageComponent` הוא Container שמחבר. שאר הקומפוננטות Presentational (Inputs/Outputs בלבד, `OnPush`).

## החלטות טכנולוגיות וחלופות שנשקלו

1. **Optimistic concurrency עם `rowversion` בבסיס הנתונים, מול נעילה (Pessimistic) או בדיקה באפליקציה.**
   נעילה (`SELECT ... WITH (UPDLOCK)`) מחזיקה נעילות בזמן שמשתמש חושב, ולא מתאימה לאפליקציית Web. בדיקה באפליקציה ("קרא, השווה, כתוב") פגיעה למרוץ בין הקריאה לכתיבה, במיוחד עם כמה מופעי שרת. `rowversion` נאכף אטומית ע"י SQL Server ונתמך ישירות ב-EF Core.
   חלופה נוספת שנשקלה: להעביר את הגרסה ב-Header `If-Match`/ETag במקום בגוף הבקשה. זה נכון יותר סמנטית ב-HTTP, אבל ב-Bulk כל פריט צריך גרסה משלו, ולכן נבחר פורמט אחיד: `rowVersion` בגוף הבקשה בשני ה-Endpoints.

2. **Partial Success ב-Bulk, מול All-or-Nothing.** הנימוק בסעיף Bulk למעלה. המחיר: עד 100 טרנזקציות קצרות במקום אחת. ב-100 פריטים זה עשרות מילישניות, ומקובל.

3. **EF Core עם LINQ, מול Dapper ו-SQL ידני.** EF Core מייצר SQL פרמטרי ויעיל לשאילתות האלה (נבדק ב-Execution plans), נותן Concurrency token ו-Migrations מובנים, ומונע SQL Injection במיון הדינמי (Whitelist + ביטויים מוקלדים). Dapper היה נותן שליטה מלאה ב-SQL, אבל במחיר של הרבה קוד בנייה דינמית של WHERE/ORDER BY.

## מגבלה ידועה ומה אפשר לשפר

- **מגבלה:** חיפוש חופשי (`LIKE '%x%'`) לא יכול להשתמש ב-Seek, ולכן הוא סורק את כל אינדקס `SearchText`. ב-100 אלף רשומות זה 58ms. במיליוני רשומות זה יהיה איטי. הפתרון: Full-Text Search של SQL Server או מנוע חיפוש ייעודי (פירוט ב-[performance.md](docs/performance.md)).
- **שיפור:** דפדוף Keyset (לפי `CreatedAt, Id` של השורה האחרונה) במקום `OFFSET`. דפדוף עמוק (עמוד 4,000) עם `OFFSET` קורא את כל השורות שלפניו, ו-Keyset עולה אותו דבר בכל עמוד. במחיר: אין "קפיצה לעמוד X".
- עוד: Authentication אמיתי (JWT) במקום Header, בדיקות יחידה ל-Angular, ו-Outbox אם בעתיד עדכון סטטוס יצטרך לשלוח אירועים למערכות אחרות.

## שימוש בכלי AI

> **להשלמה ע"י המגיש/ה לפני ההגשה.** הסעיף הזה חייב לשקף את מה שנעשה בפועל.

הפתרון נבנה בסיוע Claude (Anthropic): שלד הפרויקטים, רוב הקוד ב-Backend וב-Angular, הבדיקות, סקריפטי המדידה וטיוטת התיעוד.

מה נבדק בפועל (בהרצה, לא רק בקריאה):
- כל 25 הבדיקות האוטומטיות עוברות מול SQL Server אמיתי.
- תרחישי 409 / 422 / 404 / 400 / Bulk נבדקו גם ידנית מול ה-API וגם דרך הדפדפן (כולל 409 אמיתי ממשתמש שני).
- ה-Execution plans והמדידות ב-performance.md נמדדו על 100,000 רשומות. שני ממצאים מהמדידה שינו את הקוד: אינדקס `CreatedAt DESC` שגרם ל-Sort, וחיפוש חופשי איטי שהוחלף בעמודה מחושבת בינארית.

מה נבדק או שונה ידנית: _[להשלים: אילו חלקים עברו Code Review אישי, מה שונה ולמה]_
