using System.Linq.Expressions; 
using IdentityModule.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore; 

namespace Shared.Infrastructure;

// public class SharedDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
// {
//     public SharedDbContext(DbContextOptions<SharedDbContext> options)
//         : base(options) { }

//     protected override void OnModelCreating(ModelBuilder builder)
//     {
//         base.OnModelCreating(builder);

//     }
// }


