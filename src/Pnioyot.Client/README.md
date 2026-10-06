# Angular — Pnioyot.Client

ממשק Angular 19 למערכת הפניות. כל הנתונים מגיעים מ־REST API: `/api/inquiries` (proxy ל־`http://localhost:5080`).

## הרצה

בטרמינל אחד:

```powershell
dotnet run --project src/Pnioyot.Web
```

בטרמינל שני:

```powershell
cd src/Pnioyot.Client
npm start
```

ואז פותחים http://localhost:4200

| מסך | נתיב |
| --- | --- |
| לוח בקרה | `/` |
| רשימה, סינון, ייצוא | `/inquiries` |
| פרטי פנייה ועדכון סטטוס | `/inquiries/:id` |
