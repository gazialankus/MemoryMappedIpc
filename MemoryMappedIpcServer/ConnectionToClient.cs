using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MemoryMappedIpcServer.Shared;
using wyUpdate;

namespace MemoryMappedIpcServer {
    class ConnectionToClient {

        // the pipe on the server side
        private PipeServer _pipeServer;
        private readonly GyroCalibrator _gyroCalibrator;

        private readonly BinaryWriter _pipeToClient;
        private readonly string id;

        // these are all in the shared memory. write code to get and set them. 

        public SharedMemoryAccessor SharedMemoryAccessor { get; private set; }

        public ConnectionToClient(string id, PipeServer matchedPipeServer, GyroCalibrator gyroCalibrator) {
            this.id = id;
            this._pipeServer = matchedPipeServer;
            _gyroCalibrator = gyroCalibrator;
            _isConnected = true;

            _pipeServer.ClientDisconnected += ConnectionBroken;
            _pipeServer.MessageReceived += MessageReceived;

            // TODO if we're not going to use this for anything other than greeting, perhaps don't keep the stream around
            _pipeToClient = new BinaryWriter(_pipeServer.GetStream());

            int lineSize = MotionMessage.GetByteSize();
            int totalBufferSizeInLines = 100;

            SharedMemoryAccessor = new SharedMemoryAccessor(
                clientId: id, 
                isServer: true, 
                lineSize: lineSize, 
                totalBufferSizeInLines: totalBufferSizeInLines);


            //Thread connectionMonitorThread = new Thread(ConnectionMonitorThread);
            //connectionMonitorThread.Start();
        }

        private bool _isConnected;

        private void ConnectionBroken() {
            _isConnected = false;
            Console.WriteLine("****server detected a disconnection.");
        }

        //private List<short[]> _rawGyroscopeReadings;

        //public bool IsCollectingRawGyro() {
        //    return _rawGyroscopeReadings != null;
        //}

        //public void CollectRawGyroscopeReadings() {
            
        //}

        //public List<byte> CalibrationDesiredForWiis; 

        private readonly List<byte> _iStartedGyroCalibrationFor = new List<byte>();

        private void MessageReceived(byte[] message) {
            //TODO client sent a message to server. use it here. 
            // for example, it may initiate a gyroscope calibration
            Console.WriteLine("Pipe message received!");
            if (message[0] == PipeMessage.START_GYRO_CALIBRATION) {
                byte wid = message[1];
                bool accepted = _gyroCalibrator.StartCalibrationDesired(wid);
                Console.WriteLine("m1");
                if (accepted) {
                    Console.WriteLine("m1.1");
                    _iStartedGyroCalibrationFor.Add(wid);
                }
            } else if (message[0] == PipeMessage.STOP_GYRO_CALIBRATION) {
                Console.WriteLine("m2");
                byte wid = message[1];
                if (_iStartedGyroCalibrationFor.Contains(wid)) {
                    Console.WriteLine("m2.1");
                    _iStartedGyroCalibrationFor.Remove(wid);
                    _gyroCalibrator.EndCalibrationDesired(wid);
                }

                // stop collecting raw calibration data. 
                // create a copy of the ones collected so far
                // average them.
                // use this average as the new calibration. 
            } else {
                Console.WriteLine("UNKNOWN MESSAGE FROM PIPE");
            }

        }

        //private void ConnectionMonitorThread() {
        //    // If you need to actually read from the client, you have to modify this to get data as well.
        //    while (_pipeServer.IsConnected) {
        //        // create a request and wait on it
        //        IAsyncResult asyncResult = null;
        //        try {
        //            asyncResult = _pipeServer.BeginRead(buffer: ConnectionTestingByteArray,
        //                offset: 0,
        //                count: 0,
        //                callback: null,
        //                state: null);
        //        } catch (Exception) {
        //            _pipeServer.Close();
        //        } finally {
        //            if (asyncResult != null) {
        //                _pipeServer.EndRead(asyncResult);
        //                _pipeServer.Close();
        //            }
        //        }
        //    }
        //    Console.WriteLine("****server detected a disconnection");
        //}


        private static readonly byte[] ConnectionTestingByteArray = new byte[] {0x20};

        public bool IsConnected() {
            return _isConnected;
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

        static byte[] GetBytes(string str) {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes) {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public void Greet() {
            _pipeToClient.Write(id);
            _pipeToClient.Write(SharedMemoryAccessor.LineSize);
            _pipeToClient.Write(SharedMemoryAccessor.TotalBufferSizeInLines);

            //_pipeServer.SendMessage(GetBytes(id));
            //_pipeServer.SendMessage(BitConverter.GetBytes(SharedMemoryAccessor.LineSize));
            //_pipeServer.SendMessage(BitConverter.GetBytes(SharedMemoryAccessor.TotalBufferSizeInLines));

            _pipeToClient.Flush(); // TODO see if this is necessary
        }
    }
}
