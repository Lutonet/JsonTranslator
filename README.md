# Lutonet-JsonTranslator 

*Your helper with translations of your pages or application*

Thank you for your interest in this project. 
I hope you will find it helpful and useful.

## What is Lutonet JsonTranslator?

**Json translator is an application which helps you to translate your pages or application to multiple version in json format.**

It can be useful mainly for MERN or other web developers, application is small, console application, but it can be useful for any developer.

Executables are available for Windows and Linux, they include also all .NET libraries.

Most of i18n projects use sort of dictionaries for translations, stored in different formats, where I like to use json. 
In such as case all phrases are stored in one file, and each phrase is stored in one line, and they are based on key-value pairs where I call those too as 
phrase and a text. Phrase is a key which stays unchanged in different translation files, while text is localized. 

for example en.json can look like this: 

`{
    "greeting": "Hello",
    "message": "How are you?"
}`

and the same example for de.json would be 
`{
    "greeting": "Hallo",
    "message": "Wie gehts dir?"
}`

Application is using different user defined (on premises or public) LibreTranslate API servers between which is work automatically distributed. 

Important configuration is stored in appsettings.json file - which **can be modified during execution of the service** and new settings should be used immediately
important section is: 

  
