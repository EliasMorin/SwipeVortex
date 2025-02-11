
# SwipeVortex

Swipe Vortex is an ASP .NET tool using various APIs to collect data and also interact with it. The project is in build and supports main APIs like Tinder or Bumble, Happn and Instagram.


## Installation

You will differents configurations before using the tool.              
Please ensure that you have NET 8.0 installed on your computer.  
The project is also builded for arm64.
```bash
  curl -o dotnet-install.sh https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh && chmod +x dotnet-install.sh && bash dotnet-install.sh
  git clone https://github.com/EliasMorin/SwipeVortex/ 
```
    
## Deployment

You can  the precompiled dll directly or even build it yourself after modification 

```bash
  dotnet build && dotnet run
```


## API Reference

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `X-Auth-Token` | `string` | Tinder |
| `User ID` | `string` | Tinder |


| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `session_cookie` | `string` | Bumble |

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `Username & Password` | `string` | Instagram |

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `token` | `string` | Happn |




## Screenshots

-Main Page (Dashboard) 

![App Screenshot](https://github.com/user-attachments/assets/da836331-725e-47c7-a657-aa9083bc2802)

-Bottom of Dashboard

![image](https://github.com/user-attachments/assets/dfd4776a-51b1-412f-ab15-e50b5b82f789)

-Example of data that the program can show (Happn case)

![image](https://github.com/user-attachments/assets/477c0158-7b0e-4490-a80d-f594e5c1ee51)


## Demo

-Example of use (Bumble)

https://github.com/user-attachments/assets/499c388d-ea9a-4af7-ba87-c5d0638f4362.mp4
