using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomWar.Core
{
    /// <summary>
    /// Simple service registry. Used as a bridge between current singleton pattern
    /// and future DI container (VContainer). Register services at startup,
    /// resolve them via TryGet&lt;T&gt;().
    /// </summary>
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            Type type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Overwriting existing service: {type.Name}");
            }
            _services[type] = service;
        }

        public static T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return service as T;
            }
            Debug.LogError($"[ServiceLocator] Service not registered: {typeof(T).Name}");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object obj) && obj is T result)
            {
                service = result;
                return true;
            }
            service = null;
            return false;
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}
