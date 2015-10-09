using System;
using System.Collections.Generic;
using System.Diagnostics;
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcServer {
    class Program {

        private static readonly List<ConnectionToClient> Connections = new List<ConnectionToClient>();
        private static readonly List<ConnectionToClient> NewConnections = new List<ConnectionToClient>();

        // Instead of these pipes, create the common shared memory.
        //private static NamedPipeServerStream CreateNewPipeServer() {
        //    var namedPipeServerStream = new NamedPipeServerStream(SharedMemoryAccessor.PipeName, PipeDirection.InOut,
        //        NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        //    return namedPipeServerStream;
        //}

        //private static void ConnectionReceived(IAsyncResult result) {
        //    // when a connection comes in, 
        //    // open another pipe with the client and create a listener record with it
        //    // create a mutex and a shmem

        //    int id = GetNextClientId();
        //    Console.WriteLine("server received a connection " + id);

        //    // TODO don't forget to do this line upon application exit
        //    _welcomingPipeServer.EndWaitForConnection(result);

        //    NamedPipeServerStream matchedPipeServer = _welcomingPipeServer;
        //    var connectionToClient = new ConnectionToClient(id, matchedPipeServer, _gyroCalibrator);
        //    lock (NewConnections) {
        //        NewConnections.Add(connectionToClient);
        //    }

        //    // tell the connected client
        //        // his id
        //        // the address in shared memory that he should use
        //        // he does not have to say anything
        //    connectionToClient.Greet();


        //    // for the next client, recreate and keep listening
        //    _welcomingPipeServer = CreateNewPipeServer();
        //    _welcomingPipeServer.BeginWaitForConnection(ConnectionReceived, null);

        //    // TODO don't forget to do this line upon application exit
        //    //_welcomingPipeServer.Disconnect();
        //}

        //private static NamedPipeServerStream _welcomingPipeServer;


        private static short _prevButtonsPressed = 0;
        private static short _prevButtonsHeld = 0;
        private static short _prevButtonsReleased = 0;

        //private static int _clientCount = 0;
        //static int GetNextClientId() {
        //    ++_clientCount;
        //    return _clientCount;
        //}

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

                    bool isCalibratingGyro = false;
                    byte calibratingGyroFor = 0;
                    long millisecondsNow = stopwatch.ElapsedMilliseconds;
                    if (WiiuseSimple.wus_poll() > 0 && Connections.Count > 0) {

                        for (byte wmi = 0; wmi < numConnected; ++wmi) {
                            // this kind of button asking at every poll is good. 
                            // the server is up-to-date about the buttons
                            // the question is, how will the client know??
                            // we have to write it in the shared memory
                            short buttonsPressed = WiiuseSimple.wus_get_buttons_pressed(wmi);
                            short buttonsHeld = WiiuseSimple.wus_get_buttons_held(wmi);
                            short buttonsReleased = WiiuseSimple.wus_get_buttons_released(wmi);

                            if (buttonsPressed != _prevButtonsPressed || buttonsHeld != _prevButtonsHeld ||
                                buttonsReleased != _prevButtonsReleased) {

                                ButtonMessage m = new ButtonMessage(
                                    messageType: MessageType.ButtonMessage,
                                    milliseconds: millisecondsNow,
                                    wid: wmi, 
                                    pressed: buttonsPressed, 
                                    held: buttonsHeld, 
                                    released: buttonsReleased);

                                foreach (ConnectionToClient connection in Connections) {
                                    connection.SharedMemoryAccessor.AddLine(m);
                                }
                            }

                            _prevButtonsPressed = buttonsPressed;
                            _prevButtonsHeld = buttonsHeld;
                            _prevButtonsReleased = buttonsReleased;

                            if (WiiuseSimple.wus_accel_received(wmi) > 0) {
                                float x, y, z;
                                WiiuseSimple.wus_get_accel(wmi, out x, out y, out z);
                                if (_verbose) {
                                    Console.WriteLine("accel: " + x + " " + y + " " + z);
                                }

                                MotionMessage m = new MotionMessage(
                                    messageType: MessageType.AccelMessage, 
                                    milliseconds: millisecondsNow, 
                                    wid: wmi,
                                    x: -x * 9.81f, 
                                    y: -y * 9.81f, 
                                    z: -z * 9.81f);

                                foreach (ConnectionToClient connection in Connections) {
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
                                    messageType: MessageType.GyroMessage,
                                    milliseconds: millisecondsNow,
                                    wid: wmi,
                                    x: -x,
                                    y: -y,
                                    z: -z);

                                foreach (ConnectionToClient connection in Connections) {
                                    connection.SharedMemoryAccessor.AddLine(m);
                                }

                                if (GyroCalibrator.IsCalibrationUnderwayFor(wmi)) {
                                    isCalibratingGyro = true;
                                    calibratingGyroFor = wmi;
                                    short xs, ys, zs;
                                    WiiuseSimple.wus_get_raw_gyro(wmi, out xs, out ys, out zs);
                                    GyroCalibrator.RawGyroReceived(wmi, xs, ys, zs);
                                }

                                if (GyroCalibrator.IsCalibrationValuesReadyFor(wmi)) {
                                    short[] c = GyroCalibrator.ConsumeCalibrationValuesFor(wmi);
                                    // use them now
                                    WiiuseSimple.wus_set_gyro_calib(wmi, c[0], c[1], c[2]);

                                    // tell them what came from the server (also tell everybody else when it comes from the client)
                                    // WHAT: as the server, we just recalibrated a wii remote. now we need to tell the clients the values of that recalibration.

                                    var gyroCalibrationMessage = CreateGyroCalibrationMessage(millisecondsNow, wmi, c);

                                    foreach (ConnectionToClient connection in Connections)
                                    {
//                                        connection.SharedMemoryAccessor.GyroCalibrationValues = c;
                                        connection.SharedMemoryAccessor.AddLine(gyroCalibrationMessage);
                                        connection.RememberGyroCalibSetFor(wmi);
                                    }
                                }
                            }

                        }
                    }

                    // even if nothing comes from wii

                    foreach (ConnectionToClient connection in Connections) {
                        connection.UpdateGyroCalibrationStatus();
                    }

                    for (byte wmi = 0; wmi < numConnected; ++wmi) {
                        // read calibration values supplied and ignore if currently calibrating

                        GyroCalibrationMessage gyroCalibrationMessage = null;
                        foreach (ConnectionToClient connection in Connections) {
                            if (connection.GyroCalibNeverSetFor(wmi)) {
                                // WHAT: gyro calib never arrived at this client. therefore, we are sending it just so they know. 
                                // TODO this may be earlier than the recalibration that wiiuse is doing. should track that instead.
                                short[] c = new short[3];
                                WiiuseSimple.wus_get_gyro_calib(wmi, out c[0], out c[1], out c[2]);

                                if (gyroCalibrationMessage == null) {
                                    gyroCalibrationMessage = CreateGyroCalibrationMessage(millisecondsNow, wmi, c);
                                }
                                connection.SharedMemoryAccessor.AddLine(gyroCalibrationMessage);
                                connection.RememberGyroCalibSetFor(wmi);
                                //connection.SetGyroCalibFor(wmi, c);
                            }
                        }


                    }
                    ConnectionToClient connectionThatSuppliedCalibration = null;
                    byte calibrationIsForWii = 0;
                    foreach (ConnectionToClient connection in Connections)
                    {
                        // TODO make it -1 initially as well
                        if (connection.SharedMemoryAccessor.ClientSuppliedWiiGyroRecalibrationFor != -1) {
                            byte i = (byte)connection.SharedMemoryAccessor.ClientSuppliedWiiGyroRecalibrationFor;
                            connection.SharedMemoryAccessor.ClientSuppliedWiiGyroRecalibrationFor = -1;
                            if (!isCalibratingGyro || calibratingGyroFor != i) {
                                connectionThatSuppliedCalibration = connection;
                                calibrationIsForWii = i;
                            }
                        }
                    }
                    if (connectionThatSuppliedCalibration != null) {
                        short[] c = connectionThatSuppliedCalibration.SharedMemoryAccessor.WiiGyroCalibrationValues;
                        // WHAT: some client told us to use specific calibration values that they saved in the past.
                        // ok, let everybody else know. or put it in their shared memory so they know if they read it. 
                        GyroCalibrationMessage m = CreateGyroCalibrationMessage(millisecondsNow, calibrationIsForWii, c);
                        foreach (ConnectionToClient connection in Connections) {
                            if (connection != connectionThatSuppliedCalibration) {
//                                        connection.SharedMemoryAccessor.GyroCalibrationValues = c;
                                connection.SharedMemoryAccessor.AddLine(m);
                                connection.RememberGyroCalibSetFor(calibrationIsForWii);
                                //connection.SetGyroCalibFor(wmi, c);
                            }
                        }
                        // ok, now actually apply these calibration values to this wii remote
                        WiiuseSimple.wus_set_gyro_calib(calibrationIsForWii, c[0], c[1], c[2]);
                    }

                }
            }
        }

        private static GyroCalibrationMessage CreateGyroCalibrationMessage(long millisecondsNow, byte wmi, short[] c) {
            GyroCalibrationMessage gyroCalibrationMessage = new GyroCalibrationMessage(
                milliseconds: millisecondsNow,
                wid: wmi,
                x: c[0],
                y: c[1],
                z: c[2]);
            return gyroCalibrationMessage;
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
                bool disconnected = !connection.IsConnected(); //TODO never happens at the moment
                if (disconnected) {
                    // dismantle whatever we have
                    connection.Dispose();
                    // TODO reduce both requested and accepted clients. of course, setting them requires a mutex.
                    
                    // this would mean that we recycle ids. let it stay, don't care. 
                    //_commonSharedMemoryAccessor.LockMutex();
                    //_commonSharedMemoryAccessor.NumberOfClientRequests -= 1;
                    //_commonSharedMemoryAccessor.NumberOfAcceptedClients -= 1;
                    //_commonSharedMemoryAccessor.ReleaseMutex();
                }
                return disconnected;
            });

            // look at the common shared memory for new requests
            int acceptedClients = _commonSharedMemoryAccessor.NumberOfAcceptedClients;
            int requestedClients = _commonSharedMemoryAccessor.NumberOfClientRequests;

            // create connections for each and add them to connections
            for (int id = acceptedClients; id < requestedClients; ++id) {
                ConnectionToClient connectionToClient = new ConnectionToClient(
                    id: id, 
                    gyroCalibrator: GyroCalibrator, 
                    lineSize: _commonSharedMemoryAccessor.LineSize, 
                    totalBufferSizeInLines: _commonSharedMemoryAccessor.TotalBufferSizeInLines);
                Connections.Add(connectionToClient);

            }

            // increase the number to say that they are ready to use
            _commonSharedMemoryAccessor.LockMutex();
            _commonSharedMemoryAccessor.NumberOfAcceptedClients = requestedClients;
            _commonSharedMemoryAccessor.ReleaseMutex();
        }

        private static readonly GyroCalibrator GyroCalibrator = new GyroCalibrator();

        private static readonly int TotalBufferSizeInLines = ServerNames.DefaultTotalBufferSizeInLines;

        private static void Main(string[] args) {
            //_welcomingPipeServer = CreateNewPipeServer();
            //_welcomingPipeServer.BeginWaitForConnection(ConnectionReceived, null);

            InitializeCommonSharedMemory();

            MainLoop();
        }

        private static CommonSharedMemoryAccessor _commonSharedMemoryAccessor;

        private static void InitializeCommonSharedMemory() {
            // will monitor this in the main loop.
            _commonSharedMemoryAccessor = new CommonSharedMemoryAccessor(
                lineSize: AbstractMessage.LineSize,
                totalBufferSizeInLines: TotalBufferSizeInLines);
        }
    }
}
