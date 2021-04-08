package com.androidsensors.sensor;

import androidx.appcompat.app.AppCompatActivity;

import android.os.Bundle;
import android.view.View;

import java.io.IOException;

public class MainActivity extends AppCompatActivity {
  
  @Override
  protected void onCreate(Bundle savedInstanceState) {
    super.onCreate(savedInstanceState);
    setContentView(R.layout.activity_main);
    
    String recordFilename = getFilesDir().getAbsolutePath() + "/sensor_temp.3gp";
    sensor = new Sensor(recordFilename);
  }
  
  protected Sensor sensor;
  
  public void onStartButtonClick(View view) throws IOException {
    sensor.start();
  }
  
  public void onStopButtonClick(View view) {
    sensor.stop();
  }
  
  public void onPlaybackButtonClick(View view) throws IOException {
    sensor.playback();
  }
}
