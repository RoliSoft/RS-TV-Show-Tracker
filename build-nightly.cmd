:: This batch file will:
:: 1. Compile the "Nightly" configuration of the solution with MSBuild
:: 2. Create portable archive with 7-Zip or WinRAR (tvshowtracker_v2_nightly_portable.zip)
:: 3. Create installer with NSIS (tvshowtracker_v2_nightly_setup.exe)
:: 4. Sign the installer
:: 5. Create JSON file with compile date and commit hash (tvshowtracker_v2_nightly_info.js)
:: 6. Copy the files via the upload-nightly.cmd command you create (see upload-nightly.sample.cmd)
:: 7. Remove anything leftover (bin\Nightly and tvshowtracker_v2_nightly_[portable.zip|setup.exe|info.js])
:: 8. Call post-build.cmd, if exists
:: 
:: It assumes that:
:: - The following files exist:
::   - %WinDir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
::   - %ProgramFiles%\7-Zip\7z.exe OR %ProgramFiles%\WinRAR\WinRar.exe
::   - %ProgramFiles(x86)%\NSIS\makensis.exe OR %ProgramFiles%\NSIS\makensis.exe
:: - Regional settings are set to US date and time format (for %date% and %time%)
:: - Git is in your %PATH%
:: - You have written a functional upload-nightly.cmd script

@echo off

if not exist upload-nightly.cmd (
	echo You have not written a batch script to upload the files somewhere.
	echo Copy upload-nightly.sample.cmd to upload-nightly.cmd then edit it.
	pause
	exit
)

::
title [01/12] Removing previous build...
::

if exist bin\Nightly (
	rmdir /S /Q bin\Nightly
)

::
title [02/12] Building solution...
::

if not exist %WinDir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe (
	echo MSBuild.exe was not found in %WinDir%\Microsoft.NET\Framework\v4.0.30319
	pause
	exit
)

%WinDir%\Microsoft.NET\Framework\v4.0.30319\MSBuild "RS TV Show Tracker.csproj" /p:Configuration=Nightly

::
title [03/12] Copying dependencies...
::

rmdir /S /Q bin\Nightly\libs
mkdir bin\Nightly\libs
xcopy /Y /C Dependencies\*.dll bin\Nightly\libs\
xcopy /Y /C Dependencies\*.txt bin\Nightly\libs\

::
title [04/12] Creating ZIP archive...
::

if exist "%ProgramFiles%\7-Zip\7z.exe" (
	"%ProgramFiles%\7-Zip\7z" a -tzip tvshowtracker_v2_nightly_portable .\bin\Nightly\*
) else (
	if exist "%ProgramFiles%\WinRAR\WinRar.exe" (
		"%ProgramFiles%\WinRAR\WinRar" a -afzip tvshowtracker_v2_nightly_portable bin\Nightly\*
	) else (
		echo Neither 7-Zip or WinRAR were found.
		echo Make sure 7-Zip is installed in %ProgramFiles%\7-Zip
		echo *or* WinRAR is installed in %ProgramFiles%\WinRAR
		pause
	)
)

::
title [05/12] Creating installer...
::

if exist "%ProgramFiles(x86)%\NSIS\makensis.exe" (
	"%ProgramFiles(x86)%\NSIS\makensis" /DINSTALLER_NAME=tvshowtracker_v2_nightly_setup.exe /DTARGET_DIR=Nightly installer.nsi
) else (
	if exist "%ProgramFiles%\NSIS\makensis.exe" (
		"%ProgramFiles%\NSIS\makensis" /DINSTALLER_NAME=tvshowtracker_v2_nightly_setup.exe /DTARGET_DIR=Nightly installer.nsi
	) else (
		echo NSIS was not found.
		echo Make sure NSIS is installed in "%ProgramFiles(x86)%\NSIS"
		pause
	)
)

::
title [06/12] Signing installer...
::

if exist sign.cmd call sign tvshowtracker_v2_nightly_setup.exe

::
title [07/12] Removing build...
::

rmdir /S /Q bin\Nightly

::
title [08/12] Uploading ZIP archive...
::

if exist tvshowtracker_v2_nightly_portable.zip (
	call upload-nightly tvshowtracker_v2_nightly_portable.zip
	del tvshowtracker_v2_nightly_portable.zip
) else (
	echo tvshowtracker_v2_nightly_portable.zip doesn't exist!
	pause
)

::
title [09/12] Uploading installer...
::

if exist tvshowtracker_v2_nightly_setup.exe (
	call upload-nightly tvshowtracker_v2_nightly_setup.exe
	del tvshowtracker_v2_nightly_setup.exe
) else (
	echo tvshowtracker_v2_nightly_setup.exe doesn't exist!
	pause
)

::
title [10/12] Creating JSON file...
::

for /f "tokens=2,3,4,5,6 usebackq delims=:/ " %%a in ('%date% %time%') do set datum=%%c-%%a-%%b %%d:%%e
for /f "tokens=1 delims=" %%a in (version.txt) do set version=%%a
for /f "tokens=1 delims=" %%a in ('git rev-parse HEAD') do set commit=%%a
echo. | set /p={"date":"%datum%","version":"%version%","commit":"%commit%"} > tvshowtracker_v2_nightly_info.js

::
title [11/12] Uploading JSON file...
::

call upload-nightly tvshowtracker_v2_nightly_info.js
del tvshowtracker_v2_nightly_info.js

::
title [12/12] Finishing up...
::

if exist post-build.cmd call post-build.cmd 