using System;

namespace fbognini.WebFramework.Logging
{
    public enum RequestLoggingAttributeMode
    {
        Ignore = 0,
        Force = 1
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class IgnoreRequestLoggingAttribute : Attribute
    {
        private RequestLoggingAttributeMode mode = RequestLoggingAttributeMode.Ignore;

        /// <summary>
        /// no logt all
        /// </summary>
        public IgnoreRequestLoggingAttribute()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        public IgnoreRequestLoggingAttribute(RequestLoggingAttributeMode mode)
        {
            this.mode = mode;
        }

        /// <summary>
        /// log but no request (or response) column
        /// </summary>
        /// <param name="ignoreRequest"></param>
        /// <param name="ignoreResponse"></param>
        public IgnoreRequestLoggingAttribute(bool ignoreRequest, bool ignoreResponse)
        {
            mode = RequestLoggingAttributeMode.Force;
            IgnoreRequestLogging = ignoreRequest;
            IgnoreResponseLogging = ignoreResponse;
        }

        public bool IgnoreLogging => mode == RequestLoggingAttributeMode.Ignore;
        public bool IgnoreRequestLogging { get; private set; }
        public bool IgnoreResponseLogging { get; private set; }
    }
}
