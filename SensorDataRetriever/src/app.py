'''
Used to retrieve audio and sensor reading data from a Tizen Sensor.

Project WISE -- Wearable-ML
Qianlang Chen
F 06/04/21
'''

from collections.abc import Callable
from http.client import HTTPResponse
import math
from os import path
import PySimpleGUI
from PySimpleGUI import Button, Element, Input, Listbox, Text, Window
from threading import Thread
from urllib import request
from urllib.error import URLError
import webbrowser

class App:
    def start():
        Gui.start()

class Gui:
    _window: Window = None
    _info_text: Text = None
    _address_input: Input = None
    _address_input_value = ''
    _load_button: Button = None
    _file_list: Listbox = None
    _file_list_selection: tuple[str] = ()
    _selection_text: Text = None
    _select_all_button: Button = None
    _deselect_all_button: Button = None
    _retrieve_button: Button = None
    _retrieve_button_handle: Button = None
    _retrieve_button_selection = ''
    _delete_button: Button = None
    _is_element_disabled: dict[Element, bool] = None
    
    def start():
        Gui._info_text = Text(background_color='#101010',
                              font=(None, 18, 'italic'),
                              justification='c',
                              key='info_text',
                              border_width=8,
                              size=(24, 1),
                              text='Enter sensor address')
        Gui._address_input = Input(default_text='192.168.0.', key='address_input', size=(16, 1))
        Gui._load_button = Button(button_text='Load', key='load_button', size=(6, 1))
        Gui._file_list = Listbox(values=(),
                                 background_color='#e0e4f0',
                                 enable_events=True,
                                 highlight_background_color='#a0c0e0',
                                 highlight_text_color='#000000',
                                 key='file_list',
                                 select_mode=PySimpleGUI.SELECT_MODE_MULTIPLE,
                                 size=(28, 12),
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
                                        size=(12, 1))
        Gui._deselect_all_button = Button(button_text='Deselect All',
                                          disabled=True,
                                          key='deselect_all_button',
                                          size=(12, 1))
        Gui._retrieve_button = PySimpleGUI.FolderBrowse(button_text='Retrieve',
                                                        disabled=True,
                                                        key='retrieve_button',
                                                        size=(12, 1),
                                                        target='retrieve_button_handle')
        # FolderBrowser is buggy at reporting events
        Gui._retrieve_button_handle = Button(enable_events=True,
                                             key='retrieve_button_handle',
                                             visible=False)
        Gui._delete_button = Button(button_text='Delete',
                                    disabled=True,
                                    key='delete_button',
                                    size=(12, 1))
        Gui._is_element_disabled = {
            x: False for x in (Gui._address_input, Gui._load_button, Gui._file_list,
                               Gui._select_all_button, Gui._deselect_all_button,
                               Gui._retrieve_button, Gui._delete_button)
        }
        
        Gui._window = Window(background_color='#101010',
                             element_justification='c',
                             element_padding=(9, 9),
                             font=(None, 16),
                             layout=(
                                 (Gui._info_text,),
                                 (Gui._address_input, Gui._load_button),
                                 (Gui._file_list,),
                                 (Gui._selection_text,),
                                 (Gui._select_all_button, Gui._deselect_all_button),
                                 (Gui._retrieve_button, Gui._delete_button),
                                 (Gui._retrieve_button_handle,),
                             ),
                             margins=(48, 48),
                             title='Retrieve Data From Sensor')
        Gui._window.finalize()
        Gui._address_input.update(select=True)
        Gui._address_input.set_focus(True)
        
        while True:
            event, values = Gui._window.read()
            if event == PySimpleGUI.WIN_CLOSED: break
            Gui._address_input_value = values['address_input']
            Gui._file_list_selection = values['file_list']
            Gui._retrieve_button_selection = values['retrieve_button']
            if event == 'load_button': Gui._handle_load_button_clicked()
            elif event == 'file_list': Gui._handle_file_list_selected()
            elif event == 'select_all_button': Gui._handle_select_all_button_clicked()
            elif event == 'deselect_all_button': Gui._handle_deselect_all_button_clicked()
            elif event == 'retrieve_button_handle': Gui._handle_retrieve_button_selected()
            elif event == 'delete_button': Gui._handle_delete_button_clicked()
    
    def _format_x_files(x):
        return f'{x:,} file{(x > 1) * "s"}'
    
    def _disable_element(should_disable, *elements):
        for element in elements:
            element.update(disabled=should_disable)
            Gui._is_element_disabled[element] = should_disable
    
    def _disable_window(should_disable):
        for element, was_disabled in Gui._is_element_disabled.items():
            element.update(disabled=(should_disable or was_disabled))
    
    def _update_file_list(files):
        Gui._file_list.update(files)
        Gui._disable_element(not files, Gui._select_all_button, Gui._deselect_all_button)
    
    def _update_selection_indication(selection_size):
        Gui._selection_text.update(
            (selection_size > 0) * f'{Gui._format_x_files(selection_size)} selected')
        Gui._disable_element(selection_size == 0, Gui._retrieve_button, Gui._delete_button)
    
    def _handle_load_button_clicked():
        Gui._disable_window(True)
        Gui._info_text.update('Loading files...')
        Gui._update_file_list(())
        Gui._update_selection_indication(0)
        Retriever.address = Gui._address_input_value
        Retriever.get_files(Gui._handle_retriever_got_files)
    
    def _handle_file_list_selected():
        Gui._update_selection_indication(len(Gui._file_list_selection))
    
    def _handle_select_all_button_clicked():
        count = len(Gui._file_list.get_list_values())
        Gui._file_list.update(set_to_index=tuple(range(count)))
        Gui._update_selection_indication(count)
    
    def _handle_deselect_all_button_clicked():
        Gui._file_list.update(set_to_index=())
        Gui._update_selection_indication(0)
    
    def _handle_retrieve_button_selected():
        if not Gui._retrieve_button_selection: return
        Gui._disable_window(True)
        Retriever.retrieve_files(Gui._file_list_selection, Gui._retrieve_button_selection,
                                 Gui._handle_retriever_retrieving_files)
        Gui._info_text.update(f'Preparing to retrieve...')
        Gui._retrieve_button.TKStringVar.set('')
        Gui._retrieve_button_selection = ''
    
    def _handle_delete_button_clicked():
        Gui._disable_window(True)
        count = len(Gui._file_list_selection)
        if PySimpleGUI.popup(f'Delete {Gui._format_x_files(count)} from the watch?',
                             button_type=PySimpleGUI.POPUP_BUTTONS_YES_NO,
                             font=(None, 16),
                             keep_on_top=True,
                             title='Delete') != 'Yes':
            Gui._disable_window(False)
            return
        Gui._info_text.update(f'Deleting {Gui._format_x_files(count)}...')
        Retriever.delete_files(Gui._file_list_selection, Gui._handle_retriever_deleted_files)
    
    def _handle_retriever_got_files(message, files):
        Gui._disable_window(False)
        if message: files = ()
        Gui._info_text.update(message or 'Select files to retrieve or delete')
        Gui._load_button.update(text='Reload')
        Gui._update_file_list(files)
    
    def _handle_retriever_retrieving_files(message, progress):
        if message:
            Gui._disable_window(False)
            Gui._info_text.update(message)
            return
        if progress == 1.:
            Gui._disable_window(False)
            Gui._info_text.update(
                f'Successfully retrieved {Gui._format_x_files(len(Gui._file_list_selection))}!')
            webbrowser.open(Gui._retrieve_button_selection)
            return
        Gui._info_text.update(f'Retrieving... ({progress:.0%})')
    
    def _handle_retriever_deleted_files(message):
        Gui._disable_window(False)
        if message:
            Gui._info_text.update(message)
            return
        Gui._info_text.update(
            f'Successfully deleted {Gui._format_x_files(len(Gui._file_list_selection))}!')
        Gui._update_file_list(
            tuple(x for x in Gui._file_list.get_list_values()
                  if x not in Gui._file_list_selection))
        Gui._update_selection_indication(0)

