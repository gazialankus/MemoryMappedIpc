using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MemoryMappedIpcServer.Shared;

namespace MemoryMappedIpcClient {
    class ClientProgram {
        private static void Main(string[] args) {

            //while (true) {
            //    ConnectionToServer c = new ConnectionToServer();

            //    Console.WriteLine("created");

            //    Thread.Sleep(1000);

            //    c.Dispose();
                
            //    Console.WriteLine("disposed");

            //    Thread.Sleep(1000);

            //    GC.Collect();
            //    Console.WriteLine("gc collected");
            //}



            ConnectionToServer connectionToServer = new ConnectionToServer();


            //NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "wii_welcomer_pipe",
            //    PipeDirection.InOut, PipeOptions.None,
            //    TokenImpersonationLevel.Impersonation);
            //Console.WriteLine("Connecting to server...\n");
            //pipeClient.Connect();

            //BinaryReader sr = new BinaryReader(pipeClient);
            //string id = sr.ReadString();
            //int lineSize = sr.ReadInt32();
            //int totalBufferSizeInLines = sr.ReadInt32();

            //SharedMemoryAccessor sharedMemoryAccessor = new SharedMemoryAccessor(
            //    clientId: id, 
            //    isServer: false, 
            //    lineSize: lineSize, 
            //    totalBufferSizeInLines: totalBufferSizeInLines);

            //Console.WriteLine("received from server: " + id + " " + lineSize + " " + totalBufferSizeInLines);

            //Console.WriteLine("Connected\n");

            //Console.WriteLine(sharedMemoryAccessor.ReadLine());
            //Console.WriteLine(sharedMemoryAccessor.ReadLine());

            //while (true) {
            //    Thread.Sleep(2000);
            //    Console.WriteLine(sharedMemoryAccessor.ReadLine());
            //}


            bool calibrating = false;
            for (int ii = 0;; ++ii) {

                bool first = true;
                foreach (AbstractMessage i in connectionToServer.GetAvailableLines()) {
                    if (first) {
                        //Console.WriteLine("first");
                        first = false;
                    }
                    //Console.WriteLine("read this: " + i.Wid + " " + i.IsGyro + " " + i.Milliseconds + " " + i.X + " " + i.Y + " " + i.Z);

                    //Console.Write(i.Wid + " " + i.Milliseconds + " ");

                    switch (i.MessageType) {
                        case MessageType.ButtonMessage:
                            ButtonMessage b = i as ButtonMessage;
                            if (b != null) {
                                Console.WriteLine("btn: " + b.Pressed + " " + b.Held + " " + b.Released);
                            } else {
                                throw new InvalidDataException();
                            }
                            break;
                        case MessageType.GyroMessage:
                            MotionMessage g = i as MotionMessage;
                            if (g != null) {
                                //Console.WriteLine("gyro: " + g.X + "\t" + g.Y + "\t" + g.Z);
                            }
                            break;
                        case MessageType.AccelMessage:
                            MotionMessage a = i as MotionMessage;
                            if (a != null) {
                                //Console.WriteLine("accel: " + a.X + "\t" + a.Y + "\t" + a.Z);
                            }
                            break;
                        case MessageType.GyroCalibrationMessage:
                            GyroCalibrationMessage gy = i as GyroCalibrationMessage;
                            if (gy != null) {
                                Console.WriteLine("gyro calib: " + gy.X + " " + gy.Y + " " + gy.Z + " ");
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                }
                //Console.WriteLine("hit enter");
                //Console.ReadLine();
                // TODO instead of user input, use the value of ii to decide when to start and stop calibration. 
                //Console.WriteLine(ii);



                if (ii == 1000000) {
                    calibrating = true;
                    connectionToServer.StartGyroCalibration(0);
                    Console.WriteLine("started calibrating");
                }
                if (ii == 2000000) {
                    calibrating = false;
                    connectionToServer.StopGyroCalibration(0);
                    Console.WriteLine("stopped calibrating");
                }



                //if (Console.InKeyAvailable) {
                //    if (calibrating) {
                //        if (Console.ReadKey().Key == ConsoleKey.Spacebar) {
                //            calibrating = false;
                //            connectionToServer.StopGyroCalibration(0);
                //            Console.WriteLine("stopped calibrating");
                //        }
                //    } else {
                //        if (Console.ReadKey().Key == ConsoleKey.Enter) {
                //            calibrating = true;
                //            connectionToServer.StartGyroCalibration(0);
                //            Console.WriteLine("started calibrating");
                //        } 
                //    }
                //}
            }


            // this doesn't break it either. 

            connectionToServer.Dispose();


        }

    }
}
