using System.Text;
using System.Net.Sockets;
using System;
using System.Net;
using System.Threading.Tasks;
using GBN.IO;
using GBN.Types;
using System.IO;

namespace GBNrecver {
    class Program {
        static int current_ack = -1;
        static async Task Main (string[] args) {
            byte[] buffer = new byte[4];
            TcpListener listener = new TcpListener (IPAddress.Loopback, 1234);
            listener.Start ();
            var virtual_addr = new Address ("ac:de:48:00:11:22");
            var client = await listener.AcceptTcpClientAsync ();
            using (var stream = client.GetStream ()) {
                await stream.ReadAsync (buffer, 0, 4);
                int id = BitConverter.ToInt32 (buffer);
                try {
                    var frm = await stream.ReadFrameAsync (virtual_addr);
                    Console.WriteLine (frm);
                    if (id == current_ack + 1)
                        current_ack++;
                } catch (InvalidDataException e) {
                    Console.WriteLine (e);
                }
                await stream.WriteAsync (BitConverter.GetBytes (current_ack));
            }
        }
    }
}
