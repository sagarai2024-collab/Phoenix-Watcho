using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using PhoenixWatcho.Data;
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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Semantic Kernel — chat completion added only when a real key is present
var kernelBuilder = Kernel.CreateBuilder();
var openAIKey = builder.Configuration["OpenAI:Key"] ?? "";
if (openAIKey.StartsWith("sk-") && openAIKey.Length > 20)
    kernelBuilder.AddOpenAIChatCompletion(
        builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini", openAIKey);
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
