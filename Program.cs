using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using PhoenixWatcho.Data;
using PhoenixWatcho.Models;
using PhoenixWatcho.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<VectorSearchService>();
builder.Services.AddSingleton<BillingService>();

// RAG services: OpenAI embeddings + Supabase pgvector + the pipeline orchestrator
builder.Services.AddHttpClient<EmbeddingService>();
builder.Services.AddHttpClient<SupabaseVectorService>();
builder.Services.AddScoped<RagService>();
builder.Services.AddResponseCompression();
builder.Services.AddSpaStaticFiles(config => config.RootPath = "ClientApp/dist");

// Allow large file uploads (up to 10 MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

// Cookie auth: subscribers sign in with VC number + password, the admin with the
// configured Admin:Password. API calls get 401/403 instead of a login redirect.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name     = "phoenix.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan  = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IPasswordHasher<Subscriber>, PasswordHasher<Subscriber>>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Semantic Kernel — chat completion added only when a real key is present.
// Prefer Google Gemini (free tier) when configured, else OpenAI.
var kernelBuilder = Kernel.CreateBuilder();
var geminiKey = builder.Configuration["Gemini:Key"] ?? "";
var openAIKey = builder.Configuration["OpenAI:Key"] ?? "";
if (geminiKey.StartsWith("AIza"))
{
#pragma warning disable SKEXP0070 // Gemini connector is experimental
    kernelBuilder.AddGoogleAIGeminiChatCompletion(
        builder.Configuration["Gemini:Model"] ?? "gemini-2.0-flash", geminiKey);
#pragma warning restore SKEXP0070
}
else if (openAIKey.StartsWith("sk-") && openAIKey.Length > 20)
{
    kernelBuilder.AddOpenAIChatCompletion(
        builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini", openAIKey);
}
builder.Services.AddSingleton(kernelBuilder.Build());

var app = builder.Build();

// Apply migrations and seed reference + demo data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbInitializer.SeedAsync(db);
}

app.UseResponseCompression();
app.UseStaticFiles();
app.UseSpaStaticFiles();

// API routes MUST be registered before UseSpa
// UseSpa is a catch-all — if it runs first, it swallows every request including /api
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
    if (app.Environment.IsDevelopment())
        spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
});

app.Run();
