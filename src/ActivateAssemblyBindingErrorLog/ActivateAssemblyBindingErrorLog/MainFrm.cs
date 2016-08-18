/*
 * Assembly binding logging is turned OFF
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ActivateAssemblyBindingErrorLog.Helper;

namespace ActivateAssemblyBindingErrorLog
{
    public partial class MainFrm : Form
    {
        /// <summary>
        /// Fusion log
        /// </summary>
        private const string REGISTRY_PATH = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Fusion";

        private const string ENABLE_LOG = "EnableLog";
        private const string FORCE_LOG = "ForceLog";
        private const string LOG_FAILURES = "LogFailures";
        private const string LOG_RESOURCE_BINDS = "LogResourceBinds";
        private const string LOG_PATH = "LogPath";

        private const string BUTTON_ACTIVATE = "Activate";
        private const string BUTTON_DEACTIVATE = "Deactivate";

        public MainFrm()
        {
            InitializeComponent();

            this.Icon = Properties.Resources.main_icon;

            PrepareImageList();

            this.activeButton.Enabled = false;
            this.splitContainer1.Panel2Collapsed = true;

            this.Load += (s, e) =>
            {
                try
                {
                    CheckFusionLog();

                    messageToolStripStatusLabel1.Text = "Verified current setting.";
                }
                catch (Exception ex)
                {
                    Logging("Error. Check a console log.");
                    Logging(ex);
                }
            };

            this.browseButton.Click += (s, e) =>
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.ShowNewFolderButton = true;
                dialog.SelectedPath = String.IsNullOrEmpty(this.directoryTextBox.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : this.directoryTextBox.Text;
                if (DialogResult.OK == dialog.ShowDialog())
                {
                    this.directoryTextBox.Text = dialog.SelectedPath;
                    if (!this.directoryTextBox.Text.EndsWith("\\"))
                    {
                        this.directoryTextBox.AppendText("\\");
                    }
                }
            };

            this.activeButton.Click += (s, e) =>
            {
                Button thisControl = s as Button;
                IList<RegistryValue> values = null;
                try
                {
                    if (thisControl != null)
                    {
                        switch (thisControl.Text)
                        {
                            case BUTTON_ACTIVATE:

                                if (String.IsNullOrEmpty(this.directoryTextBox.Text))
                                {
                                    throw new ApplicationException($"The log directory does not allow a empty string.");
                                }

                                if (!Directory.Exists(this.directoryTextBox.Text))
                                {
                                    try
                                    {
                                        Directory.CreateDirectory(this.directoryTextBox.Text);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new ApplicationException($"Could not create a directory; [{this.directoryTextBox.Text}]", ex);
                                    }
                                }

                                values = GetFusionValues(logPath: this.directoryTextBox.Text);
                                RegistryHelper.AddValues(REGISTRY_PATH, values.ToArray());
                                messageToolStripStatusLabel1.Text = "Activated.";
                                Logging("Activated.");
                                break;
                            case BUTTON_DEACTIVATE:
                                values = GetFusionValues();
                                RegistryHelper.RemoveValues(REGISTRY_PATH, values.ToArray());
                                messageToolStripStatusLabel1.Text = "Deactivated.";
                                Logging("Deactivated.");
                                break;
                        }

                        this.CheckFusionLog();
                    }
                }
                catch (Exception ex)
                {
                    Logging("Error. Check a console log.");
                    Logging(ex);
                }
            };

            this.checkButton.Click += (s, e) =>
            {
                try
                {
                    CheckFusionLog();

                    messageToolStripStatusLabel1.Text = "Verified current setting.";
                }
                catch (Exception ex)
                {
                    Logging("Error. Check a console log.");
                    Logging(ex);
                }
            };

            this.directoryTextBox.TextChanged += (s, e) =>
            {
                this.activeButton.Enabled = (this.directoryTextBox.TextLength > 0);
            };

            this.exitToolStripMenuItem.Click += (s, e) =>
            {
                Application.Exit();
            };

            this.showConsoleToolStripMenuItem.Click += (s, e) =>
            {
                var thisControl = s as ToolStripMenuItem;
                if (thisControl != null)
                {
                    thisControl.Checked = !thisControl.Checked;
                    this.splitContainer1.Panel2Collapsed = !thisControl.Checked;
                }
            };

            this.openLogDirectoryToolStripMenuItem.Click += (s, e) =>
            {
                if(this.activeButton.Text .Equals(BUTTON_DEACTIVATE))
                {
                    var value = RegistryHelper.GetValues(REGISTRY_PATH, LOG_PATH).FirstOrDefault();
                    if(value != null)
                    {
                        Process.Start($"{value.Value}");
                    }
                }
            };

            this.aboutToolStripMenuItem.Click += (s, e) =>
            {
                var frm = new AboutFrm();
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.ShowDialog();
            };
        }

        /// <summary>
        /// Verify that a fusion log was activated.
        /// </summary>
        private void CheckFusionLog()
        {
            Logging("Registry Path: {0}", REGISTRY_PATH);
            var items = Helper.RegistryHelper.GetValues(REGISTRY_PATH);
            Logging("Items:");
            if (items.Count() > 0)
            {
                // log path
                var itemLogPath = items.Where(item => item.Name.Equals(LOG_PATH)).FirstOrDefault();

                if (itemLogPath != null)
                {
                    this.directoryTextBox.Text = $"{ itemLogPath.Value}";
                }

                foreach (var item in items)
                {
                    Logging("{0,20}: {1}", item.Name, item.Value);
                }
            }
            else
            {
                Logging("Could not find values.");
            }

            bool activated = ActivatedFusionLog(items);

            Logging("Activated: {0}", activated ? "YES" : "NO");

            this.activeButton.Text = activated ? BUTTON_DEACTIVATE : BUTTON_ACTIVATE;
            this.openLogDirectoryToolStripMenuItem.Enabled = activated;
            this.pictureBox.Image = this.imageList.Images[this.activeButton.Text];
            this.statusLabel.Text = activated ? "Activated" : "Deactivated";
        }

        /// <summary>
        /// Verify that a fusion log was activated.
        /// </summary>
        /// <param name="values">From registry values</param>
        /// <returns></returns>
        private bool ActivatedFusionLog(IEnumerable<RegistryValue> values)
        {
            bool activated = false;
            int a = 0;  // 비교
            int b = 0;  // 전체
            if (values != null || values.Count() > 0)
            {
                foreach (var value in GetFusionValues())
                {
                    if (value.ValueKind == Microsoft.Win32.RegistryValueKind.DWord)
                    {
                        if (values
                            .Where(v => v.Name.ToUpper().Equals(value.Name.ToUpper()) && $"{v.Value}".Equals($"{ value.Value}")).Count() > 0)
                        {
                            a++;
                        }
                        b++;
                    }
                }
                activated = (a == b);
            }

            return activated;
        }

        /// <summary>
        /// Sample data for to Activate a fusion log.
        /// </summary>
        /// <param name="enbleLog"></param>
        /// <param name="forceLog"></param>
        /// <param name="logFailures"></param>
        /// <param name="logResourceBinds"></param>
        /// <param name="logPath"></param>
        /// <returns></returns>
        private IList<RegistryValue> GetFusionValues(int enbleLog = 1, int forceLog = 1, int logFailures = 1, int logResourceBinds = 1, string logPath = "")
        {
            string logPathString = String.Empty;

            logPathString = logPath;
            if (!String.IsNullOrEmpty(logPathString) && !logPathString.EndsWith("\\"))
            {
                logPathString += "\\";
            }

            List<RegistryValue> values = new List<RegistryValue>();

            values.Add(new RegistryValue(ENABLE_LOG, enbleLog, Microsoft.Win32.RegistryValueKind.DWord));
            values.Add(new RegistryValue(FORCE_LOG, forceLog, Microsoft.Win32.RegistryValueKind.DWord));
            values.Add(new RegistryValue(LOG_FAILURES, logFailures, Microsoft.Win32.RegistryValueKind.DWord));
            values.Add(new RegistryValue(LOG_RESOURCE_BINDS, logResourceBinds, Microsoft.Win32.RegistryValueKind.DWord));
            values.Add(new RegistryValue(LOG_PATH, logPathString, Microsoft.Win32.RegistryValueKind.String));

            return values;
        }

        /// <summary>
        /// Logging
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        private void Logging(string format, params object[] args)
        {
            this.logTextBox.AppendText(String.Format(format, args));
            this.logTextBox.AppendText("\r\n");
            this.logTextBox.ScrollToCaret();
        }

        /// <summary>
        /// Logging
        /// </summary>
        /// <param name="message"></param>
        private void Logging(string message)
        {
            Logging("{0}", message);
        }

        /// <summary>
        /// Logging for exceotion
        /// </summary>
        /// <param name="exception"></param>
        private void Logging(Exception exception)
        {
            Logging("Message:{0}", exception.Message);
            Logging("Stack Trace: {0}", exception.StackTrace);
        }

        /// <summary>
        /// prepare image list
        /// </summary>
        private void PrepareImageList()
        {
            this.imageList.Images.Clear();
            this.imageList.ImageSize = new Size(128, 128);
            this.imageList.Images.Add(BUTTON_ACTIVATE, Properties.Resources.locked);
            this.imageList.Images.Add(BUTTON_DEACTIVATE, Properties.Resources.unlocked);
        }

    }
}
