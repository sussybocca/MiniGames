using System.Reflection;

namespace MiniGames.Services;

public static class TrippyExecutor
{
    public static string? Execute(Assembly assembly, string typeName, string methodName, object[]? parameters = null)
    {
        var type = assembly.GetType(typeName);
        if (type == null) return $"Type '{typeName}' not found.";

        var method = type.GetMethod(methodName);
        if (method == null) return $"Method '{methodName}' not found.";

        try
        {
            var instance = Activator.CreateInstance(type);
            var result = method.Invoke(instance, parameters);
            return result?.ToString() ?? "Executed successfully (no return value).";
        }
        catch (Exception ex)
        {
            return $"Runtime Error: {ex.InnerException?.Message ?? ex.Message}";
        }
    }
}