using System;

using Tizen.Multimedia;

namespace TizenSensor.lib
{
	/// <summary>
	/// Captures and records audio from the watch's microphone.
	/// </summary>
	public class Recorder
	{
		public static void Create(Action<Recorder> onCreated)
		{
			Permission.Check(
				isAllowed => onCreated(isAllowed ? new Recorder() : null),
				"http://tizen.org/privilege/recorder",
				"http://tizen.org/privilege/mediastorage"
			);
		}

		protected Recorder()
		{
			audioRecorder = new AudioRecorder(RecorderAudioCodec.Pcm, RecorderFileFormat.Wav)
			{
				AudioDevice = RecorderAudioDevice.Mic,
				AudioSampleRate = 16000,
				AudioChannels = 1,
			};
		}

		public bool IsRunning { get; protected set; } = false;

		protected AudioRecorder audioRecorder;

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
