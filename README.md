# Amperage
Amperage is a console Windows app designed to help you enable Recall on devices that aren't officially supported.

At the moment, Amperage can only enable recall if your machine has an _Arm64 CPU / SoC_. That is, any **Qualcomm Snapdragon**, **Microsoft SQ**, or **Ampere** chipset.

Make sure that you're running a build of Windows 11 that contains Recall. Here's a list of valid builds as of 6/21/2024:
* Windows 11 version 24H2
  * 26100.7xx
  * 26120.7xx
* Windows 11 Insider Preview (Canary)
  * 26227.5000
  * 26231.5000
  * 26236.5000

Older revisions of Windows 11 version 24H2 (.6xx and lower) as well as newer ones (.8xx) do not include the necessary OS level components for Recall. The same applies for Canary builds before 26227 and after 26236.

Most x86_64 users will have to wait until Microsoft publishes AI Components for their platform. _However_, if you're feeling particularly adventurous you can try [Emulating Arm64 Windows on your x86_64 Windows PC](https://github.com/thebookisclosed/AmperageKit/blob/main/ArmOnX86_64.md)

26100.712 is also available on [Azure ARM VMs](https://learn.microsoft.com/en-us/windows/arm/create-arm-vm) by using a 24H2 image (currently 26100.560) and installing KB5037850.

# How do I get started?
1) Head to the [Releases](https://github.com/thebookisclosed/AmperageKit/releases) page and download the latest version
2) Download the AI Components (Machine Learning workloads) for Arm64 from [here](https://archive.org/details/windows-workloads-0.3.252.0-arm-64.7z)
3) Unpack the release you've just downloaded
4) Unpack the contents of the Workloads archive into the `WorkloadComponents` folder next to `Amperage.exe`
5) Fire up Command Prompt as Administrator and navigate to the directory you extracted Amperage to
6) Type in `amperage /install` and hit Enter

The tool will now guide you through Recall installation. It should all finish in one fell swoop but in case you happened to misplace any files, it will let you know as soon as you run it.
![image](https://github.com/thebookisclosed/AmperageKit/assets/13197516/722ffccb-3c16-4d3e-bf4c-b959d01588e3)
