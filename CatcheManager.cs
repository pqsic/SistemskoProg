using System;
using System.Collections.Generic;

public class CacheManager
{
    private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
    private readonly object _lock = new object();
    private readonly TimeSpan _ttl;

    public CacheManager(TimeSpan ttl)
    {
        _ttl = ttl;
    }

    public bool TryGet(string key, out int result)
    {
        lock (_lock)
        {
            while (true)
            {
                if (_cache.TryGetValue(key, out CacheEntry? entry))
                {
                    if (!entry.IsReady)
                    {
                        Logger.LogInfo($"Nit ceka na rezultat za '{key}'");
                        Monitor.Wait(_lock);
                        continue;
                    }

                    if (DateTime.Now - entry.CreatedAt > _ttl)
                    {
                        Logger.LogInfo($"Cache istekao za '{key}', brisemo");
                        _cache.Remove(key);
                        _cache[key] = new CacheEntry { IsReady = false };
                        result = 0;
                        return false;
                    }

                    Logger.LogInfo($"Cache hit za '{key}'");
                    result = entry.PalindromeCount;
                    return true;
                }
                else
                {
                    _cache[key] = new CacheEntry { IsReady = false };
                    result = 0;
                    return false;
                }
            }
        }
    }

    public void Set(string key, int result)
    {
        lock (_lock)
        {
            _cache[key] = new CacheEntry
            {
                PalindromeCount = result,
                CreatedAt = DateTime.Now,
                IsReady = true
            };
            Monitor.PulseAll(_lock);
            Logger.LogInfo($"Cache upisan za '{key}': {result} palindroma");
        }
    }

    public void SetError(string key)
    {
        lock (_lock)
        {
            _cache.Remove(key);
            Monitor.PulseAll(_lock);
        }
    }
}