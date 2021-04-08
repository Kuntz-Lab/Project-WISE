const {shell} = require('electron');
const fs = require('fs');
const net = require('net');
const os = require('os');
const RecordRTC = require('recordrtc');

class Util {
  /**
   * yy-MM-dd-hh-mm-ss
   * @param {Date} date
   */
  static formatDate(date) {
    return [
      (date.getFullYear() % 100).toString().padStart(2, '0'),
      (date.getMonth() + 1).toString().padStart(2, '0'),
      (date.getDate()).toString().padStart(2, '0'),
      (date.getHours()).toString().padStart(2, '0'),
      (date.getMinutes()).toString().padStart(2, '0'),
      (date.getSeconds()).toString().padStart(2, '0'),
    ].join('-');
  }

  /** @param {string} path */
  static showInExplorer(path) {
    shell.openItem(path);
  }
}

/** Currently supports only one client socket. */
class Server {
  /** @type {function():void} */
  static onConnected;

  /** @type {function(string):void} */
  static onReceived;

  /** @type {function():void} */
  static onDisconnected;

  static getAddr() {
    let interfaces = os.networkInterfaces();
    for (let p in interfaces) {
      for (let q in interfaces[p]) {
        let addr = interfaces[p][q];
        if (addr.family == 'IPv4' && !addr['internal'])
          Server.hostAddr = addr.address;
      }
    }
    return Server.hostAddr;
  }

  static start() {
    Server.server = net.createServer();
    Server.server.maxConnections = 1;
    Server.server.listen(Server.PORT, Server.hostAddr);
    Server.server.on('connection', Server.onServerConnection);
  }

  /** @param {string} data */
  static send(data) {
    Server.socket.write(data + '\n');
  }

  /**
   * @protected
   * @constant
   */
  static PORT = 6912;

  /**
   * @protected
   * @type {string}
   */
  static hostAddr;

  /**
   * @protected
   * @type {net.Server}
   */
  static server;

  /**
   * @protected
   * @type {net.Socket}
   */
  static socket;

  /**
   * @protected
   * @param {net.Socket} socket
   */
  static onServerConnection(socket) {
    Server.socket = socket;
    Server.onConnected?.call(socket);
    socket.on('data', Server.onSocketData);
    socket.on('close', Server.onSocketClose);
  }

  /**
   * @protected
   * @param {Buffer} data
   */
  static onSocketData(data) {
    for (let token of data.toString().trim().split('\n'))
      Server.onReceived?.call(Server.socket, token);
  }

  /** @protected */
  static onSocketClose() {
    Server.onDisconnected?.call(Server.socket);
    Server.socket = null;
  }
}

class SensorData {
  /** @constant */
  static HEADER = [
    'Seconds', 'Heart Rate', 'X Acceleration', 'Y Acceleration',
    'Z Acceleration', 'X Angular Velocity', 'Y Angular Velocity',
    'Z Angular Velocity'
  ].join(',');

  /**
   * @param {string} json
   * @returns {SensorData}
   */
  static fromJSON(json) {
    return Object.assign(new SensorData(), JSON.parse(json));
  }

  toCSV() {
    return [
      this.seconds.toFixed(2),
      this.heartRate,
      this.accelerationX.toFixed(2),
      this.accelerationY.toFixed(2),
      this.accelerationZ.toFixed(2),
      this.angularVelocityX.toFixed(1),
      this.angularVelocityY.toFixed(1),
      this.angularVelocityZ.toFixed(1),
    ].join(',');
  }

  /** @protected */
  constructor() {
    this.seconds = 0;
    this.heartRate = 0;
    this.accelerationX = 0;
    this.accelerationY = 0;
    this.accelerationZ = 0;
    this.angularVelocityX = 0;
    this.angularVelocityY = 0;
    this.angularVelocityZ = 0;
  }
}

class SensorDataRecorder {
  /** @type {function():void} */
  static onConnected;

  /** @type {function():void} */
  static onDisconnected;

