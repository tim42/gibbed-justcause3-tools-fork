using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Gibbed.IO;

namespace Gibbed.JustCause3.RenderBlockModel
{
    internal static class StreamHelpers
    {
        public static void ReadFaces(this Stream input, List<short> faces, Endian endian)
        {
            if (faces == null)
            {
                throw new ArgumentNullException("value");
            }

            var count = input.ReadValueS32(endian);
            Debug.Assert(count >= 0);
            faces.Clear();
            faces.Capacity = count;
            for (int i = 0; i < count; i++)
            {
                faces.Add(input.ReadValueS16(endian));
            }
        }

        public static void WriteFaces(this Stream output, List<short> faces, Endian endian)
        {
            output.WriteValueS32(faces.Count, endian);
            foreach (var face in faces)
            {
                output.WriteValueS16(face, endian);
            }
        }

        public static void ReadArray<TType>(this Stream input, List<TType> array, Endian endian)
            where TType: IFormat, new()
        {
            if (array == null)
            {
                throw new ArgumentNullException("value");
            }

            var count = input.ReadValueS32(endian);
            Debug.Assert(count >= 0);
            array.Clear();
            Console.WriteLine("cap: {0} vs {1}", array.Capacity, count);
            array.Capacity = count;
            for (int i = 0; i < count; i++)
            {
                var item = new TType();
                item.Deserialize(input, endian);
                array.Add(item);
            }
        }

        public static void ReadArray<TType>(this Stream input, int count, List<TType> array, Endian endian)
            where TType : IFormat, new()
        {
            if (array == null)
            {
                throw new ArgumentNullException("value");
            }

            Debug.Assert(count >= 0);
            array.Clear();
            array.Capacity = count;
            for (int i = 0; i < count; i++)
            {
                var item = new TType();
                item.Deserialize(input, endian);
                array.Add(item);
            }
        }

        public static void WriteArray<TType>(this Stream output, List<TType> array, Endian endian)
            where TType: IFormat
        {
            output.WriteValueS32(array.Count, endian);
            foreach (var item in array)
            {
                Debug.Assert(item != null);
                item.Serialize(output, endian);
            }
        }
    }
}
