﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryMappedIpcServer {
    class Program {

        private static void ConnectionReceived(IAsyncResult result)
        {
            string id = result.AsyncState.ToString();
            Console.WriteLine("server received a connection " + id);

            pipeServer.EndWaitForConnection(result);

            pipeServer.Disconnect();

            pipeServer.BeginWaitForConnection(ConnectionReceived, id + ".");

            Console.ReadLine();
        }
        static NamedPipeServerStream pipeServer;

        static void PiperThread() {
            pipeServer = new NamedPipeServerStream("wii_welcomer_pipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            string nextId = "client1";
            pipeServer.BeginWaitForConnection(ConnectionReceived, nextId);
            while (true) ;

            // piper thread 
                // listen to the welcomer pipe
                    // when a connection comes in, 
                        // open another pipe with the client and create a listener record with it
                        // create a mutex and a shmem
                // check existing client pipes
                    // if one of them is not connected
                        // dismantle it 
        }

        static void DeviceThread() {
            // device thread
                // if there are listeners, grab the data (or grab all the time maybe)
                // do this in mutex
                    // add the data to each of the listeners 
        }

        private static void Main(string[] args) {
            Thread piperThread = new Thread(PiperThread);
            piperThread.Start();
        }

        static void Main_old(string[] args) {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("testmap", 10000)) {
                bool mutexCreated;
                Mutex mutex = new Mutex(true, "testmapmutex", out mutexCreated);
                using (MemoryMappedViewStream stream = mmf.CreateViewStream()) {
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(1);
                }
                mutex.ReleaseMutex();

                Console.WriteLine("Start Process B and press ENTER to continue.");
                
                NamedPipeServerStream pipeServer = new NamedPipeServerStream("testpipe", PipeDirection.InOut, 1);
                pipeServer.WaitForConnection();

                try {
                    // Read user input and send that to the client process. 
                    using (StreamWriter sw = new StreamWriter(pipeServer)) {
                        sw.AutoFlush = true;
                        Console.Write("Enter text: ");
                        sw.WriteLine(Console.ReadLine());
                    }
                }
                // Catch the IOException that is raised if the pipe is broken 
                // or disconnected. 
                catch (IOException e) {
                    Console.WriteLine("ERROR: {0}", e.Message);
                }

                Console.ReadLine();

                mutex.WaitOne();
                using (MemoryMappedViewStream stream = mmf.CreateViewStream()) {
                    BinaryReader reader = new BinaryReader(stream);
                    Console.WriteLine("Process A says: {0}", reader.ReadBoolean());
                    Console.WriteLine("Process B says: {0}", reader.ReadBoolean());
                }
                mutex.ReleaseMutex();
            }
            Console.ReadLine();
        }
    }
}