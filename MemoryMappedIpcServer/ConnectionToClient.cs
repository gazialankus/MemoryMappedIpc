using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryMappedIpcServer {
    class ConnectionToClient {

        // the pipe on the server side
        private NamedPipeServerStream pipeServer;

        // the mutex
        private Mutex bufferSwitchMutex;

        // the buffer
        private MemoryMappedFile memoryMappedFile;
        private BinaryWriter bufferWriter;
        private string id;

        public ConnectionToClient(string id, NamedPipeServerStream matchedPipeServer) {
            this.id = id;
            this.pipeServer = matchedPipeServer;
            bufferSwitchMutex = new Mutex(false, id);
        }

        public void WaitForMutex() {
            bufferSwitchMutex.WaitOne();
        }

        public void ReleaseMutex() {
            bufferSwitchMutex.ReleaseMutex();
        }

    }
}
