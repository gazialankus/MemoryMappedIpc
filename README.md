# MemoryMappedIpc
A simple C# project to get processes on the same machine to communicate.

This is a simple project that will the publisher-subscriber kind of IPC on Windows. I will use it as a bridge between wiiuse and Unity3D. The server process will maintain a wiiuse context and feed multiple Unity3D clients with Wii data. 

The clients will connect to the server with a named pipe. The server then will initialize a shared memory area per client for fast operation. The server will receive events from wiiuse, timestamp them, and fill them into clients' shared memory areas. 

The shared memory areas will be implemented as a double buffer. The server will write to one buffer. When the client issues a poll request, the server will switch buffers and let the client read the other buffer. This way, the two processes will minimally wait for each other and the client will get accurately timestamped data even when it is busy. 
