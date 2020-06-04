using System;
using Serilog;
using Serilog.Events;

namespace Backup4
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information,
                    standardErrorFromLevel: LogEventLevel.Verbose)
                .CreateLogger();

            Console.WriteLine("Hello World!");
        }
    }
}