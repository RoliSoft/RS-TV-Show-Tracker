#!/bin/sh

rm -rf BouncyCastle
mkdir BouncyCastle
cd BouncyCastle

export CVSROOT=:pserver:anonymous@cvs.bouncycastle.org:/home/users/bouncy/cvsroot
cvs co csharp
mv csharp/crypto/src/* ./
mkdir bzip2
mv csharp/crypto/bzip2/src/* bzip2/
rm -rf csharp
rm -rf AssemblyInfo.cs
find . -name 'CVS' | xargs rm -rf

cd ..
rm -rf Hammock
mkdir Hammock
cd Hammock

git clone git://github.com/danielcrenna/hammock.git
mv hammock/src/net35/Hammock/* ./
mv -f hammock/src/net40/Hammock/* ./
rm -rf *.csproj
rm -rf *.cd
rm -rf *.snk
rm -rf Properties
rm -rf Mono
rm -rf hammock

cd ..
rm -rf HtmlAgilityPack
mkdir HtmlAgilityPack
cd HtmlAgilityPack

svn checkout https://htmlagilitypack.svn.codeplex.com/svn/Trunk
mv Trunk/HtmlAgilityPack/* ./
rm -rf *.csproj
rm -rf *.vspscc
rm -rf *.snk
rm -rf Properties
rm -rf Trunk

cd ..
rm -rf JSON.NET
mkdir JSON.NET
cd JSON.NET

svn checkout https://json.svn.codeplex.com/svn/trunk/Src
mv Src/Newtonsoft.Json/* ./
rm -rf *.csproj
rm -rf *.snk
rm -rf *.ruleset
rm -rf Properties
rm -rf Src

cd ..
rm -rf SharpCompress
mkdir SharpCompress
cd SharpCompress

hg clone https://hg.codeplex.com/sharpcompress
mv sharpcompress/SharpCompress/* ./
rm -rf *.csproj
rm -rf *.pfx
rm -rf AssemblyInfo.cs
rm -rf VersionInfo.cs
rm -rf sharpcompress

cd ..
rm -rf StarksoftBiko
mkdir StarksoftBiko
cd StarksoftBiko

svn checkout https://biko.svn.codeplex.com/svn/Biko
mv Biko/"Starksoft Biko"/* ./
rm -rf *.csproj
rm -rf *.vspscc
rm -rf *.vssscc
rm -rf *.sln
rm -rf Properties
rm -rf Tests
rm -rf Biko

cd ..
rm -rf TaskDialog
mkdir TaskDialog
cd TaskDialog

git clone git://github.com/yadyn/WPF-Task-Dialog.git
mv WPF-Task-Dialog/TaskDialog/* ./
rm -rf *.csproj
rm -rf Properties/AssemblyInfo.cs
rm -rf WPF-Task-Dialog

cd ..
rm -rf Transitionals
mkdir Transitionals
cd Transitionals

svn checkout https://transitionals.svn.codeplex.com/svn
mv svn/WPF/Framework/Transitionals/* ./
rm -rf *.csproj
rm -rf *.vspscc
rm -rf Properties
rm -rf Help
rm -rf Documentation
rm -rf svn

cd ..
rm -rf WPFToolkit
mkdir WPFToolkit
cd WPFToolkit

svn checkout https://wpftoolkit.svn.codeplex.com/svn/Main/Source/ExtendedWPFToolkitSolution
mv ExtendedWPFToolkitSolution/Src/Xceed.Wpf.Toolkit/* ./
rm -rf Assembly*.cs
rm -rf *.snk
rm -rf *.csproj
rm -rf Properties
rm -rf ExtendedWPFToolkitSolution

cd ..
rm -rf XML-RPC.NET
mkdir XML-RPC.NET
cd XML-RPC.NET

svn checkout http://xmlrpcnet.googlecode.com/svn/trunk/ xmlrpcnet-read-only
mv xmlrpcnet-read-only/src/* ./
rm -rf *.Targets
rm -rf Assembly*.cs
rm -rf *.csproj
rm -rf xmlrpcnet-read-only

cd ..
rm -rf WindowsAPICodePack
mkdir WindowsAPICodePack
cd WindowsAPICodePack

wget http://archive.msdn.microsoft.com/Project/Download/FileDownload.aspx?ProjectName=WindowsAPICodePack&DownloadId=13734
./"Windows API Code Pack Self Extractor.exe"
unzip "Windows API Code Pack 1.1.zip"
mv "Windows API Code Pack 1.1"/source/WindowsAPICodePack/* ./
rm -rf *.sln
find . -name 'AssemblyInfo.cs' | xargs rm -rf
find . -name 'GlobalSuppressions.cs' | xargs rm -rf
find . -name 'CustomDictionary.xml' | xargs rm -rf
find . -name '*.csproj' | xargs rm -rf
rm -rf "Windows API Code Pack 1.1"/
rm -rf *.zip
rm -rf *.exe

cd ..