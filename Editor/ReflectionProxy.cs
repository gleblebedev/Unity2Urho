using System.Reflection;

namespace UnityToCustomEngineExporter.Editor
{
    public class ReflectionProxy
    {
        private readonly object _instance;

        public ReflectionProxy(object instance)
        {
            _instance = instance;
        }

        public T GetValue<T>(string name)
        {
            if (_instance == null)
            {
                return default(T);
            }
            var property = _instance.GetType().GetProperty(name);
            if (property != null)
            {
                var res = property.GetValue(_instance);
                if (res is T res1)
                {
                    return res1;
                }
            }

            var f = _instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null)
            {
                var res = f.GetValue(_instance);
                if (res is T res1)
                {
                    return res1;
                }
            }
            return default(T);
        }
    }
}