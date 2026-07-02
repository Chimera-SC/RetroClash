using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RetroClash.Logic;
using RetroClash.Protocol;
using RetroGames.Helpers;

namespace RetroClash.Core.Network
{
    public class Gateway
    {
        private static Semaphore _semaphore;
        private readonly Pool<byte[]> _bufferPool = new Pool<byte[]>(Configuration.MaxClients);
        private readonly Pool<SocketAsyncEventArgs> _eventPool = new Pool<SocketAsyncEventArgs>(Configuration.MaxClients * 2);
        private readonly Pool<UserToken> _tokenPool = new Pool<UserToken>(Configuration.MaxClients);
        private readonly ConcurrentDictionary<SocketAsyncEventArgs, TaskCompletionSource<bool>> _sendTasks = new ConcurrentDictionary<SocketAsyncEventArgs, TaskCompletionSource<bool>>();

        public int ConnectedSockets;

        public Gateway()
        {
            try
            {
                _semaphore = new Semaphore(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2);

                Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveBufferSize = Configuration.BufferSize,
                    SendBufferSize = Configuration.BufferSize,
                    Blocking = false,
                    NoDelay = true
                };

                for (var i = 0; i < Configuration.MaxClients + Configuration.OpsToPreAlloc; i++)
                {
                    var readWriteEventArgs = new SocketAsyncEventArgs();
                    readWriteEventArgs.Completed += OnIoCompleted;
                    _eventPool.Push(readWriteEventArgs);

                    _bufferPool.Push(new byte[Configuration.BufferSize]);
                    _tokenPool.Push(new UserToken());
                }
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }
        }

        public int TokenCount => _tokenPool.Count;
        public int BufferCount => _bufferPool.Count;
        public int EventCount => _eventPool.Count;

        public Socket Listener { get; set; }

        public SocketAsyncEventArgs GetArgs
        {
            get
            {
                var asyncEvent = _eventPool.Pop();

                if (asyncEvent != null) return asyncEvent;

                asyncEvent = new SocketAsyncEventArgs();
                asyncEvent.Completed += OnIoCompleted;

                return asyncEvent;
            }
        }

        public byte[] GetBuffer => _bufferPool.Pop() ?? new byte[Configuration.BufferSize];

        public UserToken GetToken => _tokenPool.Pop() ?? new UserToken();

        public async Task StartAsync()
        {
            try
            {
                Listener.Bind(new IPEndPoint(IPAddress.Any, Resources.Configuration.ServerPort));
                Listener.Listen(Configuration.MaxClients);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(
                    $"RetroClash is listening on {Utils.GetIp4Address()}:{((IPEndPoint) Listener.LocalEndPoint).Port}. Let's play Clash of Clans!");
                Console.ResetColor();

                await StartAccept();
            }
            catch (Exception)
            {
                Logger.Log("Port is already in use. Check if another instance of this application is running.");
                Environment.Exit(0);
            }
        }

        public async Task StartAccept()
        {
            var acceptEvent = GetArgs;
            while (true)
                if (!Listener.AcceptAsync(acceptEvent))
                    await ProcessAccept(acceptEvent, false);
                else
                    break;
        }

        public async Task ProcessAccept(SocketAsyncEventArgs asyncEvent, bool startNew)
        {
            var socket = asyncEvent.AcceptSocket;

            if (asyncEvent.SocketError == SocketError.Success)
                try
                {
                    var buffer = GetBuffer;
                    var readEvent = GetArgs;

                    readEvent.SetBuffer(buffer, 0, buffer.Length);
                    readEvent.AcceptSocket = socket;

                    var device = new Device(GetToken);
                    device.UserToken.Set(readEvent, device);

                    Interlocked.Increment(ref ConnectedSockets);

                    await StartReceive(readEvent);
                }
                catch (Exception)
                {
                    Disconnect(asyncEvent);
                }
            else
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception exception)
                {
                    Logger.Log(exception, Enums.LogType.Error);
                }

            asyncEvent.AcceptSocket = null;
            _eventPool.Push(asyncEvent);

