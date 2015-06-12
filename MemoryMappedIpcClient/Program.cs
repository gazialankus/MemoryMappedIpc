using System;
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcClient {
    class Program {
        private static void Main(string[] args) {

            ConnectionToServer connectionToServer = new ConnectionToServer();


            //NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "wii_welcomer_pipe",
            //    PipeDirection.InOut, PipeOptions.None,
            //    TokenImpersonationLevel.Impersonation);
            //Console.WriteLine("Connecting to server...\n");
            //pipeClient.Connect();

            //BinaryReader sr = new BinaryReader(pipeClient);
            //string id = sr.ReadString();
            //int lineSize = sr.ReadInt32();
            //int totalBufferSizeInLines = sr.ReadInt32();

            //SharedMemoryAccessor sharedMemoryAccessor = new SharedMemoryAccessor(
            //    clientId: id, 
            //    isServer: false, 
            //    lineSize: lineSize, 
            //    totalBufferSizeInLines: totalBufferSizeInLines);

            //Console.WriteLine("received from server: " + id + " " + lineSize + " " + totalBufferSizeInLines);

            //Console.WriteLine("Connected\n");

            //Console.WriteLine(sharedMemoryAccessor.ReadLine());
            //Console.WriteLine(sharedMemoryAccessor.ReadLine());

            //while (true) {
            //    Thread.Sleep(2000);
            //    Console.WriteLine(sharedMemoryAccessor.ReadLine());
            //}

            bool calibrating = false;
            for(int ii = 0; ii < 1000; ) {
//            while(true) { 
                bool first = true;
                foreach (MotionMessage i in connectionToServer.GetAvailableLines()) {
                    if (first) {
                        Console.WriteLine("first");
                        first = false;
                    }
                    Console.WriteLine("read this: " + i.Wid + " " + i.IsGyro + " " + i.Milliseconds + " " + i.X + " " + i.Y + " " + i.Z);
                }
                //Console.WriteLine("hit enter");
                //Console.ReadLine();
                if (Console.KeyAvailable) {
                    if (calibrating) {
                        if (Console.ReadKey().Key == ConsoleKey.Spacebar) {
                            calibrating = false;
                            connectionToServer.StopGyroCalibration(0);
                            Console.WriteLine("stopped calibrating");
                        }
                    } else {
                        if (Console.ReadKey().Key == ConsoleKey.Enter) {
                            calibrating = true;
                            connectionToServer.StartGyroCalibration(0);
                            Console.WriteLine("started calibrating");
                        } 
                    }
                }
            }
            // this doesn't break it either. 

            connectionToServer.Dispose();


        }

    }
}
