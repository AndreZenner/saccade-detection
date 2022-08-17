# saccade-detection
A simple (heuristic-based) algorithm for online **saccade onset** and **blink** detection with the HTC Vive Pro Eye VR headset.

## Download
To use our saccade detection, simply download and import [**saccade-detection-v2.unitypackage**](saccade-detection-v2.unitypackage) into your Unity project (tested with Unity version 2021.3.7f1 LTS).

## Requirements
To use our saccade detection in your own Unity application, make sure to have the following assets imported in your project:

- [**SteamVR**](https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647)
- [**SRanipal**](https://developer.vive.com/resources/vive-sense/eye-and-facial-tracking-sdk/download/latest/)

## How to use
To check out an example scene, just open and start any of the scenes that come with our package.  
To add saccade detection to your own scene do the following:

add to your scene:  

  - **`SaccadeDetection prefab`**  
  - **`SRanipal Eye Framework prefab`** from SRanipal (ViveSR)   
    - check the `Enable Eye Data` box  
    - select *Version 2* for `Enable Eye Version`  
  - **`CameraRig prefab`** from SteamVR  
  
    
<p><img src="https://github.com/AndreZenner/saccade-detection/tree/main/pics/SRanipal-inspector-screenshot.PNG" alt="SRanipal Eye Framework Inspector" width="350"></p>


## Saccade Detection Settings
The picture below shows the *Saccade Detection Inspector* including all *adjustable variables*.

<p><img src="https://github.com/AndreZenner/saccade-detection/tree/main/pics/SD-inspector-screenshot-variables.PNG" alt="Saccade Detection Inspector Variables" width="350"></p>


**Shown Data Settings**  
`Show_PhysicalCalculations`  
true: writes angle, speed and acceleration values for current retrieved data into the console.

`Show_Framerate`  
true: writes the current Update and EyeTracker frequence for each second into the console.

`Show_Eye`  
true: writes 'Eyes closed' into the console whenever the EyeValue undercuts the closedEyeThreshold.


**Test Mode**  
`Test Mode`  


`Simulate Input`  
true: the `.csv inputFile` in `TestScenario` is used for every algorithm analysis, resulting in better comparisons.


**Saccade Detection Mode**  

`Separate Eye`  
true: all physical calculations are computed for each eye. The corresponding thresholds must exceed **both** eye values.
false: all physical calculations are computed according to the combined eye value.


**Saccade Detection Thresholds**  

`Speed Threshold`  
Speed Threshold for Saccade Detection [degrees/ second]. If *eye rotation > threshold* then it might be a  saccade.

`Speed Threshold Once`  
Speed Threshold for Saccade Detection [degrees/ second^2] which only needs to be *exceeded ONCE in 3 frames*. This is included in the sample threshold.

`Speed Noise Threshold`  
Speed Threshold above which considered measured speed as noise [degrees/ second]. If *eye rotation > threshold* then the current sample does not increase the sample counter.

`Acceleration Threshold`  
Acceleration Threshold for Saccade Detection [degrees/ second^2]. If *eye rotation > threshold* then it might be a saccade.

`Sample Threshold`  
How many of the most recent of all speed samples must exceed the defined speedThreshold. OnceSpeed is included in this one.

`Break Threshold`  
For *breakThreshold seconds* after a blink no saccades will be detected.

`Closed Eye Threshold`  
Threshold which determines whether the eye is interpreted as closed (if *eyeOpeness < closedEyeThreshold*) or not. Eye Openess values are in the range *from 0.0 (closed) to 1.0 (open)*.


## Example Scenes
There are *three* different Example Scenes which differ in the occuring saccade size: **Easy, Medium, Difficult**.

<p><img src="https://github.com/AndreZenner/saccade-detection/tree/main/pics/SD-easyScene-setup.PNG" alt="Saccade Detection Example Scene Easy Setup" width="350"></p>

<p><img src="https://github.com/AndreZenner/saccade-detection/tree/main/pics/SD-easyScene-hierarchy-2.PNG" alt="Saccade Detection Example Scene Easy Hierarchy" width="350"></p>

<p><img src="https://github.com/AndreZenner/saccade-detection/tree/main/pics/Logging-inspector.PNG" alt="Logging" width="350"></p>

## Credits
Before use, please see the [LICENSE](LICENSE.md) for copyright and license details.

<p><img src="https://www.inf.uni-hamburg.de/25610386/vhive-logo-10cb0fb4711320d5f662386dd29b49889c5ff3b0.png" alt="VHIVE Logo" width="250"></p>
<p><img src="https://www.inf.uni-hamburg.de/25610329/dfg-b87508c85acc9755665f0b2d363660ccf2a403ce.jpg" alt="DFG Logo" width="250"></p>

This open-source package is part of the DFG project [VHIVE](https://www.inf.uni-hamburg.de/en/inst/ab/hci/projects/vhive.html) and was created by [André Zenner](https://umtl.cs.uni-saarland.de/people/andre-zenner.html) and Chiara Karr at the [UMTL](https://umtl.cs.uni-saarland.de/) at Saarland University.
This work was supported by the Deutsche Forschungsgemeinschaft (DFG, German Research Foundation), the [Deutsches Forschungszentrum für Künstliche Intelligenz GmbH](https://www.dfki.de/) (DFKI; German Research Center for Artificial Intelligence), and [Saarland University](https://www.uni-saarland.de/).
<p><img src="pics/dfki-logo.jpg" alt="DFKI Logo" width="250"></p>
<p><img src="pics/uds-logo.png" alt="Saarland University Logo" width="250"></p>
