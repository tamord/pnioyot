using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pnioyot.Web.Models;

namespace Pnioyot.Web.Data;

public class InquirySeeder(AppDbContext db, ILogger<InquirySeeder> logger)
{
    public const int DefaultCount = 100_000;

    private static readonly string[] Organizations =
    [
        "בנק הפועלים", "בנק לאומי", "משרד החינוך", "משרד הבריאות", "עיריית תל אביב",
        "עיריית ירושלים", "חברת החשמל", "בזק", "פרטנר", "סלקום", "הוט", "אל על",
        "רכבת ישראל", "שירות התעסוקה", "המוסד לביטוח לאומי", "מכבי שירותי בריאות",
        "כללית", "לאומית שירותי בריאות", "מאוחדת", "טבע", "אינטל ישראל", "מיקרוסופט ישראל",
        "גוגל ישראל", "אמדוקס", "צ'ק פוינט", "נייס", "אלביט מערכות", "רפאל", "התעשייה האווירית",
        "שופרסל", "רמי לוי", "אושר עד", "Super-Pharm", "כאל", "ישראכרט", "מקס", "הראל",
        "מגדל", "כלל ביטוח", "הפניקס", "מנורה מבטחים", "פז", "סונול", "דלק", "יס",
        "כאן", "חדשות 12", "חדשות 13", "אוניברסיטת תל אביב", "האוניברסיטה העברית",
        "הטכניון", "אוניברסיטת בן-גוריון", "מכון ויצמן", "נטפליקס ישראל", "וויקס",
        "monday.com", "פיוניר", "פלייטיקה", "SimilarWeb", "Fiverr", "ג'ט בלו",
        "אובר ישראל", "Gett", "וולט", "תנובה", "שטראוס", "אסם", "עלית", "טמפו",
        "מקורות", "נתיבי ישראל", "נמל אשדוד", "נמל חיפה", "חברת נמלי ישראל",
        "רשות שדות התעופה", "משרד התחבורה", "משרד האוצר", "רשות המסים", "מע\"מ",
        "הלשכה המרכזית לסטטיסטיקה", "צה\"ל", "משטרת ישראל", "כבאות והצלה",
        "מד\"א", "האגודה למען החייל", "יד שרה", "לתת", "הלב", "עזר מציון"
    ];

    private static readonly string[] Handlers =
    [
        "דנה כהן", "יוסי לוי", "מיכל אברהם", "אורי מזרחי", "נועה ברק",
        "איתי שפירא", "שירה גולן", "רון חדד", "יעל בן דוד", "עידו פרץ",
        "תמר אזולאי", "ליאור כץ", "הילה רוזן", "אביב נחום", "מאיה דהן",
        "ניר סגל", "קרן ביטון", "עומר אלון", "סיון חיים", "גלעד משה",
        "רותם אשכנזי", "בן עמית", "ליאת שמעוני", "אדם וקנין", "הדר פרידמן"
    ];

    private static readonly string[] Topics =
    [
        "חיוב שגוי", "בקשת מידע", "תלונה על שירות", "עדכון פרטים", "ביטול מנוי",
        "חידוש רישיון", "בעיית גישה למערכת", "בקשת החזר", "תיאום פגישה", "דיווח תקלה",
        "שאלה משפטית", "הצעת מחיר", "ליווי פרויקט", "בדיקת זכאות", "ערעור על החלטה"
    ];

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }

    public async Task RecreateAsync(int count, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Recreating database and seeding {Count} inquiries...", count);
        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await SeedAsync(count, cancellationToken);
    }

    public async Task SeedIfEmptyAsync(int count, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        if (await db.Inquiries.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Inquiries table already has data. Skip seed.");
            return;
        }

        await SeedAsync(count, cancellationToken);
    }

    public async Task SeedAsync(int count, CancellationToken cancellationToken = default)
    {
        var random = new Random(42);
        var start = DateTime.UtcNow.AddYears(-2);
        var rangeSeconds = (int)(DateTime.UtcNow - start).TotalSeconds;
        var batchSize = 10_000;

        logger.LogInformation("Seeding {Count} inquiries in batches of {Batch}...", count, batchSize);

        var connection = (SqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        for (var offset = 0; offset < count; offset += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var size = Math.Min(batchSize, count - offset);
            var table = CreateTable(size, offset, random, start, rangeSeconds);

            using var bulk = new SqlBulkCopy(connection)
            {
                DestinationTableName = "Inquiries",
                BatchSize = size,
                BulkCopyTimeout = 600
            };
            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("Title", "Title");
            bulk.ColumnMappings.Add("OrganizationName", "OrganizationName");
            bulk.ColumnMappings.Add("Status", "Status");
            bulk.ColumnMappings.Add("Priority", "Priority");
            bulk.ColumnMappings.Add("AssignedTo", "AssignedTo");
            bulk.ColumnMappings.Add("CreatedAt", "CreatedAt");
            bulk.ColumnMappings.Add("UpdatedAt", "UpdatedAt");
            await bulk.WriteToServerAsync(table, cancellationToken);
            logger.LogInformation("Inserted {Done}/{Total}", offset + size, count);
        }

        logger.LogInformation("Seed completed.");
    }

    private static DataTable CreateTable(int size, int offset, Random random, DateTime start, int rangeSeconds)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("Title", typeof(string));
        table.Columns.Add("OrganizationName", typeof(string));
        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("Priority", typeof(string));
        table.Columns.Add("AssignedTo", typeof(string));
        table.Columns.Add("CreatedAt", typeof(DateTime));
        table.Columns.Add("UpdatedAt", typeof(DateTime));

        for (var i = 0; i < size; i++)
        {
            var n = offset + i + 1;
            var created = start.AddSeconds(random.Next(rangeSeconds));
            var updated = created.AddHours(random.Next(1, 400));
            if (updated > DateTime.UtcNow)
            {
                updated = DateTime.UtcNow;
            }

            table.Rows.Add(
                Guid.NewGuid(),
                $"{Topics[random.Next(Topics.Length)]} #{n:000000}",
                Organizations[random.Next(Organizations.Length)],
                PickStatus(random).ToString(),
                PickPriority(random).ToString(),
                Handlers[random.Next(Handlers.Length)],
                created,
                updated);
        }

        return table;
    }

    private static InquiryStatus PickStatus(Random random)
    {
        var roll = random.Next(100);
        return roll switch
        {
            < 40 => InquiryStatus.New,
            < 65 => InquiryStatus.InProgress,
            < 85 => InquiryStatus.Waiting,
            _ => InquiryStatus.Completed
        };
    }

    private static InquiryPriority PickPriority(Random random)
    {
        var roll = random.Next(100);
        return roll switch
        {
            < 30 => InquiryPriority.Low,
            < 80 => InquiryPriority.Medium,
            _ => InquiryPriority.High
        };
    }
}
