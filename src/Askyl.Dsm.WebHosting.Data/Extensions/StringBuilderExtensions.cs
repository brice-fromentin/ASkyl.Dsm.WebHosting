using System.Text;

namespace Askyl.Dsm.WebHosting.Data.Extensions;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendSeparator(this StringBuilder builder, char separator = '&')
        => (builder.Length == 0) ? builder : builder.Append(separator);

    public static StringBuilder InsertSeparator(this StringBuilder builder, int index = 0, char separator = '&')
        => (builder.Length == 0) ? builder : builder.Insert(index, separator);
}
