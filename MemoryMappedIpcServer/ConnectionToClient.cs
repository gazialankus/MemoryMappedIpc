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

            int lineSize = MotionMessage.GetByteSize();
            int totalBufferSizeInLines = 100;

            SharedMemoryAccessor = new SharedMemoryAccessor(
                clientId: id, 
                isServer: true, 
                lineSize: lineSize, 
                totalBufferSizeInLines: totalBufferSizeInLines);


            Thread connectionMonitorThread = new Thread(ConnectionMonitorThread);
            connectionMonitorThread.Start();
        }

        private void ConnectionMonitorThread() {
            // If you need to actually read from the client, you have to modify this to get data as well.
            while (_pipeServer.IsConnected) {
                // create a request and wait on it
                IAsyncResult asyncResult = null;
                try {
                    asyncResult = _pipeServer.BeginRead(buffer: ConnectionTestingByteArray,
                        offset: 0,
                        count: 0,
                        callback: null,
                        state: null);
                } catch (Exception) {
                    _pipeServer.Close();
                } finally {
                    if (asyncResult != null) {
                        _pipeServer.EndRead(asyncResult);
                        _pipeServer.Close();
                    }
                }
            }
            Console.WriteLine("****server detected a disconnection");
        }


        private static readonly byte[] ConnectionTestingByteArray = new byte[] {0x20};

        public bool IsConnected() {
            return _pipeServer.IsConnected;
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
            _pipeServer.Dispose();

            SharedMemoryAccessor.CleanUp();
        }


        public void Greet() {
            _pipeToClient.Write(id);
            _pipeToClient.Write(SharedMemoryAccessor.LineSize);
            _pipeToClient.Write(SharedMemoryAccessor.TotalBufferSizeInLines);

            _pipeToClient.Flush(); // TODO see if this is necessary
        }
    }
}
