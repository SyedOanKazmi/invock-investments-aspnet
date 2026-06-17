using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using InvockApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDb>(o => o.UseSqlite("Data Source=invock.db"));

// JSON: use snake_case so the Vue frontend (expects answered_by, is_read, ...) works.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-change-me-please-32-bytes-long!";
var keyBytes = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
JwtSecurityTokenHandler.DefaultMapInboundClaims = false; // keep claim names as-is (sub/role/name)

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = keyBytes,
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Create + seed the database on startup.
using (var scope = app.Services.CreateScope())
    Seed.Run(scope.ServiceProvider.GetRequiredService<AppDb>());

app.UseCors();
app.UseDefaultFiles();   // serve index.html at "/"
app.UseStaticFiles();    // serve the built Vue site from wwwroot (production)
app.UseAuthentication();
app.UseAuthorization();

// ── Helpers ───────────────────────────────────────────────────────────────────
string Email(ClaimsPrincipal u) => u.FindFirstValue("sub") ?? "";
string UserName(ClaimsPrincipal u) => u.FindFirstValue("name") ?? "";

string CreateToken(User u)
{
    var claims = new[]
    {
        new Claim("sub", u.Email),
        new Claim("role", u.Role),
        new Claim("name", u.Name),
    };
    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddDays(1),
        signingCredentials: new SigningCredentials(keyBytes, SecurityAlgorithms.HmacSha256));
    return new JwtSecurityTokenHandler().WriteToken(token);
}

void Notify(AppDb db, string? userEmail, string msg, string type)
{
    db.Notifications.Add(new Notification
    {
        UserEmail = userEmail,
        Message = msg,
        Type = type,
        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
    });
    db.SaveChanges();
}

// Run the Python model and return its raw JSON output.
async Task<string> RunPython(string args)
{
    // ML_DIR / PYTHON_BIN are set in the container; locally they default to ../ml + "python".
    var mlDir = Environment.GetEnvironmentVariable("ML_DIR")
                ?? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "ml"));
    var pythonBin = Environment.GetEnvironmentVariable("PYTHON_BIN") ?? "python";
    var psi = new ProcessStartInfo
    {
        FileName = pythonBin,
        Arguments = "predict.py " + args,
        WorkingDirectory = mlDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    using var p = Process.Start(psi)!;
    string output = await p.StandardOutput.ReadToEndAsync();
    string err = await p.StandardError.ReadToEndAsync();
    await p.WaitForExitAsync();
    if (p.ExitCode != 0) throw new Exception("Python error: " + err);
    return output;
}
IResult PyJson(string json) => Results.Content(json, "application/json");

