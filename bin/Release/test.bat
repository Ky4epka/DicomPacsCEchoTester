@echo off
DicomPacsCEchoTester.exe -host=172.16.1.22 -port=104 -clientae=BISTester -hostae=PROXY_KB50
echo ERROR: %ERRORLEVEL%
pause
@echo on