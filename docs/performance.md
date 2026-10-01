# ביצועים ואינדקסים

כל המדידות בוצעו על 100,000 פניות שנוצרו ע"י ה-Seeder (`dotnet run -- seed`), SQL Server 2022 ב-Docker, Cache חם (הרצה שנייה).
השאילתות הן בדיוק ה-SQL ש-EF Core מייצר (נלכד מלוג ה-API) ורצות דרך `sp_executesql` עם אותם פרמטרים.

סקריפטים לשחזור (בתיקייה `docs/perf`):

| קובץ | תפקיד |
|---|---|
| `benchmark.sql` | מריץ את כל השאילתות עם `SET STATISTICS IO, TIME ON` |
| `drop-indexes.sql` / `create-indexes.sql` | מצב בסיס (רק PK) מול מצב סופי |
| `plans.sql` | Execution Plan בפועל (`STATISTICS PROFILE`) |
| `plans-output.txt` | הפלט של `plans.sql` על הנתונים האלה |

```bash
docker cp docs/perf sql:/tmp/perf
docker exec sql /opt/mssql-tools18/bin/sqlcmd -C -I -S localhost -U sa -P 'Your_strong_Pass123' -d RequestsManager -i /tmp/perf/benchmark.sql
```

## מניעת טעינת כלל הנתונים לזיכרון

- כל הסינון, החיפוש, המיון והדפדוף נבנים כ-`IQueryable` ומתורגמים ל-SQL אחד (`WHERE ... ORDER BY ... OFFSET/FETCH`). `RequestQueryService` מממש זאת.
- ל-Client מגיע עמוד אחד בלבד (עד 100 שורות, נאכף ב-Validation). השרת מחזיק בזיכרון רק את השורות של העמוד.
- ה-Projection (`Select`) קורא רק את העמודות של ה-DTO. `AsNoTracking` מונע עלות של Change Tracking בקריאה.
- ה-Aggregations הם `GROUP BY` בבסיס הנתונים, ולשרת מגיעות רק שורות הסיכום (4 סטטוסים, 3 עדיפויות, 5 ארגונים).
- מיון רק לפי רשימה סגורה של שדות (Whitelist). `Id` מתווסף תמיד כמפתח שובר-שוויון, כך שהדפדוף יציב.

## תוצאות: לפני האינדקסים ואחריהם

"לפני" = רק ה-Clustered PK. "אחרי" = האינדקסים של ה-Migration. Logical reads ו-elapsed הם סכום שאילתת ה-COUNT ושאילתת העמוד.

| # | תרחיש | Reads לפני | ms לפני | Reads אחרי | ms אחרי |
|---|---|---:|---:|---:|---:|
| Q1 | סטטוס (New, InProgress) + עדיפות High, מיון תאריך, עמוד 3 | 4,262 | 348 | 2,066 | 32 |
| Q2 | מסך ברירת מחדל: בלי סינון, החדשות ראשונות | 2,238 | 73 | **65** | <1 |
| Q3 | ארגון מתחיל ב-Acme + טווח תאריכים | 4,262 | 116 | 301 | 24 |
| Q4 | מטפל + סטטוסים פתוחים (ספירה) | 2,131 | 51 | 13 | 1 |
| Q5a | חיפוש חופשי נאיבי: `Title LIKE '%x%' OR OrganizationName LIKE '%x%'` | 4,262 | 1,565 | 3,322 | 892 |
| Q5b | חיפוש חופשי סופי: עמודת `SearchText` (BIN2) | 4,262 | 298 | 2,627 | **58** |
| Q6 | Aggregation: ספירה לפי סטטוס | 2,131 | 35 | 239 | 13 |
| Q7 | Aggregation: פתוחות לפי עדיפות | 2,131 | 25 | 83 | 7 |
| Q8 | Aggregation: 5 הארגונים עם הכי הרבה פתוחות | 2,131 | 72 | 559 | 36 |

## שתי הפעולות המרכזיות שנבחנו

### 1. `GET /api/requests`: רשימה עם סינון, מיון ודפדוף

Execution plan של מסך ברירת המחדל (Q2), אחרי האינדקסים:

```
Top(OFFSET @o, TOP @n)                                   rows=20
  Nested Loops
    Index Scan  IX_Requests_CreatedAt  ORDERED BACKWARD   rows=20
    Clustered Index Seek  PK_Requests  (Key Lookup)       rows=20
```

נקראות 20 שורות בלבד מתוך 100,000. אין Sort ואין סריקה מלאה.

**ממצא שתוקן בזכות המדידה:** בגרסה הראשונה האינדקס הוגדר `(CreatedAt DESC)` וה-Plan עדיין כלל Sort על כל הטבלה (2,238 reads). הסיבה: SQL Server מוסיף לכל אינדקס את מפתח ה-Clustered (`Id ASC`), כך שהאינדקס היה ממוין `CreatedAt DESC, Id ASC`, ואילו ה-API ממיין `CreatedAt DESC, Id DESC`. אינדקס עולה `(CreatedAt)` שנקרא לאחור (`BACKWARD`) נותן בדיוק `CreatedAt DESC, Id DESC`. זה הוריד את השאילתה ל-65 reads.

### 2. `GET /api/requests/stats`: Aggregations

