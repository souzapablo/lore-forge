using LoreForge.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;

namespace LoreForge.Infrastructure.Persistence;

public class LoreForgeDbContext : DbContext
{
    public LoreForgeDbContext(DbContextOptions<LoreForgeDbContext> options) : base(options) { }

    public DbSet<Work> Works => Set<Work>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<WorldNote> WorldNotes => Set<WorldNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        var vectorConverter = new ValueConverter<float[], Vector>(
            v => new Vector(v),
            v => v.ToArray()
        );

        var vectorComparer = new ValueComparer<float[]>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v.Aggregate(0, (hash, f) => HashCode.Combine(hash, f.GetHashCode())),
            v => v.ToArray()
        );

        modelBuilder.Entity<Work>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Genres).HasColumnType("text[]");
            entity.Property(e => e.Tags).HasColumnType("text[]");
            entity.Property(e => e.Progress).HasColumnType("jsonb");
            entity.OwnsOne(e => e.Notes, notes => notes.ToJson());
            entity.Property(e => e.Embedding)
                .HasConversion(vectorConverter, vectorComparer)
                .HasColumnType("vector(1536)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RawContent).IsRequired();
            entity.Property(e => e.Embedding)
                .HasConversion(vectorConverter, vectorComparer)
                .HasColumnType("vector(1536)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<WorldNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Embedding)
                .HasConversion(vectorConverter, vectorComparer)
                .HasColumnType("vector(1536)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });
    }
}
