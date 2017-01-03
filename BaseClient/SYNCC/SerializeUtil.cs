using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SYNCC
{
    public class SerializeUtil
    {
        public static byte[] encode(object value)
        {
            MemoryStream ms = new MemoryStream();
            //BinaryFormatter bf = new BinaryFormatter();
            //bf.Serialize(ms, value);
            //CompactFormatter.CompactFormatter ser = new CompactFormatter.CompactFormatter();
            //ser.Serialize(ms, value);
            CompactFormatter.CompactFormatterPlus CFormatterPlus = new CompactFormatter.CompactFormatterPlus();
            CFormatterPlus.Serialize(ms, value);
            byte[] result = new byte[ms.Length];
            Buffer.BlockCopy(ms.GetBuffer(), 0, result, 0, (int)ms.Length);
            return result;
        }

        public static object decoder(byte[] value)
        {
            MemoryStream ms = new MemoryStream(value);
            //BinaryFormatter bf = new BinaryFormatter();
            //return bf.Deserialize(ms);
           // CompactFormatter.CompactFormatter ser = new CompactFormatter.CompactFormatter();
            CompactFormatter.CompactFormatterPlus CFormatterPlus = new CompactFormatter.CompactFormatterPlus();
            return (Object)CFormatterPlus.Deserialize(ms);
        }
    }
}
