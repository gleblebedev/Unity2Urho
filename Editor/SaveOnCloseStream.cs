using System;
using System.IO;
using System.Text;
using System.Xml;

namespace UnityToCustomEngineExporter.Editor
{
    public class SaveOnCloseStream: Stream
    {
        private readonly string _fileName;
        private readonly MemoryStream _buffer;

        public SaveOnCloseStream(string fileName)
        {
            _fileName = fileName;
            _buffer = new MemoryStream();
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                using (var s = File.Open(_fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    _buffer.Position = 0;
                    _buffer.CopyTo(s);
                }
            }
        }

        public override void Flush()
        {
            _buffer.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _buffer.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _buffer.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _buffer.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _buffer.Write(buffer, offset, count);
        }

        public override bool CanRead => _buffer.CanRead;
        public override bool CanSeek => _buffer.CanSeek;
        public override bool CanWrite => _buffer.CanWrite;
        public override long Length => _buffer.Length;

        public override long Position
        {
            get => _buffer.Position;
            set => _buffer.Position = value;
        }
    }
}
