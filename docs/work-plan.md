# תוכנית עבודה וחלוקה למשימות

האפיון פורק ל-8 Features (יחידות ערך), וכל Feature פורק ל-Tasks עם תוצר מוגדר.
ההערכה בשעות עבודה נטו. העמודה "דרישה" מפנה לסעיף במבדק.

## סדר ביצוע ותלויות

```
F0 הקמה ──► F1 מודל נתונים ו-Seed ──► F2 API שליפה ──────► F5 Angular ──► F8 תיעוד והגשה
                                  │                     ▲
                                  ├─► F3 עדכון סטטוס ───┤
                                  │      └─► F4 Bulk ───┤
                                  └─► F6 ביצועים ◄── F2 │
                                      F7 בדיקות ◄── F2,F3,F4
```

- F1 הוא צוואר הבקבוק: בלי מודל ו-100 אלף רשומות אי אפשר למדוד ביצועים או לבדוק.
- F2 ו-F3 יכולים לרוץ במקביל אחרי F1. F4 משתמש בלוגיקה של F3.
- F5 מתחיל אחרי שחוזה ה-API (DTOs) נסגר ב-F2/F3, ויכול לעבוד מול Swagger.
- F6 (מדידה) רץ אחרי ש-F2 עובד, והממצאים שלו חוזרים לשנות את F1 (הגדרות אינדקסים ועמודת חיפוש).

## Features ו-Tasks

### F0: הקמה ותכנון (2.5 ש')

| # | משימה | תוצר | הערכה | דרישה |
|---|---|---|---:|---|
| 0.1 | קריאת האפיון, רשימת שאלות פתוחות והחלטות (DB, Bulk, Concurrency) | רשימת החלטות, סעיף "החלטות" ב-README | 1 | 9, 11 |
| 0.2 | שלד Solution: Domain / Application / Infrastructure / Api / Tests | Solution שנבנה | 1 | 10 |
| 0.3 | SQL Server ב-Docker, `docker-compose.yml` | סביבה שעולה בפקודה אחת | 0.5 | 11 |

### F1: מודל נתונים ונתוני בדיקה (4.5 ש')

| # | משימה | תוצר | הערכה | דרישה |
|---|---|---|---:|---|
| 1.1 | תכנון טבלאות Requests ו-RequestStatusHistory, טיפוסים, RowVersion | `ServiceRequestConfiguration`, דיאגרמה ב-README | 1 | מבנה פנייה, 3 |
| 1.2 | ישויות Domain וכללי מעבר סטטוס | `ServiceRequest`, `StatusTransitions` | 1 | 2 |
| 1.3 | Migration ראשון | `InitialCreate` | 0.5 | |
| 1.4 | Seeder עם SqlBulkCopy, Random עם seed קבוע, פקודת `seed` | 100k רשומות בכ-10 שניות, ניתן להרצה חוזרת | 2 | מבנה פנייה |

### F2: API שליפה (6 ש')

| # | משימה | תוצר | הערכה | דרישה |
|---|---|---|---:|---|
| 2.1 | DTOs וחוזה API (Request/Response, PagedResult) | `RequestDto`, `RequestQuery` | 1 | 1 |
| 2.2 | סינון משולב, חיפוש חופשי, מיון (Whitelist), דפדוף בצד השרת | `RequestQueryService` | 2 | 1 |
| 2.3 | Validation לפרמטרים (PageSize ≤ 100, טווח תאריכים, Enum, Sort) | שגיאות 400 ברורות | 1 | 1, 10 |
| 2.4 | Aggregations: לפי סטטוס, פתוחות לפי עדיפות, Top ארגונים | `GET /stats` | 1 | 1 |
| 2.5 | Global error handling (Problem Details), Logging, CancellationToken | `GlobalExceptionHandler` | 1 | 10 |

### F3: עדכון סטטוס, Concurrency והיסטוריה (5 ש')

| # | משימה | תוצר | הערכה | דרישה |
|---|---|---|---:|---|
| 3.1 | `PATCH /{id}/status` עם RowVersion מה-Client | Endpoint | 1.5 | 2 |
| 3.2 | Optimistic concurrency ברמת ה-DB, מיפוי ל-409 | UPDATE ... WHERE RowVersion | 1.5 | 2 |
| 3.3 | 404, 422 למעבר לא חוקי, 400 לסטטוס לא חוקי | קודי HTTP לפי החוזה | 0.5 | 2 |
| 3.4 | Audit באותה טרנזקציה, `GET /{id}/history` | טבלת היסטוריה + Endpoint | 1.5 | 3 |

