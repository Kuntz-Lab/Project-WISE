# Wearable-ML

The current software requires both a Samsung watch which runs Tizen OS, and a PC that runs Windows, macOS, or Linux. Currently, the app can record the heartbeat, accelerometer through the smartwatch, and voice through the PC app. We will guide you through the installation steps in the following document. This is still at the early stage of the software development; we have not created the machine learning component. If you have further questions regarding installing the software, please do not hesitate to reach out to us through Professor Alan Kuntz or directly to us: kevin.song@utah.edu, qianlangchen@gmail.com.

***

## How to Setup

### Windows Server

1. Download the server from [here](https://github.com/asianboii-chen/WearableML/raw/main/CrossPlatformServer/WindowsServer.exe)

2. Double-click to run

### Tizen Sensor

1. Download most up to date [Visual Studio](https://visualstudio.microsoft.com/)

2. Download most up to date [Tizen Studio](https://developer.tizen.org/development/tizen-studio/download)

3. Connecting to a Wearable Device from Tizen Studio

  * Setting up Galaxy Watch Device to a Host PC via Wi-Fi [here](https://developer.samsung.com/galaxy-watch-develop/testing-your-app-on-galaxy-watch.html)
  
  * In order to install a Tizen application, you must first register certificates. You might encounter errors when creating the certificate profile, here is the link that go over [these steps](https://developer.samsung.com/tizen/blog/en-us/2019/03/04/samsung-certificate-profile-for-samsung-wearables). 
  
  * If you have any more questions, please feel free to reach out, we are more than happy to help you with any debugging.

*Once you establish connection between the watch and the Tizen Studio, now its time to install the WearableML to the watch!*

4. Install the following Visual Studio extensions:

  * [Visual Studio Tools for Tizen](https://marketplace.visualstudio.com/items?itemName=tizen.VisualStudioToolsforTizen)
  
  * [Dogfood VSIX](https://marketplace.visualstudio.com/items?itemName=JamieCansdale.DogfoodVsix)

5. Clone or download this repository and open `TizenSensor/TizenSensor.sln` with Visual Studio.

6. Deploy the application onto the watch: follow [the steps under *Run MySteps on Samsung Galaxy Watch*](https://developer.samsung.com/tizen/Galaxy-Watch/get-started/creating-and-running-a-project.html#Deploying-and-running-your-application)

***

## How to Connect and Run

1. Once you have completed the steps above, congratulations! You are ready to run the app!

2. Make sure the watch is connected with the same WiFi as the PC

3. Open up the PC server app and you should see an IP address

4. Type out the IP address in the watch and hit *Connect!*

5. Hit *Start* button on the PC server app and begin the recording. 

6. You can hit *Stop* button to end the recording, and the PC will open up a folder with the audio and sensor recording data (Note that if the recording session is long, this process might up to 30 seconds).
