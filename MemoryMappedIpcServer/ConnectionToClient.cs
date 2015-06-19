using System;
using System.Collections.Generic;
using System.Diagnostics;
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcServer {
    class ConnectionToClient {

        private readonly GyroCalibrator _gyroCalibrator;

        private readonly int id;

        // these are all in the shared memory. write code to get and set them. 
        public SharedMemoryAccessor SharedMemoryAccessor { get; private set; }

        public ConnectionToClient(int id, GyroCalibrator gyroCalibrator, int lineSize, int totalBufferSizeInLines) {
            this.id = id;
            _gyroCalibrator = gyroCalibrator;

            SharedMemoryAccessor = new SharedMemoryAccessor(
                clientId: id, 
                isServer: true, 
                lineSize: lineSize, 
                totalBufferSizeInLines: totalBufferSizeInLines);
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

        private readonly Stopwatch _pingStopWatch = Stopwatch.StartNew();
        private int _prevPingValue;

        public bool IsConnected() {
            if (SharedMemoryAccessor.ClientClosedConnection) {
                return false;
            } else {
                int prevPingValue = _prevPingValue;
                int ping = SharedMemoryAccessor.ClientPing;

                _prevPingValue = ping;

                if (ping != 0) {
                    SharedMemoryAccessor.ClientPing = 0;
                }

                if (prevPingValue != ping) {
                    _pingStopWatch.Reset();
                    _pingStopWatch.Start();
                    return true;
                } else {
                    return _pingStopWatch.ElapsedMilliseconds < 10000;
                }
            }
        }

        public void Dispose() {
            Console.WriteLine("Disposed of client connection.");

            SharedMemoryAccessor.CleanUp();
        }

        private readonly List<int> _gyroCalibSetForRemote = new List<int>();

        //public void SetGyroCalibFor(byte wmi, short[] c) {
        //    GyroCalibSetForRemote.Add(wmi);
        //    //why would the client need this? (for archival purposes maybe) will set anyway.
        //    SharedMemoryAccessor.GyroCalibrationValues = c;
        //    Console.WriteLine(c[0] + " " + c[1] + " " + c[2] + " GYRO CALIB");
        //}

        public bool GyroCalibNeverSetFor(byte wmi) {
            return !_gyroCalibSetForRemote.Contains(wmi);
        }

        public void RememberGyroCalibSetFor(byte wmi) {
            _gyroCalibSetForRemote.Add(wmi);
        }
    }
}
