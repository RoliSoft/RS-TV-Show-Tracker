!define APP_NAME "RS TV Show Tracker"
!define COMP_NAME "RoliSoft"
!define WEB_SITE "http://lab.rolisoft.net"
!define COPYRIGHT "© 2013 RoliSoft"
!define DESCRIPTION "RS TV Show Tracker"
!ifndef INSTALLER_NAME
	!define INSTALLER_NAME "tvshowtracker_v2_setup.exe"
!endif
!ifndef TARGET_DIR
	!define TARGET_DIR "Release"
!endif
!ifndef VERSION
	!include "version.nsh"
!endif
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
RequestExecutionLevel admin

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
	AccessControl::GrantOnFile "$INSTDIR" "(BU)" "FullAccess"
	AccessControl::EnableFileInheritance "$INSTDIR"
	
	ClearErrors
	
	${GetParameters} $R0
	${GetOptions} $R0 "/AR" $R1
	IfErrors checkSilent runNormal
	
runNormal:
	ExecShell "open" '"$INSTDIR\${MAIN_APP_EXE}"'
	
checkSilent:
	ClearErrors
	
	${GetParameters} $R0
	${GetOptions} $R0 "/SR" $R1
	IfErrors done runSilent
	
runSilent:
	ExecShell "open" '"$INSTDIR\${MAIN_APP_EXE}"' "-hide"
	
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

!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_TEXT "Start ${APP_NAME}"
!define MUI_FINISHPAGE_RUN_FUNCTION FinishLaunchApplication
!define MUI_FINISHPAGE_SHOWREADME
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!define MUI_FINISHPAGE_SHOWREADME_TEXT "Create Desktop Shortcut"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION FinishCreateDesktopShortcut
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM

!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

######################################################################

Function WelcomeChangeFonts
	FindWindow $1 "#32770" "" $HWNDPARENT
	GetDlgItem $2 $1 1201
	CreateFont $0 "Segoe UI" "13" "700"
	SendMessage $2 ${WM_SETFONT} $0 0
FunctionEnd

Function HeaderChangeFonts
	GetDlgItem $1 $HWNDPARENT 1037
	CreateFont $0 "Segoe UI" "10" "700"
	SendMessage $1 ${WM_SETFONT} $0 0
	
	#GetDlgItem $1 $HWNDPARENT 1038
	#CreateFont $0 "Segoe UI" "9" "500"
	#SendMessage $1 ${WM_SETFONT} $0 0
FunctionEnd

Function FinishCreateDesktopShortcut
	CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
FunctionEnd

Function FinishLaunchApplication
	ExecShell "open" '"$INSTDIR\${MAIN_APP_EXE}"'
FunctionEnd

######################################################################

