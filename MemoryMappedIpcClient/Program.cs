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
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcClient {
    class Program {
        private static void Main(string[] args) {
            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "wii_welcomer_pipe",
                PipeDirection.InOut, PipeOptions.None,
                TokenImpersonationLevel.Impersonation);
            Console.WriteLine("Connecting to server...\n");
            pipeClient.Connect();

            BinaryReader sr = new BinaryReader(pipeClient);
            string id = sr.ReadString();
            int lineSize = sr.ReadInt32();
            int totalBufferSizeInLines = sr.ReadInt32();

            SharedMemoryAccessor sharedMemoryAccessor = new SharedMemoryAccessor(
                clientId: id, 
                isServer: false, 
                lineSize: lineSize, 
                totalBufferSizeInLines: totalBufferSizeInLines);

            Console.WriteLine("received from server: " + id + " " + lineSize + " " + totalBufferSizeInLines);

            Console.WriteLine("Connected\n");

            //Console.WriteLine(sharedMemoryAccessor.ReadLine());
            //Console.WriteLine(sharedMemoryAccessor.ReadLine());

            //while (true) {
            //    Thread.Sleep(2000);
            //    Console.WriteLine(sharedMemoryAccessor.ReadLine());
            //}


            while (true) {
                bool first = true;
                foreach (MotionMessage i in sharedMemoryAccessor.GetAvailableLinesToClient()) {
                    if (first) {
                        Console.WriteLine("first");
                        first = false;
                    }
                    Console.WriteLine("read this: " + i.IsGyro + " " + i.Milliseconds + " " + i.X + " " + i.Y + " " + i.Z);
                }
                //Console.WriteLine("hit enter");
                //Console.ReadLine();
            }



            //using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("wii_" + id)) {

            //    using (MemoryMappedViewStream mmStream = mmf.CreateViewStream()) {
            //        BinaryReader reader = new BinaryReader(mmStream);
            //        reader.BaseStream.Seek(0, SeekOrigin.Begin);
            //        Console.WriteLine(reader.ReadInt32());
            //    }

            //    using (MemoryMappedViewStream mmStream = mmf.CreateViewStream(4, 0)) {
            //        BinaryReader reader = new BinaryReader(mmStream);
            //        while (true) {
            //            Thread.Sleep(2000);

            //            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            //            Console.WriteLine(reader.ReadInt32());
            //        }
            //    }
            //    //Mutex mutex = Mutex.OpenExisting("testmapmutex");
            //    //mutex.WaitOne();

            //    //using (MemoryMappedViewStream stream = mmf.CreateViewStream(1, 0)) {
            //    //    BinaryWriter writer = new BinaryWriter(stream);
            //    //    writer.Write(1);
            //    //}
            //    //mutex.ReleaseMutex();
            //}



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
