using System.Collections.Concurrent;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class ApiInformationCollection
{
    private ConcurrentDictionary<string, ApiInformation>? _collection;

    public void Replace(IDictionary<string, ApiInformation> source)
        => _collection = new ConcurrentDictionary<string, ApiInformation>(source);

    public ApiInformation? Get(string name)
    {
        if (_collection == null || !_collection.TryGetValue(name, out var value))
        {
            return null;
        }

        return value;
    }
}
