using System.Collections;
using Avalonia.Data.Converters;

namespace Jc.MediaImporter.Converters;

public class CollectionConverters
{
    public static readonly IValueConverter IsNullOrEmpty =
        new FuncValueConverter<ICollection?, bool>(x => (x?.Count ?? 0) == 0);

    public static readonly IValueConverter IsNotNullOrEmpty =
        new FuncValueConverter<ICollection?, bool>(x => x?.Count > 0);
}