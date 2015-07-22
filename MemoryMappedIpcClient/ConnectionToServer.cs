using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcClient {
    public class ConnectionToServer {
        public ConnectionToServer() {
            Console.WriteLine("will create common shmem");
            CommonSharedMemoryAccessor commonSharedMemoryAccessor = new CommonSharedMemoryAccessor();
            Console.WriteLine("created common shmem");


            int id = commonSharedMemoryAccessor.ConnectClientAndWaitForServer();
            Console.WriteLine("Connected\n");

            int lineSize = commonSharedMemoryAccessor.LineSize;
            int totalBufferSizeInLines = commonSharedMemoryAccessor.TotalBufferSizeInLines;


            Console.WriteLine("received from server: " + id + " " + lineSize + " " + totalBufferSizeInLines);

            _sharedMemoryAccessor = new SharedMemoryAccessor(
                clientId: id,
                isServer: false,
                lineSize: lineSize,
                totalBufferSizeInLines: totalBufferSizeInLines);

            Thread pingThread = new Thread(PingThread) {IsBackground = true};
            pingThread.Start();
        }

        public void RequestStreamFromSource(InfoType infoType) {
            RequestStreamFromSource((int)infoType, -1, -1);
        }

        public void RequestStreamFromSource(InfoType infoType, DeviceType deviceType, int deviceId = -1) {
            RequestStreamFromSource((int)infoType, (int)deviceType, -1);
        }

        private void RequestStreamFromSource(int infoType, int deviceType, int deviceId) {
            _sharedMemoryAccessor.ClientDesiredInfoType = infoType;
            _sharedMemoryAccessor.ClientDesiredDeviceType = deviceType;
            _sharedMemoryAccessor.ClientDesiredDeviceId = deviceId;
            _sharedMemoryAccessor.ClientHasSuppliedDesiredCriteria = true;
        }

        private void PingThread() {
            while (!_isDisposed) {
                Console.WriteLine("ping");
                _sharedMemoryAccessor.ClientPing = 1;
                Thread.Sleep(5000);
            }
            Console.WriteLine("out of while " + _isDisposed);
        }

        private readonly SharedMemoryAccessor _sharedMemoryAccessor;

        public IEnumerable<AbstractMessage> GetAvailableLines() {
            if (_sharedMemoryAccessor != null) {
                return _sharedMemoryAccessor.GetAvailableLinesToClient();
            } else {
                return new List<AbstractMessage>();
            }
        }

        public void StartGyroCalibration(byte wid) {
            _sharedMemoryAccessor.ClientWantsWiiGyroRecalibrationFor |= 1 << wid;
        }

        public void StopGyroCalibration(byte wid) {
            _sharedMemoryAccessor.ClientWantsWiiGyroRecalibrationFor &= ~(1 << wid);
        }

        public void SetGyroCalibrationFor(byte wid, short x, short y, short z) {
            _sharedMemoryAccessor.WiiGyroCalibrationValues = new short[] {x, y, z};
            _sharedMemoryAccessor.ClientSuppliedWiiGyroRecalibrationFor = wid;
        }

        private bool _isDisposed = false;

        public void Dispose() {
            _isDisposed = true;
            _sharedMemoryAccessor.ClientClosedConnection = true;
            _sharedMemoryAccessor.CleanUp();
            // maybe there should be more here. read up on how to encapsulate disposables in other disposables
        }
    }
}
