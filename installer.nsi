!define APP_NAME "RS TV Show Tracker"
!define COMP_NAME "RoliSoft"
!define WEB_SITE "http://lab.rolisoft.net"
!define VERSION "2.0.0.0"
!define COPYRIGHT "� 2011 RoliSoft"
!define DESCRIPTION "RS TV Show Tracker"
!define INSTALLER_NAME "tvshowtracker_v2_setup.exe"
!define MAIN_APP_EXE "RSTVShowTracker.exe"
!define INSTALL_TYPE "SetShellVarContext current"
!define REG_ROOT "HKCU"
!define REG_APP_PATH "Software\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}"
!define UNINSTALL_PATH "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"

!define MUI_ICON "${NSISDIR}\contrib\graphics\icons\orange-install.ico"
!define MUI_UNICON "${NSISDIR}\contrib\graphics\icons\orange-uninstall.ico"
!define MUI_HEADERIMAGE on
!define MUI_HEADERIMAGE_RIGHT on
!define MUI_HEADERIMAGE_BITMAP "Images\nsis-header.bmp"
!define MUI_HEADERIMAGE_UNBITMAP "${NSISDIR}\contrib\graphics\header\orange-uninstall.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "Images\nsis-wizard.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "${NSISDIR}\contrib\graphics\wizard\orange-uninstall.bmp"

!define REG_START_MENU "Start Menu Folder"

var SM_Folder

!include "FileFunc.nsh"

######################################################################

VIProductVersion  "${VERSION}"
VIAddVersionKey "ProductName"  "${APP_NAME}"
VIAddVersionKey "CompanyName"  "${COMP_NAME}"
VIAddVersionKey "LegalCopyright"  "${COPYRIGHT}"
VIAddVersionKey "FileDescription"  "${DESCRIPTION}"
VIAddVersionKey "FileVersion"  "${VERSION}"

######################################################################

SetCompressor LZMA
Name "${APP_NAME}"
Caption "${APP_NAME}"
OutFile "${INSTALLER_NAME}"
BrandingText "${APP_NAME}"
XPStyle on
InstallDirRegKey "${REG_ROOT}" "${REG_APP_PATH}" ""
InstallDir "$PROGRAMFILES\RoliSoft\RS TV Show Tracker"
RequestExecutionLevel user

######################################################################

Function .onInit
	ReadRegStr $R0 HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion" CurrentVersion
	IntCmp $R0 6 seven notseven seven
	
notseven:
	MessageBox MB_OK|MB_ICONSTOP "This software doesn't support systems older than Windows 7."
	Quit
	
seven:
	IfSilent silent done
	
silent:
	KillProcDLL::KillProc "${MAIN_APP_EXE}"
	
done:
FunctionEnd

Function .onInstSuccess
	${GetParameters} $R0
	${GetOptions} $R0 "/AR" $R1
	IfErrors done run
	
run:
	Exec "$INSTDIR\${MAIN_APP_EXE}"
	
done:
FunctionEnd

######################################################################

!include "MUI.nsh"

!define MUI_ABORTWARNING
!define MUI_UNABORTWARNING

!define MUI_PAGE_CUSTOMFUNCTION_SHOW WelcomeChangeFonts
!insertmacro MUI_PAGE_WELCOME

!ifdef LICENSE_TXT
	!define MUI_PAGE_CUSTOMFUNCTION_PRE HeaderChangeFonts
	!insertmacro MUI_PAGE_LICENSE "${LICENSE_TXT}"
!endif

!define MUI_PAGE_CUSTOMFUNCTION_PRE HeaderChangeFonts
!insertmacro MUI_PAGE_DIRECTORY

!ifdef REG_START_MENU
	!define MUI_STARTMENUPAGE_DEFAULTFOLDER "RS TV Show Tracker"
	!define MUI_STARTMENUPAGE_REGISTRY_ROOT "${REG_ROOT}"
	!define MUI_STARTMENUPAGE_REGISTRY_KEY "${UNINSTALL_PATH}"
	!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "${REG_START_MENU}"
	!define MUI_PAGE_CUSTOMFUNCTION_PRE HeaderChangeFonts
	!insertmacro MUI_PAGE_STARTMENU Application $SM_Folder
!endif

!define MUI_PAGE_CUSTOMFUNCTION_PRE HeaderChangeFonts
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_RUN "$INSTDIR\${MAIN_APP_EXE}"
!define MUI_FINISHPAGE_SHOWREADME ""
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!define MUI_FINISHPAGE_SHOWREADME_TEXT "Create Desktop Shortcut"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION FinishCreateDesktopShortcut
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM

!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

######################################################################

function WelcomeChangeFonts
	FindWindow $1 "#32770" "" $HWNDPARENT
	GetDlgItem $2 $1 1201
	CreateFont $0 "Segoe UI" "13" "700"
	SendMessage $2 ${WM_SETFONT} $0 0
functionend

function HeaderChangeFonts
	GetDlgItem $1 $HWNDPARENT 1037
	CreateFont $0 "Segoe UI" "10" "700"
	SendMessage $1 ${WM_SETFONT} $0 0
	
	#GetDlgItem $1 $HWNDPARENT 1038
	#CreateFont $0 "Segoe UI" "9" "500"
	#SendMessage $1 ${WM_SETFONT} $0 0
functionend

Function FinishCreateDesktopShortcut
	CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
