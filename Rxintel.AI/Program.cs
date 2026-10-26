using Rxintel.AI.Extensions;

var builder = WebApplication.CreateBuilder(args);

var logDir = builder.Configuration["Logging:LogDirectory"];
if (string.IsNullOrWhiteSpace(logDir))
{
    logDir = Path.Combine(builder.Environment.ContentRootPath, "logs");
}
else if (!Path.IsPathRooted(logDir))
{
    logDir = Path.Combine(builder.Environment.ContentRootPath, logDir);
}
Directory.CreateDirectory(logDir);
log4net.GlobalContext.Properties["LogDir"] = logDir.Replace('\\', '/');

builder.Logging.ClearProviders();
builder.Logging.AddLog4Net("log4net.config");

builder.Services.InitializeDefaultService(builder.Configuration);
builder.Services.AddInfrastructure();
builder.Services.AddTypeSafeClient(options =>
{
    builder.Configuration.GetSection("TypeSafeAI").Bind(options);
});
builder.Services.AddMetaAiClient(options =>
{
    builder.Configuration.GetSection("MetaAI").Bind(options);
});
var app = builder.Build();

await app.InitializeReferenceDataAsync();
await app.InitializeSigMappingsAsync();

var refDataFolder = builder.Configuration["AuditReferenceData:FolderPath"];
Rxintel.Generic.Services.MetaAI.AiSigKnowledgeMemory.Initialize(refDataFolder);
Rxintel.Generic.Services.MetaAI.MuseClinicalMemoryStore.Initialize(refDataFolder);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
