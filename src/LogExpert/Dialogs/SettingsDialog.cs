using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using LogExpert.Classes.Columnizer;

namespace LogExpert.Dialogs
{
    public partial class SettingsDialog : Form
    {
        #region Fields

        private readonly Image _emptyImage = new Bitmap(16, 16);
        private readonly LogTabWindow _logTabWin;

        private ILogExpertPluginConfigurator _selectedPlugin;
        private ToolEntry _selectedTool;

        #endregion

        #region cTor

        public SettingsDialog(Preferences prefs, LogTabWindow logTabWin)
        {
            Preferences = prefs;
            _logTabWin = logTabWin;
            InitializeComponent();
        }

        public SettingsDialog(Preferences prefs, LogTabWindow logTabWin, int tabToOpen) : this(prefs, logTabWin)
        {
            tabControlSettings.SelectedIndex = tabToOpen;
        }

        #endregion

        #region Properties

        public Preferences Preferences { get; private set; }

        #endregion

        #region Private Methods

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private void FillDialog()
        {
            if (Preferences == null)
            {
                Preferences = new Preferences();
            }

            if (Preferences.fontName == null)
            {
                Preferences.fontName = "Courier New";
            }

            if (Math.Abs(Preferences.fontSize) < 0.1)
            {
                Preferences.fontSize = 9.0f;
            }

            FillPortableMode();

            checkBoxTimestamp.Checked = Preferences.timestampControl;
            checkBoxSyncFilter.Checked = Preferences.filterSync;
            checkBoxFilterTail.Checked = Preferences.filterTail;
            checkBoxFollowTail.Checked = Preferences.followTail;

            radioButtonHorizMouseDrag.Checked = Preferences.timestampControlDragOrientation == DateTimeDragControl.DragOrientations.Horizontal;
            radioButtonVerticalMouseDrag.Checked = Preferences.timestampControlDragOrientation == DateTimeDragControl.DragOrientations.Vertical;
            radioButtonVerticalMouseDragInverted.Checked = Preferences.timestampControlDragOrientation == DateTimeDragControl.DragOrientations.InvertedVertical;

            checkBoxSingleInstance.Checked = Preferences.allowOnlyOneInstance;
            checkBoxOpenLastFiles.Checked = Preferences.openLastFiles;
            checkBoxTailState.Checked = Preferences.showTailState;
            checkBoxColumnSize.Checked = Preferences.setLastColumnWidth;
            cpDownColumnWidth.Enabled = Preferences.setLastColumnWidth;

            if (Preferences.lastColumnWidth != 0)
            {
                if (Preferences.lastColumnWidth < cpDownColumnWidth.Minimum)
                {
                    Preferences.lastColumnWidth = (int) cpDownColumnWidth.Minimum;
                }

                if (Preferences.lastColumnWidth > cpDownColumnWidth.Maximum)
                {
                    Preferences.lastColumnWidth = (int) cpDownColumnWidth.Maximum;
                }

                cpDownColumnWidth.Value = Preferences.lastColumnWidth;
            }

            checkBoxTimeSpread.Checked = Preferences.showTimeSpread;
            checkBoxReverseAlpha.Checked = Preferences.reverseAlpha;
            radioButtonTimeView.Checked = Preferences.timeSpreadTimeMode;
            radioButtonLineView.Checked = !Preferences.timeSpreadTimeMode;

            checkBoxSaveSessions.Checked = Preferences.saveSessions;

            switch (Preferences.saveLocation)
            {
                case SessionSaveLocation.OwnDir:
                {
                    radioButtonSessionSaveOwn.Checked = true;
                }
                break;
                case SessionSaveLocation.SameDir:
                {
                    radioButtonSessionSameDir.Checked = true;
                }
                break;
                case SessionSaveLocation.DocumentsDir:
                {
                    radioButtonsessionSaveDocuments.Checked = true;
                }
                break;
            }

            upDownMaximumFilterEntriesDisplayed.Value = Preferences.maximumFilterEntriesDisplayed;
            upDownMaximumFilterEntries.Value = Preferences.maximumFilterEntries;

            labelSessionSaveOwnDir.Text = Preferences.sessionSaveDirectory ?? string.Empty;
            checkBoxSaveFilter.Checked = Preferences.saveFilters;
            upDownBlockCount.Value = Preferences.bufferCount;
            upDownLinesPerBlock.Value = Preferences.linesPerBuffer;
            upDownPollingInterval.Value = Preferences.pollingInterval;
            checkBoxMultiThread.Checked = Preferences.multiThreadFilter;

            dataGridViewColumnizer.DataError += columnizerDataGridView_DataError;

            FillColumnizerList();
            FillPluginList();
            DisplayFontName();
            FillHighlightMaskList();
            FillToolListbox();
            FillMultifileSettings();
            FillEncodingList();

            encodingComboBox.SelectedItem = Encoding.GetEncoding(Preferences.defaultEncoding);
            checkBoxMaskPrio.Checked = Preferences.maskPrio;
            checkBoxAutoPick.Checked = Preferences.autoPick;
            checkBoxAskCloseTabs.Checked = Preferences.askForClose;
            checkBoxColumnFinder.Checked = Preferences.showColumnFinder;
            checkBoxLegacyReader.Checked = Preferences.useLegacyReader;
        }