            if (startNew)
                await StartAccept();
        }

        public async Task ProcessReceive(SocketAsyncEventArgs asyncEvent, bool startNew)
        {
            if (asyncEvent.BytesTransferred == 0 || asyncEvent.SocketError != SocketError.Success)
            {
                Disconnect(asyncEvent);
                Recycle(asyncEvent);
            }
            else
            {
                var token = asyncEvent.UserToken as UserToken;
                if (token == null || token.EventArgs == null || token.Stream == null || token.Socket == null)
                {
                    Disconnect(asyncEvent);
                    Recycle(asyncEvent);
                    return;
                }

                await token.SetData();

                try
                {
                    if (token.Socket.Available == 0 && token.Device != null)
                        await token.Device.HandleMessage(token.Stream.ToArray());
                }
                catch (Exception exception)
                {
                    Disconnect(asyncEvent);
                    Logger.Log(exception, Enums.LogType.Error);
                }

                if (startNew)
                    await StartReceive(asyncEvent);
            }
        }

        public async Task StartReceive(SocketAsyncEventArgs asyncEvent)
        {
            try
            {
                while (true)
                    if (!((UserToken) asyncEvent.UserToken).Socket.ReceiveAsync(asyncEvent))
                        await ProcessReceive(asyncEvent, false);
                    else
                        break;
            }
            catch (ObjectDisposedException)
            {
                Recycle(asyncEvent);
            }
            catch (Exception)
            {
                Disconnect(asyncEvent);
            }
        }

        public async void Disconnect(SocketAsyncEventArgs asyncEvent)
        {
            if (asyncEvent == null) return;
            try
            {
                if (ConnectedSockets > 0)
                    Interlocked.Decrement(ref ConnectedSockets);

                var token = (UserToken) asyncEvent.UserToken;


                var player = token.Device?.Player;

                if (player != null)
                    await Resources.PlayerCache.RemovePlayer(player.AccountId, Guid.Empty, true);

                token.ResetFull();
                _tokenPool.Push(token);

                Logger.Log("Client disconnected.", Enums.LogType.Debug);
            }
            catch (ObjectDisposedException)
            {
                Recycle(asyncEvent);
            }
            catch (Exception exception)
            {
                Logger.Log("Error when disconnecting a client. " + exception, Enums.LogType.Error);
            }
        }

        public async Task Send(PiranhaMessage message)
        {
            var token = message.Device.UserToken;

            try
            {
                await message.Encode();
                message.Encrypt();
                Logger.Log($"Message {message.Id} ({message.GetType().Name}) sent.", Enums.LogType.Debug);
                var buffer = await message.BuildPacket();

                token.SendQueue.Enqueue(buffer);
                await TryStartSend(token);

                message.Dispose();
            }
            catch (Exception exception)
            {
                Disconnect(message.Device.UserToken.EventArgs);
                Logger.Log(exception, Enums.LogType.Error);
            }
        }

        private async Task TryStartSend(UserToken token)
        {
            if (Interlocked.CompareExchange(ref token.IsSending, 1, 0) != 0)
                return;

            try
            {
                while (token.SendQueue.TryDequeue(out var buffer))
                {
                    var asyncEvent = GetArgs;

                    asyncEvent.SetBuffer(buffer, 0, buffer.Length);

                    var sock = token.Socket;
                    if (sock == null)
                    {
                        Recycle(asyncEvent);
                        continue;
                    }

                    try
                    {
                        asyncEvent.AcceptSocket = sock;
                        try
                        {
                            asyncEvent.RemoteEndPoint = sock.RemoteEndPoint;
                        }
                        catch (ObjectDisposedException)
                        {
                            Recycle(asyncEvent);
                            continue;
                        }

                        asyncEvent.UserToken = token;

                        await StartSend(asyncEvent);
                    }
                    catch (ObjectDisposedException)
                    {
                        Recycle(asyncEvent);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex, Enums.LogType.Error);
                        Recycle(asyncEvent);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref token.IsSending, 0);
                if (!token.SendQueue.IsEmpty)
                    await TryStartSend(token);
            }
        }

        public async Task StartSend(SocketAsyncEventArgs asyncEvent)
        {
            var token = (UserToken) asyncEvent.UserToken;
            var socket = token?.Socket;

            if (socket == null)
            {
                Recycle(asyncEvent);
                return;
            }

            var completionSource = _sendTasks.GetOrAdd(asyncEvent,
                _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));

            try
            {
                if (!socket.SendAsync(asyncEvent))
                    await ProcessSend(asyncEvent);

                await completionSource.Task;
            }
            catch (NullReferenceException exception)
            {
                if (_sendTasks.TryRemove(asyncEvent, out var source))
                    source.TrySetException(exception);

                Recycle(asyncEvent);
            }
            catch (ObjectDisposedException exception)
            {
                if (_sendTasks.TryRemove(asyncEvent, out var source))
                    source.TrySetException(exception);

                Recycle(asyncEvent);
            }
            catch (Exception exception)
            {
                if (_sendTasks.TryRemove(asyncEvent, out var source))
                    source.TrySetException(exception);

                Disconnect(asyncEvent);
                Logger.Log(exception, Enums.LogType.Error);
            }
        }

        public async Task ProcessSend(SocketAsyncEventArgs asyncEvent)
        {
            var transferred = asyncEvent.BytesTransferred;
            if (transferred == 0 || asyncEvent.SocketError != SocketError.Success)
            {
                if (_sendTasks.TryRemove(asyncEvent, out var completionSource))
                    completionSource.TrySetException(new SocketException((int)asyncEvent.SocketError));

                Disconnect(asyncEvent);
                Recycle(asyncEvent);
            }
            else
            {
                try
                {
                    var count = asyncEvent.Count;
                    if (transferred < count && asyncEvent.UserToken != null)
                    {
                        asyncEvent.SetBuffer(transferred, count - transferred);

                        await StartSend(asyncEvent);
                    }
                    else
                    {
                        if (_sendTasks.TryRemove(asyncEvent, out var completionSource))
                            completionSource.TrySetResult(true);

                        Recycle(asyncEvent);
                    }
                }
                catch (Exception exception)
                {
                    if (_sendTasks.TryRemove(asyncEvent, out var completionSource))
                        completionSource.TrySetException(exception);

                    Disconnect(asyncEvent);
                    Logger.Log(exception, Enums.LogType.Error);
                }
            }
        }


        public async void OnIoCompleted(object sender, SocketAsyncEventArgs asyncEvent)
        {
            var success = false;

            try
            {
                success = _semaphore.WaitOne(5000);

                if (!success) return;
                if (asyncEvent.SocketError == SocketError.Success)
                    switch (asyncEvent.LastOperation)
                    {
                        case SocketAsyncOperation.Accept:
                            await ProcessAccept(asyncEvent, true);
                            break;
                        case SocketAsyncOperation.Receive:
                            await ProcessReceive(asyncEvent, true);
                            break;
                        case SocketAsyncOperation.Send:
                            await ProcessSend(asyncEvent);
                            break;
                        default:
                            throw new ArgumentException(
                                "The last operation completed on the socket was not a receive, send or accept");
                    }
                else
                    await ProcessAccept(GetArgs, true);
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }
            finally
            {
                if (success)
                    _semaphore.Release();
            }
        }

        public void Recycle(SocketAsyncEventArgs asyncEvent)
        {
            try
            {
                if (asyncEvent == null)
                    return;

                Recycle(asyncEvent.Buffer);

                asyncEvent.UserToken = null;
                asyncEvent.SetBuffer(null, 0, 0);
                asyncEvent.AcceptSocket = null;

                _eventPool.Push(asyncEvent);
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }
        }

        public void DissolveSocket(Socket socket)
        {
            if (socket == null) return;

            try
            {
                socket.Disconnect(false);
            }
            catch (ObjectDisposedException)
            {
                // 
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }

            try
            {
                socket.Close(4);
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }

            try
            {
                socket.Dispose();
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }
        }

        public void Recycle(byte[] buffer)
        {
            if (buffer?.Length == Configuration.BufferSize)
                _bufferPool.Push(buffer);
        }
    }
}