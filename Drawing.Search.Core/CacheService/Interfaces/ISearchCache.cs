namespace Drawing.Search.Core.CacheService.Interfaces;

public interface ISearchCache
{
    void RefreshCache();
    void ClearCache();
    void AddMainKeyToCache(string mainKey);
    void AddEntryByMainKey(string mainKey, string entryKey, object entryValue);
}