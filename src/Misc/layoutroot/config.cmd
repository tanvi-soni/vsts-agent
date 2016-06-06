@echo off
setlocal
if defined VERBOSE_ARG (
  set VERBOSE_ARG='Continue'
) else (
  set VERBOSE_ARG='SilentlyContinue'
)

powershell.exe -NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "$VerbosePreference = %VERBOSE_ARG% ; Get-ChildItem -LiteralPath '%~dp0' | ForEach-Object { Write-Verbose ('Unblock: {0}' -f $_.FullName) ; $_ } | Unblock-File | Out-Null ; Get-ChildItem -Recurse -LiteralPath '%~dp0bin', '%~dp0externals' | Where-Object { $_ -match '\.(ps1|psd1|psm1)$' } | ForEach-Object { Write-Verbose ('Unblock: {0}' -f $_.FullName) ; $_ } | Unblock-File | Out-Null"

:run

IF "%~1" neq "remove" goto config
shift
set "args="
:parse
if "%~1" neq "" (
    set args=%args% %1
    shift
    goto :parse
)
if defined args set args=%args:~1%
"%~dp0bin\Agent.Listener.exe" unconfigure %args%
goto end

:config

"%~dp0bin\Agent.Listener.exe" configure %*
IF EXIST .Agent (attrib +h .Agent)
IF EXIST .Credentials (attrib +h .Credentials)
IF EXIST .Credentials_rsaparams (attrib +h .Credentials_rsaparams)
IF EXIST .Service (attrib +h .Service)

:end



