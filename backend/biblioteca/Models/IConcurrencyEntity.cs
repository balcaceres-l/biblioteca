namespace biblioteca.Models;

public interface IConcurrencyEntity
{
    byte[]? RowVersion { get; set; }
}
