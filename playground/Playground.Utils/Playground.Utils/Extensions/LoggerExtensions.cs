using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Playground.Utils.Extensions
{
    public static class LoggerExtensions
    {
        public static void LogErrorWithCode(this ILogger logger, string code, string message, params object[] args)
        {
            //
        }
        public static void LogErrorWithCode(this ILogger logger, string code, Exception ex, string message, params object[] args)
        {
            //
        }
    }
}
