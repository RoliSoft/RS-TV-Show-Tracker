echo Do not run this file directly.
exit

:: if you want to upload it to an SSH server with SCP:
scp -v -i PATH_TO_YOUR_SSH_KEY %1 YOUR_REMOTE_USER@YOUR_SERVER:/PATH_TO_YOUR_DIR/%~1

:: if you want to upload it to an FTP server with a tool installed in Windows by default:
echo user YOUR_REMOTE_USER> ftpcmd.dat
echo YOUR_PASSWORD>> ftpcmd.dat
echo bin>> ftpcmd.dat
echo cd PATH_TO_YOUR_DIR/>> ftpcmd.dat
echo del %1>> ftpcmd.dat
echo put %1>> ftpcmd.dat
echo quit>> ftpcmd.dat
ftp -n -s:ftpcmd.dat YOUR_SERVER
del ftpcmd.dat

:: if you want to copy it to your Dropbox/SkyDrive/Google Drive folder:
cp %1 "%HomePath%\Dropbox\PATH_TO_SUBFOLDER"
cp %1 "%HomePath%\SkyDrive\PATH_TO_SUBFOLDER"
cp %1 "%HomePath%\Google Drive\PATH_TO_SUBFOLDER"