        private void FillPortableMode()
        {
            checkBoxPortableMode.CheckState = File.Exists(ConfigManager.PortableMode) ? CheckState.Checked : CheckState.Unchecked;
        }

        private void DisplayFontName()
        {
            labelFont.Text = Preferences.fontName + @" " + (int) Preferences.fontSize;
            labelFont.Font = new Font(new FontFamily(Preferences.fontName), Preferences.fontSize);
        }

        private void SaveMultifileData()
        {
            if (radioButtonLoadEveryFileIntoSeperatedTab.Checked)
            {
                Preferences.multiFileOption = MultiFileOption.SingleFiles;
            }

            if (radioButtonTreatAllFilesAsOneMultifile.Checked)
            {
                Preferences.multiFileOption = MultiFileOption.MultiFile;
            }

            if (radioButtonAskWhatToDo.Checked)
            {
                Preferences.multiFileOption = MultiFileOption.Ask;
            }

            Preferences.multiFileOptions.FormatPattern = textBoxMultifilePattern.Text;
            Preferences.multiFileOptions.MaxDayTry = (int)upDownMultifileDays.Value;
        }

        private void ToolButtonClick(TextBox textBox)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(textBox.Text))
            {
                FileInfo info = new FileInfo(textBox.Text);
                if (info.Directory.Exists)
                {
                    dlg.InitialDirectory = info.DirectoryName;
                }
            }

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = dlg.FileName;
            }
        }

        private void ArgsButtonClick(TextBox textBox)
        {
            ToolArgsDialog dlg = new ToolArgsDialog(_logTabWin, this);
            dlg.Arg = textBox.Text;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = dlg.Arg;
            }
        }

        private void WorkingDirButtonClick(TextBox textBox)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.RootFolder = Environment.SpecialFolder.MyComputer;
            dlg.Description = @"Select a working directory";
            if (!string.IsNullOrEmpty(textBox.Text))
            {
                DirectoryInfo info = new DirectoryInfo(textBox.Text);
                if (info.Exists)
                {
                    dlg.SelectedPath = info.FullName;
                }
            }

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = dlg.SelectedPath;
            }
        }

        private void FillColumnizerForToolsList()
        {
            if (_selectedTool != null)
            {
                FillColumnizerForToolsList(comboBoxColumnizer, _selectedTool.columnizerName);
            }
        }

        private void FillColumnizerForToolsList(ComboBox comboBox, string columnizerName)
        {
            int selIndex = 0;
            comboBox.Items.Clear();
            IList<ILogLineColumnizer> columnizers = PluginRegistry.GetInstance().RegisteredColumnizers;
            foreach (ILogLineColumnizer columnizer in columnizers)
            {
                int index = comboBox.Items.Add(columnizer.GetName());
                if (columnizer.GetName().Equals(columnizerName))
                {
                    selIndex = index;
                }
            }

            //ILogLineColumnizer columnizer = Util.FindColumnizerByName(columnizerName, this.logTabWin.RegisteredColumnizers);
            //if (columnizer == null)
            //  columnizer = this.logTabWin.RegisteredColumnizers[0];
            comboBox.SelectedIndex = selIndex;
        }

        private void FillColumnizerList()
        {
            dataGridViewColumnizer.Rows.Clear();

            DataGridViewComboBoxColumn comboColumn = (DataGridViewComboBoxColumn) dataGridViewColumnizer.Columns[1];
            comboColumn.Items.Clear();

            DataGridViewTextBoxColumn textColumn = (DataGridViewTextBoxColumn) dataGridViewColumnizer.Columns[0];

            IList<ILogLineColumnizer> columnizers = PluginRegistry.GetInstance().RegisteredColumnizers;
            foreach (ILogLineColumnizer columnizer in columnizers)
            {
                comboColumn.Items.Add(columnizer.GetName());
            }
            //comboColumn.DisplayMember = "Name";
            //comboColumn.ValueMember = "Columnizer";

            foreach (ColumnizerMaskEntry maskEntry in Preferences.columnizerMaskList)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.Cells.Add(new DataGridViewTextBoxCell());
                DataGridViewComboBoxCell cell = new DataGridViewComboBoxCell();

                foreach (ILogLineColumnizer logColumnizer in columnizers)
                {
                    cell.Items.Add(logColumnizer.GetName());
                }

                row.Cells.Add(cell);
                row.Cells[0].Value = maskEntry.mask;
                ILogLineColumnizer columnizer = ColumnizerPicker.DecideColumnizerByName(maskEntry.columnizerName,
                    PluginRegistry.GetInstance().RegisteredColumnizers);

                row.Cells[1].Value = columnizer.GetName();
                dataGridViewColumnizer.Rows.Add(row);
            }

            int count = dataGridViewColumnizer.RowCount;
            if (count > 0 && !dataGridViewColumnizer.Rows[count - 1].IsNewRow)
            {
                DataGridViewComboBoxCell comboCell = (DataGridViewComboBoxCell) dataGridViewColumnizer.Rows[count - 1].Cells[1];
                comboCell.Value = comboCell.Items[0];
            }
        }

        private void FillHighlightMaskList()
        {
            dataGridViewHighlightMask.Rows.Clear();

            DataGridViewComboBoxColumn comboColumn = (DataGridViewComboBoxColumn) dataGridViewHighlightMask.Columns[1];
            comboColumn.Items.Clear();

            DataGridViewTextBoxColumn textColumn = (DataGridViewTextBoxColumn) dataGridViewHighlightMask.Columns[0];

            IList<HilightGroup> groups = _logTabWin.HilightGroupList;
            foreach (HilightGroup group in groups)
            {
                comboColumn.Items.Add(group.GroupName);
            }

            foreach (HighlightMaskEntry maskEntry in Preferences.highlightMaskList)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.Cells.Add(new DataGridViewTextBoxCell());
                DataGridViewComboBoxCell cell = new DataGridViewComboBoxCell();

                foreach (HilightGroup group in groups)
                {
                    cell.Items.Add(group.GroupName);
                }

                row.Cells.Add(cell);
                row.Cells[0].Value = maskEntry.mask;
                HilightGroup currentGroup = _logTabWin.FindHighlightGroup(maskEntry.highlightGroupName);
                if (currentGroup == null)
                {
                    currentGroup = groups[0];
                }

                if (currentGroup == null)
                {
                    currentGroup = new HilightGroup();
                }

                row.Cells[1].Value = currentGroup.GroupName;
                dataGridViewHighlightMask.Rows.Add(row);
            }

            int count = dataGridViewHighlightMask.RowCount;
            if (count > 0 && !dataGridViewHighlightMask.Rows[count - 1].IsNewRow)
            {
                DataGridViewComboBoxCell comboCell =
                    (DataGridViewComboBoxCell) dataGridViewHighlightMask.Rows[count - 1].Cells[1];
                comboCell.Value = comboCell.Items[0];
            }
        }

        private void SaveColumnizerList()
        {
            Preferences.columnizerMaskList.Clear();

            foreach (DataGridViewRow row in dataGridViewColumnizer.Rows)
            {
                if (!row.IsNewRow)
                {
                    ColumnizerMaskEntry entry = new ColumnizerMaskEntry();
                    entry.mask = (string)row.Cells[0].Value;
                    entry.columnizerName = (string)row.Cells[1].Value;
                    Preferences.columnizerMaskList.Add(entry);
                }
            }
        }

        private void SaveHighlightMaskList()
        {
            Preferences.highlightMaskList.Clear();

            foreach (DataGridViewRow row in dataGridViewHighlightMask.Rows)
            {
                if (!row.IsNewRow)
                {
                    HighlightMaskEntry entry = new HighlightMaskEntry();
                    entry.mask = (string)row.Cells[0].Value;
                    entry.highlightGroupName = (string)row.Cells[1].Value;
                    Preferences.highlightMaskList.Add(entry);
                }
            }
        }

        private void FillPluginList()
        {
            listBoxPlugin.Items.Clear();
            foreach (IContextMenuEntry entry in PluginRegistry.GetInstance().RegisteredContextMenuPlugins)
            {
                listBoxPlugin.Items.Add(entry);
                if (entry is ILogExpertPluginConfigurator configurator)
                {
                    configurator.StartConfig();
                }
            }

            foreach (IKeywordAction entry in PluginRegistry.GetInstance().RegisteredKeywordActions)
            {
                listBoxPlugin.Items.Add(entry);
                if (entry is ILogExpertPluginConfigurator configurator)
                {
                    configurator.StartConfig();
                }
            }

            foreach (IFileSystemPlugin entry in PluginRegistry.GetInstance().RegisteredFileSystemPlugins)
            {
                listBoxPlugin.Items.Add(entry);
                if (entry is ILogExpertPluginConfigurator configurator)
                {
                    configurator.StartConfig();
                }
            }

            buttonConfigPlugin.Enabled = false;
        }

        private void SavePluginSettings()
        {
            _selectedPlugin?.HideConfigForm();

            foreach (IContextMenuEntry entry in PluginRegistry.GetInstance().RegisteredContextMenuPlugins)
            {
                if (entry is ILogExpertPluginConfigurator configurator)
                {
                    configurator.SaveConfig(ConfigManager.ConfigDir);
                }
            }

            foreach (IKeywordAction entry in PluginRegistry.GetInstance().RegisteredKeywordActions)
            {
                if (entry is ILogExpertPluginConfigurator configurator)
                {
                    configurator.SaveConfig(ConfigManager.ConfigDir);
                }
            }
        }

        private void FillToolListbox()
        {
            listBoxTools.Items.Clear();
            foreach (ToolEntry tool in Preferences.toolEntries)
            {
                listBoxTools.Items.Add(tool.Clone(), tool.isFavourite);
            }

            if (listBoxTools.Items.Count > 0)
            {
                listBoxTools.SelectedIndex = 0;
            }
        }

        private void FillMultifileSettings()
        {
            switch (Preferences.multiFileOption)
            {
                case MultiFileOption.SingleFiles:
                {
                    radioButtonLoadEveryFileIntoSeperatedTab.Checked = true;
                    break;
                }
                case MultiFileOption.MultiFile:
                {
                    radioButtonTreatAllFilesAsOneMultifile.Checked = true;
                    break;
                }
                case MultiFileOption.Ask:
                {
                    radioButtonAskWhatToDo.Checked = true;
                    break;
                }
            }

            textBoxMultifilePattern.Text = Preferences.multiFileOptions.FormatPattern;
            upDownMultifileDays.Value = Preferences.multiFileOptions.MaxDayTry;
        }

        private void GetToolListBoxData()
        {
            GetCurrentToolValues();
            Preferences.toolEntries.Clear();
            for (int i = 0; i < listBoxTools.Items.Count; ++i)
            {
                Preferences.toolEntries.Add(listBoxTools.Items[i] as ToolEntry);
                (listBoxTools.Items[i] as ToolEntry).isFavourite = listBoxTools.GetItemChecked(i);
            }
        }

        private void GetCurrentToolValues()
        {
            if (_selectedTool != null)
            {
                _selectedTool.name = Util.IsNullOrSpaces(textBoxToolName.Text) ? textBoxTool.Text : textBoxToolName.Text;
                _selectedTool.cmd = textBoxTool.Text;
                _selectedTool.args = textBoxArguments.Text;
                _selectedTool.columnizerName = comboBoxColumnizer.Text;
                _selectedTool.sysout = checkBoxSysout.Checked;
                _selectedTool.workingDir = textBoxWorkingDir.Text;
            }
        }

        private void ShowCurrentToolValues()
        {
            if (_selectedTool != null)
            {
                textBoxToolName.Text = _selectedTool.name;
                textBoxTool.Text = _selectedTool.cmd;
                textBoxArguments.Text = _selectedTool.args;
                comboBoxColumnizer.Text = _selectedTool.columnizerName;
                checkBoxSysout.Checked = _selectedTool.sysout;
                comboBoxColumnizer.Enabled = _selectedTool.sysout;
                textBoxWorkingDir.Text = _selectedTool.workingDir;
            }
        }

        private void DisplayCurrentIcon()
        {
            if (_selectedTool != null)
            {
                Icon icon = Win32.LoadIconFromExe(_selectedTool.iconFile, _selectedTool.iconIndex);
                if (icon != null)
                {
                    Image image = icon.ToBitmap();
                    buttonIcon.Image = image;
                    DestroyIcon(icon.Handle);
                    icon.Dispose();
                }
                else
                {
                    buttonIcon.Image = _emptyImage;
                }
            }
        }

        private void FillEncodingList()
        {
            encodingComboBox.Items.Clear();

            encodingComboBox.Items.Add(Encoding.ASCII);
            encodingComboBox.Items.Add(Encoding.Default);
            encodingComboBox.Items.Add(Encoding.GetEncoding("iso-8859-1"));
            encodingComboBox.Items.Add(Encoding.UTF8);
            encodingComboBox.Items.Add(Encoding.Unicode);

            encodingComboBox.ValueMember = "HeaderName";
        }

        #endregion

        #region Events handler

        private void SettingsDialog_Load(object sender, EventArgs e)
        {
            FillDialog();
        }

        private void changeFontButton_Click(object sender, EventArgs e)
        {
            FontDialog dlg = new FontDialog();
            dlg.ShowEffects = false;
            dlg.AllowVerticalFonts = false;
            dlg.AllowScriptChange = false;
            dlg.Font = new Font(new FontFamily(Preferences.fontName), Preferences.fontSize);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Preferences.fontSize = dlg.Font.Size;
                Preferences.fontName = dlg.Font.FontFamily.Name;
            }

            DisplayFontName();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Preferences.timestampControl = checkBoxTimestamp.Checked;
            Preferences.filterSync = checkBoxSyncFilter.Checked;
            Preferences.filterTail = checkBoxFilterTail.Checked;
            Preferences.followTail = checkBoxFollowTail.Checked;

            if (radioButtonVerticalMouseDrag.Checked)
            {
                Preferences.timestampControlDragOrientation = DateTimeDragControl.DragOrientations.Vertical;
            }
            else if (radioButtonVerticalMouseDragInverted.Checked)
            {
                Preferences.timestampControlDragOrientation = DateTimeDragControl.DragOrientations.InvertedVertical;
            }
            else
            {
                Preferences.timestampControlDragOrientation = DateTimeDragControl.DragOrientations.Horizontal;
            }

            SaveColumnizerList();
            
            Preferences.maskPrio = checkBoxMaskPrio.Checked;
            Preferences.autoPick = checkBoxAutoPick.Checked;
            Preferences.askForClose = checkBoxAskCloseTabs.Checked;
            Preferences.allowOnlyOneInstance = checkBoxSingleInstance.Checked;
            Preferences.openLastFiles = checkBoxOpenLastFiles.Checked;
            Preferences.showTailState = checkBoxTailState.Checked;
            Preferences.setLastColumnWidth = checkBoxColumnSize.Checked;
            Preferences.lastColumnWidth = (int) cpDownColumnWidth.Value;
            Preferences.showTimeSpread = checkBoxTimeSpread.Checked;
            Preferences.reverseAlpha = checkBoxReverseAlpha.Checked;
            Preferences.timeSpreadTimeMode = radioButtonTimeView.Checked;

            Preferences.saveSessions = checkBoxSaveSessions.Checked;
            Preferences.sessionSaveDirectory = labelSessionSaveOwnDir.Text;

            if (radioButtonsessionSaveDocuments.Checked)
            {
                Preferences.saveLocation = SessionSaveLocation.DocumentsDir;
            }
            else if (radioButtonSessionSaveOwn.Checked)
            {
                Preferences.saveLocation = SessionSaveLocation.OwnDir;
            }
            else
            {
                Preferences.saveLocation = SessionSaveLocation.SameDir;
            }

            Preferences.saveFilters = checkBoxSaveFilter.Checked;
            Preferences.bufferCount = (int) upDownBlockCount.Value;
            Preferences.linesPerBuffer = (int) upDownLinesPerBlock.Value;
            Preferences.pollingInterval = (int) upDownPollingInterval.Value;
            Preferences.multiThreadFilter = checkBoxMultiThread.Checked;
            Preferences.defaultEncoding = encodingComboBox.SelectedItem != null ? (encodingComboBox.SelectedItem as Encoding).HeaderName : Encoding.Default.HeaderName;
            Preferences.showColumnFinder = checkBoxColumnFinder.Checked;
            Preferences.useLegacyReader = checkBoxLegacyReader.Checked;

            Preferences.maximumFilterEntries = (int)upDownMaximumFilterEntries.Value;
            Preferences.maximumFilterEntriesDisplayed = (int)upDownMaximumFilterEntriesDisplayed.Value;

            SavePluginSettings();
            SaveHighlightMaskList();
            GetToolListBoxData();
            SaveMultifileData();
        }

        private void toolButtonA_Click(object sender, EventArgs e)
        {
            ToolButtonClick(textBoxTool);
        }

        private void argButtonA_Click(object sender, EventArgs e)
        {
            ArgsButtonClick(textBoxArguments);
        }

        //TODO Remove or refactor this function
        private void columnizerDataGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            DataGridViewComboBoxCell comboCell = (DataGridViewComboBoxCell) dataGridViewColumnizer.Rows[e.RowIndex].Cells[1];
            if (comboCell.Items.Count > 0)
            {
//        comboCell.Value = comboCell.Items[0];
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (dataGridViewColumnizer.CurrentRow != null && !dataGridViewColumnizer.CurrentRow.IsNewRow)
            {
                int index = dataGridViewColumnizer.CurrentRow.Index;
                dataGridViewColumnizer.EndEdit();
                dataGridViewColumnizer.Rows.RemoveAt(index);
            }
        }

        private void columnizerDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void sysoutCheckBoxA_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxColumnizer.Enabled = checkBoxSysout.Checked;
        }

        private void tailColorButton_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new ColorDialog();
            dlg.Color = Preferences.showTailColor;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Preferences.showTailColor = dlg.Color;
            }
        }

        private void columnSizeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            cpDownColumnWidth.Enabled = checkBoxColumnSize.Checked;
        }

        private void timespreadColorButton_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new ColorDialog();
            dlg.Color = Preferences.timeSpreadColor;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Preferences.timeSpreadColor = dlg.Color;
            }
        }

        private void pluginListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedPlugin?.HideConfigForm();

            object o = listBoxPlugin.SelectedItem;
            
            if (o != null)
            {
                _selectedPlugin = o as ILogExpertPluginConfigurator;
                
                if (o is ILogExpertPluginConfigurator)
                {
                    if (_selectedPlugin.HasEmbeddedForm())
                    {
                        buttonConfigPlugin.Enabled = false;
                        buttonConfigPlugin.Visible = false;
                        _selectedPlugin.ShowConfigForm(panelPlugin);
                    }
                    else
                    {
                        buttonConfigPlugin.Enabled = true;
                        buttonConfigPlugin.Visible = true;
                    }
                }
            }
            else
            {
                buttonConfigPlugin.Enabled = false;
                buttonConfigPlugin.Visible = true;
            }
        }

        private void sessionSaveDirButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();

            if (Preferences.sessionSaveDirectory != null)
            {
                dlg.SelectedPath = Preferences.sessionSaveDirectory;
            }

            dlg.ShowNewFolderButton = true;
            dlg.Description = @"Choose folder for LogExpert's session files";
            
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                labelSessionSaveOwnDir.Text = dlg.SelectedPath;
            }
        }

        private void OnPortableModeCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                switch (checkBoxPortableMode.CheckState)
                {
                    case CheckState.Checked when !File.Exists(ConfigManager.PortableMode):
                    {
                        File.Create(ConfigManager.PortableMode);
                        break;
                    }
                    case CheckState.Unchecked when File.Exists(ConfigManager.PortableMode):
                    {
                        File.Delete(ConfigManager.PortableMode);
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($@"Could not create / delete marker for Portable Mode: {exception}", @"Error", MessageBoxButtons.OK);
            }
          
        }

        private void configPluginButton_Click(object sender, EventArgs e)
        {
            if (!_selectedPlugin.HasEmbeddedForm())
            {
                _selectedPlugin.ShowConfigDialog(this);
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            //TODO implement
        }   

        private void toolListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetCurrentToolValues();
            _selectedTool = listBoxTools.SelectedItem as ToolEntry;
            ShowCurrentToolValues();
            listBoxTools.Refresh();
            FillColumnizerForToolsList();
            DisplayCurrentIcon();
        }

        private void toolUpButton_Click(object sender, EventArgs e)
        {
            int i = listBoxTools.SelectedIndex;
        
            if (i > 0)
            {
                bool isChecked = listBoxTools.GetItemChecked(i);
                object item = listBoxTools.Items[i];
                listBoxTools.Items.RemoveAt(i);
                i--;
                listBoxTools.Items.Insert(i, item);
                listBoxTools.SelectedIndex = i;
                listBoxTools.SetItemChecked(i, isChecked);
            }
        }

        private void toolDownButton_Click(object sender, EventArgs e)
        {
            int i = listBoxTools.SelectedIndex;
            
            if (i < listBoxTools.Items.Count - 1)
            {
                bool isChecked = listBoxTools.GetItemChecked(i);
                object item = listBoxTools.Items[i];
                listBoxTools.Items.RemoveAt(i);
                i++;
                listBoxTools.Items.Insert(i, item);
                listBoxTools.SelectedIndex = i;
                listBoxTools.SetItemChecked(i, isChecked);
            }
        }

        private void toolAddButton_Click(object sender, EventArgs e)
        {
            listBoxTools.Items.Add(new ToolEntry());
            listBoxTools.SelectedIndex = listBoxTools.Items.Count - 1;
        }

        private void toolDeleteButton_Click(object sender, EventArgs e)
        {
            int i = listBoxTools.SelectedIndex;
            
            if (i < listBoxTools.Items.Count && i >= 0)
            {
                listBoxTools.Items.RemoveAt(i);
                if (i < listBoxTools.Items.Count)
                {
                    listBoxTools.SelectedIndex = i;
                }
                else
                {
                    if (listBoxTools.Items.Count > 0)
                    {
                        listBoxTools.SelectedIndex = listBoxTools.Items.Count - 1;
                    }
                }
            }
        }

        private void iconButton_Click(object sender, EventArgs e)
        {
            if (_selectedTool != null)
            {
                string iconFile = _selectedTool.iconFile;
            
                if (Util.IsNullOrSpaces(iconFile))
                {
                    iconFile = textBoxTool.Text;
                }

                ChooseIconDlg dlg = new ChooseIconDlg(iconFile);
                
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _selectedTool.iconFile = dlg.FileName;
                    _selectedTool.iconIndex = dlg.IconIndex;
                    DisplayCurrentIcon();
                }
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            _selectedPlugin?.HideConfigForm();
        }

        private void workingDirButton_Click(object sender, EventArgs e)
        {
            WorkingDirButtonClick(textBoxWorkingDir);
        }

        private void multifilePattern_TextChanged(object sender, EventArgs e)
        {
            string pattern = textBoxMultifilePattern.Text;
            upDownMultifileDays.Enabled = pattern.Contains("$D");
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = @"Export Settings to file";
            dlg.DefaultExt = "dat";
            dlg.AddExtension = true;
            dlg.Filter = @"Settings (*.dat)|*.dat|All files (*.*)|*.*";
            
            DialogResult result = dlg.ShowDialog();
            
            if (result == DialogResult.OK)
            {
                using (Stream fs = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write))
                {
                    ConfigManager.Export(fs);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void importButton_Click(object sender, EventArgs e)
        {
            ImportSettingsDialog dlg = new ImportSettingsDialog();

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(dlg.FileName))
                {
                    return;
                }

                Stream fs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
                ConfigManager.Import(fs, dlg.ImportFlags);
                fs.Close();
                Preferences = ConfigManager.Settings.preferences;
                FillDialog();
                MessageBox.Show(this, "Settings imported", "LogExpert");
            }
        }

        #endregion

        //TODO why is this class here, its not referenced anywhere and not used!
        //TODO Refactor or DELTE
        private class ColumnizerEntry
        {
            #region cTor

            public ColumnizerEntry(ILogLineColumnizer columnizer)
            {
                Columnizer = columnizer;
            }

            #endregion

            #region Properties

            public string Name => Columnizer.GetName();

            public ILogLineColumnizer Columnizer { get; }

            #endregion
        }

    }
}