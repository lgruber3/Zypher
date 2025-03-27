using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Model.Model;

namespace Model;

public class ContextClass : DbContext
{
    public DbSet<Ranking> Rankings { get; set; }
    
    public ContextClass(DbContextOptions<ContextClass> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}