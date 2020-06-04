using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace Backup4.Synchronization
{
    public static class Pipe
    {
        public static async Task Connect(Action<Stream> inputFunc, Action<Stream> outputFunc, int capacity,
            params Action<Stream, Stream>[] funcs)
        {
            if (funcs.Length == 0)
            {
                throw new ArgumentException("Must connect input and output with at least one function.");
            }

            var servers = Enumerable.Range(0, funcs.Length + 1)
                .Select(x => new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, capacity))
                .Select(x => (WriteStream: x, ReadStream: new AnonymousPipeClientStream(x.GetClientHandleAsString())))
                .ToArray();

            var pairs = new List<(Stream Read, Stream Write)> {(servers[0].ReadStream, servers[1].WriteStream)};

            for (var i = 1; i < servers.Length - 1; ++i)
            {
                pairs.Add((servers[i].ReadStream, servers[i + 1].WriteStream));
            }

            Debug.Assert(pairs.Count == funcs.Length);

            var taskList = funcs.Zip(pairs).Select(x =>
                {
                    var (func, pair) = x;
                    var (read, write) = pair;

                    return Task.Run(() =>
                    {
                        try
                        {
                            func(read, write);
                        }
                        finally
                        {
                            read.Dispose();
                            write.Dispose();
                        }
                    });
                })
                .Concat(new[]
                {
                    Task.Run(() => {
                        try
                        {
                            inputFunc(servers[0].WriteStream);
                        }
                        finally
                        {
                            servers[0].WriteStream.Dispose();
                        }
                    }),
                    Task.Run(() =>
                    {
                        try
                        {
                            outputFunc(servers.Last().ReadStream);
                        }
                        finally
                        {
                            servers.Last().ReadStream.Dispose();
                        }
                    })
                });


            await Task.WhenAll(taskList);
        }

        public static async Task Connect(Stream input, Stream output, int capacity,
            params Action<Stream, Stream>[] funcs)
        {
            if (funcs.Length == 0)
            {
                throw new ArgumentException("Must connect input and output with at least one function.");
            }

            var servers = Enumerable.Range(0, funcs.Length - 1)
                .Select(x => new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, capacity))
                .Select(x => (WriteStream: x, ReadStream: new AnonymousPipeClientStream(x.GetClientHandleAsString())))
                .ToArray();

            var pairs = new List<(Stream Read, Stream Write)> {(input, servers[0].WriteStream)};

            for (var i = 0; i < servers.Length - 1; ++i)
            {
                pairs.Add((servers[i].ReadStream, servers[i + 1].WriteStream));
            }

            pairs.Add((servers.Last().ReadStream, output));

            Debug.Assert(pairs.Count == funcs.Length);

            var taskList = funcs.Zip(pairs).Select(x =>
            {
                var (func, pair) = x;
                var (read, write) = pair;

                return Task.Run(() =>
                {
                    try
                    {
                        func(read, write);
                    }
                    finally
                    {
                        read.Dispose();
                        write.Dispose();
                    }
                });
            });

            await Task.WhenAll(taskList);
        }
    }
}