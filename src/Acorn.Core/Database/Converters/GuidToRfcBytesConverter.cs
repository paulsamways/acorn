using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Acorn.Core.Database.Converters;

internal sealed class GuidToRfcBytesConverter : ValueConverter<Guid, byte[]>
{
  public GuidToRfcBytesConverter()
    : base(
      value => value.ToByteArray(bigEndian: true),
      value => new Guid(value, bigEndian: true))
  {
  }
}
