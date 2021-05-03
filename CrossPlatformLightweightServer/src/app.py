'''
Application entry point and main controller.

Project WISE -- Wearable-ML
Qianlang Chen and Kevin Song
U 05/02/21
'''

from model import audio_transcriber

def start():
    audio_transcriber.start('dist/wearable-ml-ff8f2f105b71.json')
    audio_transcriber.transcribe('records/jensen.wav', 'records/jensen.srt')
    # audio_transcriber.transcribe('records/experience-proves-this.wav',
    #                              'records/experience-proves-this.srt')

if __name__ == '__main__':
    start()