### F4: Bulk update (3 ש')

| # | משימה | תוצר | הערכה | דרישה |
|---|---|---|---:|---|
| 4.1 | החלטה: Partial Success מול All-or-Nothing, ותיעוד הנימוק | סעיף ב-README | 0.5 | 4 |
| 4.2 | `POST /bulk-status` עד 100 פריטים, תוצאה לכל פריט | Endpoint + `BulkUpdateStatusResult` | 2 | 4 |
| 4.3 | Validation (1..100, בלי כפילויות, RowVersion תקין) | שגיאות 400 | 0.5 | 4 |

### F5: Angular client (10 ש')

| # | משימה | תוצר | הערכה | דרישה |
|---|---|---|---:|---|
| 5.1 | הקמת פרויקט, Proxy ל-API, Interceptor למשתמש | אפליקציה ריקה שמדברת עם ה-API | 1 | 5 |
| 5.2 | `RequestsApiService` + מודלים | שכבת HTTP אחת | 1 | 5 |
| 5.3 | `RequestsStore`: RxJS, debounce, switchMap לביטול בקשות קודמות | ניהול State | 2 | 5 |
| 5.4 | טבלה, מיון, דפדוף, סינון | קומפוננטות Presentational | 2.5 | 5 |
| 5.5 | מצבי Loading / Empty / Error | תצוגה לכל מצב | 0.5 | 5 |
| 5.6 | עדכון סטטוס + טיפול ב-409 (רענון והודעה) ו-422 | חוויית Conflict | 1 | 5 |
| 5.7 | פאנל Aggregations, פאנל היסטוריה, סרגל Bulk | קומפוננטות | 2 | 5 |

### F6: ביצועים ו-Cache (5 ש')

| # | משימה | תוצר | הערכה | דרישה |
|---|---|---|---:|---|
| 6.1 | הגדרת אינדקסים לפי תרחישי השאילתות | אינדקסים ב-Migration | 1 | 6 |
| 6.2 | סקריפט Benchmark, מדידה לפני/אחרי, Execution plans | `docs/perf/*` | 2 | 6 |
| 6.3 | טיפול ב-Bottleneck שנמצא (אינדקס DESC, חיפוש חופשי) | שיפור מדוד | 1 | 6 |
| 6.4 | Cache ל-Aggregations: Expiration, Invalidation, IDistributedCache | `RequestStatsService` | 1 | 7 |

### F7: בדיקות אוטומטיות (4 ש')

| # | משימה | תוצר | הערכה | דרישה |
|---|---|---|---:|---|
| 7.1 | Unit: מעברי סטטוס, Validation | `Unit/*` | 1 | 8 |
| 7.2 | תשתית Integration: WebApplicationFactory + Testcontainers SQL Server | `ApiFactory` | 1.5 | 8 |
| 7.3 | Integration: שליפה/סינון, 409 מקבילי, קודי שגיאה, Bulk, Invalidation של Cache | `RequestsApiTests` | 1.5 | 8 |

### F8: תיעוד והגשה (3 ש')

| # | משימה | תוצר | הערכה | דרישה |
|---|---|---|---:|---|
| 8.1 | README: הרצה, מבנה, החלטות, Concurrency, Bulk, Cache, מגבלות, AI | `README.md` | 1.5 | 11 |
| 8.2 | מסמך ביצועים | `docs/performance.md` | 0.5 | 6 |
| 8.3 | בדיקת הרצה נקייה מאפס (clone ואז run) ופרסום ב-GitHub | Repository ציבורי | 1 | הגשה |

## סיכום מאמץ

| Feature | שעות |
|---|---:|
| F0 הקמה | 2.5 |
| F1 מודל ו-Seed | 4.5 |
| F2 API שליפה | 6 |
| F3 עדכון, Concurrency, היסטוריה | 5 |
| F4 Bulk | 3 |
| F5 Angular | 10 |
| F6 ביצועים ו-Cache | 5 |
| F7 בדיקות | 4 |
| F8 תיעוד והגשה | 3 |
| **סה"כ** | **43** |

כ-5.5 ימי עבודה. רזרבה מומלצת של 15% לבעיות סביבה ולתיקונים אחרי המדידה.
