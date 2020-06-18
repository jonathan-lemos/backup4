using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Backup4.Processes;
using NUnit.Framework;
using Serilog;
using Serilog.Events;

namespace Backup4Tests.Processes
{
    public class TarTests
    {
        [SetUp]
        public void Setup()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information,
                    standardErrorFromLevel: LogEventLevel.Verbose)
                .CreateLogger();
        }

        [Test]
        public async Task MakeTest()
        {
            using var tempDir = new TempDir();
            using var contextHolder = TempContext.Create();
            var ctx = contextHolder.Object;

            var outputStream = new MemoryStream();

            var res = await Tar.Make(outputStream, ctx, new[] {tempDir.Path});

            var outputRes = outputStream.ToArray();

            using var zip = new ZipArchive(outputRes.ToStream(), ZipArchiveMode.Read);
            
            Assert.True(zip.Entries.Count == 4);
        }
    }
}