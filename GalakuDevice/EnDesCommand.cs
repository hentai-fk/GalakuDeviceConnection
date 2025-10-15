using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButtplugIo.GalakuDevice
{
    public class EnDesCommand
    {
        // 使用 unchecked 块来初始化 KeyTab，确保 Java 中的负值被正确转换为 C# byte 的无符号值。
        public static readonly byte[][] KeyTab = unchecked(new byte[][]
        {
            // Java: { 0, 24, -104, -9, -91, 61, 13, 41, 37, 80, 68, 70 }
            new byte[] { 0, 24, (byte)-104, (byte)-9, (byte)-91, 61, 13, 41, 37, 80, 68, 70 },
        
            // Java: { 0, 69, 110, 106, 111, 120, 32, 83, 45, 49, 46, 55 }
            new byte[] { 0, 69, 110, 106, 111, 120, 32, 83, 45, 49, 46, 55 },
        
            // Java: { 0, 101, 120, 32, 84, 111, 121, 115, 10, -114, -99, -93 }
            new byte[] { 0, 101, 120, 32, 84, 111, 121, 115, 10, (byte)-114, (byte)-99, (byte)-93 },
        
            // Java: { 0, -59, -42, -25, -8, 10, 50, 32, 111, 98, 13, 10 }
            new byte[] { 0, (byte)-59, (byte)-42, (byte)-25, (byte)-8, 10, 50, 32, 111, 98, 13, 10 }
        });

        /// <summary>
        /// 计算字节数组前 i 个元素的校验和。
        /// 模拟 Java 的有符号字节加法。
        /// </summary>
        public static int CalcCheckSum(byte[] bArr, int i)
        {
            int i2 = 0;
            for (int i3 = 0; i3 < i; i3++)
            {
                // 在 C# 中，将 byte 转换为有符号的 int (sbyte) 以模拟 Java 的有符号字节加法。
                i2 += (sbyte)bArr[i3];
            }
            return i2;
        }

        /// <summary>
        /// 生成 [i, i2] 范围内的随机数。
        /// </summary>
        public static int GetRandomNumber(int i, int i2)
        {
            Random random = new Random();
            return random.Next(i, i2 + 1);
        }

        /// <summary>
        /// 根据传入的字节值，从 KeyTab 中获取对应的密钥字节。
        /// </summary>
        public static byte GetTabKey(byte b, int i)
        {
            return KeyTab[b & 3][i];
        }

        /// <summary>
        /// 加密方法。
        /// </summary>
        public static byte[] Encrypt(byte[] bArr)
        {
            if (bArr == null || bArr.Length < 12)
            {
                throw new ArgumentException("Input array must be at least 12 bytes long.");
            }

            byte[] bArr2 = new byte[12];
            byte b = bArr[0];
            bArr2[0] = b;

            // 使用 unchecked 块包围循环，以确保中间的字节运算模拟 Java 的溢出行为
            unchecked
            {
                for (int i = 1; i < 12; i++)
                {
                    byte lastByte = bArr2[i - 1];
                    byte tabKey = GetTabKey(lastByte, i);

                    // Java 逻辑: (byte) (((GetTabKey ^ b) ^ bArr[i]) + GetTabKey)
                    // 核心: (A ^ B ^ C) + A

                    bArr2[i] = (byte)((byte)((tabKey ^ b) ^ bArr[i]) + tabKey);
                }
            }
            return bArr2;
        }

        /// <summary>
        /// 解密方法。
        /// </summary>
        public static byte[] Decrypt(byte[] bArr)
        {
            if (bArr == null || bArr.Length < 12)
            {
                throw new ArgumentException("Input array must be at least 12 bytes long.");
            }

            byte[] bArr2 = new byte[12];
            byte b = bArr[0];
            bArr2[0] = b;

            // 使用 unchecked 块包围循环，以确保中间的字节运算模拟 Java 的溢出行为
            unchecked
            {
                for (int i = 1; i < 12; i++)
                {
                    byte lastEncryptedByte = bArr[i - 1];
                    byte tabKey = GetTabKey(lastEncryptedByte, i);

                    // Java 逻辑: (byte) (GetTabKey ^ ((bArr[i] - GetTabKey) ^ b))
                    // 核心: A ^ ((B - A) ^ C)

                    byte diff = (byte)(bArr[i] - tabKey);
                    byte xorResult = (byte)(diff ^ b);

                    bArr2[i] = (byte)(tabKey ^ xorResult);
                }
            }
            return bArr2;
        }

        /// <summary>
        /// 组装命令，计算校验和并加密。
        /// </summary>
        public static byte[] SendBytes(byte[] bArr, int i)
        {
            if (bArr == null || i > 10)
            {
                throw new ArgumentException("Command body length is too long or array is null.");
            }

            byte[] bArr2 = new byte[12];

            bArr2[0] = (byte)35;

            // 拷贝命令体
            int i2 = 0;
            while (i2 < i)
            {
                bArr2[i2 + 1] = bArr[i2];
                i2++;
            }

            // 计算并设置校验和 (模拟 Java byte 溢出，校验和结果自动被截断)
            bArr2[11] = (byte)CalcCheckSum(bArr2, 11);

            // 加密并返回
            return Encrypt(bArr2);
        }
    }
}