Section -MainProgram
	${INSTALL_TYPE}
	SetOverwrite ifnewer
	
	SetOutPath "$INSTDIR\libs"
	
	File "Dependencies\BouncyCastle.Crypto.dll"
	File "Dependencies\CookComputing.XmlRpcV2.dll"
	File "Dependencies\Hammock.ClientProfile.dll"
	File "Dependencies\HtmlAgilityPack.dll"
	File "Dependencies\IronPython.dll"
	File "Dependencies\Microsoft.Dynamic.dll"
	File "Dependencies\Microsoft.Scripting.dll"
	File "Dependencies\Microsoft.WindowsAPICodePack.dll"
	File "Dependencies\Microsoft.WindowsAPICodePack.Shell.dll"
	File "Dependencies\Newtonsoft.Json.dll"
	File "Dependencies\nunit.framework.dll"
	File "Dependencies\SharpCompress.dll"
	File "Dependencies\StarksoftBiko.dll"
	File "Dependencies\TaskDialog.dll"
	File "Dependencies\Transitionals.dll"
	File "Dependencies\WPFToolkit.Extended.dll"
	
	SetOutPath "$INSTDIR"
	
	File "bin\${TARGET_DIR}\RSTVShowTracker.exe"
	File "bin\${TARGET_DIR}\RSTVShowTracker.exe.config"
	File "bin\${TARGET_DIR}\RSTVShowTracker.pdb"
	
	# remove old leftover files
	
	Delete "BouncyCastle.Crypto.dll"
	Delete "CookComputing.XmlRpcV2.dll"
	Delete "Hammock.ClientProfile.dll"
	Delete "HtmlAgilityPack.dll"
	Delete "Ionic.Zip.Reduced.dll"
	Delete "IronPython.dll"
	Delete "Microsoft.Dynamic.dll"
	Delete "Microsoft.Scripting.dll"
	Delete "Microsoft.Windows.Shell.dll"
	Delete "Microsoft.WindowsAPICodePack.dll"
	Delete "Microsoft.WindowsAPICodePack.Shell.dll"
	Delete "Newtonsoft.Json.dll"
	Delete "nunit.framework.dll"
	Delete "protobuf-net.dll"
	Delete "SharpCompress.dll"
	Delete "StarksoftBiko.dll"
	Delete "Starksoft.Net.Proxy.dll"
	Delete "System.Data.SQLite.dll"
	Delete "TaskDialog.dll"
	Delete "Transitionals.dll"
	Delete "Twitterizer2.dll"
	Delete "VistaControls.dll"
	Delete "WPFToolkit.Extended.dll"
	Delete "handle.exe"
SectionEnd

######################################################################

Section -Prerequisites
	SetOutPath "$INSTDIR"
	
	; Check .Net Framework 4.5
	
	ClearErrors
	ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
	
	IfErrors installNetFx45
	IntCmp $0 378389 done1 installNetFx45 done1
    
installNetFx45:
	Banner::show /NOUNLOAD /set 76 ".Net Framework 4.5" "Downloading web installer..."
	NSISdl::download_quiet http://download.microsoft.com/download/B/A/4/BA4A7E71-2906-4B2D-A0E1-80CF16844F5F/dotNetFx45_Full_setup.exe "$INSTDIR\dotNetFx45_Full_setup.exe"
	Banner::destroy
	Banner::show /NOUNLOAD /set 76 ".Net Framework 4.5" "Waiting for installation to finish..."
	ExecWait "$INSTDIR\dotNetFx45_Full_setup.exe /passive /norestart"
	Delete "$INSTDIR\dotNetFx45_Full_setup.exe"
	Banner::destroy
	
done1:
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
	Delete "$INSTDIR\libs\BouncyCastle.Crypto.dll"
	Delete "$INSTDIR\libs\CookComputing.XmlRpcV2.dll"
	Delete "$INSTDIR\libs\Hammock.ClientProfile.dll"
	Delete "$INSTDIR\libs\HtmlAgilityPack.dll"
	Delete "$INSTDIR\libs\IronPython.dll"
	Delete "$INSTDIR\libs\Microsoft.Dynamic.dll"
	Delete "$INSTDIR\libs\Microsoft.Scripting.dll"
	Delete "$INSTDIR\libs\Microsoft.WindowsAPICodePack.dll"
	Delete "$INSTDIR\libs\Microsoft.WindowsAPICodePack.Shell.dll"
	Delete "$INSTDIR\libs\Newtonsoft.Json.dll"
	Delete "$INSTDIR\libs\nunit.framework.dll"
	Delete "$INSTDIR\libs\SharpCompress.dll"
	Delete "$INSTDIR\libs\StarksoftBiko.dll"
	Delete "$INSTDIR\libs\TaskDialog.dll"
	Delete "$INSTDIR\libs\Transitionals.dll"
	Delete "$INSTDIR\libs\WPFToolkit.Extended.dll"
	Delete "$INSTDIR\RSTVShowTracker.exe"
	Delete "$INSTDIR\RSTVShowTracker.exe.config"
	Delete "$INSTDIR\RSTVShowTracker.pdb"
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