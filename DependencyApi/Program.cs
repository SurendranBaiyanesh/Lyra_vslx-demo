using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Dependency API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// DLLs live on the host disk as {StorageRoot}/{name}/{version}/{name}
var configuredRoot = builder.Configuration["StorageRoot"];
var storageRoot = Path.GetFullPath(string.IsNullOrEmpty(configuredRoot) ? Path.Combine(app.Environment.ContentRootPath, "App_Data", "dlls") : configuredRoot);
var apiKey = builder.Configuration["ApiKey"];

var api = app.MapGroup("/api/dependencies").WithTags("Dependencies");

// list every stored DLL with its versions
api.MapGet("/", (HttpRequest req) =>
{
    if (!Authorized(req, required: false)) return Results.Unauthorized();
    if (!Directory.Exists(storageRoot)) return Results.Ok(Array.Empty<object>());
    return Results.Ok(Directory.GetDirectories(storageRoot).Select(d => new
    {
        Name = Path.GetFileName(d),
        Versions = Directory.GetDirectories(d).Select(Path.GetFileName).Order().ToArray()
    }));
});

// download: used by the VSIX Deploy button
api.MapGet("/{name}/{version}", (string name, string version, HttpRequest req) =>
{
    if (!Authorized(req, required: false)) return Results.Unauthorized();
    if (FilePath(name, version) is not { } path) return Results.BadRequest("Invalid name or version.");
    return File.Exists(path)
        ? Results.File(path, "application/octet-stream", name)
        : Results.NotFound($"{name} {version} not found.");
});

// upload or replace a DLL version
api.MapPut("/{name}/{version}", async (string name, string version, IFormFile file, HttpRequest req) =>
{
    if (!Authorized(req, required: true)) return Results.Unauthorized();
    if (FilePath(name, version) is not { } path) return Results.BadRequest("Invalid name or version.");
    if (!name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) return Results.BadRequest("Only .dll files are accepted.");

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    var temp = path + ".upload";
    await using (var stream = File.Create(temp))
        await file.CopyToAsync(stream);
    File.Move(temp, path, overwrite: true);
    return Results.Ok(new { name, version, file.Length });
}).DisableAntiforgery();

api.MapDelete("/{name}/{version}", (string name, string version, HttpRequest req) =>
{
    if (!Authorized(req, required: true)) return Results.Unauthorized();
    if (FilePath(name, version) is not { } path) return Results.BadRequest("Invalid name or version.");
    if (!File.Exists(path)) return Results.NotFound();
    Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    return Results.NoContent();
});

app.Run();

// writes always need the key; reads need it only when one is configured
bool Authorized(HttpRequest req, bool required)
{
    if (string.IsNullOrEmpty(apiKey)) return !required;
    return req.Headers.Authorization.ToString() == $"Bearer {apiKey}";
}

// null when name/version would escape the storage folder
string? FilePath(string name, string version)
{
    if (name != Path.GetFileName(name) || version != Path.GetFileName(version) || name is "." or ".." || version is "." or "..")
        return null;
    var path = Path.GetFullPath(Path.Combine(storageRoot, name, version, name));
    return path.StartsWith(storageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? path : null;
}
