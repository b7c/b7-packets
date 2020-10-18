using System;
using System.Reflection;

namespace TanjiWPF
{
    public class DataInterceptorException : Exception
    {
        public object Interceptor { get; }
        public MethodInfo Method { get; }

        public DataInterceptorException(string message, Exception innerException, object interceptor, MethodInfo method)
            : base(message, innerException)
        {
            Interceptor = interceptor;
            Method = method;
        }
    }
}
