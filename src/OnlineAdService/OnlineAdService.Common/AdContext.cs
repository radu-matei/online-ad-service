using System.Data.Entity;

namespace OnlineAdService.Common
{
    public class AdContext : DbContext
    {
        public AdContext() : base("name=AdContext")
        {
        }

        public DbSet<Ad> Ads { get; set; }
    }
}
