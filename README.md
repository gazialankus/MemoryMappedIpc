# MemoryMappedIpc
A simple C# project to get processes on the same machine to communicate.

This is a simple project that will the publisher-subscriber kind of IPC on Windows. I will use it as a bridge between wiiuse and Unity3D. The server process will maintain a wiiuse context and feed multiple Unity3D clients with Wii data. 

The initial plan was to get the clients toconnect to the server with a named pipe. However, Unity has a bug that causes crashes when disposing pipes. Instead, there is a common shared memory that takes care of the initial connection. 

The server then initialize a per-client shared memory area for fast operation. The server receives events from wiiuse, timestamps them, and fills them into clients' shared memory areas. The shared memory areas are implemented as a circular buffer. This way, the client and the server never wait for each other and there are no threads that do that. 
