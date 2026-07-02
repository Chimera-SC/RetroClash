using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RetroClash.Logic;

namespace RetroClash.Core.Network
{
    public class UserToken : IDisposable
    {
        public SocketAsyncEventArgs EventArgs { get; set; }
        public Device Device { get; set; }
        public MemoryStream Stream { get; set; }
        public Socket Socket { get; set; }
        public ConcurrentQueue<byte[]> SendQueue { get; } = new ConcurrentQueue<byte[]>();
        public int IsSending;

        public void Dispose()
        {
            Device?.Dispose();
            Stream?.Dispose();

            Device = null;
            Stream = null;
        }

        public void ResetFull()
        {
            Device?.Dispose();
            Stream?.Dispose();

            Device = null;
            Stream = null;
            EventArgs = null;
            Socket = null;

            while (SendQueue.TryDequeue(out _)) { }

            IsSending = 0;
        }

        public void Set(SocketAsyncEventArgs args, Device device)
        {
            Device = device;
            Device.UserToken = this;

            EventArgs = args;
            EventArgs.UserToken = this;

            Socket = EventArgs.AcceptSocket;

            Stream = new MemoryStream();
        }

        public async Task SetData()
        {
            if (Stream == null || EventArgs == null || EventArgs.Buffer == null || EventArgs.BytesTransferred <= 0)
                return;

            await Stream.WriteAsync(EventArgs.Buffer, 0, EventArgs.BytesTransferred);
        }

        public void ResetStream()
        {
            Stream.Position = 0;
            Stream.SetLength(0);
        }
    }
}