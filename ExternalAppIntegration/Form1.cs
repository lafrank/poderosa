using Poderosa.Boot;
using Poderosa.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExternalAppIntegration
{
	public partial class MainForm : Form
	{
		private IPoderosaApplication _poderosaApplication;
		public MainForm()
		{
			InitializeComponent();
			_poderosaApplication = PoderosaStartup.CreatePoderosaApplication(new string[] { });
			_poderosaApplication.Start(true);
		}

		private void btnTestConnect_Click(object sender, EventArgs e)
		{
			var pt = new PoderosaTerminal();
			pt.Connect(this.OnConnected);
		}

		private void OnConnected(PoderosaTerminal connected)
		{
			if (InvokeRequired) Invoke(new Action(() => OnConnected(connected)));
			else
			{
				splitContainer1.Panel1.Controls.Add(connected.TerminalControl);
				Controls.SetChildIndex(connected.TerminalControl, 0);
			}
		}
	}
}