FunctionEnd

######################################################################

Section -MainProgram
	${INSTALL_TYPE}
	SetOverwrite ifnewer
	SetOutPath "$INSTDIR"
	
	File "Dependencies\BouncyCastle.Crypto.dll"
	File "Dependencies\CookComputing.XmlRpcV2.dll"
	File "Dependencies\handle.exe"
	File "Dependencies\HtmlAgilityPack.dll"
	File "Dependencies\Ionic.Zip.Reduced.dll"
	File "Dependencies\Microsoft.Windows.Shell.dll"
	File "Dependencies\Microsoft.WindowsAPICodePack.dll"
	File "Dependencies\Microsoft.WindowsAPICodePack.Shell.dll"
	File "Dependencies\Newtonsoft.Json.dll"
	File "Dependencies\nunit.framework.dll"
	File "bin\Release\RSTVShowTracker.exe"
	File "bin\Release\RSTVShowTracker.exe.config"
	File "bin\Release\RSTVShowTracker.pdb"
	File "Dependencies\Starksoft.Net.Proxy.dll"
	File "Dependencies\System.Data.SQLite.dll"
	File "Dependencies\Transitionals.dll"
	File "Dependencies\Twitterizer2.dll"
	File "Dependencies\VistaControls.dll"
SectionEnd

######################################################################

Section -Icons_Reg
	SetOutPath "$INSTDIR"
	WriteUninstaller "$INSTDIR\uninstall.exe"
	
	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
	
	CreateDirectory "$SMPROGRAMS\RS TV Show Tracker"
	CreateShortCut "$SMPROGRAMS\RS TV Show Tracker\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
	CreateShortCut "$SMPROGRAMS\RS TV Show Tracker\Uninstall.lnk" "$INSTDIR\uninstall.exe"
	
	!ifdef WEB_SITE
		WriteIniStr "$INSTDIR\${APP_NAME} website.url" "InternetShortcut" "URL" "${WEB_SITE}"
		WriteIniStr "$INSTDIR\${APP_NAME} website.url" "InternetShortcut" "IconFile" "%SystemRoot%\system32\SHELL32.dll"
		WriteIniStr "$INSTDIR\${APP_NAME} website.url" "InternetShortcut" "IconIndex" "277"
		CreateShortCut "$SMPROGRAMS\RS TV Show Tracker\Website.lnk" "$INSTDIR\${APP_NAME} website.url" "" "%SystemRoot%\system32\SHELL32.dll" "277"
	!endif
	
	!insertmacro MUI_STARTMENU_WRITE_END
	
	WriteRegStr ${REG_ROOT} "${REG_APP_PATH}" "" "$INSTDIR\${MAIN_APP_EXE}"
	WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayName" "${APP_NAME}"
	WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "UninstallString" "$INSTDIR\uninstall.exe"
	WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayIcon" "$INSTDIR\${MAIN_APP_EXE}"
	WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayVersion" "${VERSION}"
	WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "Publisher" "${COMP_NAME}"
	
	!ifdef WEB_SITE
		WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "URLInfoAbout" "${WEB_SITE}"
	!endif
SectionEnd

######################################################################

Section Uninstall
	${INSTALL_TYPE}
	Delete "$INSTDIR\BouncyCastle.Crypto.dll"
	Delete "$INSTDIR\CookComputing.XmlRpcV2.dll"
	Delete "$INSTDIR\handle.exe"
	Delete "$INSTDIR\HtmlAgilityPack.dll"
	Delete "$INSTDIR\Ionic.Zip.Reduced.dll"
	Delete "$INSTDIR\Microsoft.Windows.Shell.dll"
	Delete "$INSTDIR\Microsoft.WindowsAPICodePack.dll"
	Delete "$INSTDIR\Microsoft.WindowsAPICodePack.Shell.dll"
	Delete "$INSTDIR\Newtonsoft.Json.dll"
	Delete "$INSTDIR\nunit.framework.dll"
	Delete "$INSTDIR\RSTVShowTracker.exe"
	Delete "$INSTDIR\RSTVShowTracker.exe.config"
	Delete "$INSTDIR\RSTVShowTracker.pdb"
	Delete "$INSTDIR\Starksoft.Net.Proxy.dll"
	Delete "$INSTDIR\System.Data.SQLite.dll"
	Delete "$INSTDIR\Transitionals.dll"
	Delete "$INSTDIR\Twitterizer2.dll"
	Delete "$INSTDIR\VistaControls.dll"
	Delete "$INSTDIR\uninstall.exe"
	
	!ifdef WEB_SITE
		Delete "$INSTDIR\${APP_NAME} website.url"
	!endif
	
	RmDir "$INSTDIR"
	
	Delete "$SMPROGRAMS\RS TV Show Tracker\${APP_NAME}.lnk"
	Delete "$SMPROGRAMS\RS TV Show Tracker\Uninstall.lnk"
	
	!ifdef WEB_SITE
		Delete "$SMPROGRAMS\RS TV Show Tracker\Website.lnk"
	!endif
	
	Delete "$DESKTOP\${APP_NAME}.lnk"
	RMDir /r "$SMPROGRAMS\RS TV Show Tracker"
	
	DeleteRegKey ${REG_ROOT} "${REG_APP_PATH}"
	DeleteRegKey ${REG_ROOT} "${UNINSTALL_PATH}"
SectionEnd