# TODO: remove these fake stuffs
import random, time
from threading import Timer

class Retriever:
    _CONNECTION_FAILED_MESSAGE = 'Connection failed!'
    
    address: str = None
    _has_stopped = False
    
    def get_files(on_got_files: Callable[[str, tuple[str]], None]):
        def f():
            try:
                response = Retriever._request('list')
                file_names = response.read().decode('utf-8').strip().split('\n')
                on_got_files(None, tuple(sorted(file_names)))
            except URLError:
                on_got_files(Retriever._CONNECTION_FAILED_MESSAGE, None)
        
        Thread(target=f).start()
    
    def retrieve_files(file_names: tuple[str], target_dir_path: str,
                       on_retrieving_files: Callable[[str, float], None]):
        def f():
            try:
                total_bytes = 0
                for name in file_names:
                    response = Retriever._request('size', name)
                    total_bytes += int(response.read().decode('utf-8'))
                on_retrieving_files(None, 0.)
                bytes_loaded = 0
                for name in file_names:
                    response = Retriever._request('retrieve', name)
                    target_path = path.join(target_dir_path, name)
                    with open(target_path, 'wb') as target:
                        while True:
                            data = response.read(2**20)
                            if not data: break
                            target.write(data)
                            bytes_loaded += len(data)
                            on_retrieving_files(None, bytes_loaded / total_bytes)
                on_retrieving_files(None, 1.)
            except URLError:
                on_retrieving_files(Retriever._CONNECTION_FAILED_MESSAGE, math.nan)
        
        Thread(target=f).start()
    
    def delete_files(file_names: tuple[str], on_deleted_files: Callable[[str], None]):
        def f():
            try:
                for name in file_names:
                    response = Retriever._request('delete', name)
                    if response.read() != b'1':
                        on_deleted_files(Retriever._CONNECTION_FAILED_MESSAGE)
                on_deleted_files(None)
            except URLError:
                on_deleted_files(Retriever._CONNECTION_FAILED_MESSAGE)
        
        Thread(target=f).start()
    
    def _request(command, *args) -> HTTPResponse:
        return Retriever._fake_request(command, *args)
        formatted_args = (len(args) > 0) * ':' + ','.join(map(str, args))
        print(f'http://{Retriever.address}:3456/{command}{formatted_args}')
        return request.urlopen(f'http://{Retriever.address}:3456/{command}{formatted_args}',
                               timeout=1)
    
    def _fake_request(command, arg=''):
        time.sleep(random.random() * 2)
        if not random.randint(0, 34): return request.urlopen('http://gorgeous-failure')
        if command == 'list':
            return request.urlopen(
                'http://urlecho.appspot.com/echo?status=200&Content-Type=text%2Fhtml&body=21-02-21-12-31-15-Audio.wav%0A21-04-29-18-57-30-Sensor.csv%0A21-04-29-18-57-30-Audio.wav%0A20-10-31-02-14-10-Audio.wav%0A20-10-31-02-14-10-Sensor.csv%0A21-02-21-12-31-15-Sensor.csv%0A'
            )
        elif command == 'size':
            return request.urlopen(
                f'http://urlecho.appspot.com/echo?status=200&Content-Type=text%2Fhtml&body={(len(arg) + 1) * int(arg[3:5]) * 12}'
            )
        elif command == 'retrieve':
            return request.urlopen(
                f'http://urlecho.appspot.com/echo?status=200&Content-Type=text%2Fhtml&body={(arg + "%0a") * int(arg[3:5]) * 12}'
            )
        elif command == 'delete':
            return request.urlopen(
                f'http://urlecho.appspot.com/echo?status=200&Content-Type=text%2Fhtml&body=1')
    
    def _fake_get_files(on_got_files):
        Timer(
            random.random() * 2,
            on_got_files,
            (None if random.randint(0, 2) else Retriever._CONNECTION_FAILED_MESSAGE,
             random.sample(
                 ('haha.wav', 'haha.csv', 'wow.wav', 'wow.csv', 'bye.wav', 'bye.csv',
                  'memes.wav', 'memes.csv', 'echo.wav', 'echo.csv', 'creativity.wav',
                  'creativity.csv', 'less.wav', 'less.csv', 'orange.wav', 'orange.csv'),
                 k=random.randint(0, 16))),
        ).start()
    
    def _fake_retrieve_files(file_names: tuple[str], target_dir_path: str,
                             on_retrieving_files: Callable[[str, float], None]):
        def f(p):
            if p >= 1.:
                on_retrieving_files(None, 1.)
                return
            if math.isnan(p):
                p = 0
            else:
                if not random.randint(0, 34):
                    on_retrieving_files(Retriever._CONNECTION_FAILED_MESSAGE, math.nan)
                    return
                on_retrieving_files(None, p)
            Timer(random.random() * 1 + .5, f, ((p or 0.) + random.random() * .25,)).start()
        
        f(math.nan)
    
    def _fake_delete_files(file_names: tuple[str], on_deleted_files: Callable[[str], None]):
        Timer(
            random.random() * 2, on_deleted_files,
            (None if random.randint(0, 2) else Retriever._CONNECTION_FAILED_MESSAGE,)).start()

if __name__ == '__main__': App.start()
