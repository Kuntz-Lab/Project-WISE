'''
Application entry point.

Project WISE -- Wearable-ML
Qianlang Chen and Kevin Song
M 05/03/21
'''

from model import audio_transcriber
from view import main_gui

def start():
    # audio_transcriber.start('wearable-ml-ff8f2f105b71.json')
    main_gui.start()

if __name__ == '__main__':
    start()