```
Hash Match (Aggregate) BY Priority                        rows=3
  Index Seek IX_Requests_Status_CreatedAt  Status<>3      rows=32,027

Top 5 <- Sort <- Stream Aggregate BY OrganizationName     rows=500
  Index Scan IX_Requests_OrganizationName (INCLUDE Status) rows=32,027
```

שתי השאילתות מכוסות במלואן ע"י אינדקס צר (Covering) ולא נוגעות בטבלה עצמה. בנוסף, התוצאה נשמרת ב-Cache (ראו README), כך שרוב הקריאות לא מגיעות לבסיס הנתונים בכלל.

## האינדקסים והסיבה לכל אחד

| אינדקס | משרת את | הערה |
|---|---|---|
| `PK_Requests (Id)` clustered | שליפה/עדכון לפי Id, מפתח שובר-שוויון בדפדוף | |
| `IX_Requests_Status_CreatedAt (Status, CreatedAt) INCLUDE (Priority)` | סינון לפי סטטוס + מיון לפי תאריך, ספירה לפי סטטוס ולפי עדיפות | ה-INCLUDE הופך את ה-Aggregations ל-Covering |
| `IX_Requests_CreatedAt (CreatedAt)` | מסך ברירת המחדל וטווח תאריכים | עולה בכוונה, ראו ממצא למעלה |
| `IX_Requests_Priority_CreatedAt (Priority, CreatedAt)` | מיון לפי עדיפות | מיון לפי סטטוס/עדיפות משתמש ב-CreatedAt כמפתח משני, כך שהאינדקס מחזיר את העמוד בלי למיין את כל הטבלה (2,238 → 848 קריאות, ללא Sort) |
| `IX_Requests_OrganizationName (OrganizationName) INCLUDE (Status)` | סינון "מתחיל ב-" (`LIKE 'x%'` הוא Seek), מיון לפי ארגון, Top ארגונים | |
| `IX_Requests_AssignedTo_Status (AssignedTo, Status)` | "הפניות שלי" עם סטטוס | Q4: מ-2,131 ל-13 reads |
| `IX_Requests_SearchText (SearchText)` | חיפוש חופשי | ראו Bottleneck |
| `IX_RequestStatusHistory_RequestId_ChangedAt` | מסך היסטוריה של פנייה | |

**Trade-off בכתיבה:** עדכון סטטוס מעדכן 3 אינדקסים שמכילים את `Status`. עדכון בודד נוגע בשורה אחת, ולכן העלות זניחה לעומת החיסכון בקריאה, שהיא הפעולה השכיחה. `SearchText` לא משתנה בעדכון סטטוס, ולכן האינדקס שלו מתעדכן רק ביצירת פנייה.

## Bottleneck שזוהה: חיפוש טקסט חופשי

`LIKE '%term%'` לא יכול לבצע Seek באף אינדקס, כך שכל חיפוש סורק את כל השורות. בגרסה הנאיבית (Q5a) החיפוש לקח **1.5 שניות**: השוואה case-insensitive ב-collation ברירת המחדל על שתי עמודות, לכל 100,000 השורות.

**השיפור שמומש** (Q5b, מ-1,565ms ל-58ms, פי 27):
- עמודה מחושבת `SearchText = UPPER(Title + ' ' + OrganizationName) COLLATE Latin1_General_100_BIN2`. השוואה בינארית זולה בערך פי 10 מהשוואה לשונית. ה-API ממיר את מילת החיפוש ל-Upper, כך שהחיפוש נשאר case-insensitive.
- העמודה לא נשמרת בטבלה (לא `PERSISTED`), והאינדקס עליה הוא העותק היחיד. כך הטבלה לא גדלה, ונסרק אינדקס צר במקום כל הטבלה.

**השיפור הבא, אם הנתונים יגדלו:** Full-Text Search של SQL Server (`CONTAINS`, חיפוש לפי מילים עם אינדקס הפוך), או מנוע חיפוש ייעודי (Elasticsearch/OpenSearch). הם לא מומשו כאן כי Full-Text לא מותקן בתמונת ה-Docker הרשמית של SQL Server ל-Linux, ובנפח הנוכחי 58ms מספיק.

## Bottlenecks נוספים ידועים

- **דפדוף עמוק (OFFSET):** `OFFSET 90000` עדיין קורא 90,000 שורות מהאינדקס. משתמשים כמעט לא מגיעים לעמוד 4,500, ולכן נבחר OFFSET (פשוט, תומך ב"קפוץ לעמוד"). השיפור האפשרי הוא Keyset pagination (`WHERE (CreatedAt, Id) < (@lastCreatedAt, @lastId)`).
- **COUNT בכל חיפוש:** כדי להציג "עמוד X מתוך Y" מתבצעת ספירה בכל בקשה. כשהסינון מכוסה באינדקס הספירה זולה (Q4: 13 reads). אם זה יהפוך לבעיה, אפשר להחזיר ספירה משוערת או לשמור את הספירה ב-Cache לפי מפתח הסינון.
- **Q1 (סטטוס + עדיפות):** ה-Optimizer בוחר סריקה של הטבלה (2,066 reads, 32ms) כי כ-3,200 שורות תואמות. אם זה יהיה מסך מרכזי, אינדקס `(Status, Priority, CreatedAt)` יאפשר Seek ישיר.
