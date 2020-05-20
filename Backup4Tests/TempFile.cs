using System;
using System.IO;

namespace Backup4Tests
{
    public class TempFile : IDisposable
    {
        public string Filename { get; }
        
        public TempFile(byte[] content)
        {
            Filename = Path.GetTempPath() + Guid.NewGuid() + ".tmp";
            File.WriteAllBytes(Filename, content);
        }

        public static implicit operator string(TempFile tmp) => tmp.Filename;

        public void Dispose()
        {
            try
            {
                File.Delete(Filename);
            }
            catch (FileNotFoundException)
            {
            }
        }
    }
}