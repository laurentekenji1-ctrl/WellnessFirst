using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WellnessPlatform.Models;

namespace WellnessPlatform.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Permission> Permissions { get; set; } = null!;

    public DbSet<UserPermission> UserPermissions { get; set; } = null!;

    public DbSet<ServiceItem> Services { get; set; } = null!;

    public DbSet<Appointment> Appointments { get; set; } = null!;

    public DbSet<BusinessHours> BusinessHours { get; set; } = null!;

    public DbSet<AvailabilitySettings> AvailabilitySettings { get; set; } = null!;

    public DbSet<Goal> Goals { get; set; } = null!;

    public DbSet<ActionPlan> ActionPlans { get; set; } = null!;

    public DbSet<WellnessRecord> WellnessRecords { get; set; } = null!;

    public DbSet<Payment> Payments { get; set; } = null!;

    public DbSet<PaymentRefund> PaymentRefunds { get; set; } = null!;

    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    public DbSet<Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .ToTable("Users");

        modelBuilder.Entity<AppUser>()
            .Property(x => x.EmailConfirmed)
            .HasColumnName("EmailVerified");

        modelBuilder.Entity<AppUser>()
            .Property(x => x.Email)
            .HasMaxLength(450);

        modelBuilder.Entity<AppUser>()
            .Property(x => x.NormalizedEmail)
            .HasMaxLength(450);

        // ================================
        // APP USER
        // ================================

        modelBuilder.Entity<AppUser>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.Email)
            .IsUnique();

        // ================================
        // PERMISSION
        // ================================

        modelBuilder.Entity<Permission>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Permission>()
            .HasIndex(x => x.Key)
            .IsUnique();

        // ================================
        // USER PERMISSION
        // ================================

        modelBuilder.Entity<UserPermission>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<UserPermission>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserPermissions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserPermission>()
            .HasOne(x => x.Permission)
            .WithMany(x => x.UserPermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent duplicate permission assignments
        modelBuilder.Entity<UserPermission>()
            .HasIndex(x => new
            {
                x.UserId,
                x.PermissionId
            })
            .IsUnique();

        // ================================
        // SERVICE
        // ================================

        modelBuilder.Entity<ServiceItem>()
            .Property(x => x.Price)
            .HasPrecision(18, 2);

        // ================================
        // PAYMENT
        // ================================

        modelBuilder.Entity<Payment>()
            .Property(x => x.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .Property(payment => payment.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("php");

        modelBuilder.Entity<Payment>()
            .Property(payment => payment.CheckoutSessionId)
            .HasMaxLength(255);

        modelBuilder.Entity<Payment>()
            .Property(payment => payment.CheckoutUrl)
            .HasMaxLength(2048);

        modelBuilder.Entity<Payment>()
            .Property(payment => payment.StripePaymentIntentId)
            .HasMaxLength(255);

        modelBuilder.Entity<Payment>()
            .Property(payment => payment.ChargedCurrency)
            .HasMaxLength(3);

        modelBuilder.Entity<PaymentRefund>()
            .Property(refund => refund.Currency)
            .HasMaxLength(3);

        modelBuilder.Entity<PaymentRefund>()
            .Property(refund => refund.StripeRefundId)
            .HasMaxLength(255);

        modelBuilder.Entity<PaymentRefund>()
            .Property(refund => refund.Reason)
            .HasMaxLength(500);

        modelBuilder.Entity<PaymentRefund>()
            .HasIndex(refund => refund.StripeRefundId)
            .IsUnique()
            .HasFilter("[StripeRefundId] IS NOT NULL");

        modelBuilder.Entity<Payment>()
            .HasIndex(payment => payment.CheckoutSessionId)
            .IsUnique()
            .HasFilter("[CheckoutSessionId] IS NOT NULL");

        modelBuilder.Entity<Payment>()
            .HasIndex(payment => payment.StripePaymentIntentId)
            .IsUnique()
            .HasFilter("[StripePaymentIntentId] IS NOT NULL");

        // ================================
        // APPOINTMENT RELATIONSHIPS
        // ================================

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Staff)
            .WithMany()
            .HasForeignKey(x => x.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.ServiceItem)
            .WithMany()
            .HasForeignKey(x => x.ServiceItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BusinessHours>()
            .HasIndex(hours => hours.DayOfWeek)
            .IsUnique();

        modelBuilder.Entity<AvailabilitySettings>()
            .Property(settings => settings.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<AvailabilitySettings>()
            .Property(settings => settings.TimeZoneId)
            .HasMaxLength(100);

        modelBuilder.Entity<Payment>()
            .HasOne(payment => payment.Appointment)
            .WithMany(appointment => appointment.Payments)
            .HasForeignKey(payment => payment.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentRefund>()
            .HasOne(refund => refund.Payment)
            .WithMany(payment => payment.Refunds)
            .HasForeignKey(refund => refund.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentRefund>()
            .HasOne(refund => refund.CreatedBy)
            .WithMany()
            .HasForeignKey(refund => refund.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ================================
        // GOALS
        // ================================

        modelBuilder.Entity<Goal>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActionPlan>()
            .HasOne(actionPlan => actionPlan.Client)
            .WithMany()
            .HasForeignKey(actionPlan => actionPlan.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActionPlan>()
            .HasOne(actionPlan => actionPlan.Staff)
            .WithMany()
            .HasForeignKey(actionPlan => actionPlan.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActionPlan>()
            .Property(actionPlan => actionPlan.Title)
            .HasMaxLength(160);

        modelBuilder.Entity<ActionPlan>()
            .Property(actionPlan => actionPlan.Description)
            .HasMaxLength(2000);

        // ================================
        // WELLNESS RECORDS
        // ================================

        modelBuilder.Entity<WellnessRecord>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}