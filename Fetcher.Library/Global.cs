global using System.Text.Json;

public static class Global
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
}