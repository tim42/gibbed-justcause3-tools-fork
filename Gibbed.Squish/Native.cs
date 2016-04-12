using System;
using System.Runtime.InteropServices;

namespace Gibbed.Squish
{
	public sealed class Native
	{
        public enum Flags
        {
            None = 0,

            /// <summary>
            /// Use DXT1 compression.
            /// </summary>
            DXT1 = 1 << 0,
            
            /// <summary>
            /// Use DXT3 compression.
            /// </summary>
            DXT3 = 1 << 1,

            /// <summary>
            /// Use DXT5 compression.
            /// </summary>
            DXT5 = 1 << 2,

            /// <summary>
            /// Use a slow but high quality colour compressor (the default).
            /// </summary>
            ColourClusterFit = 1 << 3,
            
            /// <summary>
            /// Use a fast but low quality colour compressor.
            /// </summary>
            ColourRangeFit = 1 << 4,

            /// <summary>
            /// Use a perceptual metric for colour error (the default).
            /// </summary>
            ColourMetricPerceptual = 1 << 5,
            
            /// <summary>
            /// Use a uniform metric for colour error.
            /// </summary>
            ColourMetricUniform = 1 << 6,

            /// <summary>
            /// Weight the colour by alpha during cluster fit (disabled by default).
            /// </summary>
            WeightColourByAlpha = 1 << 7,

            /// <summary>
            /// Use a very slow but very high quality colour compressor.
            /// </summary>
            ColourIterativeClusterFit = 1 << 8,
        }

		private	static bool	Is64Bit()
		{
			return Marshal.SizeOf(IntPtr.Zero) == 8; 
		}

		private sealed class Native32
		{
			[DllImport("squish_32.dll", EntryPoint = "SquishCompressImage", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void CompressImage([MarshalAs(UnmanagedType.LPArray)] byte[] rgba, int width, int height, [MarshalAs(UnmanagedType.LPArray)] byte[] blocks, int flags);

            [DllImport("squish_32.dll", EntryPoint = "SquishDecompressImage", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void DecompressImage([MarshalAs(UnmanagedType.LPArray)] byte[] rgba, int width, int height, [MarshalAs(UnmanagedType.LPArray)] byte[] blocks, int flags);
		}
   
		private sealed class Native64
		{
            [DllImport("squish_64.dll", EntryPoint = "SquishCompressImage", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void CompressImage([MarshalAs(UnmanagedType.LPArray)] byte[] rgba, int width, int height, [MarshalAs(UnmanagedType.LPArray)] byte[] blocks, int flags);

            [DllImport("squish_64.dll", EntryPoint = "SquishDecompressImage", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void DecompressImage([MarshalAs(UnmanagedType.LPArray)] byte[] rgba, int width, int height, [MarshalAs(UnmanagedType.LPArray)] byte[] blocks, int flags);
		}
	
		private static void CallDecompressImage(byte[] rgba, int width, int height, byte[] blocks, int flags)
		{
            if (Is64Bit() == true)
            {
                Native64.DecompressImage(rgba, width, height, blocks, flags);
            }
            else
            {
                Native32.DecompressImage(rgba, width, height, blocks, flags);
			}
		}

		public static byte[] DecompressImage(byte[] blocks, int width, int height, Flags flags)
		{
			var pixelOutput = new byte[width * height * 4];
			CallDecompressImage(pixelOutput, width, height, blocks, (int)flags);
			return pixelOutput;
		}
	}
}
