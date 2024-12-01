using Microsoft.EntityFrameworkCore;

namespace REST_e_bancoDeDados.db;

public partial class TarefasContext : DbContext
{
    public TarefasContext()
    {
    }

    public TarefasContext(DbContextOptions<TarefasContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Tarefa> Tarefa { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { // aqui é a função de configuração no momento em que a conexão é criada, como vamos fazer pela aplicação
    } // podemos deixar a função vazia

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb3_general_ci")
            .HasCharSet("utf8mb3");

        modelBuilder.Entity<Tarefa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tarefa");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Concluida).HasColumnName("concluida");
            entity.Property(e => e.Descricao)
                .HasMaxLength(200)
                .HasColumnName("descricao");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
