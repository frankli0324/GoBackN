using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GBN.IO;
using GBN.Types;

namespace GBNsender {
    class RollbackException : Exception {
        int broken_frame;
        public RollbackException (int b) { broken_frame = b; }
    }
    class Control {
        int window_now, window_begin, window_size;
        string src, dst;
        TcpClient client;
        Queue<(int, CancellationTokenSource)> tokenSources
            = new Queue<(int, CancellationTokenSource)> ();
        CancellationTokenSource windowFullTokenSource;

        public Control (string src, string dst, string ip = "127.0.0.1", int port = 1234, int window_size = 4) {
            this.src = src;
            this.dst = dst;
            this.client = new TcpClient (ip, port);
            this.window_size = window_size;
        }
        async Task SendFrame (Stream stream, int id, Frame frm) {
            byte[] ack_buf = new byte[4];
            await stream.WriteAsync (BitConverter.GetBytes (id), 0, 4);
            await stream.WriteFrameAsync (frm);
            var tokenSource = new CancellationTokenSource ();
            tokenSources.Enqueue ((id, tokenSource));
            var token = tokenSource.Token;
            // presume T_transport = 1000
            if (!stream.ReadAsync (ack_buf, 0, 4).Wait (2000, token)) {
                if (!token.IsCancellationRequested)
                    await SendFrame (stream, id, frm);
                // successfully delivered if cancellation requested
                return;
            }
            int ack_id = BitConverter.ToInt32 (ack_buf);

            if (ack_id < id)  // frame not properly received
                throw new RollbackException (ack_id + 1);

            // no need to wait for prior acks
            while (tokenSources.Peek ().Item1 < id)
                tokenSources.Dequeue ().Item2.Cancel ();

            // what if larger ack arrived first
            if (window_begin < id + 1)
                window_begin = id + 1;
            if (windowFullTokenSource != null &&
                !windowFullTokenSource.IsCancellationRequested)
                windowFullTokenSource.Cancel ();
        }
        public async Task Send (string data) {
            var builder = new FrameBuilder (src, dst);
            using (var stream = client.GetStream ()) {
                foreach (var frm in builder.GetFrames (Encoding.UTF8.GetBytes (data))) {
                    if (window_now > window_begin + window_size) {
                        windowFullTokenSource = new CancellationTokenSource ();
                        await Task.Delay (-1, windowFullTokenSource.Token);
                    }
                    var send_task = SendFrame (stream, window_now++, frm);
                }
            }
        }
    }
}