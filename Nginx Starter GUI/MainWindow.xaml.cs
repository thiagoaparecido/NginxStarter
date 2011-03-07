﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Win32;

namespace NginxStarterGUI
{

	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		private static Settings _settings;
		private static bool _inGreenMode = false;
		private static string _configFilePath;
		private static System.Diagnostics.ProcessStartInfo _nginxInfo;
		private static System.Diagnostics.Process _nginx;
		private static System.Diagnostics.ProcessStartInfo _phpInfo;
		private static System.Diagnostics.Process _php;
		private static NotifyIcon _notifyIcon;

		public MainWindow()
		{
			_configFilePath = AppDomain.CurrentDomain.BaseDirectory + "Nginx Starter GUI.config.xml";
			_settings = readConfigFile();
			InitializeComponent();
			this.txtNPath.Text = _settings.nginxPath;
			this.txtNConfigPath.Text = _settings.nginxConfigPath;
			this.txtPPath.Text = _settings.phpPath;
			this.txtPConfigPath.Text = _settings.phpConfigPath;
			this.txtPHost.Text = _settings.phpHost;
			this.txtPPort.Text = _settings.phpPort.ToString();
			this.chkPUseIniFile.IsChecked = _settings.phpUseIniFile;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			this.saveConfigFile();
			if (_nginx != null)
			{
				_nginx.Close();
				try
				{
					_nginx.Kill();
				}
				catch
				{
				}
			}
		}

