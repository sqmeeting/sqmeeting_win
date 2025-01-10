# SQMeeting Windows客户端

# 系统要求

## 操作系统
Windows 7， Windows 8（8.1）， Windows 10 和 Windows 11。 推荐Windows 10和Windows 11。

目前只支持生成为Win32程序，不支持生成64位。生成的Win32程序在64位操作系统中可正常运行。

## .Net Framework,MSVC和Windows SDK版本

SQMeeting Windows使用.Net Framework 4.7.2， VC 141， Windows SDK 17763。

# 编译生成和运行

## 编译工具
[Visual Studio](https://visualstudio.microsoft.com/) 2017 或更高版本。  
在build之前，请一定要通过vs installer安装.Net Framework 4.7.2， VC 141 2017 c/c++ build tool， Windows SDK 17763,  否则build会失败!

## 生成
安装好对应版本的.Net Framework， VC++和Windows SDK后， 使用VS2017或以上版本打开项目根目录下SQMeeting.sln解决方案， 直接进行编译即可。

## 运行
生成成功后在项目根目录中找到生成好的Release_FRTC目录, 运行SQMeeting.exe。

## SDK Demo生成和运行
SDKDemo演示如何在已有应用中集成frtc_sdk, 获得强大的音视频协作能力.  
SDKDemo基于.net core 6.0框架，使用VS2022打开项目根目录SDKDemo/SDKDemo.sln解决方案， 编译配置选择Release或Debug, 目标平台选择x86， 直接进行编译即可。  
编译成功后运行生成目录中的SDKDemo.exe。


# License
本项目基于 [Apache License, Version 2.0](./LICENSE) 开源，请在开源协议约束范围内使用源代码。  
本项目代码仅用于学习和研究使用。 任何使用本代码产生的后果，我们不承担任何法律责任。  
请联系我们获取商业支持.

# SQMeeting(神旗视讯)官网
访问[神旗视讯官网](https://shenqi.internetware.cn) 可以免费下载神旗视讯服务平台.  
3分钟，就可以部署一套完整的企业视频会议系统. 

