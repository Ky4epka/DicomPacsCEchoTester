@echo off
DicomPacsConTest.exe -host=172.16.1.22 -port=104 -clientae=1 -hostae=PROXY_KB50
echo ERROR: %ERRORLEVEL%
@echo on