  static init() {
    Server.onConnected = SensorDataRecorder.onServerConnected;
    Server.onReceived = SensorDataRecorder.onServerReceived;
    Server.onDisconnected = SensorDataRecorder.onServerDisconnected;
    Server.start();
  }

  /**
   * @param {number} updateInterval
   * @param {string} dirName
   * @param {string} fileName
   */
  static start(updateInterval, dirName, fileName) {
    SensorDataRecorder.dirName = dirName;
    SensorDataRecorder.path = dirName + '/' + fileName;
    SensorDataRecorder.buffer.length = 0;
    SensorDataRecorder.buffer.push(SensorData.HEADER + '\n');
    Server.send('start ' + updateInterval);
  }

  static stop() {
    Server.send('stop');
    SensorDataRecorder.save();
  }

  /**
   * @protected
   * @constant
   */
  static BUFFER_SIZE = 1728;

  /**
   * @protected
   * @type {string[]}
   */
  static buffer = [];

  /**
   * @protected
   * @type {string}
   */
  static path;

  /**
   * @protected
   * @type {string}
   */
  static dirName;

  /** @protected */
  static save() {
    const toSave = SensorDataRecorder.buffer.concat();
    SensorDataRecorder.buffer.length = 0;
    if (!fs.existsSync(SensorDataRecorder.dirName))
      fs.mkdirSync(SensorDataRecorder.dirName);
    fs.appendFile(SensorDataRecorder.path, toSave.join(''), ex => {
      if (ex) throw ex;
    });
  }

  /** @protected */
  static onServerConnected() {
    SensorDataRecorder.onConnected?.apply();
  }

  /**
   * @protected
   * @param {string} data
   */
  static onServerReceived(data) {
    let sensorData = null;
    try {
      sensorData = SensorData.fromJSON(data);
    } catch (ex) {
      console.warn(
          `Error occurred while parsing sensor data: '${data}'\n${ex}`);
      return;
    }
    SensorDataRecorder.buffer.push(sensorData.toCSV() + '\n');
    if (SensorDataRecorder.buffer.length == SensorDataRecorder.BUFFER_SIZE)
      SensorDataRecorder.save();
  }

  /** @protected */
  static onServerDisconnected() {
    SensorDataRecorder.onDisconnected?.apply();
    if (SensorDataRecorder.buffer.length) SensorDataRecorder.save();
  }
}

class AudioRecorder {
  static init() {
    navigator.mediaDevices.getUserMedia({audio: true}).then(stream => {
      AudioRecorder.recorder = new RecordRTC(stream, {
        type: 'audio',
        recorderType: RecordRTC.StereoAudioRecorder,
        mimeType: 'audio/wav',
        numberOfAudioChannels: 1,
        desiredSampRate: 16000,
        disableLogs: true,
      });
    });
  }

  /**
   * @param {string} dirName
   * @param {string} fileName
   */
  static start(dirName, fileName) {
    AudioRecorder.dirName = dirName;
    AudioRecorder.path = dirName + '/' + fileName;
    AudioRecorder.recorder.startRecording();
  }

  static stop() {
    AudioRecorder.recorder.stopRecording(AudioRecorder.onRecorderStopped);
  }

  /**
   * @protected
   * @type {RecordRTC}
   */
  static recorder;

  /**
   * @protected
   * @type {string}
   */
  static dirName;

  /**
   * @protected
   * @type {string}
   */
  static path;

  /** @protected */
  static onRecorderStopped() {
    if (!fs.existsSync(AudioRecorder.dirName))
      fs.mkdirSync(AudioRecorder.dirName);
    let reader = new FileReader();
    reader.onload = function() {
      AudioRecorder.recorder.reset();
      fs.writeFile(
          AudioRecorder.path, Buffer.from(new Uint8Array(this.result)), ex => {
            if (ex) throw ex;
          });
    };
    reader.readAsArrayBuffer(AudioRecorder.recorder.getBlob());
  }
}

module.exports = {
  Util,
  Server,
  SensorData,
  SensorDataRecorder,
  AudioRecorder
};
