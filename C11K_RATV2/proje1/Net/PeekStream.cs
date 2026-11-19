// Dosya: Net/PeekStream.cs
using System;
using System.Collections.Generic;
using System.IO;

namespace App.Service
{
    public sealed class PeekStream : Stream
    {
        readonly Stream _s;
        readonly Queue<byte> _buf = new Queue<byte>();

        public PeekStream(Stream s) { _s = s; }

        public int PeekByte()
        {
            if (_buf.Count == 0)
            {
                int x = _s.ReadByte();
                if (x < 0) return -1;
                _buf.Enqueue((byte)x);
            }
            return _buf.Peek();
        }

        public byte[] Peek(int n)
        {
            while (_buf.Count < n)
            {
                int x = _s.ReadByte();
                if (x < 0) break;
                _buf.Enqueue((byte)x);
            }
            byte[] r = _buf.ToArray();
            if (r.Length > n) Array.Resize(ref r, n);
            return r;
        }

        public override int ReadByte()
        {
            if (_buf.Count > 0) return _buf.Dequeue();
            return _s.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count <= 0) return 0;
            int done = 0;
            while (done < count && _buf.Count > 0) { buffer[offset + done] = _buf.Dequeue(); done++; }
            if (done == count) return done;
            int x = _s.Read(buffer, offset + done, count - done);
            return x <= 0 ? done : done + x;
        }

        public override void Write(byte[] buffer, int offset, int count) { _s.Write(buffer, offset, count); }
        public override void Flush() { _s.Flush(); }
        public override bool CanRead { get { return true; } }
        public override bool CanWrite { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override long Seek(long o, SeekOrigin s) { throw new NotSupportedException(); }
        public override void SetLength(long v) { throw new NotSupportedException(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { try { _s.Dispose(); } catch { } }
            base.Dispose(disposing);
        }
    }
}