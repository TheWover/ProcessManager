# ProcessManager
ps-like .NET Assembly for enumerating processes on the current machine or a remote machine (using current token). Has the unique feature of telling you whether each process is managed (has the CLR loaded). Compatibly with .NET v3.5.

All enumeration is done with only built-in .NET APIs and PInvoke, rather than any third-party libraries or usage of WMI.

![Alt text](https://github.com/TheWover/ProcessManager/blob/master/img/usage.JPG?raw=true "General Usage")

# Usage

```
| Process Manager [v0.1]  
| Copyright (c) 2019 TheWover

Usage: ProcessManager.exe [machine] 

      -h, --help           Display this help menu. 
      
Examples:  

ProcessManager.exe
ProcessManager.exe workstation2  
ProcessManager.exe 10.30.134.13 
```
