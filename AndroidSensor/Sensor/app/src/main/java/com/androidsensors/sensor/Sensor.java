package com.androidsensors.sensor;

import android.media.MediaPlayer;
import android.media.MediaRecorder;
import android.util.Log;

import java.io.IOException;

public class Sensor {
  
  public Sensor(String recordFilename) {
    this.recordFilename = recordFilename;
  }
  
  protected String recordFilename;
  protected MediaRecorder recorder;
  
  public void start() throws IOException {
    if (recorder != null) return;
    
    recorder = new MediaRecorder();
    recorder.setAudioSource(MediaRecorder.AudioSource.MIC);
    recorder.setOutputFormat(MediaRecorder.OutputFormat.THREE_GPP);
    recorder.setAudioEncoder(MediaRecorder.OutputFormat.AMR_NB);
    recorder.setOutputFile(recordFilename);
    recorder.prepare();
    recorder.start();
    
    Log.i("sensor", "started");
  }
  
  public void stop() {
    if (recorder == null) return;
    
    recorder.stop();
    recorder.reset();
    recorder.release();
    recorder = null;
    
    Log.i("sensor", "stopped");
  }
  
  public void playback() throws IOException {
    if (recorder != null) return;
    
    MediaPlayer player = new MediaPlayer();
    player.setDataSource(recordFilename);
    player.prepare();
    player.start();
    
    Log.i("sensor", "playing back; duration: " + player.getDuration());
  }
}
