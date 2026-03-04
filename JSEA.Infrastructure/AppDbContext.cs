using System;
using System.Collections.Generic;
using JSEA_Application.Models;
using JSEA_Infrastructure.Data;
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
    public virtual DbSet<Experience> Experiences { get; set; }
    public virtual DbSet<ExperienceDetail> ExperienceDetails { get; set; }
    public virtual DbSet<ExperienceMetric> ExperienceMetrics { get; set; }
    public virtual DbSet<ExperiencePhoto> ExperiencePhotos { get; set; }
    public virtual DbSet<ExperienceTag> ExperienceTags { get; set; }
    public virtual DbSet<Factor> Factors { get; set; }
    public virtual DbSet<Feedback> Feedbacks { get; set; }
    public virtual DbSet<Journey> Journeys { get; set; }
    public virtual DbSet<JourneyCrowdLog> JourneyCrowdLogs { get; set; }
    public virtual DbSet<JourneyMoodLog> JourneyMoodLogs { get; set; }
    public virtual DbSet<JourneySuggestion> JourneySuggestions { get; set; }
    public virtual DbSet<JourneyWaypoint> JourneyWaypoints { get; set; }
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
    public virtual DbSet<UserVibe> UserVibes { get; set; }
    public virtual DbSet<Visit> Visits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("fuzzystrmatch")
            .HasPostgresExtension("pg_trgm")
            .HasPostgresExtension("postgis")
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ActionType).HasConversion<string>().HasMaxLength(50);
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
            entity.HasKey(e => e.Id).HasName("events_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(d => d.Experience).WithMany(p => p.Events)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_event_exp");
            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Events)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_event_user");
        });

        modelBuilder.Entity<EventOccurrence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("event_occurrences_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(d => d.Event).WithMany(p => p.EventOccurrences)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_occ_event");
        });

        modelBuilder.Entity<Experience>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("experiences_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Country).HasDefaultValue("Vietnam");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Status).HasDefaultValue("active");
            entity.HasOne(d => d.Category).WithMany(p => p.Experiences)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_exp_cat");
            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Experiences)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_exp_user");
        });

        modelBuilder.Entity<ExperienceDetail>(entity =>
        {
            entity.HasKey(e => e.ExperienceId).HasName("experience_details_pkey");
            entity.Property(e => e.ExperienceId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Experience)
                .WithOne(p => p.ExperienceDetail)
                .HasForeignKey<ExperienceDetail>(d => d.ExperienceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_detail_exp");
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
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Experience).WithMany(p => p.ExperiencePhotos)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_photo_exp");
            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ExperiencePhotos)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_photo_user");
        });

        modelBuilder.Entity<ExperienceTag>(entity =>
        {
            entity.HasKey(e => new { e.ExperienceId, e.FactorId }).HasName("experience_tags_pkey");
            entity.HasOne(d => d.Experience).WithMany(p => p.ExperienceTags)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_exp_tags_exp");
            entity.HasOne(d => d.Factor).WithMany(p => p.ExperienceTags)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_exp_tags_factor");
        });

        modelBuilder.Entity<Factor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("factors_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("feedbacks_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsFlagged).HasDefaultValue(false);
            entity.HasOne(d => d.Visit).WithOne(p => p.Feedback)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_feedback_visit");
        });

        modelBuilder.Entity<Journey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("journeys_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.PreferredCrowdLevel).HasDefaultValue("all");
            entity.Property(e => e.Status).HasDefaultValue("planning");
            entity.HasOne(d => d.Traveler).WithMany(p => p.Journeys)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_journey_user");
            entity.HasOne(d => d.CurrentMoodFactor).WithMany(p => p.Journeys)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_journey_mood");
        });

        modelBuilder.Entity<JourneyCrowdLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("journey_crowd_logs_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Journey).WithMany(p => p.JourneyCrowdLogs)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_crowd_log_journey");
        });

        modelBuilder.Entity<JourneyMoodLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("journey_mood_logs_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Journey).WithMany(p => p.JourneyMoodLogs)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_mood_log_journey");
            entity.HasOne(d => d.Factor).WithMany(p => p.JourneyMoodLogs)
                .HasConstraintName("fk_mood_log_factor");
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
            entity.HasOne(d => d.Suggestion).WithMany(p => p.JourneyWaypoints)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_waypoint_suggestion");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
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
            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_refresh_token_user");
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
            entity.HasKey(e => e.Id).HasName("suggestion_interactions_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.InteractedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.InteractionType).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(d => d.Suggestion).WithMany(p => p.SuggestionInteractions)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_interact_sugg");
        });

        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("system_configs_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(d => d.UpdatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_config_user");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transactions_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.User).WithMany(p => p.Transactions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_trans_user");
            entity.HasOne(d => d.Package).WithMany(p => p.Transactions)
                .HasConstraintName("fk_trans_pkg");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.EmailVerified).HasDefaultValue(false);
            entity.Property(e => e.PhoneVerified).HasDefaultValue(false);
        });

        modelBuilder.Entity<UserFavorite>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ExperienceId }).HasName("user_favorites_pkey");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(d => d.Experience).WithMany(p => p.UserFavorites)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_fav_exp");
            entity.HasOne(d => d.User).WithMany(p => p.UserFavorites)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_fav_user");
        });

        modelBuilder.Entity<UserPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_packages_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ActivatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UsedKm).HasDefaultValue(0m);
            entity.HasOne(d => d.Package).WithMany(p => p.UserPackages).HasConstraintName("fk_upkg_pkg");
            entity.HasOne(d => d.User).WithMany(p => p.UserPackages)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_upkg_user");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_profiles_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(d => d.User)
                .WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_profile");
        });

        modelBuilder.Entity<UserVibe>(entity =>
        {
            entity.HasKey(uv => new { uv.UserProfileId, uv.FactorId }).HasName("user_vibes_pkey");
            entity.HasOne(uv => uv.UserProfile)
                .WithMany(up => up.UserVibes)
                .HasForeignKey(uv => uv.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_vibes_profile");
            entity.HasOne(uv => uv.Factor)
                .WithMany(f => f.UserVibes)
                .HasForeignKey(uv => uv.FactorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_vibes_factor");
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

        modelBuilder.Entity<RouteSuggestionSqlRow>().HasNoKey();

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
