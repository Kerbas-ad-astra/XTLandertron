using System;

namespace Landertron
{
    class Logger
    {
        public string prefix
        {
            get;
            set;
        }

        public Logger(string prefix = "")
        {
            this.prefix = prefix;
        }

        public void debug(object message, UnityEngine.Object context = null)
        {
            #if DEBUG
            UnityEngine.Debug.Log(prefix + message, context);
            #endif
        }

        public void info(object message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.Log(prefix + message, context);
        }

        public void warning(object message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogWarning(prefix + message, context);
        }

        public void error(object message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogError(prefix + message, context);
        }
    }
}
