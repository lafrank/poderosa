using Granados;
using Poderosa.Protocols;
using Poderosa.Sessions;
using Poderosa.Terminal;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExternalAppIntegration
{
	internal delegate void ConnectCallback(PoderosaTerminal connected);
	/// <summary>
	/// 
	/// This class is used internally by ScriptableTerminal, it is not intended to be instantiated from user code.
	/// 
	/// Implements ITerminal by using the 3rd party Terminal implementation of Poderosa Terminal Control.
	/// 
	/// The encapsulated TerminalControl processes all Telnet/SSH communication and handle terminal character decoding.
	/// PoderosaTerminal reads the TerminalControl object's data through PoderosaTerminalScreen, an implementation of ITerminalScreen.
	/// 
	/// </summary>
	internal class PoderosaTerminal : IInterruptableConnectorClient
	{
		private TerminalSession _terminalSession;
		private TerminalControl _terminalControl;
		private ITerminalSettings _terminalSettings;
		private ITerminalConnection _terminalConnection;
		private ConnectCallback _connectCallback;
		//private PoderosaTerminalScreen _terminalScreen;
		public string errorMsg;
		private ManualResetEvent mreConnect = new ManualResetEvent(false);

		/// <summary>
		/// Terminal socket connection timeout, expressed in milliseconds
		/// </summary>
		public int ConnectionTimeout
		{
			get
			{
				if (_terminalSession?.TerminalConnection is SSHTerminalConnection)
				{
					return ((_terminalSession?.TerminalConnection as SSHTerminalConnection).Socket as SSHSocket).Connection.ConnectionParameter.Timeouts.ResponseTimeout;
				}
				else return 0;
			}
			set
			{
				if (_terminalSession?.TerminalConnection is SSHTerminalConnection)
				{
					((_terminalSession?.TerminalConnection as SSHTerminalConnection).Socket as SSHSocket).Connection.ConnectionParameter.Timeouts.ResponseTimeout = value;
				}
			}
		}

		public ITerminalConnection Connection
		{
			get
			{
				return _terminalSession.TerminalConnection;
			}
		}

		public object Session => _terminalSession;

		public Control TerminalControl => _terminalControl;

		public PoderosaTerminal(/*int TerminalBufferSize, PoderosaTerminalScreen TerminalScreen*/)
		{
			//_terminal = new PGT.TerminalControl();
			//Poderosa.GEnv.Options.WarningOption = Poderosa.Config.WarningOption.Ignore;
			//Poderosa.GEnv.Options.TerminalBufferSize = TerminalBufferSize == 0 ? 99999 : TerminalBufferSize;
			////Poderosa.GEnv.Options.CipherAlgorithmOrder = new string[] { "AES128", "AES128CTR", "Blowfish", "TripleDES" };
			//Poderosa.GEnv.Options.CipherAlgorithmOrder = new string[] { "AES128", "Blowfish", "TripleDES" };
			//_terminal.AuthType = Poderosa.ConnectionParam.AuthType.Password;
			//_terminal.Host = "";
			//_terminal.IdentifyFile = "";
			//_terminal.Method = ConnectionMethod.Telnet;
			//_terminal.Name = "_terminal";
			//_terminal.Password = "";
			//_terminal.Size = new System.Drawing.Size(691, 1000);
			//_terminal.TabIndex = 0;
			//_terminal.Text = "_terminal";
			//_terminal.UserName = "";
			//_terminalScreen = TerminalScreen;
		}

		public bool Connect(ConnectCallback callback)
		{
			_connectCallback = callback;
			var ssh = new SSHLoginParameter();
			ssh.Method = SSHProtocol.SSH2;
			ssh.AuthenticationType = AuthenticationType.Password;
			ssh.Account = "cxn2356";
			ssh.PasswordOrPassphrase = "Jumanji123.";
			//--- X11 forwarding settings
			ssh.EnableX11Forwarding = false;
			ssh.X11Forwarding = null;
			//--- Agent forwarding settings
			ssh.EnableAgentForwarding = false;
			ssh.AgentForwardingAuthKeyProvider = null;
			// -- tcp
			var tcp = (ITCPParameter)ssh.GetAdapter(typeof(ITCPParameter));
			tcp.Destination = "10.93.1.255";
			tcp.Port = 22;
			//--- Log settings
			ISimpleLogSettings logSettings = TerminalSessionsPlugin.Instance.TerminalEmulatorService.CreateDefaultSimpleLogSettings();
			logSettings.LogType = Poderosa.ConnectionParam.LogType.None;
			if (logSettings.LogType != Poderosa.ConnectionParam.LogType.None)
			{
				logSettings.LogPath = Path.GetTempFileName();
				logSettings.LogAppend = true;
			}
			// -- terminal settings
			_terminalSettings = TerminalSessionsPlugin.Instance.TerminalEmulatorService.CreateDefaultTerminalSettings("Terminal", null);
			_terminalSettings.BeginUpdate();
			_terminalSettings.EnabledCharTriggerIntelliSense = false;
			_terminalSettings.Encoding = Poderosa.ConnectionParam.EncodingType.UTF8;
			_terminalSettings.LocalEcho = false;
			_terminalSettings.TransmitNL = Poderosa.ConnectionParam.NewLine.CR;
			_terminalSettings.TerminalType = Poderosa.ConnectionParam.TerminalType.XTerm;
			_terminalSettings.LogSettings.Reset(logSettings);
			_terminalSettings.EndUpdate();
			IProtocolService protocolservice = TerminalSessionsPlugin.Instance.ProtocolService;
			mreConnect.Reset();
			IInterruptable interruptable = protocolservice.AsyncSSHConnect(this, ssh);
			Task.Factory.StartNew( ()=> AwaitConnectResult());

			return true;
		}

		private void AwaitConnectResult()
		{
			mreConnect.WaitOne();
			if (_terminalConnection != null )
			{
				_terminalControl = new TerminalControl();
				_terminalSession = new TerminalSession(_terminalConnection, _terminalSettings);
				_terminalControl.Attach(_terminalSession);
				_connectCallback?.Invoke(this);
			}
			else
			{
				MessageBox.Show("Connection error : " + errorMsg, "Unable to connect", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public bool Disconnect()
		{
			bool result = false;
			//try
			//{
			//  if (IsConnected())
			//  {
			//    _terminal.Close();
			//    result = true;
			//  }
			//}
			//catch
			//{

			//}
			return result;
		}

		public bool IsConnected()
		{
			return false; // _terminal.TerminalPane.Connection != null;
		}

		public int SendNewLine()
		{
			//_terminal.SendCharArray(Poderosa.Terminal.TerminalUtil.NewLineChars(_terminal.TransmitNL));
			return 0;
		}

		public void SendBreak()
		{
			// _terminal.SendBreak();
		}

		public void SendKeepAlive()
		{
			//	_terminal.SendKeepAlive();
		}

		public int SendText(string Text, bool AutoLinefeed)
		{
			int result = 0;
			return result;
		}

		#region IInterruptableConnectorClient implementation
		public void SuccessfullyExit(ITerminalConnection result)
		{
			_terminalConnection = result;
			mreConnect.Set();
		}

		public void ConnectionFailed(string message)
		{
			_terminalConnection = null;
			errorMsg = message;
			mreConnect.Set();
		}
		#endregion
	}

}
