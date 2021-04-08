const d3 = require('d3');
const {BrowserWindow, ipcRenderer, remote} = require('electron');
const dialog = remote.dialog;

class View {
  /** @type {function(MouseEvent)} */
  static onStartButtonClicked;

  /** @type {function()} */
  static onWindowClose;

  /** @param {string} hostAddr */
  static init(hostAddr) {
    View.window = remote.getCurrentWindow();
    View.mainScreen = d3.select('#main-screen');
    View.addrLabel = d3.select('#addr-label');
    View.disconnectedLabel = d3.select('#disconnected-label');
    View.connectedLabel = d3.select('#connected-label');
    View.runningLabel = d3.select('#running-label');
    View.timeLabel = d3.select('#time-label');
    View.startButton = d3.select('#start-button');
    View.settingsButton = d3.select('#settings-button');
    View.settingsScreen = d3.select('#settings-screen');

    ipcRenderer.on('close', () => View.onWindowClose?.apply());
    // app.on('close', e => View.onWindowClose?.apply(e));
    View.startButton.on('click', e => View.onStartButtonClicked?.apply(e));
    View.settingsButton.on('click', () => View.toggleSettingsScreen(true));

    View.addrLabel.text(hostAddr);
  }

  /** Connected but not yet running. */
  static gotoConnected() {
    View.disconnectedLabel.classed('hidden', true);
    View.connectedLabel.classed('hidden', false);
    View.runningLabel.classed('hidden', true);
    View.startButton.classed('btn-outline-secondary', false)
        .classed('btn-secondary', true)
        .attr('disabled', null)
        .style('cursor', 'pointer')
        .text('Start');

    if (View.runningTimeIntervalHandle) {
      clearInterval(View.runningTimeIntervalHandle);
      View.runningTimeIntervalHandle = null;
    }
  }

  static gotoRunning() {
    View.disconnectedLabel.classed('hidden', true);
    View.connectedLabel.classed('hidden', true);
    View.runningLabel.classed('hidden', false);
    View.startButton.classed('btn-outline-secondary', false)
        .classed('btn-secondary', true)
        .attr('disabled', null)
        .style('cursor', 'pointer')
        .text('Stop');

    View.timeLabel.text('0:00');
    View.runningTimeElapsed = 0;
    View.runningTimeIntervalHandle = setInterval(() => {
      View.runningTimeElapsed++;
      View.timeLabel.text([
        // m:ss
        Math.floor(View.runningTimeElapsed / 60).toString(),
        (View.runningTimeElapsed % 60).toString().padStart(2, '0'),
      ].join(':'));
    }, 1000);
  }

  static gotoDisconnected() {
    View.disconnectedLabel.classed('hidden', false);
    View.connectedLabel.classed('hidden', true);
    View.runningLabel.classed('hidden', true);
    View.startButton.classed('btn-outline-secondary', true)
        .classed('btn-secondary', false)
        .attr('disabled', '')
        .style('cursor', 'not-allowed')
        .text('Start');

    if (View.runningTimeIntervalHandle) {
      clearInterval(View.runningTimeIntervalHandle);
      View.runningTimeIntervalHandle = null;
    }
  }

  /** @param {function(boolean)} onClicked Called with `true` if yes. */
  static showRunningCloseConfirmation(onClicked) {
    dialog
        .showMessageBox(View.window, {
          title: 'WearableML Server',
          type: 'warning',
          message: 'Stop running?',
          buttons: ['&Yes', '&No'],
          noLink: true,
        })
        .then(res => onClicked(res.response == 0));
  }

  static closeWindow() {
    View.window.removeAllListeners('close');
    View.window.close();
  }

  /** @param {boolean} show */
  static toggleSettingsScreen(show) {
    View.mainScreen.classed('hidden', show);
    View.settingsScreen.classed('hidden', !show);
  }

  /**
   * @protected
   * @type {BrowserWindow}
   */
  static window;

  /**
   * @protected
   * @type {d3.Selection<d3.BaseType, *, HTMLElement, *>}
   */
  static mainScreen;

  /**
   * @protected
   * @type {d3.Selection<d3.BaseType, *, HTMLElement, *>}
   */
  static addrLabel;

  /**
   * @protected
   * @type {d3.Selection<d3.BaseType, *, HTMLElement, *>}
   */
  static disconnectedLabel;

  /**
   * @protected
   * @type {d3.Selection<d3.BaseType, *, HTMLElement, *>}
   */
  static connectedLabel;

  /**
   * @protected
   * @type {d3.Selection<d3.BaseType, *, HTMLElement, *>}
   */
  static runningLabel;

  /**
   * @protected
   * @type {d3.Selection<d3.BaseType, *, HTMLElement, *>}
   */
  static timeLabel;

  /**
   * @protected
   * @type {d3.Selection<d3.BaseType, *, HTMLElement, *>}
   */
  static startButton;

  /**
   * @protected
   * @type {d3.Selection<d3.BaseType, *, HTMLElement, *>}
   */
  static settingsButton;

  /**
   * @protected
   * @type {d3.Selection<d3.BaseType, *, HTMLElement, *>}
   */
  static settingsScreen;

  /** @protected */
  static runningTimeIntervalHandle = null;

  /**
   * @protected
   * @type {number}
   */
  static runningTimeElapsed;
}

module.exports = {View};
