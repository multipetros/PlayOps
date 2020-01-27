# PlayOps
Copyright (c) 2012, Petros Kyladitis

## Description
Some times, **before runing a program** is usefull to **change the system date** to a specific one.  
PlayOps is a command line tool to automate this process, with the ability to **restore to the original current date**, after the programs termination.

## Requirements
This program is designed for MS Windows with [.NET Framework 3.5](https://www.microsoft.com/en-US/download/details.aspx?id=21) installed.

## Usage
Running PlayOps for first time, request you to specify the running parameters, such as:

* The **path** to executable  
* The **year** of temporary change date  
* The **month** of temporary change date  
* The **day** of temporary change date  

These parameters are stored in an ini file inside PlayOps Folder.  
If you need to change the setting you can configure this file by the hand, or by running the PlayOps with `-r` or `-reset` switch.  

From now and every time you execute the PlayOps, the system date changed and the specified program starts.  
When the process ends, the system date restore to the current date value and this PlayOps will close after 3 seconds.  
To terminate PlayOps and restore the current date before the running process ends give the command: `quit`

### Switches

| Short Switch | Long Switch | Description                                                           | 
| ------------ | ----------- | --------------------------------------------------------------------- |
| `-s`         | `-show`     | Display the existed ini file properties                               | 
| `-r`         | `-reset`    | Reconfigure the parameters                                            | 
| `-?`         | `-help`     | Show info about the program and the available command line parameters |

## Download
 * [Portable in zip archive](https://github.com/multipetros/PlayOps/releases/download/v1.0/playops-1.0.zip)

## License
This is free software distributed under the FreeBSD license, for details see at [License.txt](https://github.com/multipetros/PlayOps/blob/master/License.txt)  

## Donations
If you think that this program is helpful for you and you are willing to support the developer, feel free to  make a donation through [PayPal](https://www.paypal.me/PKyladitis).  

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.me/PKyladitis)