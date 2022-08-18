﻿using System.IO;
using LogExpert.Entities;

namespace LogExpert.Classes.Log
{
    public class PositionAwareStreamReaderLegacy : PositionAwareStreamReaderBase
    {
        #region Fields

        private readonly char[] _charBuffer = new char[MaxLineLen];

        private int _charBufferPos;
        private bool _crDetect;

        #endregion

        #region cTor

        public PositionAwareStreamReaderLegacy(Stream stream, EncodingOptions encodingOptions) : base(stream, encodingOptions)
        {

        }

        #endregion

        #region Public methods

        public override string ReadLine()
        {
            int readInt;

            while (-1 != (readInt = ReadChar()))
            {
                char readChar = (char)readInt;

                switch (readChar)
                {
                    case '\n':
                    {
                        _crDetect = false;
                        return GetLineAndResetCharBufferPos();
                    }
                    case '\r':
                    {
                        if (_crDetect)
                        {
                            return GetLineAndResetCharBufferPos();
                        }

                        _crDetect = true;
                        break;
                    }
                    default:
                    {
                        if (_crDetect)
                        {
                            _crDetect = false;
                            string line = GetLineAndResetCharBufferPos();
                            AppendToCharBuffer(readChar);
                            return line;
                        }

                        AppendToCharBuffer(readChar);
                        break;
                    }
                }
            }

            string result = GetLineAndResetCharBufferPos();
            if (readInt == -1 && result.Length == 0 && !_crDetect)
            {
                return null; // EOF
            }
            _crDetect = false;
            return result;
        }

        protected override void ResetReader()
        {
            ResetCharBufferPos();

            base.ResetReader();
        }

        #endregion

        #region Private Methods

        private string GetLineAndResetCharBufferPos()
        {
            string result = new string(_charBuffer, 0, _charBufferPos);
            ResetCharBufferPos();
            return result;
        }

        private void AppendToCharBuffer(char readChar)
        {
            if (_charBufferPos < MaxLineLen)
            {
                _charBuffer[_charBufferPos++] = readChar;
            }
        }

        private void ResetCharBufferPos()
        {
            _charBufferPos = 0;
        }

        #endregion
    }
}