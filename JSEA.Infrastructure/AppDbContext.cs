using System;
using System.Collections.Generic;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<EmailOtp> EmailOtps { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventOccurrence> EventOccurrences { get; set; }

    public virtual DbSet<ExperienceDetail> ExperienceDetails { get; set; }

    public virtual DbSet<ExperienceMetric> ExperienceMetrics { get; set; }

    public virtual DbSet<ExperiencePhoto> ExperiencePhotos { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Journey> Journeys { get; set; }

    public virtual DbSet<JourneySuggestion> JourneySuggestions { get; set; }

    public virtual DbSet<JourneyWaypoint> JourneyWaypoints { get; set; }

    public virtual DbSet<MicroExperience> MicroExperiences { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Package> Packages { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<RouteSegment> RouteSegments { get; set; }

    public virtual DbSet<SuggestionInteraction> SuggestionInteractions { get; set; }

    public virtual DbSet<SystemConfig> SystemConfigs { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserFavorite> UserFavorites { get; set; }

    public virtual DbSet<UserPackage> UserPackages { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<Visit> Visits { get; set; }

    public virtual DbSet<TravelStyle> TravelStyles { get; set; }
    public virtual DbSet<UserVibe> UserVibes { get; set; }

    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=JSEA;Username=postgres;Password=5432", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("fuzzystrmatch")
            .HasPostgresExtension("pg_trgm")
            .HasPostgresExtension("postgis")
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.ActionType).HasConversion<string>().HasMaxLength(50);
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_audit_user");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categories_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("email_otps_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(e => e.RecurrencePattern).HasConversion<string>().HasMaxLength(50);
            entity.HasKey(e => e.Id).HasName("events_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(d => d.Experience).WithMany(p => p.Events)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_event_exp");
        });

        modelBuilder.Entity<EventOccurrence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("event_occurrences_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(d => d.Event).WithMany(p => p.EventOccurrences)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_occ_event");
        });

        modelBuilder.Entity<ExperienceDetail>(entity =>
        {
            entity.HasKey(e => e.ExperienceId).HasName("experience_details_pkey");

            entity.Property(e => e.ExperienceId).ValueGeneratedNever();

            entity.HasOne(d => d.Experience)
                .WithOne(p => p.ExperienceDetail)
                .HasForeignKey<ExperienceDetail>(d => d.ExperienceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_detail_exp");

            entity.HasOne(d => d.FeaturedByUser).WithMany(p => p.ExperienceDetailFeaturedByUsers).HasConstraintName("fk_detail_featured");

            entity.HasOne(d => d.VerifiedByUser).WithMany(p => p.ExperienceDetailVerifiedByUsers).HasConstraintName("fk_detail_verifier");
        });

        modelBuilder.Entity<ExperienceMetric>(entity =>
        {
            entity.HasKey(e => e.ExperienceId).HasName("experience_metrics_pkey");
            entity.Property(e => e.ExperienceId).ValueGeneratedNever();
            entity.Property(e => e.AvgRating).HasDefaultValue(0m);
            entity.Property(e => e.TotalRatings).HasDefaultValue(0);
            entity.Property(e => e.TotalVisits).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Experience)
                .WithOne(p => p.ExperienceMetric)
                .HasForeignKey<ExperienceMetric>(d => d.ExperienceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_metric_exp");
        });

        modelBuilder.Entity<ExperiencePhoto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("experience_photos_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsCover).HasDefaultValue(false);

            entity.HasOne(d => d.Experience).WithMany(p => p.ExperiencePhotos)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_photo_exp");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("feedbacks_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.IsFlagged).HasDefaultValue(false);

            entity.HasOne(d => d.Visit).WithOne(p => p.Feedback)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_feedback_visit");
        });

        modelBuilder.Entity<Journey>(entity =>
        {
            entity.Property(e => e.CurrentMood).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.VehicleType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasKey(e => e.Id).HasName("journeys_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Traveler).WithMany(p => p.Journeys)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_journey_user");
        });

        modelBuilder.Entity<JourneySuggestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("journey_suggestions_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.SuggestedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Experience).WithMany(p => p.JourneySuggestions).HasConstraintName("fk_sugg_exp");

            entity.HasOne(d => d.Journey).WithMany(p => p.JourneySuggestions)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_sugg_journey");

            entity.HasOne(d => d.Segment).WithMany(p => p.JourneySuggestions).HasConstraintName("fk_sugg_segment");
        });

        modelBuilder.Entity<JourneyWaypoint>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("journey_waypoints_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(d => d.Experience).WithMany(p => p.JourneyWaypoints).HasConstraintName("fk_waypoint_exp");

            entity.HasOne(d => d.Journey).WithMany(p => p.JourneyWaypoints)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_waypoint_journey");
        });

        modelBuilder.Entity<MicroExperience>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasKey(e => e.Id).HasName("micro_experiences_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Country).HasDefaultValueSql("'Vietnam'::character varying");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Category).WithMany(p => p.MicroExperiences)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_exp_cat");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.MicroExperiences)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_exp_user");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.NotificationType).HasConversion<string>().HasMaxLength(100);
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_notif_user");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.HasKey(e => e.Id).HasName("packages_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsPopular).HasDefaultValue(false);
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ratings_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Visit).WithOne(p => p.Rating)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_rating_visit");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens).HasConstraintName("fk_refresh_token_user");
        });

        modelBuilder.Entity<RouteSegment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("route_segments_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(d => d.Journey).WithMany(p => p.RouteSegments)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_segment_journey");
        });

        modelBuilder.Entity<SuggestionInteraction>(entity =>
        {
            entity.Property(e => e.InteractionType).HasConversion<string>().HasMaxLength(50);
            entity.HasKey(e => e.Id).HasName("suggestion_interactions_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.InteractedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Suggestion).WithMany(p => p.SuggestionInteractions)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_interact_sugg");
        });

        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("system_configs_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasKey(e => e.Id).HasName("transactions_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.Transactions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_trans_user");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");
            entity.Property(e => e.Role)
            .HasConversion<string>()
            .HasMaxLength(50);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.EmailVerified).HasDefaultValue(false);
            entity.Property(e => e.PhoneVerified).HasDefaultValue(false);
        });

        modelBuilder.Entity<UserFavorite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_favorites_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Experience).WithMany(p => p.UserFavorites).HasConstraintName("fk_fav_exp");

            entity.HasOne(d => d.User).WithMany(p => p.UserFavorites).HasConstraintName("fk_fav_user");
        });

        modelBuilder.Entity<UserPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_packages_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ActivatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Package).WithMany(p => p.UserPackages).HasConstraintName("fk_upkg_pkg");

            entity.HasOne(d => d.User).WithMany(p => p.UserPackages)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_upkg_user");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_profiles_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.RewardPoints).HasDefaultValue(0);

            entity.HasOne(d => d.User)
                .WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_profile");
        });

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("visits_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.VisitedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Experience).WithMany(p => p.Visits).HasConstraintName("fk_visit_exp");

            entity.HasOne(d => d.Journey).WithMany(p => p.Visits).HasConstraintName("fk_visit_journey");

            entity.HasOne(d => d.Traveler).WithMany(p => p.Visits).HasConstraintName("fk_visit_user");
        });

        modelBuilder.Entity<UserVibe>(entity =>
        {
            entity.HasKey(uv => new { uv.UserProfileId, uv.TravelStyleId })
                  .HasName("user_vibes_pkey");

            entity.HasOne(uv => uv.UserProfile)
                  .WithMany(up => up.UserVibes)
                  .HasForeignKey(uv => uv.UserProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uv => uv.TravelStyle)
                  .WithMany(ts => ts.UserVibes)
                  .HasForeignKey(uv => uv.TravelStyleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
