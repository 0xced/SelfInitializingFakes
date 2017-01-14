﻿namespace SelfInitializingFakes.Infrastructure
{
    using System;
#if FRAMEWORK_EXCEPTION_DISPATCH_INFO
    using System.Runtime.ExceptionServices;
#else
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
#endif

    /// <summary>
    /// Extension methods for exceptions.
    /// </summary>
    internal static class ExceptionExtensions
    {
#if !FRAMEWORK_EXCEPTION_DISPATCH_INFO
        private static readonly Action<Exception> PreserveStackTrace = CreatePreserveStackTrace();
#endif

        /// <summary>
        /// Re-throws an exception, trying to preserve its stack trace.
        /// </summary>
        /// <param name="exception">The exception to rethrow.</param>
#if FRAMEWORK_EXCEPTION_DISPATCH_INFO
        public static void Rethrow(this Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
#else
        public static void Rethrow(this Exception exception)
        {
            try
            {
                PreserveStackTrace(exception);
            }
            catch
            {
            }

            throw exception;
        }

        private static Action<Exception> CreatePreserveStackTrace()
        {
            var method = typeof(Exception).GetMethod(
                "InternalPreserveStackTrace",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (Action<Exception>)Delegate.CreateDelegate(typeof(Action<Exception>), null, method);
        }
#endif
    }
}
