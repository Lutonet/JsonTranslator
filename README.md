# Lutonet-JsonTranslator 

*Your helper with translations of your pages or application*

Thank you for your interest in this project. 
I hope you will find it helpful and useful.

## The Quick start

### 1. Install from the source code
``` You need to have the [.NET6 SDK](https://dotnet.microsoft.com/en-us/download) installed. You can find it [here](https://dotnet.microsoft.com/en-us/download)
Download the project source code to your local folder 
cd to the folder 
edit the appsettings.json file to set API servers and managed folders in the filesystem, or on the FTP servers. Also set the code of the defaul language ("en")
in the same folder open the terminal application and run following set of commands: 
dotnet restore 
dotnet publish 
Run the application 
dotnet run 
```

## What is Lutonet JsonTranslator?

**Json translator is an application which helps you to translate your pages or application to multiple version in json format.**

It can be useful mainly for MERN or other web developers, application is small, console application, but it can be useful for any developer.

Executables are available for Windows and Linux, they include also all .NET libraries.

Most of i18n projects use sort of dictionaries for translations, stored in different formats, where I like to use json. 
In such as case all phrases are stored in one file, and each phrase is stored in one line, and they are based on key-value pairs where I call those too as 
phrase and a text. Phrase is a key which stays unchanged in different translation files, while text is localized. 

for example en.json can look like this: 

```
{
    "greeting": "Hello",
    "message": "How are you?"
}
```

and the same example for de.json would be 

```
{
    "greeting": "Hallo",
    "message": "Wie gehts dir?"
}
```

Application is using different user defined (on premises or public) LibreTranslate API servers between which is work automatically distributed. 

Important configuration is stored in appsettings.json file - which **can be modified during execution of the service** and new settings should be used immediately
important section is: 
```
"ServiceSettings": {
    "Servers": [
      {
        "Address": "http://10.0.0.4:5000",
        "UseKey": false,
        "Key": ""
      },
      {
        "Address": "http://10.0.0.100:5000",
        "UseKey": false,
        "Key": ""
      },
      {
        "Address": "http://10.0.0.3:5000",
        "UseKey": false,
        "Key": ""
      },
      {
        "Address": "http://10.0.0.6:5000",
        "UseKey": false,
        "Key": ""
      }
    ],
    "Folders": [ "D:\\test", "D:\\test2" ],
    "FTPs": [
      {
        "Server": "ftp://10.0.0.7",
        "Folder": [ "test" ],
        "Login": "test",
        "Password": "test"
      }
    ],
    "DefaultLanguage": "en",
    "IgnoreLanguages": [ "cs" ]
  }
```

Application runs in infinite cycles and checks for changes each 10 minutes. Running application should not consume more than 40MB of the server's memory.
** Application uses the Multi threading - and Libre translate servers are accessed in parallel.** it means more libretranslate servers you use, faster the translation should be.
Workload is distributed proportionally "on demand" to the number of servers, to minimize overal translation time. In testing environment 4 API servers generated performance of 20 translated phrases per second.


## Important sections in the JSON file

`Servers` in this section is a list of LibreTranslate servers you wish to use for translation. They can be public or you can deploy your own with a docker  container.
[Libre translate](https://github.com/LibreTranslate/LibreTranslate) is great and free to use. At least one server is required.

`Folders` in this section is a list of folders you wish to monitor - those folders must be local and application must have access right to read/write in them. 

`FTPs` in this section is a list of FTP servers you wish to monitor - on each can be monitored one or more of folders - those are defined in the `Folder` property in the
FTPs section in the array. If you **don't want to use FTP** just leave the section as it is, but don't delete it.
`DefaultLanguage` is also mandatory property and it is a code of the language you want to use as a default language. whose json file will be used as a base for all other languages.
`IgnoreLanguages` here you can define languages you don't want to translate automatically. if you want to translate into all available languages just define an empty array `[]`