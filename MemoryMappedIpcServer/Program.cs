using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcServer {
    class Program {

        private static readonly List<ConnectionToClient> Connections = new List<ConnectionToClient>();
        private static readonly List<ConnectionToClient> NewConnections = new List<ConnectionToClient>();

        private static void ConnectionReceived(IAsyncResult result) {
            // when a connection comes in, 
            // open another pipe with the client and create a listener record with it
            // create a mutex and a shmem

            string id = result.AsyncState.ToString();
            Console.WriteLine("server received a connection " + id);

            // TODO don't forget to do this line upon application exit
            _welcomingPipeServer.EndWaitForConnection(result);

            NamedPipeServerStream matchedPipeServer = _welcomingPipeServer;
            var connectionToClient = new ConnectionToClient(id, matchedPipeServer);
            lock (NewConnections) {
                NewConnections.Add(connectionToClient);
            }

            // tell the connected client
                // his id
                // the address in shared memory that he should use
                // he does not have to say anything
            connectionToClient.Greet();

            // for the next client, recreate and keep listening
            _welcomingPipeServer = CreateNewPipeServer();
            _welcomingPipeServer.BeginWaitForConnection(ConnectionReceived, GetNextClientId());

            // TODO don't forget to do this line upon application exit
            //_welcomingPipeServer.Disconnect();
        }

        private static NamedPipeServerStream CreateNewPipeServer() {
            return new NamedPipeServerStream("wii_welcomer_pipe", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }

        private static NamedPipeServerStream _welcomingPipeServer;

        private static int _clientCount = 0;

        static string GetNextClientId() {
            ++_clientCount;
            return "client" + _clientCount;
        }

        // manage a wiiuse_simple and supply data to here
        static void MainLoop() {
            //THIS WORKS! Great. 
            WiiuseSimple.wus_init(1);
            Stopwatch stopwatch = Stopwatch.StartNew();

            int numConnected = WiiuseSimple.wus_find_and_connect(1);

            if (numConnected == 0) {
                Console.WriteLine("could not connect to a wii remote");
            } else {
                int wmi = 0;

                WiiuseSimple.wus_start_accel(wmi);
                WiiuseSimple.wus_start_gyro(wmi);

                while (true) {
                    RunConnectionMaintenance();

                    if (WiiuseSimple.wus_poll() > 0) {
                        short buttonsPressed = WiiuseSimple.wus_get_buttons_pressed(wmi);
                        short buttonsHeld = WiiuseSimple.wus_get_buttons_held(wmi);
                        short buttonsReleased = WiiuseSimple.wus_get_buttons_released(wmi);

                        long millisecondsNow = stopwatch.ElapsedMilliseconds;

                        foreach (ConnectionToClient connection in Connections) {
                            if (WiiuseSimple.wus_accel_received(wmi) > 0) {
                                float x, y, z;
                                WiiuseSimple.wus_get_accel(wmi, out x, out y, out z);
                                Console.WriteLine("accel: " + x + " " + y + " " + z);

                                MotionMessage m;

                                m.IsGyro = false;
                                m.Milliseconds = millisecondsNow; 
                                m.X = x;
                                m.Y = y;
                                m.Z = z;

                                connection.SharedMemoryAccessor.AddLine(m);
                            }

                            if (WiiuseSimple.wus_gyro_received(wmi) > 0) {
                                float x, y, z;
                                WiiuseSimple.wus_get_gyro(wmi, out x, out y, out z);
                                Console.WriteLine("gyro: " + x + " " + y + " " + z);

                                MotionMessage m;

                                m.IsGyro = true;
                                m.Milliseconds = millisecondsNow;
                                m.X = x;
                                m.Y = y;
                                m.Z = z;

                                connection.SharedMemoryAccessor.AddLine(m);
                            }
                        }

                    }
                }
            }

            //int returned = WiiuseSimple.test_wiiuse_simple();
            //Console.WriteLine("wiiuse connected and returned " + returned);

            //MotionMessage counter = new MotionMessage(true, 1, .1f, .2f, .3f);

            //while (true) {
            //    if (_connections.Count == 0) {
            //        Thread.SpinWait(20);
            //    } else {
            //        // this will be done through the buffer
            //        foreach (ConnectionToClient connection in _connections) {
            //            if (counter.Milliseconds % 1000000 == 0) {
            //                //connection.MmWriter.Seek(0, SeekOrigin.Begin);
            //                counter.Milliseconds = 0;
            //                Console.WriteLine("zeroed");
            //            }
            //            //connection.MmWriter.Write(counter);
            //            connection.SharedMemoryAccessor.AddLine(counter);
            //            Console.WriteLine("added " + counter);
            //            ++counter.Milliseconds;
            //            counter.IsGyro = !counter.IsGyro;
            //            counter.X += .01f;
            //            counter.Y += .01f;
            //            counter.Z += .01f;

            //            Console.ReadLine();
            //        }

            //        Thread.Sleep(10);
            //    }
            //}
            // device thread
                // if there are listeners, grab the data (or grab all the time maybe)
                // do this in mutex
                    // add the data to each of the listeners 
        }

        private static void RunConnectionMaintenance() {
            if (Connections.Count > 0) {
                Console.WriteLine("hele");
            }

            Console.WriteLine(Connections.Count);

            Connections.RemoveAll(delegate(ConnectionToClient connection) {
                bool disconnected = !connection.IsConnected();
                if (disconnected) {
                    // dismantle whatever we have
                    connection.Dispose();
                }
                return disconnected;
            });

            if (NewConnections.Count > 0) {
                lock (NewConnections) {
                    Connections.AddRange(NewConnections);
                    NewConnections.Clear();
                }
            }
        }

        private static void Main(string[] args) {
            _welcomingPipeServer = CreateNewPipeServer();
            _welcomingPipeServer.BeginWaitForConnection(ConnectionReceived, GetNextClientId());

            MainLoop();
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
