using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Acorn.Core.Database;

internal sealed class AcornSqliteMigrationsSqlGenerator : SqliteMigrationsSqlGenerator
{
  public AcornSqliteMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    IRelationalAnnotationProvider migrationsAnnotations)
    : base(dependencies, migrationsAnnotations)
  {
  }

  protected override void Generate(
    CreateTableOperation operation,
    IModel? model,
    MigrationCommandListBuilder builder,
    bool terminate = true)
  {
    base.Generate(operation, model, builder, terminate: false);

    var tableName = operation.Name.StartsWith("ef_temp_", StringComparison.Ordinal)
      ? operation.Name["ef_temp_".Length..]
      : operation.Name;

    if (model?.GetEntityTypes().Any(entity =>
        entity.GetTableName() == tableName &&
        entity.FindAnnotation(SqliteTableAnnotations.WithoutRowid)?.Value is true) == true ||
      tableName is "blob" or "content" or "file")
    {
      _ = builder.Append(" WITHOUT ROWID");
    }

    if (terminate)
    {
      _ = builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      EndStatement(builder);
    }
  }
}
