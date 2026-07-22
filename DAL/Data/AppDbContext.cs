using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; 
using Microsoft.EntityFrameworkCore;

namespace DAL.Data
{
    public class AppDbContext(DbContextOptions options) : IdentityDbContext<AppUser>(options) 
    {
        
        public DbSet<Category> Categories { get; set; }
        public DbSet<ItemInventory> ItemInventories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }
        public DbSet<ItemDictionary> ItemDictionaries { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<IncomeSource> IncomeSources { get; set; }
        public DbSet<IncomeHistory> IncomeHistories { get; set; }
        public DbSet<BudgetMember> BudgetMembers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 

            
            // --- SEED ROLES ---
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "user-id", Name = "User", NormalizedName = "USER", ConcurrencyStamp = "STATIC-GUID-ROLE-USER" },
                new IdentityRole { Id = "admin-id", Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "STATIC-GUID-ROLE-ADMIN" }
            );
      
                // --- SEED ADMIN USER ---
                var adminId = "admin-id";
                var adminUser = new AppUser
                {
                    Id = adminId,
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@gmail.com",
                    NormalizedEmail = "ADMIN@GMAIL.COM",
                    EmailConfirmed = true,
                    SecurityStamp = "STATIC-GUID-SEC-12345",
                    ConcurrencyStamp = "STATIC-GUID-CON-67890",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                };

                // Password Hash 
                var hasher = new PasswordHasher<AppUser>();
                adminUser.PasswordHash = hasher.HashPassword(adminUser, "admin_temporary_password");

                builder.Entity<AppUser>().HasData(adminUser);

                // --- ASSIGN ROLE ---
                builder.Entity<IdentityUserRole<string>>().HasData(
                    new IdentityUserRole<string>
                    {
                        RoleId = "admin-id",
                        UserId = adminId
                    }
                );
           
            // --- Cấu hình Entity ---
            builder.Entity<Transaction>().Property(t => t.TotalAmount).HasPrecision(18, 2);
            builder.Entity<TransactionDetail>().Property(td => td.Price).HasPrecision(18, 2);

            builder.Entity<BudgetMember>()
                .HasOne(bm => bm.Budget)
                .WithMany(b => b.BudgetMembers)
                .HasForeignKey(bm => bm.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BudgetMember>()
                .HasOne(bm => bm.Member)
                .WithMany(u => u.SharedBudgets)
                .HasForeignKey(bm => bm.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}