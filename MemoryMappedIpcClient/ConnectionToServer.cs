using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcClient {
    public class ConnectionToServer {
        public ConnectionToServer() {
            _pipeClient = new NamedPipeClientStream(".", SharedMemoryAccessor.PipeName,
                PipeDirection.InOut, PipeOptions.None,
                TokenImpersonationLevel.Impersonation);

            Console.WriteLine("Connecting to server...\n");
            _pipeClient.Connect();

            //Send99();
            //Read100();

            // receive greeting
            BinaryReader sr = new BinaryReader(_pipeClient);
            string id = sr.ReadString();
            int lineSize = sr.ReadInt32();
            int totalBufferSizeInLines = sr.ReadInt32();


            Console.WriteLine("received from server: " + id + " " + lineSize + " " + totalBufferSizeInLines);
            Console.WriteLine("Connected\n");


            _sharedMemoryAccessor = new SharedMemoryAccessor(
                clientId: id,
                isServer: false,
                lineSize: lineSize,
                totalBufferSizeInLines: totalBufferSizeInLines);
        }

        private void Send99() {
            //works
            BinaryWriter bw = new BinaryWriter(_pipeClient);
            bw.Write(99);
            bw.Flush();
        }

        private void Read100() {
            BinaryReader br = new BinaryReader(_pipeClient);
            int read100 = br.Read();
            Console.WriteLine(read100);
            Console.Out.Flush();
        }

        private readonly SharedMemoryAccessor _sharedMemoryAccessor;
        private readonly NamedPipeClientStream _pipeClient;

        public IEnumerable<MotionMessage> GetAvailableLines() {
            return _sharedMemoryAccessor.GetAvailableLinesToClient();
        }

        public void StartGyroCalibration(byte wid) {
            //TODO pipe is broken here.
            BinaryWriter bw = new BinaryWriter(_pipeClient);
            bw.Write(new byte[] { PipeMessage.START_GYRO_CALIBRATION, wid });
            bw.Flush();
            Console.WriteLine("sent calib msg");
        }

        public void StopGyroCalibration(byte wid) {
            BinaryWriter bw = new BinaryWriter(_pipeClient);
            bw.Write(new byte[] { PipeMessage.STOP_GYRO_CALIBRATION, wid });
            bw.Flush();
        }

        public void Dispose() {
            _pipeClient.Dispose();
            // TODO there should be more here. read up on how to encapsulate disposables in other disposables
        }

    }
}
