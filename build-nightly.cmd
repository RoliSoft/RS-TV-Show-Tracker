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

:: check for upload-nightly.cmd

if not exist upload-nightly.cmd (
	echo You have not written a batch script to upload the files somewhere.
	echo Copy upload-nightly.sample.cmd to upload-nightly.cmd then edit it.
	pause
	exit
)

:: remove previous build

if exist bin\Nightly (
	rmdir /S /Q bin\Nightly
)

:: compile solution

if not exist %WinDir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe (
	echo MSBuild.exe was not found in %WinDir%\Microsoft.NET\Framework\v4.0.30319
	pause
	exit
)

%WinDir%\Microsoft.NET\Framework\v4.0.30319\MSBuild "RS TV Show Tracker.csproj" /p:Configuration=Nightly

:: create portable archive with 7-Zip or WinRAR

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

:: create installer with NSIS

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

:: sign the installer

if exist sign.cmd call sign tvshowtracker_v2_nightly_setup.exe

:: remove build

rmdir /S /Q bin\Nightly

:: upload portable archive

if exist tvshowtracker_v2_nightly_portable.zip (
	call upload-nightly tvshowtracker_v2_nightly_portable.zip
	del tvshowtracker_v2_nightly_portable.zip
) else (
	echo tvshowtracker_v2_nightly_portable.zip doesn't exist!
	pause
)

:: upload installer

if exist tvshowtracker_v2_nightly_setup.exe (
	call upload-nightly tvshowtracker_v2_nightly_setup.exe
	del tvshowtracker_v2_nightly_setup.exe
) else (
	echo tvshowtracker_v2_nightly_setup.exe doesn't exist!
	pause
)

:: create JSON with compile date and commit hash

for /f "tokens=2,3,4,5,6 usebackq delims=:/ " %%a in ('%date% %time%') do set datum=%%c-%%a-%%b %%d:%%e
for /f "tokens=1 delims=" %%a in ('git rev-parse HEAD') do set commit=%%a
echo. | set /p={"date":"%datum%","commit":"%commit%"} > tvshowtracker_v2_nightly_info.js

:: upload JSON

call upload-nightly tvshowtracker_v2_nightly_info.js
del tvshowtracker_v2_nightly_info.js

:: call post-build script, if exists

if exist post-build.cmd call post-build.cmd 