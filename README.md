# Amperage
Amperage is a console Windows app designed to help you enable Recall on devices that aren't officially supported.

At the moment, Amperage can only enable recall if your machine has an _Arm64 CPU / SoC_. That is, any **Qualcomm Snapdragon**, **Microsoft SQ**, or **Ampere** chipset.

Make sure that you're running Windows 11 version 24H2 **build 26100.712** before continuing. Older builds, as well as newer betas (builds 26200-26217) do not include the necessary OS level components for Recall.

Most x86_64 users will have to wait until Microsoft publishes AI Components for their platform. _However_, if you're feeling particularly adventurous you can try [Emulating Arm64 Windows on your x86_64 Windows PC](https://github.com/thebookisclosed/AmperageKit/blob/main/ArmOnX86_64.md)

26100.712 is also available on [Azure ARM VMs](https://learn.microsoft.com/en-us/windows/arm/create-arm-vm) by using a 24H2 image (currently 26100.560) and installing KB5037589.

# How do I get started?
1) Download the AI Components (Machine Learning workloads) for Arm64 from [here](https://archive.org/details/windows-workloads-0.3.252.0-arm-64.7z)
2) Unpack the contents of the 7z archive to a folder called `WorkloadComponents`
3) Head to the [Releases](https://github.com/thebookisclosed/AmperageKit/releases) page and download the latest version
4) Unpack the release you've just downloaded
5) Move the `WorkloadComponents` folder from earlier next to `Amperage.exe`
6) Fire up Command Prompt as Administrator and navigate to the directory you extracted Amperage to
7) Type in `amperage /install` and hit Enter

The tool will now guide you through Recall installation. It should all finish in one fell swoop but in case you happened to misplace any files, it will let you know as soon as you run it.
![image](https://github.com/thebookisclosed/AmperageKit/assets/13197516/722ffccb-3c16-4d3e-bf4c-b959d01588e3)
