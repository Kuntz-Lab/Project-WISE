const {AudioRecorder, SensorDataRecorder, Server, Util} = require('./model')
const {View} = require('./view');

const recordDirName =
    __dirname.substr(0, __dirname.lastIndexOf('src')) + 'records';
let isRunning = false;

View.onWindowClose = () => {
  if (!isRunning) {
    View.closeWindow();
    return;
  }

  View.showRunningCloseConfirmation(close => {
    if (close) {
      isRunning = false;
      SensorDataRecorder.stop(true);
      AudioRecorder.stop(true);
      Util.showInExplorer(recordDirName);
      setTimeout(() => View.closeWindow(), 1042);
    }
  });
};
View.onStartButtonClicked = () => {
  isRunning = !isRunning;
  if (isRunning) {
    const dateString = Util.formatDate(new Date());
    SensorDataRecorder.start(50, recordDirName, dateString + '-sensor.csv');
    AudioRecorder.start(recordDirName, dateString + '-audio.wav');
    View.gotoRunning();
  } else {
    SensorDataRecorder.stop();
    AudioRecorder.stop();
    View.gotoConnected();
    Util.showInExplorer(recordDirName);
  }
};
View.init(Server.getAddr());

SensorDataRecorder.onConnected = () => View.gotoConnected();
SensorDataRecorder.onDisconnected = () => {
  View.gotoDisconnected();
  if (!isRunning) return;

  isRunning = false;
  AudioRecorder.stop();
  Util.showInExplorer(recordDirName);
};
SensorDataRecorder.init();

AudioRecorder.init();
