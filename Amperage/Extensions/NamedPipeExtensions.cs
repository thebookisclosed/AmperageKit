using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace Amperage.Extensions
{
    static class NamedPipeExtensions
    {
        internal static int ProcessServerData(this NamedPipeClientStream pipe)
        {
            int returnCode = 0;
            PipeOpCode op;
            var smallBuf = new byte[4];
            do
            {
                op = (PipeOpCode)pipe.ReadByte();
                switch (op)
                {
                    case PipeOpCode.String:
                        {
                            var str = pipe.ReadXPCString();
                            Console.WriteLine("XPC: {0}", str);
                            break;
                        }
                    case PipeOpCode.ReturnCode:
                        {
                            pipe.Read(smallBuf, 0, smallBuf.Length);
                            returnCode = BitConverter.ToInt32(smallBuf, 0);
                            break;
                        }
                }
            } while (op != PipeOpCode.Invalid);
            return returnCode;
        }

        internal static string ReadXPCString(this Stream pipe, byte[] sizeBuf = null)
        {
            if (sizeBuf == null)
                sizeBuf = new byte[4];
            pipe.Read(sizeBuf, 0, sizeBuf.Length);
            var size = BitConverter.ToUInt32(sizeBuf, 0);
            var strBuf = new byte[size];
            pipe.Read(strBuf, 0, strBuf.Length);
            var str = Encoding.ASCII.GetString(strBuf);
            return str;
        }

        internal static void WriteXPCString(this Stream pipe, string str)
        {
            var strBuf = Encoding.ASCII.GetBytes(str);
            pipe.WriteByte((byte)PipeOpCode.String);
            pipe.Write(BitConverter.GetBytes(strBuf.Length), 0, sizeof(int));
            pipe.Write(strBuf, 0, strBuf.Length);
        }

        internal static void WriteXPCRetCode(this Stream pipe, int retCode)
        {
            var rcBuf = BitConverter.GetBytes(retCode);
            pipe.WriteByte((byte)PipeOpCode.ReturnCode);
            pipe.Write(rcBuf, 0, rcBuf.Length);
        }

        internal static bool WaitForClientFinite(this NamedPipeServerStream pipe)
        {
            var wfcTask = pipe.WaitForConnectionAsync().GetAwaiter();
            for (int i = 0; i < 100; i++)
            {
                if (!wfcTask.IsCompleted)
                    Thread.Sleep(20);
                else
                    break;
            }
            return pipe.IsConnected;
        }
    }

    enum PipeOpCode
    {
        String = 0,
        ReturnCode = 1,
        Invalid = -1
    }
}
