﻿using LogExpert;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace RegexColumnizer
{
    public class RegexColumnizerConfig
    {
        #region Properties

        public string Expression { get; set; } = "(?<text>.*)";

        public string Name { get; set; }

        #endregion
    }

    public abstract class BaseRegexColumnizer : ILogLineColumnizer, IColumnizerConfigurator
    {
        #region Fields

        private readonly XmlSerializer xml = new XmlSerializer(typeof(RegexColumnizerConfig));
        private string[] columns;

        #endregion

        #region Properties

        public RegexColumnizerConfig Config { get; private set; }
        public Regex Regex { get; private set; }

        #endregion

        #region Public methods

        public string GetName()
        {
            if (Config == null || string.IsNullOrWhiteSpace(Config.Name))
            {
                return GetNameInternal();
            }

            return Config.Name;
        }
        public string GetDescription() => "Columns are filled by regular expression named capture groups";
        public int GetColumnCount() => columns.Length;
        public string[] GetColumnNames() => columns;

        public IColumnizedLogLine SplitLine(ILogLineColumnizerCallback callback, ILogLine line)
        {
            ColumnizedLogLine logLine = new ColumnizedLogLine();

            logLine.ColumnValues = new IColumn[columns.Length];
            if (Regex != null)
            {
                var m = Regex.Match(line.FullLine);

                if (m.Success)
                {
                    for (int i = m.Groups.Count - 1; i > 0; i--)
                    {
                        logLine.ColumnValues[i - 1] = new Column
                        {
                            Parent = logLine,
                            FullValue = m.Groups[i].Value
                        };
                    }
                }
                else
                {
                    //Move non matching lines in the last column
                    logLine.ColumnValues[columns.Length - 1] = new Column
                    {
                        Parent = logLine,
                        FullValue = line.FullLine
                    };
                }
            }
            else
            {
                IColumn colVal = new Column
                {
                    Parent = logLine,
                    FullValue = line.FullLine
                };

                logLine.ColumnValues[0] = colVal;
            }

            logLine.LogLine = line;
            return logLine;
        }

        public bool IsTimeshiftImplemented() => false;

        public void SetTimeOffset(int msecOffset)
        {
            throw new NotImplementedException();
        }

        public int GetTimeOffset()
        {
            throw new NotImplementedException();
        }

        public DateTime GetTimestamp(ILogLineColumnizerCallback callback, ILogLine line)
        {
            throw new NotImplementedException();
        }

        public void PushValue(ILogLineColumnizerCallback callback, int column, string value, string oldValue)
        {
            throw new NotImplementedException();
        }

        public void Configure(ILogLineColumnizerCallback callback, string configDir)
        {
            RegexColumnizerConfigDialog d = new RegexColumnizerConfigDialog {Config = Config};
            if (d.ShowDialog() == DialogResult.OK)
            {
                var configFile = GetConfigFile(configDir);
                using (var w = new FileStream(configFile, FileMode.Create))
                {
                    xml.Serialize(w, d.Config);
                }

                Init(d.Config);
            }
        }

        public void LoadConfig(string configDir)
        {
            var configFile = GetConfigFile(configDir);
            RegexColumnizerConfig config;
            if (!File.Exists(configFile))
            {
                config = new RegexColumnizerConfig()
                {
                    Name = GetName()
                };
            }
            else
            {
                using (var reader = new StreamReader(configFile))
                {
                    config = xml.Deserialize(reader) as RegexColumnizerConfig;
                }
            }

            Init(config);
        }

        public string GetConfigFile(string configDir)
        {
            var name = GetType().Name;
            string configPath = Path.Combine(configDir, name);
            configPath = Path.ChangeExtension(configPath, "xml");
            return configPath;
        }

        #endregion

        /// <summary>
        /// ToString, this is displayed in the columnizer picker combobox only in the FilterSelectionDialog
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return GetName();
        }

        #region Private Methods

        protected abstract string GetNameInternal();

        private void Init(RegexColumnizerConfig config)
        {
            Config = config;

            try
            {
                Regex = new Regex(Config.Expression, RegexOptions.Compiled);
                int skip = Regex.GetGroupNames().Length == 1 ? 0 : 1;
                columns = Regex.GetGroupNames().Skip(skip).ToArray();
            }
            catch
            {
                Regex = null;
            }
        }

        #endregion
    }

    public class Regex1Columnizer : BaseRegexColumnizer
    {
        protected override string GetNameInternal() => "Regex1";
    }

    public class Regex2Columnizer : BaseRegexColumnizer
    {
        protected override string GetNameInternal() => "Regex2";
    }

    public class Regex3Columnizer : BaseRegexColumnizer
    {
        protected override string GetNameInternal() => "Regex3";
    }

    public class Regex4Columnizer : BaseRegexColumnizer
    {
        protected override string GetNameInternal() => "Regex4";
    }

    public class Regex5Columnizer : BaseRegexColumnizer
    {
        protected override string GetNameInternal() => "Regex5";
    }

    public class Regex6Columnizer : BaseRegexColumnizer
    {
        protected override string GetNameInternal() => "Regex6";
    }

    public class Regex7Columnizer : BaseRegexColumnizer
    {
        protected override string GetNameInternal() => "Regex7";
    }

    public class Regex8Columnizer : BaseRegexColumnizer
    {
        protected override string GetNameInternal() => "Regex8";
    }

    public class Regex9Columnizer : BaseRegexColumnizer
    {
        protected override string GetNameInternal() => "Regex9";
    }
}