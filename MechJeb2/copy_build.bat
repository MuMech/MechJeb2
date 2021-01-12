
IF NOT DEFINED MONO echo MONO env variable not defined
IF NOT DEFINED PDB2MDB echo PDB2MDB env variable not defined
IF NOT DEFINED KSPDIR echo KSPDIR env variable not defined

SET TargetPath=%1
SET TargetDir=%2
SET TargetName=%3
SET ProjectDir=%4

echo:
echo Target Path: %TargetPath%
echo Target Dir: %TargetDir%
echo Target Name: %TargetName%
echo Project Dir: %ProjectDir%
echo:
echo Mono: "%MONO%"
echo pdb2mdb: "%PDB2MDB%"
echo kspdir: "%KSPDIR%"

REM IF NOT EXIST "%MONO%" (
REM 	echo Expected "%MONO%" to point to mono.exe
REM 	exit 0
REM )

IF NOT EXIST "%PDB2MDB%" (
	echo Unable to find "%PDB2MDB%"
)

IF NOT EXIST "%KSPDIR%\*" (
	echo Expected "%KSPDIR%" to point to a directory but it is not
	exit 0
)

echo:
echo Copying to "%KSPDIR%"
echo:
IF EXIST %TargetPath% xcopy /Y /I %TargetPath% "%KSPDIR%\GameData\MechJeb2\Plugins\"
echo:

IF EXIST %TargetDir%%TargetName%.pdb xcopy /Y /I "%TargetDir%%TargetName%.pdb" "%KSPDIR%\GameData\MechJeb2\Plugins\"
echo:

IF EXIST %TargetDir%%TargetName%.dll.mdb xcopy /Y /I "%TargetDir%%TargetName%.dll.mdb" "%KSPDIR%\GameData\MechJeb2\Plugins\"
echo:


IF EXIST %ProjectDir%..\Bundles xcopy /S /Y /I "%ProjectDir%..\Bundles" "%KSPDIR%\GameData\MechJeb2\Bundles"
echo:

IF EXIST %ProjectDir%..\Icons xcopy /S /Y /I "%ProjectDir%..\Icons" "%KSPDIR%\GameData\MechJeb2\Icons"
echo:

IF EXIST %ProjectDir%..\Localization xcopy /S /Y /I "%ProjectDir%..\Localization" "%KSPDIR%\GameData\MechJeb2\Localization"
echo:

IF EXIST %ProjectDir%..\Parts xcopy /S /Y /I "%ProjectDir%..\Parts" "%KSPDIR%\GameData\MechJeb2\Parts"
echo:

IF EXIST %ProjectDir%..\LandingSites.cfg xcopy /Y /I "%ProjectDir%..\LandingSites.cfg" "%KSPDIR%\GameData\MechJeb2\"
echo:


:: Display the time of compilation so I don't waste time testing code I did not compile...
time /t