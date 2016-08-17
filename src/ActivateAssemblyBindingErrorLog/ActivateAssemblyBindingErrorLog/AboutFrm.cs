using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ActivateAssemblyBindingErrorLog
{
    public partial class AboutFrm : Form
    {
        public AboutFrm()
        {
            InitializeComponent();

            this.blogButton.Click += (s, e) =>
            {
                LinkButtonClick(s);
            };

            this.githubButton.Click += (s, e) =>
            {
                LinkButtonClick(s);
            };
        }

        private void LinkButtonClick(object control)
        {
            if (control is LinkLabel)
            {
                LinkLabel thisControl = control as LinkLabel;
                if (thisControl != null)
                {
                    ProcessStart(thisControl.Text);
                }
            }
        }

        private void ProcessStart(string url)
        {
            Process.Start(url);
        }
    }
}
