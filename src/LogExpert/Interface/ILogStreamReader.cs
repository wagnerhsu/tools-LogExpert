﻿using System;
using System.Text;

namespace LogExpert
{
    public interface ILogStreamReader : IDisposable
    {
        #region Properties

        long Position { get; set; }

        bool IsBufferComplete { get; }

        Encoding Encoding { get; }

        #endregion

        #region Public methods

        int ReadChar();
        string ReadLine();

        #endregion
    }
}