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
        private StreamString streamString;

        // the mutex
        private Mutex bufferSwitchMutex;

        // the buffer
        private MemoryMappedFile memoryMappedFile;
        private BinaryWriter bufferWriter;

    }
}
