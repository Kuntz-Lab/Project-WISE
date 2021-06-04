'''
Used to retrieve audio and sensor reading data from a Tizen Sensor.

Project WISE -- Wearable-ML
Qianlang Chen
H 06/03/21
'''

from collections.abc import Callable
import PySimpleGUI
from PySimpleGUI import Button, Element, Input, Listbox, Text, Window

class App:
    def start():
        Gui.start()

class Gui:
    _window: Window = None
    _info_text: Text = None
    _addr_input: Input = None
    _load_button: Button = None
    _file_list: Listbox = None
    _selection_text: Text = None
    _select_all_button: Button = None
    _deselect_all_button: Button = None
    _retrieve_button: Button = None
    _delete_button: Button = None
    _interactive_elements: tuple[Element] = None
    
    def start():
        Gui._info_text = Text(background_color='#101010',
                              font=(None, 20, 'italic'),
                              justification='c',
                              key='info_text',
                              border_width=8,
                              size=(24, 1),
                              text='Enter sensor address')
        Gui._addr_input = Input(default_text='192.168.0.', key='addr_input', size=(16, 1))
        Gui._load_button = Button(button_text='Load', key='load_button', size=(6, 1))
        Gui._file_list = Listbox(values=(),
                                 background_color='#e0e4f0',
                                 enable_events=True,
                                 highlight_background_color='#a0c0e0',
                                 highlight_text_color='#000000',
                                 key='file_list',
                                 select_mode=PySimpleGUI.SELECT_MODE_MULTIPLE,
                                 size=(24, 12),
                                 text_color='#202020')
        Gui._selection_text = Text(background_color='#101010',
                                   font=(None, 14, 'italic'),
                                   justification='c',
                                   key='selection_text',
                                   size=(24, 1),
                                   text_color='#808080')
        Gui._select_all_button = Button(button_text='Select All',
                                        disabled=True,
                                        key='select_all_button',
                                        size=(11, 1))
        Gui._deselect_all_button = Button(button_text='Deselect All',
                                          disabled=True,
                                          key='deselect_all_button',
                                          size=(11, 1))
        Gui._retrieve_button = Button(button_text='Retrieve',
                                      disabled=True,
                                      font=(None, 16, 'bold'),
                                      key='retrieve_button',
                                      size=(10, 1))
        Gui._delete_button = Button(button_text='Delete',
                                    disabled=True,
                                    key='delete_button',
                                    size=(11, 1))
        Gui._interactive_elements = (Gui._addr_input, Gui._load_button, Gui._file_list,
                                     Gui._select_all_button, Gui._deselect_all_button,
                                     Gui._retrieve_button, Gui._delete_button)
        
        Gui._window = Window(background_color='#101010',
                             element_justification='c',
                             element_padding=(9, 9),
                             font=(None, 16),
                             layout=(
                                 (Gui._info_text,),
                                 (Gui._addr_input, Gui._load_button),
                                 (Gui._file_list,),
                                 (Gui._selection_text,),
                                 (Gui._select_all_button, Gui._deselect_all_button),
                                 (Gui._retrieve_button, Gui._delete_button),
                             ),
                             margins=(48, 48),
                             title='Retrieve Data From Sensor')
        Gui._window.finalize()
        Gui._addr_input.update(select=True)
        Gui._addr_input.set_focus(True)
        
        while True:
            event, values = Gui._window.read()
            if event == PySimpleGUI.WIN_CLOSED: break
            print(f'event: {event}\nvalues: {values}\n')
            if event == 'load_button':
                Gui._info_text.update('Loading...')
                Gui._load_button.update(disabled=True)
                Gui._file_list.update(values=())
                Gui._selection_text.update('')
                Gui._retrieve_button.update(disabled=True)
                Gui._delete_button.update(disabled=True)
                Retriever.get_files(Gui._handle_retriever_got_files)
            elif event == 'file_list':
                count = len(values['file_list'])
                Gui._selection_text.update(
                    (count > 0) * f'{count:,} file{(count > 1) * "s"} selected')
                Gui._retrieve_button.update(disabled=(not values['file_list']))
                Gui._delete_button.update(disabled=(not values['file_list']))
            elif event == 'select_all_button':
                count = len(Gui._file_list.get_list_values())
                Gui._file_list.update(set_to_index=tuple(range(count)))
                Gui._selection_text.update(f'{count:,} file{(count > 1) * "s"} selected')
                Gui._retrieve_button.update(disabled=False)
                Gui._delete_button.update(disabled=False)
            elif event == 'deselect_all_button':
                Gui._file_list.update(set_to_index=())
                Gui._selection_text.update('')
                Gui._retrieve_button.update(disabled=True)
                Gui._delete_button.update(disabled=True)
            elif event == 'delete_button':
                Gui._set_disabled(True)
                count = len(values['file_list'])
                response = PySimpleGUI.popup(
                    f'Delete {count:,} file{(count > 1) * "s"} from the watch?',
                    button_type=PySimpleGUI.POPUP_BUTTONS_YES_NO,
                    font=(None, 16),
                    keep_on_top=True,
                    title='Delete')
                Gui._set_disabled(False)
                if response != 'Yes': continue
                print('Deleting...')
    
    def _set_disabled(is_disabled):
        for element in Gui._interactive_elements: element.update(disabled=is_disabled)
    
    def _handle_retriever_got_files(error_message, files):
        if error_message: files = ()
        message = error_message or 'Select files to retrieve or delete'
        Gui._info_text.update(message)
        Gui._load_button.update(disabled=False, text=('Reload' if files else 'Load'))
        Gui._file_list.update(values=files)
        Gui._select_all_button.update(disabled=(not files))
        Gui._deselect_all_button.update(disabled=(not files))

class Retriever:
    def get_files(on_got_files: Callable[[str, tuple[str]], None]):
        import random
        from threading import Timer
        Timer(
            random.random() * 2,
            on_got_files,
            (None,
             random.sample(
                 ('haha.wav', 'haha.csv', 'wow.wav', 'wow.csv', 'bye.wav', 'bye.csv',
                  'memes.wav', 'memes.csv', 'echo.wav', 'echo.csv', 'creativity.wav',
                  'creativity.csv', 'less.wav', 'less.csv', 'orange.wav', 'orange.csv'),
                 k=random.randint(2, 16))),
        ).start()

if __name__ == '__main__': App.start()
