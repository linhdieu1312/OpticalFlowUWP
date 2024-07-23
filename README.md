Hướng dẫn cài đặt
* Yêu cầu:
- Hệ điều hành từ Windows 10 trở lên
- Visual Studio phiên bản 2022
- Mở Visual Studio 2022, chọn mở file .sln

* Cài đặt các NuGet package trong các folder:
- Mở Solution Explorer
- Chọn folder CensorVideo :
	+ chọn Manage Nuget package > cài đặt Win2D.uwp và Microsoft.NETCore.UniversalWindowsPlatform
	+ chọn Add > Reference: chọn add component là  CommonLib và  CustomVideoEffect

- Chọn folder CommonLib > chọn Manage Nuget package 
	+ cài đặt Win2D.uwp
	+ cài đặt Newtonsoft.Json
	+ cài đặt Microsoft.NETCore.UniversalWindowsPlatform
- Chọn folder CustomVideoEffects 
	+ chọn Manage Nuget package > cài đặt Win2D.uwp và  Microsoft.NETCore.UniversalWindowsPlatform
	+ chọn Add > Reference: chọn add component là  CommonLib và  OpenCVcomp

* Cài đặt vcpkg chạy thư viện OpenCV:
1. Cài vcpkg theo hướng dẫn: https://github.com/microsoft/vcpkg#quick-start-windows
- Cài Git  
- Mở PowerShell (Administrator), cd vào thư mục dự định dùng để chứa vcpkg
- Chạy 2 lệnh: 
> git clone https://github.com/microsoft/vcpkg

> .\vcpkg\bootstrap-vcpkg.bat
 
2. Cài gói OpenCV x64
> .\vcpkg\vcpkg install opencv3[core]:x64-uwp
 
3. Chạy lệnh integrate
> .\vcpkg\vcpkg integrate install

* Chạy chương trình
- Solution platform: x64
- Trong CensorVideo, chá»n Set as Startup Project
