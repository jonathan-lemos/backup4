using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Backup4.Functional;

namespace Backup4.Synchronization
{
    public class Pipe
    {
        private Func<Stream, Task<Result<Exception>>>? _inputFunc;
        private Func<Stream, Task<Result<Exception>>>? _outputFunc;
        private readonly Func<Stream, Stream, Task>[] _transformations;

        public int ChunkLength { get; set; } = 16 * 1024;
        public int BufferSize { get; set; } = 1024 * 1024;

        public Pipe()
        {
            _transformations = new Func<Stream, Stream, Task>[0];
        }

        public Pipe(params Action<Stream, Stream>[] transformations)
        {
            _transformations = transformations
                .Select<Action<Stream, Stream>, Func<Stream, Stream, Task>>(
                    func => (i, o) => Task.Run(() => func(i, o)))
                .ToArray();
        }

        public Pipe(params Func<Stream, Stream, Task>[] transformations)
        {
            _transformations = transformations
                .Select<Func<Stream, Stream, Task>, Func<Stream, Stream, Task>>(func =>
                    (i, o) => Task.Run(async () => await func(i, o)))
                .ToArray();
        }

        private async Task<Result<AggregateException>> _executeNoTransformations()
        {
            if (_inputFunc == null || _outputFunc == null)
            {
                throw new InvalidOperationException(
                    "An input source and an output sink must both be set with SetInput/SetOutput respectively.");
            }

            var server = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, BufferSize);
            var client = new AnonymousPipeClientStream(server.GetClientHandleAsString());

            var inputTask = Task.Run(async () =>
            {
                try
                {
                    return await _inputFunc(server);
                }
                finally
                {
                    await server.DisposeAsync();
                }
            });

            var outputTask = Task.Run(async () =>
            {
                try
                {
                    return await _outputFunc(client);
                }
                finally
                {
                    await client.DisposeAsync();
                }
            });

            // server.DisposeLocalCopyOfClientHandle();

            return (await Task.WhenAll(inputTask, outputTask)).Combine();
        }

        public async Task<Result<AggregateException>> Execute()
        {
            if (_inputFunc == null || _outputFunc == null)
            {
                throw new InvalidOperationException(
                    "An input source and an output sink must both be set with SetInput/SetOutput respectively.");
            }

            if (_transformations.Length == 0)
            {
                return await _executeNoTransformations();
            }

            var servers = Enumerable.Range(0, _transformations.Length + 1)
                .Select(x => new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, BufferSize))
                .Select(x => (WriteStream: x, ReadStream: new AnonymousPipeClientStream(x.GetClientHandleAsString())))
                .ToArray();

            var pairs = new List<(Stream Read, Stream Write)> {(servers[0].ReadStream, servers[1].WriteStream)};

            for (var i = 1; i < servers.Length - 1; ++i)
            {
                pairs.Add((servers[i].ReadStream, servers[i + 1].WriteStream));
            }

            Debug.Assert(pairs.Count == _transformations.Length);

            var taskList = _transformations.Zip(pairs).Select(x =>
                {
                    var (func, pair) = x;
                    var (read, write) = pair;

                    return Task.Run(async () =>
                    {
                        try
                        {
                            await func(read, write);
                            return Result<Exception>.Success;
                        }
                        catch (Exception e)
                        {
                            return new Result<Exception>(e);
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
                    Task.Run(async () =>
                    {
                        try
                        {
                            return await _inputFunc(servers[0].WriteStream);
                        }
                        catch (Exception e)
                        {
                            return e;
                        }
                        finally
                        {
                            servers[0].WriteStream.Dispose();
                        }
                    }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            return await _outputFunc(servers.Last().ReadStream);
                        }
                        catch (Exception e)
                        {
                            return e;
                        }
                        finally
                        {
                            servers.Last().ReadStream.Dispose();
                        }
                    })
                });


            return (await Task.WhenAll(taskList)).Combine();
        }

        public void SetInput(Stream input)
        {
            _inputFunc = stream => Task.Run(() => Result.Of(() =>
            {
                var buf = new byte[ChunkLength];
                int len;
                while ((len = input.Read(buf, 0, buf.Length)) > 0)
                {
                    stream.Write(buf, 0, len);
                }
            }));
        }

        public void SetInput(Action<Stream> func)
        {
            _inputFunc = stream => Task.Run(() => Result.Of(() => func(stream)));
        }

        public void SetInput(Func<Stream, Task> func)
        {
            _inputFunc = stream => Task.Run(async () =>
            {
                try
                {
                    await func(stream);
                    return Result<Exception>.Success;
                }
                catch (Exception e)
                {
                    return e;
                }
            });
        }

        public void SetInput(Func<Stream, Task<Result<Exception>>> func)
        {
            _inputFunc = stream => Task.Run(() => func(stream));
        }

        public void SetOutput(Stream output)
        {
            _outputFunc = stream => Task.Run(() => Result.Of(() =>
            {
                var buf = new byte[ChunkLength];
                int len;
                while ((len = stream.Read(buf, 0, buf.Length)) > 0)
                {
                    output.Write(buf, 0, len);
                }
            }));
        }

        public void SetOutput(Action<Stream> func)
        {
            _outputFunc = stream => Task.Run(() => Result.Of(() => func(stream)));
        }

        public void SetOutput(Func<Stream, Task> func)
        {
            _outputFunc = stream => Task.Run(async () =>
            {
                try
                {
                    await func(stream);
                    return Result<Exception>.Success;
                }
                catch (Exception e)
                {
                    return e;
                }
            });
        }

        public void SetOutput(Func<Stream, Task<Result<Exception>>> func)
        {
            _outputFunc = stream => Task.Run(() => func(stream));
        }
    }

    /*
    public static class Pipe
    {
        public static async Task Connect(Stream input, Action<Stream> outputFunc, int capacity, int chunkLen = 65536)
        {
            var server = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, capacity);
            var client = new AnonymousPipeClientStream(server.GetClientHandleAsString());

            var inputTask = Task.Run(() =>
            {
                try
                {
                    var len = 0;
                    var buf = new byte[chunkLen];
                    while ((len = input.Read(buf, 0, buf.Length)) > 0)
                    {
                        server.Write(buf, 0, len);
                    }
                }
                finally
                {
                    server.Dispose();
                }
            });

            var outputTask = Task.Run(() =>
            {
                try
                {
                    outputFunc(client);
                }
                finally
                {
                    client.Dispose();
                }
            });

            // server.DisposeLocalCopyOfClientHandle();

            await Task.WhenAll(inputTask, outputTask);
        }

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
                    Task.Run(() =>
                    {
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
    */
}