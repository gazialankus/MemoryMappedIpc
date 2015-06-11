using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using MemoryMappedIpcServer.Shared;
using wyDay.Controls;

namespace MemoryMappedIpcClient {
    public class ConnectionToServer {
        public ConnectionToServer() {
            _pipeClient = new PipeClient();
            //_pipeClient = new NamedPipeClientStream(".", ,
            //    PipeDirection.InOut, PipeOptions.None,
            //    TokenImpersonationLevel.Impersonation);
            Console.WriteLine("Connecting to server...\n");
            _pipeClient.Connect(SharedMemoryAccessor.PipeName);
            //_pipeClient.Connect();
            BinaryReader sr = new BinaryReader(_pipeClient.GetStream());
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

        private readonly SharedMemoryAccessor _sharedMemoryAccessor;
        private readonly PipeClient _pipeClient;

        public IEnumerable<MotionMessage> GetAvailableLines() {
            return _sharedMemoryAccessor.GetAvailableLinesToClient();
        }

        public void Dispose() {
            _pipeClient.Dispose();
            // TODO there should be more here. read up on how to encapsulate disposables in other disposables
        }

    }
}
