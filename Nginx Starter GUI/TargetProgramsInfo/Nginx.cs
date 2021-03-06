﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Security.Permissions;

namespace NginxStarterGUI.TargetProgramsInfo
{
	class Nginx
	{
		private ProcessStartInfo info;
		private Process process;
		public string exePath { get; set; }
		public string configPath { get; set; }
		public string AddParams { get; set; }
		public bool IsRunning { get; set; }
		public event EventHandler HasStarted;
		public event EventHandler HasStoped;
		public const string OfdExeFilter = "Nginx默认执行文件|nginx.exe|所有执行文件|*.exe|所有文件|*.*";
		public const string OfdExeTitle = "选择Nginx执行文件";
		public const string OfdExeFileName = "nginx.exe";
		public const string OfdConfigFileFilter = "Nginx默认设置文件|nginx.conf|所有设置文件|*.conf|所有文件|*.*";
		public const string OfdConfigFileTitle = "选择Nginx主配置文件";

		/// <summary>
		/// 创建一个nginx
		/// </summary>
		/// <param name="exePath">exe文件路径</param>
		/// <param name="configPath">配置文件路径</param>
		public Nginx(string exePath, string configPath, string addParams)
		{
			this.exePath = exePath;
			this.configPath = configPath;
			this.AddParams = addParams;
		}

		/// <summary>
		/// 创建一个Nginx
		/// </summary>
		/// <param name="exePath">exe文件路径</param>
		//public Nginx(string exePath)
		//	: this(exePath, string.Empty)
		//{
		//}

		/// <summary>
		/// 启动Nginx
		/// </summary>
		/// <returns>返回一个bool值表示启动成功与否</returns>
		[EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = false)]
		public bool start()
		{
			this.info = new ProcessStartInfo();
			this.info.Arguments = this.AddParams;
			if (!String.IsNullOrEmpty(this.configPath))
				this.info.Arguments += " -c " + this.configPath;
			this.info.FileName = this.exePath;
			this.info.WorkingDirectory = this.exePath.Substring(0, this.exePath.LastIndexOf('\\'));
			this.info.CreateNoWindow = true;
			this.info.UseShellExecute = false;
			this.process = Process.Start(this.info);
			this.process.Exited += (sender, e) =>
				{
					this.IsRunning = false;
					if(this.HasStoped != null)
						this.HasStoped(sender, e);
				};
			if(this.HasStarted != null)
				this.HasStarted(this, null);
			this.IsRunning = true;

			return true;
		}

		/// <summary>
		/// 停止Nginx
		/// </summary>
		/// <returns>返回一个bool值表示停止成功与否</returns>
		[EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = false)]
		public bool quit()
		{
			this.info.Arguments = "-s stop";
			Process.Start(this.info);
			this.IsRunning = false;
			return true;
		}

		[EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = false)]
		public static bool _stop()
		{
			Process[] nginxs = Process.GetProcessesByName("nginx.exe");
			foreach (Process nginx in nginxs)
			{
				nginx.Kill();
			}
			return true;
		}
		/// <summary>
		/// 强制关闭所有nginx进程，
		/// </summary>
		/// <returns>返回一个bool值，该值只表示执行代码期间是否遇到错误，并不代表是否成功</returns>
		[EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = false)]
		public bool stop()
		{
			this.info.Arguments = "-s quit";
			Process.Start(this.info);
			if (!Nginx._stop())
				return false;
			else
				this.IsRunning = false;
				return true;
		}

		/// <summary>
		/// 调用Nginx自身reload参数重载配置文件
		/// </summary>
		[EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = false)]
		public void reload()
		{
			this.info.Arguments = "-s reload";
			Process.Start(this.info);
		}

		/// <summary>
		/// 调用Nginx自身restart参数重启Nginx
		/// </summary>
		[EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = false)]
		public void restart()
		{
			this.info.Arguments = "-s restart";
			Process.Start(this.info);
		}
	}
}
