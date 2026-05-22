using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void RegisterService<T>(T service)
    {
        var type = typeof(T);
        if (service == null)
            throw new ArgumentNullException(nameof(service), "Cannot register null service.");

        lock (_services)
        {
            if (_services.ContainsKey(type))
            {
                _services[type] = service;
                return;
            }
            _services.Add(type, service);
        }
    }

    public static T GetService<T>()
    {
        var type = typeof(T);
        lock (_services)
        {
            if (_services.TryGetValue(type, out var obj))
            {
                return (T)obj;
            }
        }
        throw new InvalidOperationException($"Service of type {type.FullName} is not registered in ServiceLocator.");
    }

    public static void UnregisterService<T>()
    {
        var type = typeof(T);
        lock (_services)
        {
            if (_services.ContainsKey(type))
                _services.Remove(type);
        }
    }

    // Optional helper: check without throwing
    public static bool HasService<T>()
    {
        var type = typeof(T);
        lock (_services)
        {
            return _services.ContainsKey(type);
        }
    }
}
