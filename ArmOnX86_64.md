# Emulating Arm64 Windows on your x86_64 Windows PC
If you got here you're most likely interested in giving Arm64 Windows a try as soon as possible.

While possible, emulating a modern day Arm64 SoC on even the most powerful x86_64 hardware puts significant strain on your resources.

Installing Windows may take upwards to 2-3 hours and the experience may be sluggish in general. Although, when has that ever stopped curiosity?

The following guide has been adapted from [Linaro's windows-arm64 VM using qemu-system](https://linaro.atlassian.net/wiki/spaces/WOAR/pages/28914909194/windows-arm64+VM+using+qemu-system) Confluence page

# Hardware requirements
* At least an (8) octa-core CPU
* At least 8 GB of RAM free
* ~70 GB of free disk space

# File prerequisites
1) Windows 11 version 24H2 build 26100.712 installation image from [here](https://uupdump.net/download.php?id=977f33e9-2c4e-4e07-9652-709b4bb4ec9f&pack=en-us&edition=professional)
2) Latest version of QEMU from [here](https://qemu.weilnetz.de/w64/)
3) VirtIO drivers from [here](https://fedorapeople.org/groups/virt/virtio-win/direct-downloads/latest-virtio/virtio-win.iso)
4) `qemu-efi-aarch64` package from the [Debian repo](https://packages.debian.org/sid/all/qemu-efi-aarch64/download) (only used as a source of firmware)

# Chapter 1: Preparing the emulation setup
## Step 1: Creating a folder where you'll store the virtual machine and related assets
For the purposes of this tutorial, the working folder will be `D:\w11arm64`

Once created, move the VirtIO drivers ISO (prerequisite #3) inside.

## Step 2: Downloading Windows setup media
Due to the limited availability of up-to-date arm64 Windows media, we'll have to get it from a place like UUPDump. Simply download the media creation kit linked above (prerequisite #1), extract it, and run the script contained within.
The process should be fully automated and once it's done, you'll have a ready-to-use ISO right next to the script you ran.

Move the generated ISO to the `D:\w11arm64` directory and give it a less cumbersome filename, say, `Windows11_arm64.iso`

## Step 3: Installing QEMU
To install QEMU simply double-click the setup you've downloaded as prerequisite #2. When asked which components you'd like to install, you can uncheck all System Emulation items except `aarch64`

![image](https://github.com/thebookisclosed/AmperageKit/assets/13197516/2fde3bb2-e341-4edd-9151-bd44706d0273)

## Step 4: Extracting the firmware image
Using 7z or another archiving tool of your choice, extract `data.tar\.\usr\share\qemu-efi-aarch64\QEMU_EFI.fd` from the package you've downloaded as prerequisite #4 into `D:\w11arm64\QEMU_EFI.fd`.

## Step 5: Creating a hard disk image for the VM
Open a command prompt and enter the following commands:
1) `cd /d D:\win11arm64` (this navigates to the folder we've been preparing)
2) `set "PATH=%PATH%;C:\Program Files\qemu"` (this lets us run QEMU's exes without always specifying the full path)
3) `qemu-img create win11arm64.img 64G` (this creates the hard drive image)

Do not close this command prompt window as it will be useful in the coming steps.

# Chatper 2: Starting installation
## Step 1
Enter this multiline command and hit enter. A virtual machine with 8 cores (`-smp 8`) and 8 GB of RAM (`-m 8G`) allocated will be started. Make sure to click into the window and once you see a `Press any key to boot from CD or DVD..` prompt hit the Enter key on your keyboard a few times.
```
qemu-system-aarch64 ^
-M virt,virtualization=true -m 8G -cpu max,pauth-impdef=on -smp 8 ^
-bios ./QEMU_EFI.fd ^
--accel tcg,thread=multi ^
-device ramfb ^
-device qemu-xhci -device usb-kbd -device usb-tablet ^
-nic user,model=virtio-net-pci ^
-device usb-storage,drive=install ^
-drive if=none,id=install,format=raw,media=cdrom,file=./Windows11_arm64.iso ^
-device usb-storage,drive=virtio-drivers ^
-drive if=none,id=virtio-drivers,format=raw,media=cdrom,file=./virtio-win-0.1.248.iso ^
-drive if=virtio,id=system,format=raw,file=./win11arm64.img
```

![image](https://github.com/thebookisclosed/AmperageKit/assets/13197516/1b1f2bb9-fed3-41a6-9b8d-1f3c0ab5e405)


## Step 2: Bypassing TPM and secure boot requirements
Once you're booted into the Windows installer, press `Shift-F10` on your keyboard to open command prompt within the installer.

![image](https://github.com/thebookisclosed/AmperageKit/assets/13197516/e1d1f1fd-b63c-4000-bc46-66f4ba58fbbd)

Type `regedit` and hit Enter. Now navigate to `HKLM\System\Setup` and create a new key called `LabConfig`. Inside that key, create two DWORD values calles `BypassTPMCheck` and `BypassSecureBootCheck` and set them to a value of `1`.
This is how the registry editor panes should look if you've done everything correctly.

![image](https://github.com/thebookisclosed/AmperageKit/assets/13197516/68410b48-dec9-47a2-a370-3840128861b8)

## Step 3: Adding storage drivers
You can continue through Windows setup as you would normally. Once you reach the disk selection page you'll notice that there are no disks to pick from. To fix this click the 💿 `Load driver` label, then `Browse` and finally select the `E:\viostor\w11\ARN64` folder. Once installed, the rest of the Windows setup process should be mostly unattended like on a standard x86_64 PC.

_Similar steps can be repeated on install the `NetKVM` driver to establish a network connection once Windows boots into the first-run experience._

![image](https://github.com/thebookisclosed/AmperageKit/assets/13197516/c8ee9439-a50d-4a61-9951-ee396cce3590)