// ── Auth ──────────────────────────────────────────────────────────────────────
app.MapPost("/api/auth/register", (RegisterReq r, AppDb db) =>
{
    if (db.Users.Any(u => u.Email == r.Email))
        return Results.Conflict(new { detail = "Email already registered" });
    var user = new User { Email = r.Email, Password = Security.Hash(r.Password), Role = "investor", Name = r.Name, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd") };
    db.Users.Add(user);
    db.SaveChanges();
    Notify(db, r.Email, "Welcome to Invock Investments!", "info");
    return Results.Ok(new { AccessToken = CreateToken(user), TokenType = "bearer", User = new { user.Email, user.Role, user.Name } });
});

app.MapPost("/api/auth/login", (LoginReq r, AppDb db) =>
{
    var user = db.Users.FirstOrDefault(u => u.Email == r.Email);
    if (user == null || !Security.Verify(r.Password, user.Password))
        return Results.Json(new { detail = "Invalid credentials" }, statusCode: 401);
    return Results.Ok(new { AccessToken = CreateToken(user), TokenType = "bearer", User = new { user.Email, user.Role, user.Name } });
});

app.MapGet("/api/auth/me", (ClaimsPrincipal u, AppDb db) =>
{
    var email = Email(u);
    var user = db.Users.FirstOrDefault(x => x.Email == email);
    return user == null ? Results.Unauthorized() : Results.Ok(new { user.Email, user.Role, user.Name });
}).RequireAuthorization();

// ── Stocks + predictions (data/model live in Python) ──────────────────────────
app.MapGet("/api/stocks", async (ClaimsPrincipal u, AppDb db) =>
{
    var json = await RunPython("stocks");
    var email = Email(u);
    var tracked = db.Watchlist.Where(w => w.UserEmail == email).Select(w => w.Symbol).ToHashSet();
    using var doc = JsonDocument.Parse(json);
    var stocks = doc.RootElement.EnumerateArray().Select(e => new
    {
        Symbol = e.GetProperty("symbol").GetString(),
        Name = e.GetProperty("name").GetString(),
        Sector = e.GetProperty("sector").GetString(),
        Price = e.GetProperty("price").GetDouble(),
        Change = e.GetProperty("change").GetDouble(),
        Tracked = tracked.Contains(e.GetProperty("symbol").GetString()!),
    }).ToList();
    return Results.Ok(new { Stocks = stocks });
}).RequireAuthorization();

app.MapGet("/api/stocks/{ticker}/history", async (string ticker) =>
    Stocks.IsValid(ticker) ? PyJson(await RunPython($"history {ticker.ToUpper()}")) : Results.NotFound()
).RequireAuthorization();

app.MapGet("/api/stocks/{ticker}/summary", async (string ticker) =>
    Stocks.IsValid(ticker) ? PyJson(await RunPython($"summary {ticker.ToUpper()}")) : Results.NotFound()
).RequireAuthorization();

app.MapGet("/api/predict/{ticker}", async (string ticker, int days) =>
{
    if (!Stocks.IsValid(ticker)) return Results.NotFound();
    days = Math.Clamp(days == 0 ? 7 : days, 7, 90);
    return PyJson(await RunPython($"predict {ticker.ToUpper()} {days}"));
}).RequireAuthorization();

// ── Watchlist ─────────────────────────────────────────────────────────────────
app.MapPost("/api/watchlist/{ticker}", (string ticker, ClaimsPrincipal u, AppDb db) =>
{
    ticker = ticker.ToUpper();
    if (!Stocks.IsValid(ticker)) return Results.NotFound();
    var email = Email(u);
    if (!db.Watchlist.Any(w => w.UserEmail == email && w.Symbol == ticker))
    {
        db.Watchlist.Add(new WatchlistItem { UserEmail = email, Symbol = ticker });
        db.SaveChanges();
    }
    return Results.Ok(new { message = "added" });
}).RequireAuthorization();

app.MapDelete("/api/watchlist/{ticker}", (string ticker, ClaimsPrincipal u, AppDb db) =>
{
    var email = Email(u);
    var sym = ticker.ToUpper();
    var item = db.Watchlist.FirstOrDefault(w => w.UserEmail == email && w.Symbol == sym);
    if (item != null) { db.Watchlist.Remove(item); db.SaveChanges(); }
    return Results.Ok(new { message = "removed" });
}).RequireAuthorization();

// ── Notifications ─────────────────────────────────────────────────────────────
app.MapGet("/api/notifications", (ClaimsPrincipal u, AppDb db) =>
{
    var email = Email(u);
    var readIds = db.NotificationReads.Where(r => r.UserEmail == email).Select(r => r.NotificationId).ToHashSet();
    var items = db.Notifications
        .Where(n => n.UserEmail == email || n.UserEmail == null)
        .OrderByDescending(n => n.Id).ToList()
        .Where(n => !readIds.Contains(n.Id))
        .Select(n => new { n.Id, n.Message, n.Type, n.CreatedAt, IsRead = false }).ToList();
    return Results.Ok(new { Notifications = items, Unread = items.Count });
}).RequireAuthorization();

app.MapPost("/api/notifications/read", (ClaimsPrincipal u, AppDb db) =>
{
    var email = Email(u);
    var ids = db.Notifications.Where(n => n.UserEmail == email || n.UserEmail == null).Select(n => n.Id).ToList();
    var have = db.NotificationReads.Where(r => r.UserEmail == email).Select(r => r.NotificationId).ToHashSet();
    foreach (var id in ids)
        if (!have.Contains(id)) db.NotificationReads.Add(new NotificationRead { UserEmail = email, NotificationId = id });
    db.SaveChanges();
    return Results.Ok(new { message = "ok" });
}).RequireAuthorization();

app.MapPost("/api/notifications/announce", (AnnounceReq r, AppDb db) =>
{
    Notify(db, null, r.Message, "announcement");
    return Results.Ok(new { message = "sent" });
}).RequireAuthorization(p => p.RequireRole("admin"));

// ── Q&A ───────────────────────────────────────────────────────────────────────
app.MapGet("/api/qa", (AppDb db) =>
    Results.Ok(db.QaPosts.OrderBy(p => p.Id)
        .Select(p => new { p.Id, p.Author, p.Role, p.Question, p.Answer, p.AnsweredBy, p.Date }).ToList())
).RequireAuthorization();

app.MapPost("/api/qa", (QuestionReq r, ClaimsPrincipal u, AppDb db) =>
{
    db.QaPosts.Add(new QaPost
    {
        Author = UserName(u), AskerEmail = Email(u), Role = u.FindFirstValue("role"),
        Question = r.Question, Date = DateTime.Now.ToString("yyyy-MM-dd"),
    });
    db.SaveChanges();
    return Results.Ok(new { message = "posted" });
}).RequireAuthorization();

app.MapPost("/api/qa/{id}/answer", (int id, AnswerReq r, ClaimsPrincipal u, AppDb db) =>
{
    var post = db.QaPosts.Find(id);
    if (post == null) return Results.NotFound();
    post.Answer = r.Answer;
    post.AnsweredBy = UserName(u);
    db.SaveChanges();
    if (!string.IsNullOrEmpty(post.AskerEmail))
        Notify(db, post.AskerEmail, $"{UserName(u)} answered your question.", "answer");
    return Results.Ok(new { message = "answered" });
}).RequireAuthorization(p => p.RequireRole("expert", "admin"));

app.MapDelete("/api/qa/{id}", (int id, AppDb db) =>
{
    var post = db.QaPosts.Find(id);
    if (post != null) { db.QaPosts.Remove(post); db.SaveChanges(); }
    return Results.Ok(new { message = "deleted" });
}).RequireAuthorization(p => p.RequireRole("expert", "admin"));

// ── Feedback ──────────────────────────────────────────────────────────────────
app.MapPost("/api/feedback", (FeedbackReq r, ClaimsPrincipal u, AppDb db) =>
{
    db.Feedbacks.Add(new Feedback
    {
        UserEmail = Email(u), Name = UserName(u), Category = r.Category,
        Message = r.Message, Status = "open", CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
    });
    db.SaveChanges();
    return Results.Ok(new { message = "Thank you for your feedback!" });
}).RequireAuthorization();

app.MapGet("/api/feedback", (ClaimsPrincipal u, AppDb db) =>
{
    var email = Email(u);
    var q = u.IsInRole("admin") ? db.Feedbacks : db.Feedbacks.Where(f => f.UserEmail == email);
    return Results.Ok(q.OrderByDescending(f => f.Id)
        .Select(f => new { f.Id, f.Category, f.Status, f.Message, f.CreatedAt, f.AdminResponse, f.Name }).ToList());
}).RequireAuthorization();

app.MapPost("/api/feedback/{id}/respond", (int id, RespondReq r, AppDb db) =>
{
    var fb = db.Feedbacks.Find(id);
    if (fb == null) return Results.NotFound();
    fb.AdminResponse = r.Response;
    fb.Status = "resolved";
    db.SaveChanges();
    if (!string.IsNullOrEmpty(fb.UserEmail))
        Notify(db, fb.UserEmail, "An admin responded to your feedback.", "feedback");
    return Results.Ok(new { message = "responded" });
}).RequireAuthorization(p => p.RequireRole("admin"));

app.MapDelete("/api/feedback/{id}", (int id, AppDb db) =>
{
    var fb = db.Feedbacks.Find(id);
    if (fb != null) { db.Feedbacks.Remove(fb); db.SaveChanges(); }
    return Results.Ok(new { message = "deleted" });
}).RequireAuthorization(p => p.RequireRole("admin"));

// ── Admin ─────────────────────────────────────────────────────────────────────
app.MapGet("/api/admin/users", (AppDb db) =>
    Results.Ok(db.Users.Select(u => new { u.Email, u.Name, u.Role }).ToList())
).RequireAuthorization(p => p.RequireRole("admin"));

app.MapDelete("/api/admin/users/{email}", (string email, ClaimsPrincipal u, AppDb db) =>
{
    if (email == Email(u)) return Results.BadRequest(new { detail = "Cannot delete yourself" });
    var user = db.Users.Find(email);
    if (user != null) { db.Users.Remove(user); db.SaveChanges(); }
    return Results.Ok(new { message = "deleted" });
}).RequireAuthorization(p => p.RequireRole("admin"));

app.MapPost("/api/admin/users/{email}/role", (string email, RoleReq r, ClaimsPrincipal u, AppDb db) =>
{
    if (r.Role is not ("investor" or "expert" or "admin")) return Results.BadRequest(new { detail = "Invalid role" });
    if (email == Email(u)) return Results.BadRequest(new { detail = "Cannot change your own role" });
    var user = db.Users.Find(email);
    if (user == null) return Results.NotFound();
    user.Role = r.Role;
    db.SaveChanges();
    Notify(db, email, $"Your account role is now '{r.Role}'.", "info");
    return Results.Ok(new { message = "updated" });
}).RequireAuthorization(p => p.RequireRole("admin"));

// SPA fallback: any non-API route returns index.html (frontend uses hash routing).
app.MapFallbackToFile("index.html");

app.Run();

// ── Request bodies ────────────────────────────────────────────────────────────
record LoginReq(string Email, string Password);
record RegisterReq(string Name, string Email, string Password);
record QuestionReq(string Question);
record AnswerReq(string Answer);
record FeedbackReq(string Category, string Message);
record RespondReq(string Response);
record AnnounceReq(string Message);
record RoleReq(string Role);
