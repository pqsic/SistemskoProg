using System;

public class CacheEntry
{
    public int PalindromeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsReady { get; set; } = false;
}