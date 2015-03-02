# OfficeDev PnP Timer Service #

### Summary ###
SharePoint Online does not provide the possibility to run timerjobs. For SharePoint on-premises it is possible create timer jobs, more and more SharePoint farm admins demand that no custom code is being deployed to the farm. As traditional SharePoint timer jobs require an assembly to deployed to the farm other solutions will have to be developed.

The timer job framework as provided by Office PnP is very flexible and allows for various deployment scenarios. Typically Azure WebJobs come to mind. If for some reason you don't want to use Azure, or want to keep all your code and business logic on premises, the only solution you have is to create a scheduled task on a Windows Server.

While functional, there are administrative limitations with those. The OfficeDev PnP Timer Service can be an answer here.

The Timer Service acts similar to the SharePoint Timer Service, where it will execute scheduled TimerJobs at specified intervals. The service can be stopped using standard functionality, similar to the SharePoint Timer Service.

The solution contains a complete timer service implementation that can be installed, uninstalled, stopped and started. The configuration of timer jobs is done through an XML file that is located on the file system.

### Applies to ###
-  Office 365 Multi Tenant (MT)
-  Office 365 Dedicated (D)
-  SharePoint On-Premises

### Prerequisites ###
The complete solution requires to be installed on a Windows Server with .NET 4.5 available.

### Solution ###
Solution | Author(s)
---------|----------
Core.SPOTimerService | Erwin van Hunen (Knowit Reaktor Stockholm AB)

### Version history ###
Version  | Date | Comments
---------| -----| --------
1.0  | February 18th 2015 | Initial release

### Disclaimer ###
**THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.**


----------

# Introduction #
This solution contains a full implementation of a Timer Service that can be deployed to a Microsoft Windows Server installation.

# Installation #
After building the solution, open the Developer Command Prompt for VS2013/VS2015 in elevated (Administrative) mode.

Navigate to the (debug|release)\bin folder and enter:

    installutil SPOTimerService.exe

To start the time service

    net start SPOTimerService
   
To stop the timer service

    net stop SPOTimerService

Of course the timer service can be stopped and started through the Services control panel app.

## Dependencies ##
Code is having references to following CSOM assemblies.

- OfficeDevPnP.Core.dll
- Microsoft.SharePoint.Client.dll
- Microsoft.SharePoint.Client.Runtime.dll


## Configuration ##
After starting the timer server a jobs.xml is created in 

    c:\ProgramData\OfficeDevPnP

In this file jobs can be configured.

The format of this XML is:

```XML
<?xml version="1.0" encoding="utf-8"?>
<Jobs>
  <Job Name = "Text" 
       Disabled= "True" | "False"                
       Assembly = "Text" 
       Class = "Text" 
       ScheduleType = "Minute" | "Daily" | "Weekly"
       Repeat = "Integer"
       Day = "Integer"
       Time = "HH:MM"
       >
    <Authentication 
          Type = "Office365" | "AppOnly" | "NetworkCredential"
          Username = "Text" 
          Password = "Text"
          Domain = "Text"
          Credential = "Text"
          AppId = "Text"
          AppSecret = "Text" />
    <Sites>
      <Site 
         Url = "Text" />
    </Sites>
  </Job>
</Jobs>
```

###Job Tag Attributes###
|Attribute|Description
|----|----|
|Name|A descriptive name of the timerjob|
|Disabled|True or False, to temporary disable a job, set the value to true. If not specified the value defaults to True.
|Assembly|The full path to the assembly (.dll) containing the timerjob, e.g. c:\path\to\assembly.dll
|Class|The full name, including namespace, of the class of the timerjob, e.g. MyNameSpace.DemoJob
|ScheduleType|Possible values: **Minute**,**Daily** or **Weekly**. If **Minute** is specified: run a job every X minutes. Specify the Repeat attribute with an interval. If **Daily**: run a job on a specific time every day. Specify the Time attribute with a value. If **Weekly** is specified: run a job on a specific day every week. Specify both the Time and Day attributes
|Repeat|Sets an interval in minutes to run a timerjob at. Only used in case of `ScheduleType="Minute"`
|Day|Specifies the day of the week to run a job when `ScheduleType="Weekly"`. Sunday = 0.
|Time|Specifies the time of the day to run a job when `ScheduleType="Daily"` or `ScheduleType="Weekly"`. Specify in HH:MM format, e.g. 14:20.

###Authentication Tag Attributes###
|Attribute|Description
|----|----|
|Type|Possible values: **Office365**, **AppOnly**, **NetworkCredential**. Specifies the type of authentication to use for authentication. 
|Username|Username to use for authentication, or in the case of `Type="AppOnly"` used for site enumeration if the site urls contain a wildcard.
|Password|Plain text password to use for authentication, or in the case of `Type="AppOnly"` used for site enumeration if the site urls contain a wildcard.
|Domain|Domain to use in the case of `Type="NetworkCredential"`, or in the case of `Type="AppOnly"` used for site enumeration if the site urls contain a wildcard.
|Credential|If specified, other username and password tags will be ignored and the credentials will be retrieved from the Windows Credential Manager, can also be used in the case of `Type="AppOnly"` and is used for site enumeration if the site urls contain a wildcard
|AppId|Used when `Type="AppOnly"`
|AppSecret|Used when `Type="AppOnly"`

###Site Tag Attributes###
|Attribute|Description
|----|----| 
|Url|Either specify a full url, or specify use a wildcard url like https://tenant.sharepoint.com/sites/*". It is important that the credentials used (also applies to AppOnly authentication) have tenant administration access.
