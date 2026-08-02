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
        public DbSet<UserCategorySetting> UserCategorySettings { get; set; }
        public DbSet<ItemInventory> ItemInventories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }
        public DbSet<ItemDictionary> ItemDictionaries { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<IncomeSource> IncomeSources { get; set; }
        public DbSet<IncomeHistory> IncomeHistories { get; set; }
        public DbSet<BudgetMember> BudgetMembers { get; set; }
        public DbSet<BudgetIncomeSource> BudgetIncomeSources { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
        public DbSet<SupportAttachment> SupportAttachments { get; set; }

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

            // --- Support Ticket Configurations ---
            builder.Entity<SupportTicket>()
                .HasOne(st => st.User)
                .WithMany()
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SupportTicket>()
                .HasOne(st => st.AssignedTo)
                .WithMany()
                .HasForeignKey(st => st.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<SupportMessage>()
                .HasOne(sm => sm.Ticket)
                .WithMany(t => t.Messages)
                .HasForeignKey(sm => sm.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SupportMessage>()
                .HasOne(sm => sm.Sender)
                .WithMany()
                .HasForeignKey(sm => sm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SupportAttachment>()
                .HasOne(sa => sa.Ticket)
                .WithMany(t => t.Attachments)
                .HasForeignKey(sa => sa.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SupportAttachment>()
                .HasOne(sa => sa.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(sa => sa.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

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

            builder.Entity<BudgetIncomeSource>()
                .HasOne(bis => bis.Budget)
                .WithMany(b => b.BudgetIncomeSources)
                .HasForeignKey(bis => bis.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BudgetIncomeSource>()
                .HasOne(bis => bis.IncomeSource)
                .WithMany(i => i.BudgetIncomeSources)
                .HasForeignKey(bis => bis.IncomeSourceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Category>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserCategorySetting>()
                .HasIndex(s => new { s.UserId, s.CategoryId })
                .IsUnique();

            builder.Entity<UserCategorySetting>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserCategorySetting>()
                .HasOne(s => s.Category)
                .WithMany()
                .HasForeignKey(s => s.CategoryId)
                // Avoid SQL Server's multiple cascade path through User -> Categories.
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
