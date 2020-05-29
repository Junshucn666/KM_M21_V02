using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Common
{
    public class IniFileCls
    {
        [DllImport("kernel32")]
        private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, byte[] retVal, int size, string filePath);

        private string _filePath;

        public IniFileCls(string filePath)
        {
            _filePath = filePath;
        }

        public bool WriteToFile(string section, string key, string val)
        {
            return WritePrivateProfileString(section, key, val, _filePath);
        }

        public string ReadFromFile(string section, string key, string def)
        {
            string res = "";
            Byte[] Buffer = new Byte[1024];
            int bufLen = GetPrivateProfileString(section, key, def, Buffer, 1024, _filePath);
            res = Encoding.GetEncoding(0).GetString(Buffer);
            res = res.Substring(0, bufLen).Trim();
            return res;
        }

        //public string ReadFromFileDecDes(string section, string key, string def)
        //{
        //    string res = ReadFromFile(section, key, "");
        //    res = Encrypt.DecryptDES(res);
        //    return res;
        //}

        //public bool WriteToFileEncDes(string section, string key, string val)
        //{
        //    val = Encrypt.EncryptDES(val);
        //    return WriteToFile(section, key, val);
        //}

        public string[] GetStringArray(string section, string key, char split)
        {
            string[] res = null;
            string temp = ReadFromFile(section, key, "");
            if (temp != string.Empty)
            {
                res = temp.Split(new char[] { split }, StringSplitOptions.RemoveEmptyEntries);
            }
            return res;
        }

    }

    public static class MyConvertion
    {
        public static sbyte[] StringToSbyteArray(string str)
        {
            return CharArrayToSbyteArray(str.ToCharArray());
        }

        public static sbyte[] CharArrayToSbyteArray(char[] chAry)
        {
            sbyte[] res = new sbyte[chAry.Length];
            for (int i = 0; i < chAry.Length; i++)
            {
                res[i] = (sbyte)chAry[i];
            }
            return res;
        }

        public static string SbyteArrayToString(sbyte[] sby)
        {
            byte[] chAry = new byte[sby.Length];
            for (int i = 0; i < sby.Length; i++)
            {
                chAry[i] = (byte)sby[i];
            }
            string res = Encoding.Default.GetString(chAry);
            res = res.Trim('\0');
            return res;
        }

        public static void CopyStringToArray(string res, sbyte[] des, int len)
        {
            if (des == null)
            {
                des = new sbyte[len];
            }
            char[] c_res = res.ToCharArray();
            int forlen = 0;
            if (len > des.Length)
            {
                len = des.Length;
            }
            for (int i = 0; i < len; i++)
                des[i] = 0;


            if (len > c_res.Length)
            {
                forlen = c_res.Length;
            }
            else
            {
                forlen = len;
            }

            for (int i = 0; i < forlen; i++)
            {
                des[i] = (sbyte)c_res[i];
            }
            for (int i = forlen; i < len; i++)
            {
                des[i] = 0;
            }
        }

        public static void CopyStringToArray(string res, char[] des, int len)
        {
            if (des == null)
            {
                des = new char[len];
            }
            char[] c_res = res.ToCharArray();
            int forlen = 0;
            if (len > des.Length)
            {
                len = des.Length;
            }
            for (int i = 0; i < len; i++)
                des[i] = '\0';


            if (len > c_res.Length)
            {
                forlen = c_res.Length;
            }
            else
            {
                forlen = len;
            }

            for (int i = 0; i < forlen; i++)
            {
                des[i] = c_res[i];
            }
            for (int i = forlen; i < len; i++)
            {
                des[i] = '\0';
            }
        }

        public static void CopyStringToArray(string res, byte[] des, int len)
        {
            if (des == null)
            {
                des = new byte[len];
            }
            char[] c_res = res.ToCharArray();
            int forlen = 0;
            if (len > des.Length)
            {
                len = des.Length;
            }
            for (int i = 0; i < len; i++)
                des[i] = 0;


            if (len > c_res.Length)
            {
                forlen = c_res.Length;
            }
            else
            {
                forlen = len;
            }

            for (int i = 0; i < forlen; i++)
            {
                des[i] = (byte)c_res[i];
            }
            for (int i = forlen; i < len; i++)
            {
                des[i] = 0;
            }
        }

        public static void CopyStringToArray(string res, sbyte[] des)
        {
            int forlen = res.Length < des.Length ? res.Length : des.Length;
            for (int i = 0; i < forlen; i++)
            {
                des[i] = (sbyte)res[i];
            }
        }

        public static void CopyStringToArrayByIndex(string res, sbyte[] des, int startIndex)
        {
            int forlen = res.Length;
            if (des.Length - startIndex < res.Length)
            {
                forlen = des.Length - startIndex;
            }

            for (int i = 0; i < forlen; i++)
            {
                des[i + startIndex] = (sbyte)res[i];
            }
        }
    }
}
