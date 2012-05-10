:: This batch file will:
:: 1. Compile the "Nightly" configuration of the solution with MSBuild
:: 2. Create portable archive with 7-Zip or WinRAR (tvshowtracker_v2_nightly_portable.zip)
:: 3. Create installer with NSIS (tvshowtracker_v2_nightly_setup.exe)
:: 4. Create JSON file with compile date and commit hash (tvshowtracker_v2_nightly_info.js)
:: 5. Copy the files to C:\Users\[You]\[Dropbox|SkyDrive|Google Drive]\RS TV Show Tracker
:: 6. Remove anything leftover (bin\Nightly and tvshowtracker_v2_nightly_[portable.zip|setup.exe|info.js])
:: 
:: It assumes that:
:: - The following files exist:
::   - %WinDir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
::   - %ProgramFiles%\7-Zip\7z.exe OR %ProgramFiles%\WinRAR\WinRar.exe
::   - %ProgramFiles(x86)%\NSIS\makensis.exe OR %ProgramFiles%\NSIS\makensis.exe
:: - Regional settings are set to US date and time format (for %date% and %time%)
:: - Git is in your %PATH%

@echo off

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

:: remove build

rmdir /S /Q bin\Nightly

:: move portable archive to Dropbox, SkyDrive and Google Drive

if exist tvshowtracker_v2_nightly_portable.zip (
	if exist "%HomePath%\Dropbox\RS TV Show Tracker" (
		cp tvshowtracker_v2_nightly_portable.zip "%HomePath%\Dropbox\RS TV Show Tracker"
	)
	if exist "%HomePath%\SkyDrive\RS TV Show Tracker" (
		cp tvshowtracker_v2_nightly_portable.zip "%HomePath%\SkyDrive\RS TV Show Tracker"
	)
	if exist "%HomePath%\Google Drive\RS TV Show Tracker" (
		cp tvshowtracker_v2_nightly_portable.zip "%HomePath%\Google Drive\RS TV Show Tracker"
	)
	
	del tvshowtracker_v2_nightly_portable.zip
) else (
	echo tvshowtracker_v2_nightly_portable.zip doesn't exist!
	pause
)

:: move installer to Dropbox, SkyDrive and Google Drive

if exist tvshowtracker_v2_nightly_setup.exe (
	if exist "%HomePath%\Dropbox\RS TV Show Tracker" (
		cp tvshowtracker_v2_nightly_setup.exe "%HomePath%\Dropbox\RS TV Show Tracker"
	)
	if exist "%HomePath%\SkyDrive\RS TV Show Tracker" (
		cp tvshowtracker_v2_nightly_setup.exe "%HomePath%\SkyDrive\RS TV Show Tracker"
	)
	if exist "%HomePath%\Google Drive\RS TV Show Tracker" (
		cp tvshowtracker_v2_nightly_setup.exe "%HomePath%\Google Drive\RS TV Show Tracker"
	)
	
	del tvshowtracker_v2_nightly_setup.exe
) else (
	echo tvshowtracker_v2_nightly_setup.exe doesn't exist!
	pause
)

:: create JSON with compile date and commit hash

for /f "tokens=2,3,4,5,6 usebackq delims=:/ " %%a in ('%date% %time%') do set datum=%%c-%%a-%%b %%d:%%e
for /f "tokens=1 delims=" %%a in ('git rev-parse HEAD') do set commit=%%a
echo. | set /p={"date":"%datum%","commit":"%commit%"} > tvshowtracker_v2_nightly_info.js

:: move JSON to Dropbox, SkyDrive and Google Drive

if exist "%HomePath%\Dropbox\RS TV Show Tracker" (
	cp tvshowtracker_v2_nightly_info.js "%HomePath%\Dropbox\RS TV Show Tracker"
)
if exist "%HomePath%\SkyDrive\RS TV Show Tracker" (
	cp tvshowtracker_v2_nightly_info.js "%HomePath%\SkyDrive\RS TV Show Tracker"
)
if exist "%HomePath%\Google Drive\RS TV Show Tracker" (
	cp tvshowtracker_v2_nightly_info.js "%HomePath%\Google Drive\RS TV Show Tracker"
)

del tvshowtracker_v2_nightly_info.js