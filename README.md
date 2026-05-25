# Tripledot-Lead-TA-Test
Lead Technical Artist — Project Review:

General notes:
I avoided redoing the animations and project structure from scratch because I preferred to focus on accurate feedback and markup that didn't distort the established structure.
Instead, I preferred to extend and improve what had been done, explain and correct the error, rather than follow a totally different approach; 
which is what I do every day in my professional context, where I apply the greatest possible pragmatism.

I haven't highlighted in this short presentation many of the issues I encountered, such as shader issues, text issues, or incorrect UI configurations, which were riddled throughout the project, but wherever possible, I've made fixes directly in the project.

ButtonFooterController:
The provided version is glitchy because it lacks the unfocused animation state, and the cursor script has various issues.
</br>
Old</br>
<img width="427" height="157" alt="BottomBarBefore" src="https://github.com/user-attachments/assets/516f7245-a3ed-4ae0-9ec7-7e88817c52b0" />
</br></br>
The new version has all the required states, doesn't rely on dotween or other tweener, has zero allocations, and is written more robustly.
</br>
New</br>
<img width="427" height="157" alt="BottomBarAfter" src="https://github.com/user-attachments/assets/09770d11-335b-4159-9b79-41a128976a42" />

Screen Ratio Responsiveness:</br>
The provided version has a distortion in the background that doesn't adapt based on the aspect ratio of the display. It has a cumbersome SafeArea script that is brought as is from the Unity Forum; he did this despite the problems reported in the same forum thread.
</br>
Old</br>
<img width="310" height="408" alt="ScreenDevicesBefore" src="https://github.com/user-attachments/assets/07d4fbc9-f0a7-4aa8-b5d5-108127336a24" />
</br></br>

The new SafeArea is way simpler; it relies on the native Unity Screen.safeArea property and has a responsive background that won't distort regardless of the device's screen ratio.</br>
New</br>
<img width="310" height="408" alt="ScreenDevicesAfter" src="https://github.com/user-attachments/assets/72202be6-f67b-43b6-abe0-e875747d5242" />


Settings Popup:


The provided version lacks some elements outlined in the brief, such as scalability, a blur feature, and base localization readiness. It also has some graphical glitches. The background is cut off under the notch, and the image spills beyond the edges of the screen.</br>
Old</br>
<img width="370" height="751" alt="PopupOpenBefore" src="https://github.com/user-attachments/assets/4f69d6cd-8317-48de-a0cb-2a909ad1bff7" />

The new pop-up is more scalable, and it presents a mobile-friendly blur feature (I developed long ago and it's still used in Scrabble Go and Monopoly Go). There is still a localization sheet missing, which I did not cover in this test.</br>
New</br>
<img width="370" height="751" alt="PopupOpenAfter" src="https://github.com/user-attachments/assets/4e1ac70a-77a3-49a4-b44d-410af182bbd8" />

Level Completed Screen:
Unfortunately, once again, the material produced falls short of its intended purpose: "impressive animation and creative flair."
Analyzing the key steps one by one, we observe that:
The transition between Home and LevelCompleted Screen relies on Load Scene, and since there's no cache, the transition is glitchy and often cuts off the initial part of the animation.
There's indeed an effort to showcase shadergraph prowess, but this misses the mark since the proposed shaders don't add much visual value; instead do add a fair amount of GPU overhead, with many passes that could be trivially rasterized to a tiny texture or replaced by a lightweight particle system.
Finally, the screen does not show a sequence of the displayed information, but rather shows everything abruptly at once.
</br>
Old</br>
<img width="370" height="751" alt="LevelCompleteBefore" src="https://github.com/user-attachments/assets/bdb5a708-9096-4a97-b78e-0644b3ae8ca6" />
</br>
In the new version, the scene is cached before starting the transition, creating a smooth effect.
Furthermore, the sine wave animation script (which also added a significant performance burden) has been replaced with a much lighter and more controllable animated text component.
Furthermore, the elements are introduced one by one, creating a more pleasing sequence effect.
</br>
New</br>
<img width="370" height="751" alt="LevelCompleteAfter" src="https://github.com/user-attachments/assets/11a8d34f-21b8-48d5-bcb8-aa0deb2b6f1d" />


"LevelCompleted!" Text

Dynamically reconstructing the appearance of a text style from Photoshop to Unity TMP is a tedious process that never guarantees 100% accuracy.
Depending on the project requirements and artistic direction, you may opt for fully rasterized text.
In this case, the Unity style is very different from the Photoshop layout; there was no great effort to get it right, and some properties highlighted in the layout, such as glint FX and sparkles, are missing.


</br>
Photoshop reference:</br>
<img width="811" height="361" alt="Screenshot 2026-05-25 173453" src="https://github.com/user-attachments/assets/97284fd8-f3c2-4bd4-80fd-3e1b03d178a9" />

Old</br>
Quite a different style, no bevel, no shadow, a random glow. No Glint, no sparkles.
<img width="811" alt="Screenshot 2026-05-25 173028" src="https://github.com/user-attachments/assets/5d8ab808-6a26-4c18-97ca-40df2ce3c7a9" />

New</br>
I recreated the Psd style, but I still have not created the glint or the sparkles VFX.
<img width="811" alt="Screenshot 2026-05-25 180058" src="https://github.com/user-attachments/assets/ae3ead76-5246-4fc9-81c2-767e50e80ed1" />



