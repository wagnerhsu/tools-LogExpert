﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;

// using System.Linq;
using System.Windows.Forms;

namespace LogExpert
{
    public partial class FilterSelectorForm : Form
    {
        #region Fields

        private readonly ILogLineColumnizerCallback _callback;
        private readonly IList<ILogLineColumnizer> _columnizerList;

        #endregion

        #region cTor

        public FilterSelectorForm(IList<ILogLineColumnizer> existingColumnizerList, ILogLineColumnizer currentColumnizer, ILogLineColumnizerCallback callback)
        {
            SelectedColumnizer = currentColumnizer;
            _callback = callback;
            InitializeComponent();
            filterComboBox.SelectedIndexChanged += filterComboBox_SelectedIndexChanged;

            // for the currently selected columnizer use the current instance and not the template instance from
            // columnizer registry. This ensures that changes made in columnizer config dialogs
            // will apply to the current instance
            _columnizerList = new List<ILogLineColumnizer>();
            foreach (ILogLineColumnizer col in existingColumnizerList)
            {
                _columnizerList.Add(col.GetType() == SelectedColumnizer.GetType() ? SelectedColumnizer : col);
            }

            foreach (ILogLineColumnizer col in _columnizerList)
            {
                filterComboBox.Items.Add(col);
            }

            foreach (ILogLineColumnizer columnizer in _columnizerList)
            {
                if (columnizer.GetType() == SelectedColumnizer.GetType())
                {
                    filterComboBox.SelectedItem = columnizer;
                    break;
                }
            }
        }

        #endregion

        #region Properties

        public ILogLineColumnizer SelectedColumnizer { get; private set; }

        public bool ApplyToAll => applyToAllCheckBox.Checked;

        public bool IsConfigPressed { get; private set; }

        #endregion

        #region Events handler

        private void filterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ILogLineColumnizer col = _columnizerList[filterComboBox.SelectedIndex];
            SelectedColumnizer = col;
            string description = col.GetDescription();
            description += "\r\nSupports timeshift: " + (SelectedColumnizer.IsTimeshiftImplemented() ? "Yes" : "No");
            commentTextBox.Text = description;
            configButton.Enabled = SelectedColumnizer is IColumnizerConfigurator;
        }


        private void configButton_Click(object sender, EventArgs e)
        {
            if (SelectedColumnizer is IColumnizerConfigurator)
            {
                ((IColumnizerConfigurator) SelectedColumnizer).Configure(_callback, ConfigManager.ConfigDir);
                IsConfigPressed = true;
            }
        }

        #endregion
    }
}