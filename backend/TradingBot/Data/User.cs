using Microsoft.AspNetCore.Identity;

namespace TradingBot.Data
{
    // Extend IdentityUser for application-specific properties
    public class User : IdentityUser
    {
        // e.g. public string FullName { get; set; }
        // Navigation: user-owned bots
        public ICollection<Bot> Bots { get; set; } = new List<Bot>();
    }
} 