#How to Build

Install:

1.  Microsoft Visual Studio Community 2017 or 
    Visual Studio Professional 2017

    https://www.visualstudio.com/downloads/download-visual-studio-vs

    Please note that C++ projects are using "Visual C++ Build Tools v14.0".
    This means that you have to include this feature on "Visual Studio 2017 Installer":
    
        Desktop development with C++
            [x] VC++ 2015.3 v140 toolset for desctop (x86,x64)
	
    Optional step when you open Solution:
   
    VS2017 will offer upgrade C++ projects when you open solution for the first time. Don't upgrade. Uncheck all boxes and press [OK].

2.  Visual Leak Detector for Visual C++

    https://vld.codeplex.com

3.  Microsoft Windows SDK for Windows 8.1

    https://msdn.microsoft.com/en-us/microsoft-sdks-msdn.aspx
   
4.  Microsoft SQL Server 2017 Data Tools (required by x360ce.Data project)

    http://msdn.microsoft.com/data/tools.aspx

    This feature is also availabe on "Visual Studio 2017 Installer":
        
	Data storage and processing
            [x] SQL Server Data Tools

   Optional:

        You can install "Microsoft SQL Server 2016 Developer Edition", which is free now:
		
		https://www.microsoft.com/en-us/server-cloud/products/sql-server-editions/sql-server-developer.aspx

		and	"Microsoft SQL Server Management Studio" (SSMS), which is free and separate download now:
		
		https://msdn.microsoft.com/en-us/library/mt238290.aspx

5.  Microsoft Internet Information Services (required by x360ce.Web project)

    Control Panel > Programs and Features > Turn Windows features on

    a. Check:

    - [x] Internet Information Services

    b. Make sure that these options are also checked:

	- [x] NET.Extensibility 4.5
	- [x] ASP.NET 4.5
	- [x] ISAPI Extensions
	- [x] ISAPI Filters
 
5.  Microsoft URL Rewrite 2.0 (required by x360ce.Web project)

    http://www.microsoft.com/web/downloads/platform.aspx
   
    Download and launch: Microsoft Web Platform Installer. Install Server\URL Rewrite 2.0 Module:

# How to include games database

a) Open [Game Settings] tab -> [Default Settings for Most Popular Games] tab
b) Press "Import..." button, and import latest x360ce.gdb file.
c) Press "Export..." button
d) Select Save as Type: Compressed Game Settings (*.xml.gz)
e) Save to c:\Projects\TocaEdit\x360ce.App\Resources\x360ce_Games.xml.gz

Building Application will include x360ce_Games.xml.gz as internal resource.

# How to Update XInput DLLs

1.  Set Solution Configuration to Release\x86,
2.  Build x360ce Project (creates: x360ce\bin\Debug\xinput1_3.dll
3.  Set Solution Configuration to Release\x64,
4.  Build x360ce Project (creates: x360ce\bin64\Debug\xinput1_3.dll)
5.  Delete 3 files inside: x360ce.App\Resources\
     xinput.dll - Any CPU (MSIL) build - we are not using it at the moment.
     xinput_x64.dll - x64 build (copy from
     xinput_x86.dll - x86 build
6.  Run x360ce.App\Documents\<YourName>-xinput.sign.bat
    it will copy XInput files into resources folder and sign them:
        x360ce\bin\Debug\xinput1_3.dll > x360ce.App\Resources\xinput.dll
        x360ce\bin\Debug\xinput1_3.dll > x360ce.App\Resources\xinput_x86.dll
        x360ce\bin64\Debug\xinput1_3.dll > x360ce.App\Resources\xinput_x64.dll
    Note: My digital signature files are outside the project folder.
    Inside the project these files are included and marked as "Embedded Resource"
    So application can extract them if missing:
        x360ce.App\Resources\xinput.dll
        x360ce.App\Resources\xinput_x86.dll
        x360ce.App\Resources\xinput_x64.dll
    Default configuration files are embedded too.
        x360ce.App\Presets\x360ce.ini
    Setting files should be converted to XML at some point.
7.  Then build 32-bit and 64-bit application with:
     Solution Configuration set to: Release\x86
     Solution Configuration set to: Release\x64

# How to Debug Application

1.  Set Solution Configuration to Debug\x86,
2.  Build 'x360ce' Project (creates: x360ce\bin\Debug\xinput1_3.dll
or
3.  Set Solution Configuration to Debug\x64,
4.  Build 'x360ce' Project (creates: x360ce\bin64\Debug\xinput1_3.dll)
5.  Select 'x360ce.App' project and press F5.
    Visual Studio will copy 'x360ce' debug files (*.dll, *.pdb) to Application's debug folder and starts debugging.

Build x360ce project in Debug x86
