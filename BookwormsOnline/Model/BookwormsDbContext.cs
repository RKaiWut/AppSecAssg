using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace BookwormsOnline.Model
{
    public class BookwormsDbContext : IdentityDbContext
    {
        private readonly IConfiguration _configuration;

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PasswordHistory> PasswordHistories { get; set; }

        //public AuthDbContext(DbContextOptions<AuthDbContext> options):base(options){ }
        public BookwormsDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = _configuration.GetConnectionString("BookwormsConnectionString"); optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Member>(); // Explicitly declare the entity
        }
    }
}