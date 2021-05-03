'''
A speech-to-text tool that uses the Google Cloud Speech API.

Requires a JSON credential to authenticate and working Internet. The `start`
function must be called first with a valid credential before transcribing an
audio.

Project WISE -- Wearable-ML
Qianlang Chen and Kevin Song
U 05/02/21
'''

from google.cloud import speech, storage
import os
from os import path
import wave

_CREDENTIAL_ENV_NAME = 'GOOGLE_APPLICATION_CREDENTIALS'
_BUCKET_NAME = 'wearable-ml-recorded-audio'

def start(credential_path: str):
    '''
    Sets the path to a JSON credential that will be used to authenticate with
    the Google Cloud.
    '''
    if not path.exists(credential_path):
        raise FileNotFoundError(
            f'Credential does not exist: \'{credential_path}\'')
    os.environ[_CREDENTIAL_ENV_NAME] = credential_path

_URI_FORMAT = 'gs://{bucket_name}/{file_name}'
_MAX_NUM_WORDS_IN_LINE = 12
_LINE_INTERVAL = 500 # ms
'''
Consider a word to be the start of a new line if it starts x milliseconds or
more after the previous word.
'''

def transcribe(source_audio_path: str, target_srt_path: str):
    '''
    Accesses Google to transcribe a WAV file at `source_audio_path` and stores
    the text transcript in SubRip Subtitle (SRT) format at `target_srt_path`.
    
    This process blocks the thread.
    '''
    name = path.basename(source_audio_path)
    # Upload the audio to Google Cloud Storage (GCS), which is required for
    # audios longer than 1 minute
    blob = storage.Client().get_bucket(_BUCKET_NAME).blob(name)
    blob.upload_from_filename(source_audio_path)
    # Request an online transcription which blocks the process
    with wave.open(source_audio_path, 'rb') as audio: # rb for read binary
        frame_rate = audio.getframerate()
    config = speech.RecognitionConfig(
        enable_word_time_offsets=True,
        encoding=speech.RecognitionConfig.AudioEncoding.LINEAR16,
        language_code='en-US',
        sample_rate_hertz=frame_rate)
    uri = _URI_FORMAT.format(bucket_name=_BUCKET_NAME, file_name=name)
    audio = speech.RecognitionAudio(uri=uri)
    response = speech.SpeechClient().long_running_recognize(
        config=config, audio=audio).result()
    # Delete the uploaded audio to save cloud storage
    blob.delete()
    # Store the transcription
    line = []
    line_index = 1
    line_start_time = line_end_time = 0 # all in milliseconds
    with open(target_srt_path, 'w') as target:
        for res in response.results:
            for word_data in res.alternatives[0].words:
                word_start_time = word_end_time = 0
                word_start_time += word_data.start_time.seconds * 10**3
                word_start_time += word_data.start_time.microseconds // 10**3
                word_end_time += word_data.end_time.seconds * 10**3
                word_end_time += word_data.end_time.microseconds // 10**3
                if (len(line) >= _MAX_NUM_WORDS_IN_LINE or line and
                        word_start_time - line_end_time >= _LINE_INTERVAL):
                    _writeLine(target, line, line_index, line_start_time,
                               line_end_time)
                    line.clear()
                    line_index += 1
                    line_start_time = word_start_time
                line.append(word_data.word)
                line_end_time = word_end_time
        if line:
            _writeLine(target, line, line_index, line_start_time, line_end_time)

def _writeLine(target, line, index, startTime, endTime):
    target.write(f'{index}\n')
    start = _formatTime(startTime)
    end = _formatTime(endTime)
    target.write(f'{start} --> {end}\n')
    target.write(' '.join(line) + '\n\n')

def _formatTime(millis):
    h = millis // (3600 * 10**3)
    m = millis // (60 * 10**3) % 60
    s = millis // 10**3 % 60
    ms = millis % 10**3
    return f'{h:02}:{m:02}:{s:02},{ms:03}'
