'''
Used to retrieve audio and sensor reading data from a Tizen Sensor.

Project WISE -- Wearable-ML
Qianlang Chen
F 07/02/21
'''

from collections.abc import Callable
from http.client import HTTPResponse
import math
from os import path
import PySimpleGUI
from PySimpleGUI import Button, Element, Input, Listbox, Text, Window
from threading import Thread
from urllib import request
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
        Gui._load_button = Button(bind_return_key=True,
                                  button_text='Load',
                                  key='load_button',
                                  size=(6, 1))
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
            else: print('Unhandled:', event, values)
    
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
        # Set internal value for retrieve_button so it wouldn't report the previous selection
        # when the user cancels the second prompt
        Gui._retrieve_button.TKStringVar.set('')
    
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

class Retriever:
    _CONNECTION_FAILED_MESSAGE = 'Connection failed!'
    _MAX_NUM_ATTEMPTS = 6
    
    address: str = None
    
    def get_files(on_got_files: Callable[[str, tuple[str]], None]):
        def f():
            for _ in range(Retriever._MAX_NUM_ATTEMPTS):
                try:
                    response = Retriever._request('list')
                    file_names = response.read().decode('utf-8').strip().split('\n')
                    on_got_files(None, tuple(sorted(file_names)))
                    break
                except Exception as ex: print('Caught:', ex)
            else:
                on_got_files(Retriever._CONNECTION_FAILED_MESSAGE, None)
        
        Thread(target=f).start()
    
    def retrieve_files(file_names: tuple[str], target_dir_path: str,
                       on_retrieving_files: Callable[[str, float], None]):
        def f():
            total_bytes = 0
            for name in file_names:
                for _ in range(Retriever._MAX_NUM_ATTEMPTS):
                    try:
                        response = Retriever._request('size', name)
                        total_bytes += int(response.read().decode('utf-8'))
                        break
                    except Exception as ex: print('Caught:', ex)
                else:
                    on_retrieving_files(Retriever._CONNECTION_FAILED_MESSAGE, math.nan)
                    return
            
            on_retrieving_files(None, 0.)
            bytes_loaded = max_bytes_loadoed = 0
            for name in file_names:
                for _ in range(Retriever._MAX_NUM_ATTEMPTS):
                    try:
                        old_bytes_loaded = bytes_loaded
                        response = Retriever._request('retrieve', name)
                        target_path = path.join(target_dir_path, name)
                        with open(target_path, 'wb') as target:
                            while True:
                                data = response.read(2**20)
                                if not data: break
                                target.write(data)
                                bytes_loaded += len(data)
                                if bytes_loaded > max_bytes_loadoed:
                                    max_bytes_loadoed = bytes_loaded
                                    on_retrieving_files(None, bytes_loaded / (total_bytes or 1))
                        break
                    except Exception as ex:
                        print('Caught:', ex)
                        bytes_loaded = old_bytes_loaded
                else:
                    on_retrieving_files(Retriever._CONNECTION_FAILED_MESSAGE, math.nan)
                    return
            
            on_retrieving_files(None, 1.)
        
        Thread(target=f).start()
    
    def delete_files(file_names: tuple[str], on_deleted_files: Callable[[str], None]):
        def f():
            for name in file_names:
                for _ in range(Retriever._MAX_NUM_ATTEMPTS):
                    try:
                        response = Retriever._request('delete', name)
                        content = response.read()
                        if content != b'1': raise Exception(f'Failed to delete: {content}')
                        break
                    except Exception as ex: print('Caught:', ex)
                else:
                    on_deleted_files(Retriever._CONNECTION_FAILED_MESSAGE)
                    return
            
            on_deleted_files(None)
        
        Thread(target=f).start()
    
    def _request(command, *args) -> HTTPResponse:
        formatted_args = (len(args) > 0) * ':' + ','.join(map(str, args))
        url = f'http://{Retriever.address}:3456/{command}{formatted_args}'
        print('Open:', url)
        return request.urlopen(url, timeout=1)

if __name__ == '__main__': App.start()
