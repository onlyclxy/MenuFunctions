@echo off

cd /d %~dp0

echo ж��dll
set dllfile=MenuFunctions.dll
if not exist %dllfile% (
    echo %dllfile% is not exist!
	pause>nul 
	exit
)
".\RegAsm.exe"  /codebase %dllfile% /u

echo ������Դ������
taskkill /f /im explorer.exe & start explorer.exe

pause

exit