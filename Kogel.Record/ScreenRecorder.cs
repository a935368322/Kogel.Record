﻿using AForge.Video;
using AForge.Video.FFMPEG;
using MiniScreenRecorder.AviFile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Kogel.Record
{
	/// <summary>
	/// 屏幕录制
	/// </summary>
	public class ScreenRecorder
	{
		#region Fields
		private int DEFAULT_FRAME_RATE = 10;
		private int ScreenWidth;
		private int ScreenHight;
		private int BitRate;
		private int FrameRate;
		private Rectangle ScreenArea;
		private VideoFileWriter VideoWriter;
		private ScreenCaptureStream VideoStreamer;
		private FolderBrowserDialog FolderBrowser;
		private VideoCodec VideoCodec;
		/// <summary>
		/// 视频路径
		/// </summary>
		private string AviFilePath { get; set; }
		#endregion

		/// <summary>
		/// 录制声音
		/// </summary>
		private WavRecorder wavRecorder { get; set; }

		/// <summary>
		/// 屏幕录制
		/// </summary>
		/// <param name="aviFilePath">视频路径</param>
		/// <param name="defaultFrameRate">默认帧数</param>
		/// <param name="isLoopingWav">是否录制声音(默认不录制)</param>
		public ScreenRecorder(string aviFilePath, int defaultFrameRate = 10, bool isLoopingWav = false)
		{
			this.AviFilePath = aviFilePath;
			this.DEFAULT_FRAME_RATE = defaultFrameRate;
			this.ScreenWidth = SystemInformation.VirtualScreen.Width;
			this.ScreenHight = SystemInformation.VirtualScreen.Height;
			this.FrameRate = DEFAULT_FRAME_RATE;
			this.ScreenArea = Rectangle.Empty;
			this.VideoWriter = new VideoFileWriter();
			this.FolderBrowser = new FolderBrowserDialog();
			this.VideoCodec = (VideoCodec)3;
			this.BitRate = 3000000;

			//是否需要录制声音
			if (isLoopingWav)
				wavRecorder = new WavRecorder(AppDomain.CurrentDomain.BaseDirectory + Guid.NewGuid().ToString().Replace("-", "") + ".wav");
		}

		/// <summary>
		/// 开始
		/// </summary>
		/// <param name="frameEventHandler">每帧回调（默认不需要填）</param>
		public void Start(NewFrameEventHandler frameEventHandler = null)
		{
			//设置所有显示器
			foreach (Screen screen in Screen.AllScreens)
			{
				this.ScreenArea = Rectangle.Union(this.ScreenArea, screen.Bounds);
			}
			//打开录制器
			this.VideoWriter.Open(this.AviFilePath, this.ScreenWidth, this.ScreenHight, this.FrameRate, this.VideoCodec, this.BitRate);
			this.VideoStreamer = new ScreenCaptureStream(this.ScreenArea);
			this.VideoStreamer.NewFrame += VideoStreamer_NewFrame;
			if (frameEventHandler != null)
				this.VideoStreamer.NewFrame += frameEventHandler;
			this.VideoStreamer.Start();
			//是否需要录制声音
			if (wavRecorder != null)
				wavRecorder.Start();
		}

		/// <summary>
		/// 每帧录制帧数回调
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void VideoStreamer_NewFrame(object sender, NewFrameEventArgs eventArgs)
		{
			this.VideoWriter.WriteVideoFrame((Bitmap)eventArgs.Frame.Clone());
		}

		/// <summary>
		/// 结束
		/// </summary>
		public void End()
		{
			VideoStreamer.Stop();
			VideoWriter.Close();
			//是否需要录制声音
			if (wavRecorder != null)
			{
				wavRecorder.End();
				//获取和保存音频流到文件(桌面录制)
				AviManager aviManager = new AviManager(AviFilePath, true);
				aviManager.AddAudioStream(wavRecorder.WavFilePath, 0);
				aviManager.Close();
				//删除临时音频文件
				try { File.Delete(wavRecorder.WavFilePath); } catch { }
			}
		}
	}
}