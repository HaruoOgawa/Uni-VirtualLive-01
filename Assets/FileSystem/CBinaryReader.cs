using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using UnityEngine;

namespace binary
{
    public class CBinaryReader
    {
        byte[] m_Data = new byte[0];
        int m_Offset = 0;

        public CBinaryReader()
        {
        }

        public bool Init(string fileName)
        {
            this.m_Data = File.ReadAllBytes(fileName);
            if (this.m_Data.Length == 0) return false;

            return true;
        }

        public bool IsEnd()
        {
            return (m_Data.Length == m_Offset);
        }

        private void UpdatePointer(int byteSize)
        {
            m_Offset += byteSize;

            // 最後まで読み取っていたらこれ以上は更新しない
            if (IsEnd()) return;
        }

        private byte[] memcpy(int byteSize)
        {
            byte[] bin = new byte[byteSize];

            Array.Copy(this.m_Data, m_Offset, bin, 0, byteSize);

            return bin;
        }

        public bool IsValid(int byteSize)
        {
            if(m_Offset + byteSize > m_Data.Length) return false;

            return true;
        }

        public Span<byte> GetPointer()
        {
            Span<byte> ptr = this.m_Data.AsSpan(m_Offset);

            return ptr;
        }

        public bool Skip(int byteSize)
        {
            if(!IsValid(byteSize)) return false;

            UpdatePointer(byteSize);

            return true;
        }

        public bool GetByte(ref byte Dst)
        {
            if (!IsValid(sizeof(byte))) return false;

            Dst = GetByte();

            return true;
        }

        public byte GetByte()
        {
            byte Dst = GetPointer()[0];

            UpdatePointer(sizeof(byte));

            return Dst;
        }

        public bool GetUShort(ref ushort Dst)
        {
            if (!IsValid(sizeof(ushort))) return false;

            Dst = GetUShort();

            return true;
        }

        public ushort GetUShort()
        {
            var pointer = GetPointer();

            var val = ((pointer[1] << 8) | (pointer[0]));

            ushort Dst = BitConverter.ToUInt16(BitConverter.GetBytes(val), 0);

            UpdatePointer(sizeof(ushort));

            return Dst;
        }

        public bool GetUShortReverse(ref ushort Dst)
        {
            if (!IsValid(sizeof(ushort))) return false;

            Dst = GetUShortReverse();

            return true;
        }

        public ushort GetUShortReverse()
        {
            var pointer = GetPointer();

            var val = ((pointer[0] << 8) | (pointer[1]));

            ushort Dst = BitConverter.ToUInt16(BitConverter.GetBytes(val), 0);

            UpdatePointer(sizeof(ushort));

            return Dst;
        }

        public bool GetShort(ref short Dst)
        {
            if (!IsValid(sizeof(short))) return false;

            Dst = GetShort();

            return true;
        }

        public short GetShort()
        {
            var pointer = GetPointer();

            var val = ((pointer[1] << 8) | (pointer[0]));

            short Dst = BitConverter.ToInt16(BitConverter.GetBytes(val), 0);

            UpdatePointer(sizeof(short));

            return Dst;
        }

        public bool GetInt(ref int Dst)
        {
            if (!IsValid(sizeof(int))) return false;

            Dst = GetInt();

            return true;
        }

        public int GetInt()
        {
            var pointer = GetPointer();

            var val = (pointer[3] << 24) | (pointer[2] << 16) | (pointer[1] << 8) | pointer[0];

            // intはキャスト不要
            int Dst = val;

            UpdatePointer(sizeof(int));

            return Dst;
        }

        public bool GetFloat(ref float Dst)
        {
            if (!IsValid(sizeof(float))) return false;

            Dst = GetFloat();

            return true;
        }

        public float GetFloat()
        {
            var pointer = GetPointer();

            var val = (pointer[3] << 24) | (pointer[2] << 16) | (pointer[1] << 8) | (pointer[0]);

            float Dst = MemoryMarshal.Cast<int, float>(MemoryMarshal.CreateSpan(ref val, 1))[0];

            UpdatePointer(sizeof(float));

            return Dst;
        }

        public bool GetString(ref string Dst, int byteSize)
        {
            if (!IsValid(byteSize)) return false;

            byte[] bin = memcpy(byteSize);

            Dst = System.Text.Encoding.UTF8.GetString(bin);

            UpdatePointer(byteSize);

            return true;
        }

        public bool GetUTF16String(ref string Dst, int byteSize)
        {
            if (!IsValid(byteSize)) return false;

            byte[] bin = memcpy(byteSize);

            Dst = System.Text.Encoding.Unicode.GetString(bin);

            UpdatePointer(byteSize);

            return true;
        }
    }
}

