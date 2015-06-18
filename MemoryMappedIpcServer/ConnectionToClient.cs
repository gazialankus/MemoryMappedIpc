using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcServer {
    class ConnectionToClient {

        // the pipe on the server side
        //private readonly NamedPipeServerStream _pipeServer;
        private readonly GyroCalibrator _gyroCalibrator;

        //private readonly BinaryWriter _pipeToClient;
        private readonly int id;

        // these are all in the shared memory. write code to get and set them. 

        public SharedMemoryAccessor SharedMemoryAccessor { get; private set; }

        public ConnectionToClient(int id, GyroCalibrator gyroCalibrator, int lineSize, int totalBufferSizeInLines) {
            this.id = id;
            //this._pipeServer = matchedPipeServer;
            _gyroCalibrator = gyroCalibrator;

            // TODO if we're not going to use this for anything other than greeting, perhaps don't keep the stream around
            //_pipeToClient = new BinaryWriter(_pipeServer);

            // header: byte, long, byte
            // motion message: float, float, float

            //int lineSize = sizeof(byte) + sizeof(long) + sizeof(byte) + 3 * sizeof(float) + 4; //+4 just in case.

            SharedMemoryAccessor = new SharedMemoryAccessor(
                clientId: id, 
                isServer: true, 
                lineSize: lineSize, 
                totalBufferSizeInLines: totalBufferSizeInLines);

            //Read99();
            //Send100();
        }

        private int _clientWantedGyroRecalibFor;

        public void UpdateGyroCalibrationStatus() {
            int clientWantsGyroRecalibrationFor = SharedMemoryAccessor.ClientWantsGyroRecalibrationFor;

            if (_clientWantedGyroRecalibFor != clientWantsGyroRecalibrationFor) {
                // turn some on
                int turnedOn = clientWantsGyroRecalibrationFor & ~_clientWantedGyroRecalibFor;

                int turnedOnLeft = turnedOn;

                for (byte i = 0; i < 32 && turnedOnLeft != 0; ++i) {
                    if ((turnedOnLeft & 1) != 0) {
                        StartCalibrationDesired(i);
                    }

                    turnedOnLeft = turnedOnLeft >> 1;
                }

                // turn some off 
                int turnedOff = _clientWantedGyroRecalibFor & ~clientWantsGyroRecalibrationFor;

                int turnedOffLeft = turnedOff;

                for (byte i = 0; i < 32 && turnedOffLeft != 0; ++i) {
                    if ((turnedOffLeft & 1) != 0) {
                        StopCalibrationDesired(i);
                    }

                    turnedOffLeft = turnedOffLeft >> 1;
                }
            }

            // did the requested state change?
            //  is there a new request?
            //      then let's start a calib.
            //  is there no longer a request?
            //      then let's stop a calib 
            _clientWantedGyroRecalibFor = clientWantsGyroRecalibrationFor;
        }

        private readonly List<byte> _iStartedGyroCalibrationFor = new List<byte>();

        private void StopCalibrationDesired(byte wid) {
            Console.WriteLine("m2");
            if (_iStartedGyroCalibrationFor.Contains(wid)) {
                Console.WriteLine("m2.1");
                _iStartedGyroCalibrationFor.Remove(wid);
                _gyroCalibrator.EndCalibrationDesired(wid);
            }
        }

        private void StartCalibrationDesired(byte wid) {
            bool accepted = _gyroCalibrator.StartCalibrationDesired(wid);
            Console.WriteLine("m1");
            if (accepted) {
                Console.WriteLine("m1.1");
                _iStartedGyroCalibrationFor.Add(wid);
            }
        }


        //private List<short[]> _rawGyroscopeReadings;

        //public bool IsCollectingRawGyro() {
        //    return _rawGyroscopeReadings != null;
        //}

        //public void CollectRawGyroscopeReadings() {
            
        //}

        //public List<byte> CalibrationDesiredForWiis; 


        // TODO TODO this is how you should receive a message. this is not called now. 
        //private void MessageReceived(byte[] message) {
        //    //TODO client sent a message to server. use it here. 
        //    // for example, it may initiate a gyroscope calibration
        //    Console.WriteLine("Pipe message received!");
        //    if (message[0] == PipeMessage.START_GYRO_CALIBRATION) {
        //        byte wid = message[1];
        //        StartCalibrationDesired(wid);
        //    } else if (message[0] == PipeMessage.STOP_GYRO_CALIBRATION) {
        //        byte wid = message[1];
        //        StopCalibrationDesired(wid);

        //        // stop collecting raw calibration data. 
        //        // create a copy of the ones collected so far
        //        // average them.
        //        // use this average as the new calibration. 
        //    } else {
        //        Console.WriteLine("UNKNOWN MESSAGE FROM PIPE");
        //    }
        //}

        //private readonly List<byte> _iStartedGyroCalibrationFor = new List<byte>();
        //private void StopCalibrationDesired(byte wid) {
        //    Console.WriteLine("m2");
        //    if (_iStartedGyroCalibrationFor.Contains(wid)) {
        //        Console.WriteLine("m2.1");
        //        _iStartedGyroCalibrationFor.Remove(wid);
        //        _gyroCalibrator.EndCalibrationDesired(wid);
        //    }
        //}

        //private void StartCalibrationDesired(byte wid) {
        //    bool accepted = _gyroCalibrator.StartCalibrationDesired(wid);
        //    Console.WriteLine("m1");
        //    if (accepted) {
        //        Console.WriteLine("m1.1");
        //        _iStartedGyroCalibrationFor.Add(wid);
        //    }
        //}

        //private void ConnectionMonitorThread() {
        //    BinaryReader br = new BinaryReader(_pipeServer);
        //    while (_pipeServer.IsConnected) {
        //        try {
        //            Console.WriteLine("WILL READ MESSAGE");
        //            int msgType = br.ReadByte();
        //            Console.WriteLine("DID READ " + msgType);
        //            byte wid;
        //            switch (msgType) {
        //                case PipeMessage.START_GYRO_CALIBRATION:
        //                    wid = br.ReadByte();
        //                    Console.WriteLine("MSG: Start Gyro Calib for " + wid);
        //                    StartCalibrationDesired(wid);
        //                    break;
        //                case PipeMessage.STOP_GYRO_CALIBRATION:
        //                    wid = br.ReadByte();
        //                    Console.WriteLine("MSG: STOP Gyro Calib for " + wid);
        //                    StopCalibrationDesired(wid);
        //                    break;
        //                default:
        //                    Console.WriteLine("UNKNOWN MESSAGE!!! " + msgType);
        //                    break;
        //            }
        //        } catch (EndOfStreamException e) {
        //            // detect that the pipe is closed, just like I used to in the async case. 
        //            // that was the only way I detected that the client app is killed
        //            Console.WriteLine("END OF STREAM!");
        //            Console.WriteLine(e);
        //            _pipeServer.Close();
        //        }
        //    }
        //    Console.WriteLine("PIPE IS DISCONNECTED!!!!");
        //}

        //private void ConnectionMonitorThread_async() {
        //    // If you need to actually read from the client, you have to modify this to get data as well.
        //    while (_pipeServer.IsConnected) {
        //        // create a request and wait on it
        //        IAsyncResult asyncResult = null;
        //        int currentOffset = 0;
        //        try {
        //            asyncResult = _pipeServer.BeginRead(buffer: ConnectionTestingByteArray,
        //                offset: currentOffset,
        //                count: 4,
        //                callback: null,
        //                state: null);
        //            currentOffset += 4;
        //        } catch (Exception e) {
        //            //_pipeServer.Close();
        //            Console.WriteLine("exception");
        //            Console.WriteLine(e);
        //        } finally {
        //            if (asyncResult != null) {
        //                _pipeServer.EndRead(asyncResult);
        //                //_pipeServer.Close();
        //            }
        //            Console.WriteLine("****FINALLY HIT");
        //        }

        //        var int32 = BitConverter.ToInt32(ConnectionTestingByteArray, 0);
        //        Console.WriteLine("R: " + int32);
        //        Console.Out.Flush();

        //    }
        //    Console.WriteLine("****server detected a disconnection");
        //}

        //private void Read99() {
        //    // async read works fine.
        //    IAsyncResult asyncResult = _pipeServer.BeginRead(buffer: ConnectionTestingByteArray,
        //        offset: 0,
        //        count: 4,
        //        callback: null,
        //        state: null);
        //    _pipeServer.EndRead(asyncResult);
        //    var int32 = BitConverter.ToInt32(ConnectionTestingByteArray, 0);
        //    Console.WriteLine(int32);
        //    Console.Out.Flush();

        //    //BinaryReader br = new BinaryReader(_pipeServer);
        //    //int read99 = br.ReadInt32();
        //    //Console.WriteLine(read99);
        //    //Console.Out.Flush();
        //}

        //private void Send100() {
        //    BinaryWriter bw = new BinaryWriter(_pipeServer);
        //    bw.Write(100);
        //    bw.Flush();
        //}


        private static readonly byte[] ConnectionTestingByteArray = new byte[] {0, 0, 0, 0};

        public bool IsConnected() {
            return !SharedMemoryAccessor.ClientClosedConnection;
            //return _pipeServer.IsConnected;
            //if (!_pipeServer.IsConnected) {
            //    return false;
            //} else {
            //    IAsyncResult asyncResult = null;
            //    try {
            //        //TODO asking to read 0 bytes now. if this does not work, ask for more. in that case, consider what to do when you actually read something...
            //        asyncResult = _pipeServer.BeginRead(buffer: ConnectionTestingByteArray,
            //            offset: 0,
            //            count: 1,
            //            callback: null,
            //            state: null);
            //        //pd.pipe.BeginRead(pd.data, 0, pd.data.Length, OnAsyncMessage, pd);
            //    } catch (Exception) {
            //        _pipeServer.Close();
            //        return false;
            //    } finally {
            //        if (asyncResult != null) {
            //            _pipeServer.EndRead(asyncResult); 
            //            // DO THIS: a thread can wait here for each pipe. and then when the pipe is broken this returns. good. 
            //            // in this class, spawn a thread that waits here. right after this, it declares the pipe broken. good. 

            //        }
            //    }
            //}
            //return true;
        }

        public void Dispose() {
            // TODO dismantle the shared memory
            Console.WriteLine("****TODO should dispose the pipe.");
            //_pipeServer.Dispose();

            SharedMemoryAccessor.CleanUp();
        }

        //public void Greet() {
        //    _pipeToClient.Write(id);
        //    _pipeToClient.Write(SharedMemoryAccessor.LineSize);
        //    _pipeToClient.Write(SharedMemoryAccessor.TotalBufferSizeInLines);
        //    _pipeToClient.Flush(); // TODO see if this is necessary

        //    //_pipeServer.SendMessage(GetBytes(id));
        //    //_pipeServer.SendMessage(BitConverter.GetBytes(SharedMemoryAccessor.LineSize));
        //    //_pipeServer.SendMessage(BitConverter.GetBytes(SharedMemoryAccessor.TotalBufferSizeInLines));


        //    Thread connectionMonitorThread = new Thread(ConnectionMonitorThread);
        //    connectionMonitorThread.Start();
        //}

        private List<int> GyroCalibSetForRemote = new List<int>();

        public void SetGyroCalibFor(byte wmi, short[] c) {
            GyroCalibSetForRemote.Add(wmi);
            //TODO why would the client need this?? will set anyway.
            SharedMemoryAccessor.GyroCalibrationValues = c;
            Console.WriteLine(c[0] + " " + c[1] + " " + c[2] + " GYRO CALIB");
        }

        public bool GyroCalibNeverSetFor(byte wmi) {
            return !GyroCalibSetForRemote.Contains(wmi);
        }
    }
}
