@echo off

cd /d %~dp0

echo ע��dll
set dllfile=MenuFunctions.dll
if not exist %dllfile% (
    echo %dllfile% is not exist!
	pause>nul 
	exit
)
".\RegAsm.exe"  /codebase %dllfile%

pause

exit