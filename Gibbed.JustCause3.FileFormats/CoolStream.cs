/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Gibbed.JustCause3.FileFormats
{
    public class CoolStream : Stream
    {
        private readonly CoolArchiveFile _Archive;
        private readonly Stream _Stream;
        private readonly byte[] _CurrentChunkBytes;
        private int _CurrentChunkIndex;
        private int _CurrentChunkSize;
        private long _Position;

        public CoolStream(CoolArchiveFile archive, Stream stream)
        {
            this._Archive = archive;
            this._Stream = stream;
            this._CurrentChunkBytes = new byte[archive.BlockSize];
            this._CurrentChunkIndex = -1;
            this._CurrentChunkSize = 0;
            this._Position = 0;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Length
        {
            get { return this._Archive.TotalUncompressedSize; }
        }

        public override long Position
        {
            get { return this._Position; }
            set { this._Position = value; }
        }

        private int ReadChunk(int chunkIndex, int chunkOffset, byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (chunkIndex != this._CurrentChunkIndex)
            {
                var chunkInfo = this._Archive.ChunkInfos[chunkIndex];
                this._Stream.Position = chunkInfo.DataOffset;

                var zlib = new InflaterInputStream(this._Stream, new Inflater(true));
                var read = zlib.Read(this._CurrentChunkBytes, 0, (int)chunkInfo.UncompressedSize);
                if (read != chunkInfo.UncompressedSize)
                {
                    throw new InvalidOperationException();
                }

                this._CurrentChunkIndex = chunkIndex;
                this._CurrentChunkSize = read;
            }

            if (chunkOffset > this._CurrentChunkSize)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (chunkOffset + count > this._CurrentChunkSize)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            Array.Copy(this._CurrentChunkBytes, chunkOffset, buffer, offset, count);
            return count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var blockSize = (int)this._Archive.BlockSize;
            var chunkIndex = (int)(this._Position / blockSize);

            var remaining = count;
            while (remaining > 0)
            {
                if (chunkIndex != 0)
                {
                }

                var chunkOffset = (int)(this._Position % blockSize);
                var chunkSize = Math.Min(remaining, blockSize);
                var read = this.ReadChunk(chunkIndex, chunkOffset, buffer, offset, chunkSize);
                if (read != chunkSize)
                {
                    throw new InvalidOperationException();
                }
                remaining -= chunkSize;
                this._Position += chunkSize;
                chunkIndex++;
            }
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                {
                    this._Position = offset;
                    break;
                }

                case SeekOrigin.Current:
                {
                    this._Position += offset;
                    break;
                }

                case SeekOrigin.End:
                {
                    this._Position = this._Archive.TotalUncompressedSize + offset;
                    break;
                }

                default:
                {
                    throw new NotSupportedException();
                }
            }

            return this._Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
