﻿using System.IO;

namespace LogExpert.Classes.Log
{
    /// <summary>
    /// This class is responsible for reading line from the log file. It also decodes characters with the appropriate charset encoding.
    /// PositionAwareStreamReaderSystem tries a BOM detection to determine correct file offsets when directly seeking into the file (on re-loading flushed buffers).
    /// UTF-8 handling is a bit slower, because after reading a character the byte length of the character must be determined.
    /// Lines are read char-by-char. StreamReader.ReadLine() is not used because StreamReader cannot tell a file position.
    /// </summary>
    public class PositionAwareStreamReaderSystem : PositionAwareStreamReaderBase
    {
        #region Fields

        private const int CHAR_CR = 0x0D;
        private const int CHAR_LF = 0x0A;

        private int _newLineSequenceLength;

        #endregion

        #region cTor

        public PositionAwareStreamReaderSystem(Stream stream, EncodingOptions encodingOptions)
            : base(stream, encodingOptions)
        {
            
        }

        #endregion

        #region Public methods

        public override string ReadLine()
        {
            StreamReader reader = GetStreamReader();

            if (_newLineSequenceLength == 0)
            {
                _newLineSequenceLength = guessNewLineSequenceLength(reader);
            }

            string line = reader.ReadLine();

            if (line != null)
            {
                MovePosition(Encoding.GetByteCount(line) + _newLineSequenceLength);

                if (line.Length > MAX_LINE_LEN)
                {
                    line = line.Remove(MAX_LINE_LEN);
                }
            }

            return line;
        }

        #endregion

        #region Private Methods

        private int guessNewLineSequenceLength(StreamReader reader)
        {
            long currentPos = Position;

            try
            {
                string line = reader.ReadLine();

                if (line != null)
                {
                    Position += Encoding.GetByteCount(line);
                    
                    int firstChar = reader.Read();
                    if (firstChar == CHAR_CR) // check \r
                    {
                        int secondChar = reader.Read();
                        if (secondChar == CHAR_LF) // check \n
                        {
                            return Encoding.GetByteCount("\r\n");
                        }
                    }
                    return Encoding.GetByteCount(((char)firstChar).ToString());
                }

                return 0;
            }
            finally
            {
                Position = currentPos;
            }
        }

        #endregion
    }
}