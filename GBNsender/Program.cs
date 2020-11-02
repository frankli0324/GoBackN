using System;
using System.Threading.Tasks;

namespace GBNsender {
    class Program {
        static async Task Main (string[] args) {
            var ctl = new Control ("14:7d:da:6a:44:78", "ac:de:48:00:11:22");
            await ctl.Send ("aoisdjfa");
            Console.ReadLine ();
        }
    }
}
