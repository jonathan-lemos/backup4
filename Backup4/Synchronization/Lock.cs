using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Backup4.Synchronization
{
    public class Lock : IDisposable
    {
        private readonly object _obj;
        public readonly int ThreadId;

        public static IDictionary<object, int> ThreadIds = new Dictionary<object, int>();
        
        public Lock(object lockObj)
        {
            _obj = lockObj;
            System.Diagnostics.Debug.WriteLine(DateTime.Now + ": " + string.Join(", ", ThreadIds.Select(x => x.ToString())));
            Monitor.Enter(_obj);
            ThreadIds[_obj] = Thread.CurrentThread.ManagedThreadId;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public void Dispose()
        {
            ThreadIds.Remove(_obj);
            Monitor.Exit(_obj);
        }
    }
}