# saccade-detection
A simple (heuristic-based) algorithm for online **saccade onset** and **blink** detection with the HTC Vive Pro Eye VR headset.

## Download
To use our saccade detection, simply download and import [`saccade-detection-v2.unitypackage`](saccade-detection-v2.unitypackage) into your Unity project (tested with Unity version 2021.3.7f1 LTS).

## Requirements
To use our saccade detection in your own Unity application, make sure to have the following assets imported in your project:

* [SteamVR](https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647)
* [SRanipal](https://developer.vive.com/resources/vive-sense/eye-and-facial-tracking-sdk/download/latest/)

## How to use
To check out an example scene, just open and start any of the scenes that come with our package.  
To add saccade detection to your own scene do the following:

add to your scene:


SaccadeDetection prefab

SRanipal Eye Framework prefab from SRanipal (ViveSR)

CameraRig prefab from SteamVR

## Credits
Before use, please see the [LICENSE](LICENSE.md) for copyright and license details.

<p><img src="https://www.inf.uni-hamburg.de/25610386/vhive-logo-10cb0fb4711320d5f662386dd29b49889c5ff3b0.png" alt="VHIVE Logo" width="250"></p>
<p><img src="https://www.inf.uni-hamburg.de/25610329/dfg-b87508c85acc9755665f0b2d363660ccf2a403ce.jpg" alt="DFG Logo" width="250"></p>

This open-source package is part of the DFG project [VHIVE](https://www.inf.uni-hamburg.de/en/inst/ab/hci/projects/vhive.html) and was created by [André Zenner](https://umtl.cs.uni-saarland.de/people/andre-zenner.html) and Chiara Karr at the [UMTL](https://umtl.cs.uni-saarland.de/) at Saarland University.
This work was supported by the Deutsche Forschungsgemeinschaft (DFG, German Research Foundation), the [Deutsches Forschungszentrum für Künstliche Intelligenz GmbH](https://www.dfki.de/) (DFKI; German Research Center for Artificial Intelligence), and [Saarland University](https://www.uni-saarland.de/).
<p><img src="pics/dfki-logo.jpg" alt="DFKI Logo" width="250"></p>
<p><img src="pics/uds-logo.png" alt="Saarland University Logo" width="250"></p>
