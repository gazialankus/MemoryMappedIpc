using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcServer {
    class ConnectionToClient {

        // the pipe on the server side
        private NamedPipeServerStream _pipeServer;

        private readonly BinaryWriter _pipeToClient;
        private readonly string id;

        // these are all in the shared memory. write code to get and set them. 

        public SharedMemoryAccessor SharedMemoryAccessor { get; private set; }

        public ConnectionToClient(string id, NamedPipeServerStream matchedPipeServer) {
            this.id = id;
            this._pipeServer = matchedPipeServer;

            // TODO if we're not going to use this for anything other than greeting, perhaps don't keep the stream around
            _pipeToClient = new BinaryWriter(matchedPipeServer);

            int lineSize = sizeof(int);
            int totalBufferSizeInLines = 100;

            SharedMemoryAccessor = new SharedMemoryAccessor(
                clientId: id, 
                isServer: true, 
                lineSize: lineSize, 
                totalBufferSizeInLines: totalBufferSizeInLines);
        }


        public void Greet() {
            _pipeToClient.Write(id);
            _pipeToClient.Write(SharedMemoryAccessor.LineSize);
            _pipeToClient.Write(SharedMemoryAccessor.TotalBufferSizeInLines);

            _pipeToClient.Flush(); // TODO see if this is necessary
        }
    }
}
