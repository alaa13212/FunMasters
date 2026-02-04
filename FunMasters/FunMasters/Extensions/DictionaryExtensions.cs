namespace FunMasters.Extensions;

public static class DictionaryExtensions
{
    public static TValue GetOrSet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory) => dictionary.TryGetValue(key, out var value) ? value : dictionary[key] = valueFactory(key);
}