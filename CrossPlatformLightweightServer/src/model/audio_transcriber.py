'''
A speech-to-text tool that uses the Google Cloud Speech API.

Requires a JSON credential to authenticate and working Internet. The `start`
function must be called first with a valid credential before transcribing an
audio.

Project WISE -- Wearable-ML
Qianlang Chen and Kevin Song
M 05/03/21
'''

from google.cloud import speech, storage
from os import path
import wave

_BUCKET_NAME = 'wearable-ml-recorded-audio'

_speech_client: speech.SpeechClient
_storage_client: storage.Client
_storage_bucket: storage.Bucket

def start(credential_path: str):
    '''
    Sets the path to a JSON credential that will be used to authenticate with
    the Google Cloud.
    '''
    if not path.exists(credential_path):
        raise FileNotFoundError(
            f'Credential does not exist: \'{credential_path}\'')
    global _speech_client, _storage_client, _storage_bucket
    _speech_client = speech.SpeechClient.from_service_account_json(
        credential_path)
    _storage_client = storage.Client.from_service_account_json(credential_path)
    _storage_bucket = _storage_client.bucket(_BUCKET_NAME)

_URI_FORMAT = f'gs://{_BUCKET_NAME}/{{file_name}}'
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
    global _speech_client, _storage_bucket
    blob = _storage_bucket.blob(name)
    blob.upload_from_filename(source_audio_path)
    # Request an online transcription which blocks the process
    with wave.open(source_audio_path, 'rb') as audio: # rb for read binary
        frame_rate = audio.getframerate()
    config = speech.RecognitionConfig(
        enable_word_time_offsets=True,
        encoding=speech.RecognitionConfig.AudioEncoding.LINEAR16,
        language_code='en-US',
        sample_rate_hertz=frame_rate)
    audio = speech.RecognitionAudio(uri=_URI_FORMAT.format(file_name=name))
    response = _speech_client.long_running_recognize(config=config,
                                                     audio=audio).result()
    # Delete the uploaded audio to save cloud storage
    blob.delete()
    # Store the transcription
    line = []
    line_index = 1
    line_start_time = line_end_time = 0 # all in milliseconds
    with open(target_srt_path, 'w') as target:
        for res in response.results:
            for word_data in res.alternatives[0].words:
                word_start_time = (word_data.start_time.seconds * 10**3 +
                                   word_data.start_time.microseconds // 10**3)
                word_end_time = (word_data.end_time.seconds * 10**3 +
                                 word_data.end_time.microseconds // 10**3)
                if (len(line) == _MAX_NUM_WORDS_IN_LINE or line and
                        word_start_time - line_end_time >= _LINE_INTERVAL):
                    _write_line(target, line, line_index, line_start_time,
                                line_end_time)
                    line.clear()
                    line_index += 1
                    line_start_time = word_start_time
                line.append(word_data.word)
                line_end_time = word_end_time
        if line:
            _write_line(target, line, line_index, line_start_time,
                        line_end_time)

def _write_line(target, line, index, start_time, end_time):
    target.write(f'{index}\n')
    start = _format_time(start_time)
    end = _format_time(end_time)
    target.write(f'{start} --> {end}\n')
    target.write(' '.join(line) + '\n\n')

def _format_time(millis):
    h = millis // (3600 * 10**3)
    m = millis // (60 * 10**3) % 60
    s = millis // 10**3 % 60
    ms = millis % 10**3
    return f'{h:02}:{m:02}:{s:02},{ms:03}'
