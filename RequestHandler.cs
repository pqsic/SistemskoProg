using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

public class RequestHandler
{
    private readonly CacheManager _cache;
    private readonly string _rootFolder;

    public RequestHandler(CacheManager cache, string rootFolder)
    {
        _cache = cache;
        _rootFolder = rootFolder;
    }

    public void Handle(HttpListenerContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // POKRENI NA POCETKU

        string fileName = context.Request.Url?.AbsolutePath.TrimStart('/');

        if (string.IsNullOrWhiteSpace(fileName))
        {
            SendResponse(context, 400, "Naziv fajla nije naveden.");
        }
        else
        {
            try
            {
                if (_cache.TryGet(fileName, out int cachedResult))
                {
                    string cachedMsg = cachedResult == 0
                        ? "Nema palindroma u fajlu."
                        : $"Broj palindroma (iz cache-a): {cachedResult}";
                    SendResponse(context, 200, cachedMsg);
                }
                else
                {
                    string[] files = Directory.GetFiles(_rootFolder, fileName, SearchOption.AllDirectories);

                    if (files.Length == 0)
                    {
                        _cache.SetError(fileName);
                        SendResponse(context, 404, $"Fajl '{fileName}' nije pronađen.");
                    }
                    else
                    {
                        int count = CountPalindromes(files[0]);
                        _cache.Set(fileName, count);
                        string message = count == 0
                            ? "Nema palindroma u fajlu."
                            : $"Broj palindroma: {count}";
                        SendResponse(context, 200, message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Greška pri obradi '{fileName}': {ex.Message}");
                _cache.SetError(fileName);
                SendResponse(context, 500, "Interna greška servera.");
            }
        }

        stopwatch.Stop();
        Logger.LogInfo($"Vreme obrade zahteva '{fileName}': {stopwatch.ElapsedMilliseconds}ms");
    }

    private int CountPalindromes(string filePath)
    {
        int count = 0;
        string text = File.ReadAllText(filePath);
        string[] words = text.Split(new char[] { ' ', '\n', '\r', '\t', ',', '.', '!', '?', ';', ':' },
                                     StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
        {
            string cleaned = word.ToLower();
            if (cleaned.Length > 1 && IsPalindrome(cleaned))
            {
                count++;
                Logger.LogInfo($"Palindrom pronađen: '{cleaned}'");
            }
        }

        return count;
    }

    private bool IsPalindrome(string word)
    {
        int left = 0;
        int right = word.Length - 1;

        while (left < right)
        {
            if (word[left] != word[right])
                return false;
            left++;
            right--;
        }

        return true;
    }

    private void SendResponse(HttpListenerContext context, int statusCode, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
        Logger.LogInfo($"Odgovor poslat: {statusCode} - {message}");
    }
}