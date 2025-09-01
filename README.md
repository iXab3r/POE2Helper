# EyeAuras PoeHelper
![Img](https://s3.eyeauras.net/media/2024/12/NVIDIA_Overlay_h5x9BnjFNpbtypN5.png)

[YouTube Demo for v0.3](https://youtu.be/SOkuX6xluww)

This is a small prototype, **free-for-all** tool, which intends to test/verify [EyeAuras](https://eyeauras.net/) memory reading API in practice.

Currently its primary role is propagating HP/MP/ES levels to EyeAuras Behavior Trees. In the future I may add cooldown/hp/buffs tracking as well.

## **It does not intent to compete with GameHelper/ExileCore/PoeHUD/etc** 
There will be no plugins, there will be no radar and other excellent features which GH/EC have. 
This code intends to fuel conditional potion/skill use and other similar automations. That is it.

![BT](https://s3.eyeauras.net/media/2024/12/NVIDIA_Overlay_ZETbjmdKSZXdNyWt.png)

## How it works
Reads game process memory using ReadProcessMemory or any other memory-reading approach supported by EyeAuras (e.g. using WinPMEM driver).
But for PoE that is not really needed, RPM is more than enough.

Initially it was using static offsets but was migrated to use approach similar to what [GameHelper](https://www.ownedcore.com/forums/mmo/path-of-exile/poe-bots-programs/953353-gamehelper-light-version-of-poehud-exile-api-351.html)
and [ExileCore](https://www.ownedcore.com/forums/path-of-exile-2/path-of-exile-2-buy-sell-trade/1057794-exilecore2-exileapi-successor-beta-access.html) use.

# Credits
- [`KronosQC`](https://www.ownedcore.com/forums/members/1524193-kronosqc.html) - for providing initially used static offsets
- [`GameHelper`](https://www.ownedcore.com/forums/members/1040190-gamehelper.html) - author of [GameHelper](https://www.ownedcore.com/forums/path-of-exile-2/path-of-exile-2-bots-program/1062208-gamehelper-poe-2-a-2.html), a lot of the code/offsets in this repo are taken from his product. 
- [`TehCheat`](https://www.ownedcore.com/forums/members/950876-tehcheat.html) - author of original [PoE Helper](https://www.ownedcore.com/forums/mmo/path-of-exile/poe-bots-programs/980571-poehelper-exileapi-3-20-forbidden-sanctum.html)
- [`cheatingeagle`](https://www.ownedcore.com/forums/members/1159132-cheatingeagle.html) - author of [ExileCore](https://www.ownedcore.com/forums/path-of-exile-2/path-of-exile-2-buy-sell-trade/1057794-exilecore2-exileapi-successor-beta-access.html) 
