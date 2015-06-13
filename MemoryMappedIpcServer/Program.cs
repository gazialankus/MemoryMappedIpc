using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcServer {
    class Program {

        private static readonly List<ConnectionToClient> Connections = new List<ConnectionToClient>();
        private static readonly List<ConnectionToClient> NewConnections = new List<ConnectionToClient>();

        private static NamedPipeServerStream CreateNewPipeServer() {
            var namedPipeServerStream = new NamedPipeServerStream(SharedMemoryAccessor.PipeName, PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            return namedPipeServerStream;
        }

        private static void ConnectionReceived(IAsyncResult result) {
            // when a connection comes in, 
            // open another pipe with the client and create a listener record with it
            // create a mutex and a shmem

            string id = GetNextClientId();
            Console.WriteLine("server received a connection " + id);

            // TODO don't forget to do this line upon application exit
            _welcomingPipeServer.EndWaitForConnection(result);

            NamedPipeServerStream matchedPipeServer = _welcomingPipeServer;
            var connectionToClient = new ConnectionToClient(id, matchedPipeServer, _gyroCalibrator);
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
                for (int wmi = 0; wmi < numConnected; ++wmi) {
                    WiiuseSimple.wus_start_accel(wmi);
                    WiiuseSimple.wus_start_gyro(wmi);
                }

                while (true) {
                    RunConnectionMaintenance();

                    if (WiiuseSimple.wus_poll() > 0 && Connections.Count > 0) {
                        long millisecondsNow = stopwatch.ElapsedMilliseconds;

                        for (byte wmi = 0; wmi < numConnected; ++wmi) {
                            short buttonsPressed = WiiuseSimple.wus_get_buttons_pressed(wmi);
                            short buttonsHeld = WiiuseSimple.wus_get_buttons_held(wmi);
                            short buttonsReleased = WiiuseSimple.wus_get_buttons_released(wmi);

                            if (WiiuseSimple.wus_accel_received(wmi) > 0) {
                                float x, y, z;
                                WiiuseSimple.wus_get_accel(wmi, out x, out y, out z);
                                if (_verbose) {
                                    Console.WriteLine("accel: " + x + " " + y + " " + z);
                                }

                                MotionMessage m = new MotionMessage(
                                    wid: wmi, 
                                    isGyro: false, 
                                    milliseconds: millisecondsNow, 
                                    x: x, 
                                    y: y, 
                                    z: z);

                                foreach (ConnectionToClient connection in Connections)
                                {
                                    connection.SharedMemoryAccessor.AddLine(m);
                                }
                            }

                            if (WiiuseSimple.wus_gyro_received(wmi) > 0)
                            {
                                float x, y, z;
                                WiiuseSimple.wus_get_gyro(wmi, out x, out y, out z);
                                if (_verbose) {
                                    Console.WriteLine("gyro: " + x + " " + y + " " + z);
                                }

                                MotionMessage m = new MotionMessage(
                                    wid: wmi,
                                    isGyro: true,
                                    milliseconds: millisecondsNow,
                                    x: x,
                                    y: y,
                                    z: z);

                                foreach (ConnectionToClient connection in Connections)
                                {
                                    connection.SharedMemoryAccessor.AddLine(m);
                                }

                                if (_gyroCalibrator.IsCalibrationUnderwayFor(wmi)) {
                                    short xs, ys, zs;
                                    WiiuseSimple.wus_get_raw_gyro(wmi, out xs, out ys, out zs);
                                    _gyroCalibrator.RawGyroReceived(wmi, xs, ys, zs);
                                }

                                if (_gyroCalibrator.IsCalibrationValuesReadyFor(wmi)) {
                                    short[] c = _gyroCalibrator.ConsumeCalibrationValuesFor(wmi);
                                    // use them now
                                    WiiuseSimple.wus_set_gyro_calib(wmi, c[0], c[1], c[2]);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool _verbose = false;

        private static void RunConnectionMaintenance() {
            if (_verbose) {
                if (Connections.Count > 0) {
                    Console.WriteLine("hele");
                }

                Console.WriteLine(Connections.Count);
            }

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

        private static readonly GyroCalibrator _gyroCalibrator = new GyroCalibrator();

        private static void Main(string[] args) {
            _welcomingPipeServer = CreateNewPipeServer();
            _welcomingPipeServer.BeginWaitForConnection(ConnectionReceived, GetNextClientId());

            MainLoop();
            Console.ReadLine();
        }

    }
}
