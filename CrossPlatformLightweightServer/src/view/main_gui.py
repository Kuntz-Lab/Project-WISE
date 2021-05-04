'''
Controls the main GUI.

Project WISE -- Wearable-ML
Qianlang Chen and Kevin Song
M 05/03/21
'''

from model import audio_transcriber

from os import path
import PySimpleGUI
import webbrowser

_gui: PySimpleGUI.Window

def start():
    global _gui
    _gui = PySimpleGUI.Window(
        background_color='#101010',
        button_color='#606060',
        element_justification='c',
        element_padding=(8, 8),
        layout=((PySimpleGUI.FileBrowse(button_text='Select Google credential',
                                        enable_events=True,
                                        file_types=(('JSON File', '*.json'),),
                                        font=('BreezeSans Condensed', 16),
                                        key='credential_button'),),
                (PySimpleGUI.FileBrowse(button_text='Select source audio',
                                        enable_events=True,
                                        file_types=(('WAV Audio', '*.wav'),),
                                        font=('BreezeSans Condensed', 16),
                                        key='source_button'),),
                (PySimpleGUI.FileSaveAs(button_text='Select target SRT',
                                        enable_events=True,
                                        file_types=(('Subtitles', '*.srt'),),
                                        font=('BreezeSans Condensed', 16),
                                        key='target_button'),),
                (PySimpleGUI.HorizontalSeparator(pad=(6, 24)),),
                (PySimpleGUI.Button(button_color='#202020',
                                    button_text='Transcribe!',
                                    disabled=True,
                                    font=('BreezeSans Condensed', 16),
                                    key='transcribe_button'),),
                (PySimpleGUI.Text(background_color='#101010',
                                  font=('BreezeSans Condensed', 12),
                                  justification='c',
                                  key='progress_text',
                                  pad=(0, 0),
                                  size=(18, None),
                                  text='0%',
                                  text_color='#606060'),),
                (PySimpleGUI.ProgressBar(bar_color=('#404040', '#000000'),
                                         key='progress_bar',
                                         max_value=1,
                                         size=(24, 12)),)),
        margins=(48, 36),
        title='Transcribe Audio')
    credential_path = source_path = target_path = None
    while True:
        event, values = _gui.read()
        if event == PySimpleGUI.WIN_CLOSED: break
        if event == 'credential_button':
            credential_path = values['credential_button']
            _gui['credential_button'].update('Credential: ' +
                                             path.basename(credential_path))
        elif event == 'source_button':
            source_path = values['source_button']
            _gui['source_button'].update('Source: ' +
                                         path.basename(source_path))
        elif event == 'target_button':
            target_path = values['target_button']
            _gui['target_button'].update('Target: ' +
                                         path.basename(target_path))
        elif event == 'transcribe_button':
            for button in ('credential', 'source', 'target', 'transcribe'):
                _gui[button + '_button'].update(button_color='#202020',
                                                disabled=True)
            _gui['progress_text'].update('0%')
            _gui['progress_bar'].update(0)
            audio_transcriber.start(credential_path)
            audio_transcriber.transcribe(source_path, target_path,
                                         _on_audio_transcriber_progress)
            for button in ('credential', 'source', 'target', 'transcribe'):
                _gui[button + '_button'].update(button_color='#606060',
                                                disabled=False)
            _gui['progress_text'].update('Done! 100%')
            _gui['progress_bar'].update(1)
            webbrowser.open(path.dirname(target_path))
        if credential_path and source_path and target_path:
            _gui['transcribe_button'].update(button_color='#606060',
                                             disabled=False)

def _on_audio_transcriber_progress(stage, progress):
    global _gui
    if stage == 'upload': progress_stage = 'Uploading...'
    elif stage == 'transcribe': progress_stage = 'Transcribing...'
    _gui['progress_text'].update(progress_stage + f' {progress:.0%}')
    _gui['progress_bar'].update(progress)
