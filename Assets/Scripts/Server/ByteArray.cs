using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace KingdomWar.Server
{
    public class ByteArray
    {
        public const int DefaultSize = 1024;
        public int initSize;
        public byte[] Bytes;
        public int readIdx;
        public int writeIdx;
        private int capacity;
        public int Remain { get { return capacity - writeIdx; } }
        public int Length { get { return writeIdx - readIdx; } }
        public ByteArray()
        {
            Bytes = new byte[DefaultSize];
            capacity = DefaultSize;
            initSize = DefaultSize;
            readIdx = 0;
            writeIdx = 0;
        }
        public ByteArray(byte[] defaultBytes)
        {
            Bytes = defaultBytes;
            capacity = defaultBytes.Length;
            initSize = defaultBytes.Length;
            readIdx = 0;
            writeIdx = defaultBytes.Length;
        }
        //移动并检测数据
        public void ChecheAndMoveBytes()
        {
            if (Length < 8)
            {
                if (readIdx < 0)
                {
                    return;
                }
                Array.Copy(Bytes, readIdx, Bytes, 0, Length);
                writeIdx = Length;
                readIdx = 0;
            }
        }
        //扩容
        public void Resize(int size)
        {
            if (readIdx < 0)
            {
                return;
            }
            if (size < Length)
            {
                return;
            }
            if (size < initSize)
            {
                return;
            }
            int n = DefaultSize;
            while (n < size)
            {
                n *= 2;
            }
            capacity = n;
            byte[] newBytes = new byte[capacity];
            Array.Copy(Bytes, readIdx, newBytes, 0, Length);
            Bytes = newBytes;
            writeIdx = Length;
            readIdx = 0;
        }
    }

}
