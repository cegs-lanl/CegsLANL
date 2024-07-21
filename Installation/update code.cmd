@echo off
if [%*]==[] %0 "C:\Data\Source\DevStudio\CegsLANL\bin\Release"
C:
CD "\Programs\Aeon Laboratories\CegsLANL"
copy "%*\*.exe" > nul
copy "%*\*.dll" > nul
copy "%*\*.config" > nul
copy "%*\*.deps.json" > nul
copy "%*\*.runtimeconfig.json" > nul
echo *** System software updated *** >> "log\Event log.txt"
exit
