using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Pnioyot.Web.Data;
using Pnioyot.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql =>
    {
        sql.CommandTimeout(120);
        sql.EnableRetryOnFailure(5);
    }));
builder.Services.AddScoped<InquirySeeder>();
builder.Services.AddScoped<InquiryService>();

var app = builder.Build();

var seedCount = builder.Configuration.GetValue("Seed:RecordCount", InquirySeeder.DefaultCount);
var recreate = args.Contains("--recreate-data", StringComparer.OrdinalIgnoreCase);
var seedOnly = args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase)
               || args.Contains("--seed", StringComparer.OrdinalIgnoreCase);

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<InquirySeeder>();
    if (recreate)
    {
        await seeder.RecreateAsync(seedCount);
    }
    else
    {
        await seeder.SeedIfEmptyAsync(seedCount);
    }
}

if (seedOnly)
{
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AngularDev");
app.MapRazorPages();
app.MapControllers();
app.Run();
