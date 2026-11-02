This GitHub page holds the updated version of the Unity plugin Realtime-CSG.

If you use this plugin, please let us know, we'd love to see what you've done with it!

![Example](Readme/Images/house_view.png)
<sup>Programmer art example. Note: couch, plants and some other small props are meshes, not CSG.</sup>


**How to install**:
---
**Git (Requires having [git](https://git-scm.com/) installed)**

* In the `Packages/` folder of your project, in the command line, use the git command `git clone https://github.com/Nuklear-Interactive/Realtime-CSG-2.0.git com.prenominal.realtimecsg`

**Manual Download**

* [Download the latest release](https://github.com/Nuklear-Interactive/Realtime-CSG-2.0/releases)
* Copy the folder in the downloaded zip to the `Packages/` folder.

**Unity Package Manager (UPM)**

* Open up the package manager in Window/Package Manager
* Press the + button in the top left corner and select Install package from Git URL
* Import the project by entering `https://github.com/Nuklear-Interactive/Realtime-CSG-2.0.git` into the text field  

---

**Updating:**
---
**Git**

* Run `git merge main` in the console, from the `com.prenominal.realtimecsg` folder

**Manual Update**
* Close Unity
* Delete the `com.prenominal.realtimecsg` from `Packages/`
* Repeat the steps from "Manual Download" above

**Unity Package Manager (UPM)**
* Simply update the package

---

**FAQs:**
---

**Q: Does Realtime-CSG support runtime editing?**

  * A: **No.** Realtime-CSG does **not** support or have API for runtime editing. This does not stop you from using the native binary to create your own bindings to use. We do not provide support for that, so you are on your own in doing this. If you are unsure where to start, [then you can check out the native folder](https://github.com/Nuklear-Interactive/Realtime-CSG-2.0/tree/main/Editor/Control/Bindings), which has the bindings that Realtime-CSG uses.

**Q: How do I turn Realtime-CSG off in the editor?**

  * A: Simply press CTRL+F3 to toggle it!

**Q: Can I use Realtime-CSG to edit other meshes?**

  * A: No. Realtime-CSG is intended to work with its own brushes, which generates its own output mesh. It cannot be used for that purpose.

**Q: Can I have access to the native C++ code?**

  * A: No. Unless stated otherwise, it will not be available to the public. Sorry for that. It only contains the CSG algorithms anyway, and the actual functionality of the tool is all done on the managed C# side of things.

**Q: Is there a build of the native binary for my OS distro?**

 * A: Realtime-CSG currently supports the following platforms:
   - Windows x64
   - MacOS (Intel)

   There are plans and work being done in the background to bring support to Linux. This does not mean it will support *all* linux distros. Just the popular ones like Debian, Ubuntu, Mint, etc.

   Unsupported platforms:
   - Any ARM/Silicon platforms. We don't have access to an ARM/Silicon system. This may/may not change if one of us gets one.

**Q: Can I build Realtime-CSG for another OS?**

  * A: See "Can I have access to the native C++ code?".

**Links:**
---

* **Discord server**: [https://discord.gg/BK5gt8R](https://discord.gg/BK5gt8R)
* **Project website**: [https://logicalerror.github.io](https://logicalerror.github.io/realtime-csg/index.html)
* ~~**Asset Store (outdated)**: https://assetstore.unity.com/packages/tools/modeling/realtime-csg-69542~~ No longer supported or updated.

---

**Contributing:**
---

* Fork this repo
* Create your own branch
* Submit a PR to `working` (do not submit PRs from the master/main branch of your fork)

<sub>README last updated November 2nd, 2026</sub>
