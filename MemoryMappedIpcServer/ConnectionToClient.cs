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
        private StreamWriter streamToClient;
        private MemoryMappedFile _memoryMappedFile;
        private MemoryMappedViewStream _mmStream;
        public BinaryWriter MmWriter { get; }

        public ConnectionToClient(string id, NamedPipeServerStream matchedPipeServer) {
            this.id = id;
            this.pipeServer = matchedPipeServer;
            bufferSwitchMutex = new Mutex(false, id);
            streamToClient = new StreamWriter(matchedPipeServer) {AutoFlush = true};

            _memoryMappedFile = MemoryMappedFile.CreateNew("testmap", 10000);
            _mmStream = _memoryMappedFile.CreateViewStream();
            MmWriter = new BinaryWriter(_mmStream);
            MmWriter.Write(114);
            MmWriter.Write(115);
        }

        public void WaitForMutex() {
            bufferSwitchMutex.WaitOne();
        }

        public void ReleaseMutex() {
            bufferSwitchMutex.ReleaseMutex();
        }

        public void Greet() {
            streamToClient.WriteLine(id);
        }
    }
}
