using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace InvockApi;

// ─── Database entities ────────────────────────────────────────────────────────
public class User
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "investor";
    public string Name { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

public class QaPost
{
    public int Id { get; set; }
    public string Author { get; set; } = "";
    public string? AskerEmail { get; set; }
    public string? Role { get; set; }
    public string Question { get; set; } = "";
    public string? Answer { get; set; }
    public string? AnsweredBy { get; set; }
    public string Date { get; set; } = "";
}

public class Feedback
{
    public int Id { get; set; }
    public string? UserEmail { get; set; }
    public string? Name { get; set; }
    public string Category { get; set; } = "General";
    public string Message { get; set; } = "";
    public string Status { get; set; } = "open";
    public string? AdminResponse { get; set; }
    public string CreatedAt { get; set; } = "";
}

public class Notification
{
    public int Id { get; set; }
    public string? UserEmail { get; set; }   // null = broadcast to everyone
    public string Message { get; set; } = "";
    public string Type { get; set; } = "info";
    public string CreatedAt { get; set; } = "";
}

public class NotificationRead   // per-user "I've read this" marker
{
    public string UserEmail { get; set; } = "";
    public int NotificationId { get; set; }
}

public class WatchlistItem
{
    public string UserEmail { get; set; } = "";
    public string Symbol { get; set; } = "";
}

// ─── Database context ─────────────────────────────────────────────────────────
public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<QaPost> QaPosts => Set<QaPost>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRead> NotificationReads => Set<NotificationRead>();
    public DbSet<WatchlistItem> Watchlist => Set<WatchlistItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasKey(u => u.Email);
        b.Entity<NotificationRead>().HasKey(n => new { n.UserEmail, n.NotificationId });
        b.Entity<WatchlistItem>().HasKey(w => new { w.UserEmail, w.Symbol });
    }
}

// ─── Password hashing (PBKDF2) + demo seed data ───────────────────────────────
public static class Security
{
    public static string Hash(string pw)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(pw, salt, 200_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
    }

    public static bool Verify(string pw, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        byte[] salt = Convert.FromHexString(parts[0]);
        byte[] expected = Convert.FromHexString(parts[1]);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(pw, salt, 200_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public static class Seed
{
    public static void Run(AppDb db)
    {
        db.Database.EnsureCreated();
        var today = DateTime.Now.ToString("yyyy-MM-dd");

        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User { Email = "admin@psx.com",  Password = Security.Hash("admin123"),  Role = "admin",    Name = "Admin",           CreatedAt = today },
                new User { Email = "expert@psx.com", Password = Security.Hash("expert123"), Role = "expert",   Name = "Dr. Ayesha Khan", CreatedAt = today },
                new User { Email = "user@psx.com",   Password = Security.Hash("user123"),   Role = "investor", Name = "Ali Raza",        CreatedAt = today }
            );
        }
        if (!db.Notifications.Any())
        {
            db.Notifications.Add(new Notification { UserEmail = null, Message = "Welcome to Invock Investments!", Type = "announcement", CreatedAt = today });
        }
        db.SaveChanges();
    }
}

// Reference list of the 3 stocks (name + sector). Prices come from the Python model.
public static class Stocks
{
    public static readonly (string Symbol, string Name, string Sector)[] All =
    {
        ("OGDC", "Oil & Gas Development Co.", "Energy"),
        ("HBL",  "Habib Bank",               "Banking"),
        ("LUCK", "Lucky Cement",             "Cement"),
    };
    public static bool IsValid(string symbol) => All.Any(s => s.Symbol == symbol.ToUpper());
}
