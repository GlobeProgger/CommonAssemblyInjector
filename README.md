# CommonAssemblyInjector
This is a small tool to inject CommonAssemblyInfo files into solutions. 

## What does it do?
* The provided CommonAssemblyInfo.cs file is added to the project files as a link
* All fields defined in the CommonAssemblyInfo file are commented out in the original AssemblyInfo.cs files

## How do I use it?
Call the tool with the following command line arguments:
* /solDir: Absolute directory of solution to inject the CommonAssemblyInfo into  (e.g. "C:\repos\MySolution\MyProject\CommonAssemblyInfo.cs")
* /path: Absolute path of CommonAssemblyInfo file (e.g. "C:\repos\MySolution")
* /version: Version of assemblies to inject (e.g. "1.0.0.0")
* /ignore: Comma separated directories to ignore
