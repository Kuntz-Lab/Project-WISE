using System;

using Tizen.Multimedia;

namespace TizenSensor.lib
{
	/// <summary>
	/// Captures and records audio from the watch's microphone.
	/// </summary>
	public class AudioRecorder
	{
		public static void Create(Action<AudioRecorder> onCreated)
		{
			PermissionManager.GetPermissions(
				isAllowed => onCreated(isAllowed ? new AudioRecorder() : null),
				"http://tizen.org/privilege/recorder",
				"http://tizen.org/privilege/mediastorage"
			);
		}

		protected AudioRecorder()
		{
			audioRecorder = new Tizen.Multimedia.AudioRecorder(RecorderAudioCodec.Pcm, RecorderFileFormat.Wav)
			{
				AudioDevice = RecorderAudioDevice.Mic,
				AudioSampleRate = 16000,
				AudioChannels = 1,
			};
		}

		public bool IsRunning { get; protected set; } = false;

		protected Tizen.Multimedia.AudioRecorder audioRecorder;

		public void Start(string recordFilePath)
		{
			audioRecorder.Prepare();
			audioRecorder.Start(recordFilePath);
			IsRunning = true;
		}

		public void Stop()
		{
			IsRunning = false;
			audioRecorder.Commit();
			audioRecorder.Unprepare();
		}
	}
}
