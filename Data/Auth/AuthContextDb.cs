using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MinimalApiDemo.Data.Auth
{
    public class AuthContextDb : IdentityDbContext<IdentityUser>
    {
        public AuthContextDb(DbContextOptions<AuthContextDb> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
