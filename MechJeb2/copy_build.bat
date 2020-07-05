
IF NOT DEFINED MONO echo MONO env variable not defined
IF NOT DEFINED PDB2MDB echo PDB2MDB env variable not defined
IF NOT DEFINED KSPDIR echo KSPDIR env variable not defined

SET TargetPath=%1
SET TargetDir=%2
SET TargetName=%3
SET ProjectDir=%4

echo %TargetPath% %TargetDir% %TargetName% %ProjectDir%

IF NOT EXIST "%MONO%" (
	echo Expected "%MONO%" to point to mono.exe
	exit 0
)

IF EXIST %PDB2MDB% (
	"%MONO%" "%PDB2MDB%" %TargetPath%
) ELSE (
	echo Unable to find %PDB2MDB%
)

IF NOT EXIST %KSPDIR%\* (
	echo Expected "%KSPDIR%" to point to a directory but it is not
	exit 0
)

echo Copying to "%KSPDIR%"
IF EXIST "%TargetPath%" xcopy /Y /I "%TargetPath%" "%KSPDIR%\GameData\MechJeb2\Plugins\"
IF EXIST "%TargetDir%%TargetName%.pdb" xcopy /Y /I "%TargetDir%%TargetName%.pdb" "%KSPDIR%\GameData\MechJeb2\Plugins\"
IF EXIST "%TargetDir%%TargetName%.dll.mdb" xcopy /Y /I "%TargetDir%%TargetName%.dll.mdb" "%KSPDIR%\GameData\MechJeb2\Plugins\"

IF EXIST "%ProjectDir%..\Bundles" xcopy /S /Y /I "%ProjectDir%..\Bundles" "%KSPDIR%\GameData\MechJeb2\Bundles"
IF EXIST "%ProjectDir%..\Icons" xcopy /S /Y /I "%ProjectDir%..\Icons" "%KSPDIR%\GameData\MechJeb2\Icons"
IF EXIST "%ProjectDir%..\Localization" xcopy /S /Y /I "%ProjectDir%..\Localization" "%KSPDIR%\GameData\MechJeb2\Localization"
IF EXIST "%ProjectDir%..\Parts" xcopy /S /Y /I "%ProjectDir%..\Parts" "%KSPDIR%\GameData\MechJeb2\Parts"
