#How to Build

Install:
1. Visual Leak Detector for Visual C++
   https://vld.codeplex.com

# How to include games database

a) Open [Game Settings] tab -> [Default Settings for Most Popular Games] tab
b) Press "Import..." button, and import latest x360ce.gdb file.
c) Press "Export..." button
d) Select Save as Type: Compressed Game Settings (*.xml.gz)
e) Save to c:\Projects\TocaEdit\x360ce.App\Resources\x360ce_Games.xml.gz

Building Application will include x360ce_Games.xml.gz as internal resource.

# How to Update XInput DLLs

1. Set Solution Configuration to Release\x86,
2. Build x360ce Project (creates: x360ce\bin\Debug\xinput1_3.dll
3. Set Solution Configuration to Release\x64,
4. Build x360ce Project (creates: x360ce\bin64\Debug\xinput1_3.dll)
5. Delete 3 files inside: x360ce.App\Resources\
     xinput.dll - Any CPU (MSIL) build - we are not using it at the moment.
     xinput_x64.dll - x64 build (copy from
     xinput_x86.dll - x86 build
6. Run x360ce.App\Documents\<YourName>-xinput.sign.bat
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
7. Then build 32-bit and 64-bit application with:
     Solution Configuration set to: Release\x86
     Solution Configuration set to: Release\x64

# How to Debug Application

1. Set Solution Configuration to Debug\x86,
2. Build 'x360ce' Project (creates: x360ce\bin\Debug\xinput1_3.dll
or
3. Set Solution Configuration to Debug\x64,
4. Build 'x360ce' Project (creates: x360ce\bin64\Debug\xinput1_3.dll)
5. Select 'x360ce.App' project and press F5.
   Visual Studio will copy 'x360ce' debug files (*.dll, *.pdb) to Application's debug folder and starts debugging.

Build x360ce project in Debug x86
