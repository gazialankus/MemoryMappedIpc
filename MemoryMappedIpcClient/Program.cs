using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryMappedIpcClient {
    class Program {
        private static void Main(string[] args) {
            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "wii_welcomer_pipe",
                PipeDirection.InOut, PipeOptions.None,
                TokenImpersonationLevel.Impersonation);
            Console.WriteLine("Connecting to server...\n");
            pipeClient.Connect();
            Console.WriteLine("Connected\n");
            Console.ReadLine();


        }

        static void Main_old(string[] args) {
            try {
                using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("testmap")) {

                    Mutex mutex = Mutex.OpenExisting("testmapmutex");
                    mutex.WaitOne();

                    using (MemoryMappedViewStream stream = mmf.CreateViewStream(1, 0)) {
                        BinaryWriter writer = new BinaryWriter(stream);
                        writer.Write(1);
                    }
                    mutex.ReleaseMutex();
                }
            } catch (FileNotFoundException) {
                Console.WriteLine("Memory-mapped file does not exist. Run Process A first.");
            }
            Console.ReadLine();
        }
    }
}
