![Alt Text](images/pthbanner.gif)
![](https://img.shields.io/github/stars/PTHunter/editor.md.svg) ![](https://img.shields.io/github/forks/PTHunter/editor.md.svg) ![](https://img.shields.io/github/tag/PTHunter/editor.md.svg) ![](https://img.shields.io/github/release/PTHunter/editor.md.svg) ![](https://img.shields.io/github/issues/PTHunter/editor.md.svg)

# PTHunter
PTHunter is a command-line tool designed to automate the detection of Path Traversal vulnerabilities in web applications.
It supports multiple hosts, custom payloads, authentication, headers, and configurable delays between requests.
With features like file injection, verbose logging, and early exit on detection

# Installation:

First you will need to install dotnet sdk 8.0.408

Next:

## Linux:
```
$ git clone https://github.com/M0RTUM/PTHunter.git
$ cd PTHunter
$ dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
# sudo mv ./publish/PTHunter /usr/local/bin/pthunter
$ cd ..
$ rm -rf PTHunter
$ pthunter --help
```
## Windows:
```
git clone https://github.com/M0RTUM/PTHunter.git
cd PTHunter
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
cd publish
pthunter.exe --help
```