		/// <summary>
		/// 从指定文件路径读取程序设置
		/// </summary>
		/// <param name="configFilePath"></param>
		/// <returns>返回程序设置类</returns>
		private Settings readConfigFile(string configFilePath)
		{
			FileStream fs = null;
			try
			{
				using (fs = File.Open(configFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					XmlSerializer formatter = new XmlSerializer(typeof(Settings));
					Settings settings = (Settings)formatter.Deserialize(fs);
					fs.Close();
					if (settings == null)
						throw new InvalidOperationException("设置文件里什么内容也没有保存");
					return settings;
				}
			}
			catch (FileNotFoundException)
			{
				return new Settings();
			}
			catch (InvalidOperationException e)
			{
				MessageBoxResult mb = MessageBox.Show(
					"设置文件格式错误，是否重建？\n选“是”将备份原有文件后新建设置文件，如果已有备份文件，将会将其替换，选“否”将继续运行，但是将不会保存设置。\n错误详情：" + e.Message,
					"设置文件格式错误", MessageBoxButton.YesNo, MessageBoxImage.Warning
				);
				if (mb == MessageBoxResult.Yes)
				{
					backupConfigFile(configFilePath);
					return new Settings();
				}
				else
				{
					_inGreenMode = true;
					return new Settings();
				}
			}
			catch (FileLoadException)
			{
				MessageBox.Show("设置文件无法读取，请检查您的权限！程序将继续运行，但是不会保存设置！", "读取设置文件出错", MessageBoxButton.OK, MessageBoxImage.Error);
				_inGreenMode = true;
				return new Settings();
			}
			catch
			{
				return new Settings();
			}
			finally
			{
				if (fs != null)
					fs.Dispose();
			}
		}

		/// <summary>
		/// 从MainWindow类中设置的默认路径读取设置文件
		/// </summary>
		/// <returns>返回程序设置类</returns>
		private Settings readConfigFile()
		{
			return this.readConfigFile(_configFilePath);
		}

		/// <summary>
		/// 将指定程序设置，保存到指定设置文件
		/// </summary>
		/// <param name="settings">程序设置</param>
		/// <param name="configFilePath">设置文件路径</param>
		private void saveConfigFile(Settings settings, string configFilePath)
		{
			if (_inGreenMode == true)
				return;
			try
			{
				using (FileStream fs = File.Open(configFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
				{
					XmlSerializer formatter = new XmlSerializer(typeof(Settings));
					formatter.Serialize(fs, settings);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("保存设置文件失败，您本次的设置可能不会被保存，请确认是否拥有在程序运行目录下的读、写、新建文件权限，或与系统管理员联系！\n错误详情：" + e.Message, "保存设置文件出错", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// 将指定程序设置，保存到MainWindow类中的默认设置文件路径
		/// </summary>
		/// <param name="settings">程序设置</param>
		private void saveConfigFile(Settings settings)
		{
			this.saveConfigFile(settings, _configFilePath);
		}

		/// <summary>
		/// 将MainWindow类中的当前设置，保存到指定设置文件
		/// </summary>
		/// <param name="configFilePath">设置文件路径</param>
		private void saveConfigFile(string configFilePath)
		{
			this.saveConfigFile(_settings, configFilePath);
		}

		/// <summary>
		/// 将MainWindow类中的当前设置，保存到MainWindow类中的默认设置文件路径
		/// </summary>
		private void saveConfigFile()
		{
			this.saveConfigFile(_settings, _configFilePath);
		}

		private void backupConfigFile(string configFilePath)
		{
			if (File.Exists(configFilePath + ".bak"))
				File.Delete(configFilePath + ".bak");
			File.Move(configFilePath, configFilePath + ".bak");
		}

		private void backupConfigFile()
		{
			backupConfigFile(_configFilePath);
		}

		public bool nginxStart()
		{
			_nginxInfo = new System.Diagnostics.ProcessStartInfo();
			_nginxInfo.Arguments = string.Empty;
			if (this.txtNConfigPath.Text != string.Empty)
				_nginxInfo.Arguments += " -c " + this.txtNConfigPath;
			_nginxInfo.FileName = this.txtNPath.Text;
			_nginxInfo.WorkingDirectory = this.txtNPath.Text.Substring(0, _settings.nginxPath.LastIndexOf('\\'));
			_nginxInfo.CreateNoWindow = true;
			_nginxInfo.UseShellExecute = false;
			try
			{
				_nginx = System.Diagnostics.Process.Start(_nginxInfo);
				_settings.nginxPath = this.txtNPath.Text;
				_settings.nginxConfigPath = this.txtNConfigPath.Text;
				return true;
			}
			catch
			{
				MessageBox.Show("启动失败！");
				return false;
			}
		}

		public bool nginxStop()
		{
			try
			{
				_nginxInfo.Arguments = "-s stop";
				System.Diagnostics.Process.Start(_nginxInfo);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool nginxQuit()
		{
			try
			{
				_nginxInfo.Arguments = "-s quit";
				System.Diagnostics.Process.Start(_nginxInfo);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public void nginxReload()
		{
			_nginxInfo.Arguments = "-s reload";
		}

		public void nginxRestart()
		{
			_nginxInfo.Arguments = "-s restart";
		}

		public string nginxBrowse()
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (_settings.nginxPath != null || _settings.nginxPath != string.Empty)
			{
				ofd.InitialDirectory = _settings.nginxPath;
			}
			else
			{
				ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
			}
			ofd.Filter = "Nginx默认执行文件|nginx.exe|所有执行文件|*.exe|所有文件|*.*";
			ofd.Title = "选择Nginx执行文件";
			if (ofd.ShowDialog() == true)
			{
				txtNPath.Text = ofd.FileName;
				return ofd.FileName;
			}
			else
			{
				return string.Empty;
			}
		}

		public string nginxConfigBrowse()
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (_settings.nginxConfigPath != null || _settings.nginxConfigPath != string.Empty)
			{
				ofd.InitialDirectory = _settings.nginxConfigPath;
			}
			else
			{
				ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\conf";
			}
			ofd.Filter = "Nginx默认设置文件|nginx.conf|所有设置文件|*.conf|所有文件|*.*";
			ofd.Title = "选择Nginx主配置文件";
			if (ofd.ShowDialog() == true)
			{
				txtNConfigPath.Text = ofd.FileName;
				return ofd.FileName;
			}
			else
			{
				return string.Empty;
			}
		}

		private void btnNStart_Click(object sender, RoutedEventArgs e)
		{
			if (txtNPath.Text == String.Empty)
			{
				if(this.nginxBrowse() != string.Empty)
					this.btnNStart_Click(sender, e);
			}
			else
			{
				_settings.nginxPath = txtNPath.Text;
				this.nginxStart();
			}
		}

		private void btnNBrowse_Click(object sender, RoutedEventArgs e)
		{
			this.nginxBrowse();
		}

		private void btnNConfigBrowse_Click(object sender, RoutedEventArgs e)
		{
			this.nginxConfigBrowse();
		}

		private void btnNReload_Click(object sender, RoutedEventArgs e)
		{
			this.nginxReload();
		}

		private void btnNRestart_Click(object sender, RoutedEventArgs e)
		{
			this.nginxRestart();
		}

		private void btnNQuit_Click(object sender, RoutedEventArgs e)
		{
			this.nginxQuit();
		}

		private void btnNStop_Click(object sender, RoutedEventArgs e)
		{
			this.nginxStop();
		}

		private void Window_StateChanged(object sender, EventArgs e)
		{
			if (this.WindowState == WindowState.Minimized)
			{
				_notifyIcon = new NotifyIcon(this);
				this.ShowInTaskbar = false;
			}
			else
			{
				this.ShowInTaskbar = true;
				if (_notifyIcon != null) _notifyIcon.Dispose();
			}
		}

		public bool phpStart()
		{
			int port = 0;
			if (this.txtPPort.Text != string.Empty)
			{
				try
				{
					port = Convert.ToUInt16(this.txtPPath.Text);
				}
				catch
				{
					MessageBox.Show("端口号请填入一个整数！", "端口号错误", MessageBoxButton.OK, MessageBoxImage.Error);
					return false;
				}
			}
			_phpInfo = new System.Diagnostics.ProcessStartInfo();
			_phpInfo.Arguments = string.Empty;
			if (this.txtPConfigPath.Text != string.Empty)
				_phpInfo.Arguments += " -c " + this.txtPConfigPath;
			if (port != 0)
				if (this.txtPHost.Text != string.Empty)
					_phpInfo.Arguments += " -b " + this.txtPHost.Text + ":" + port.ToString();
				else
					_phpInfo.Arguments += " -b " + port.ToString();
			if (this.chkPUseIniFile.IsChecked != null && this.chkPUseIniFile.IsChecked == true)
				_phpInfo.Arguments += " -n";
			_phpInfo.FileName = this.txtPPath.Text;
			_phpInfo.WorkingDirectory = this.txtPPath.Text.Substring(0, _settings.phpPath.LastIndexOf('\\'));
			_phpInfo.CreateNoWindow = true;
			_phpInfo.UseShellExecute = false;
			try
			{
				_php = System.Diagnostics.Process.Start(_phpInfo);
				_settings.phpPath = this.txtPPath.Text;
				_settings.phpConfigPath = this.txtPConfigPath.Text;
				_settings.phpHost = this.txtPHost.Text;
				_settings.phpPort = port;
				_settings.phpUseIniFile = this.chkPUseIniFile.IsChecked;
				return true;
			}
			catch (Exception e)
			{
				MessageBox.Show("启动失败！");
				return false;
			}
		}

		public bool phpRestart()
		{
			if (phpStop())
				return phpStart();
			else
				return false;
		}

		public bool phpStop()
		{
			try
			{
				_php.Dispose();
				return true;
			}
			catch
			{
				return false;
			}
		}

		public string phpBrowse()
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (_settings.phpPath != null || _settings.phpPath != string.Empty)
			{
				ofd.InitialDirectory = _settings.phpPath;
			}
			else
			{
				ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
			}
			ofd.Filter = "PHP-CGI默认执行文件|php-cgi.exe|所有执行文件|*.exe|所有文件|*.*";
			ofd.Title = "选择PHP-CGI执行文件";
			if (ofd.ShowDialog() == true)
			{
				txtPPath.Text = ofd.FileName;
				return ofd.FileName;
			}
			else
			{
				return string.Empty;
			}
		}

		public string phpConfigBrowse()
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (_settings.phpConfigPath != null || _settings.phpConfigPath != string.Empty)
			{
				ofd.InitialDirectory = _settings.phpConfigPath;
			}
			else
			{
				ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
			}
			ofd.Filter = "PHP默认设置文件|php.ini|所有设置文件|*.ini|所有文件|*.*";
			ofd.Title = "选择PHP配置文件（php.ini）";
			if (ofd.ShowDialog() == true)
			{
				this.txtPConfigPath.Text = ofd.FileName;
				return ofd.FileName;
			}
			else
			{
				return string.Empty;
			}
		}

		private void btnPStart_Click(object sender, RoutedEventArgs e)
		{
			if (txtPPath.Text == String.Empty)
			{
				if (this.phpBrowse() != string.Empty)
					this.btnPStart_Click(sender, e);
			}
			else
			{
				_settings.phpPath = txtPPath.Text;
				this.phpStart();
			}
		}

		private void btnPRestart_Click(object sender, RoutedEventArgs e)
		{
			this.phpRestart();
		}

		private void btnPStop_Click(object sender, RoutedEventArgs e)
		{
			this.phpStop();
		}

		private void btnPConfigBrowse_Click(object sender, RoutedEventArgs e)
		{
			this.phpConfigBrowse();
		}

		private void btnPBrowse_Click(object sender, RoutedEventArgs e)
		{
			this.phpBrowse();
		}

	}
}
