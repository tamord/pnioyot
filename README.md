# מערכת פניות (pnioyot)

מערכת לניהול פניות מארגונים ומעסקים: חיפוש, סינון, ייצוא CSV, עדכון סטטוס עם נעילה אופטימית (RowVersion), ולוח בקרה עם סיכומים.

מיועדת לעבודה במקביל ולכמות גדולה של רשומות (לפחות 100,000).

## דרישות

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server: **LocalDB** (מותקן עם Visual Studio) **או** SQL Server ב-Docker

## הרצה עם LocalDB

מהתיקייה `pnioyot`:

```powershell
dotnet restore
dotnet run --project src/Pnioyot.Web
```

בפעם הראשונה המערכת יוצרת את מסד `Pnioyot` ומכניסה 100,000 פניות (כמה עשרות שניות). אחרי מכן פותחים:

http://localhost:5080

## ממשק Angular

הפרויקט נמצא ב־`src/Pnioyot.Client` (שים לב לאיות). דורש Node.js. מהשורש של `pnioyot`:

```powershell
npm start
```

או:

```powershell
cd src/Pnioyot.Client
npm start
```

פותחים http://localhost:4200 — הבקשות ל־`/api` מועברות אוטומטית לשרת ב־5080. ה־API עצמו חייב לרוץ במקביל (`dotnet run --project src/Pnioyot.Web`).

| מסך Angular | נתיב |
| --- | --- |
| לוח בקרה | `/` |
| חיפוש, סינון, מיון, עימוד, ייצוא | `/inquiries` |
| עדכון סטטוס + RowVersion | `/inquiries/:id` |

## הרצה עם Docker (SQL Server 2022)

```powershell
docker compose up -d
```

עדכנו את מחרוזת החיבור ב-`src/Pnioyot.Web/appsettings.json`:

```
Server=localhost,1433;Database=Pnioyot;User Id=sa;Password=Pnioyot_Sql_2024;TrustServerCertificate=True;MultipleActiveResultSets=true
```

ואז `dotnet run --project src/Pnioyot.Web`.

## יצירה מחדש של נתוני בדיקה

מוחק את המסד, יוצר אותו מחדש ומזין שוב 100,000 רשומות דטרמיניסטיות (אותו Random seed):

```powershell
dotnet run --project src/Pnioyot.Web -- --recreate-data --seed-only
```

כדי ליצור מחדש **ולהפעיל** את האתר:

```powershell
dotnet run --project src/Pnioyot.Web -- --recreate-data
```

מספר הרשומות ניתן לשינוי ב-`Seed:RecordCount` ב-`appsettings.json`.

## REST API

משאב: `/api/inquiries`

| פעולה | שיטה | נתיב |
| --- | --- | --- |
| רשימה (חיפוש, סינון, מיון, עימוד) | `GET` | `/api/inquiries` |
| ייצוא CSV | `GET` | `/api/inquiries?format=csv` |
| סיכומים (aggregations) | `GET` | `/api/inquiries/stats` |
| פנייה בודדת | `GET` | `/api/inquiries/{id}` |
| עדכון סטטוס | `PATCH` | `/api/inquiries/{id}` |
| עדכון מרובה (עד 100) | `PATCH` | `/api/inquiries` |

## יכולות

| פעולה | איפה |
| --- | --- |
| לוח בקרה (סטטוס, עדיפות, מטפלים) | `/` או `GET /api/inquiries/stats` |
| חיפוש, סינון, מיון, עימוד | `/Inquiries` או `GET /api/inquiries` |
| ייצוא CSV לפי הסינון הנוכחי | כפתור ברשימה או `GET /api/inquiries?format=csv` |
| עדכון סטטוס + RowVersion | `/Inquiries/{id}` או `PATCH /api/inquiries/{id}` |
| עדכון סטטוס מרובה (עד 100) | `PATCH /api/inquiries` |

## עדכון מרובה (Partial Success)

`PATCH /api/inquiries` מעדכן סטטוס של עד 100 פניות באותה בקשה.

נבחר מודל **Partial Success** (לא All-or-Nothing): כל פנייה מתעדכנת בנפרד. פנייה חסרה, מעבר סטטוס אסור, או קונפליקט RowVersion **לא מבטלים** עדכונים שכבר הצליחו.

גוף הבקשה:

```json
{ "status": "InProgress", "items": [ { "id": "...", "rowVersion": "..." } ] }
```

תשובה:

| מצב | HTTP | משמעות |
| --- | --- | --- |
| הכול הצליח | 200 | `failedCount = 0` |
| חלק נכשל / הכול נכשל ברמת פריט | 207 | פירוט ב־`failed` עם `NotFound` / `Conflict` / `Validation` |
| בקשה לא תקינה (0 או יותר מ־100, סטטוס חסר) | 400 | Validation |

שדות התשובה: `mode: "PartialSuccess"`, `requestedCount`, `succeededCount`, `failedCount`, `succeeded`, `failed`.

## מבנה פנייה

Id, Title, OrganizationName, Status (`New` / `InProgress` / `Waiting` / `Completed`), Priority (`Low` / `Medium` / `High`), AssignedTo, CreatedAt, UpdatedAt, RowVersion.

אם שני משתמשים מעדכנים את אותה פנייה במקביל, העדכון השני נדחה עם הודעת קונפליקט.

## אינדקסים

על Status, Priority, OrganizationName, AssignedTo, CreatedAt, ועל הצירוף Status+Priority+CreatedAt — כדי שהסינון על 100k רשומות יישאר מהיר.
