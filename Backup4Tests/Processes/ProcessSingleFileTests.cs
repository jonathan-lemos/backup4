using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Backup4.Processes;
using NUnit.Framework;

namespace Backup4Tests.Processes
{
    public class ProcessSingleFileTests
    {
        [Test]
        public async Task ProcessSingleFileTest()
        {
            var input = Enumerable.Range(0, 1_299_709).Select(x => (byte) x).ToArray();
            var expected = input.ToArray();
            var password = "abrakadabra";
            
            var opStream = new MemoryStream();
            
            using var tmp1 = new TempFile(input);
            
            await SingleFile.Process(new FileStream(tmp1, FileMode.Open), opStream, password);
            var resEnc = opStream.ToArray();

            using var tmp2 = new TempFile(resEnc);
            
            var decStream = new MemoryStream();
            await SingleFile.Deprocess(new FileStream( tmp2, FileMode.Open), decStream, password);

            var res = decStream.ToArray();
            
            Assert.AreEqual(expected, res);
        }
    }
}