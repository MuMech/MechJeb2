# MechJeb2

Anatid Robotics and Multiversal Mechatronics proudly presents the first flight assistant autopilot: MechJeb

MechJeb2 is a mod for the game Kerbal Space Program. To learn how to use it, [visit the wiki][wiki]. For more info, [visit this KSP forum post][post].

[wiki]: https://github.com/MuMech/MechJeb2/wiki
[post]: http://forum.kerbalspaceprogram.com/index.php?/topic/154834-122-anatid-robotics-mumech-mechjeb-autopilot-260-12-dec-2016/

## Table of Contents

- [MechJeb2](#mechjeb2)
  - [Table of Contents](#table-of-contents)
  - [Install](#install)
    - [Manual install](#manual-install)
      - [Download](#download)
      - [Unpack](#unpack)
    - [Via CKAN](#via-ckan)
      - [Development version of Mechjeb](#development-version-of-mechjeb)
  - [Common Issues](#common-issues)
  - [Development](#development)
    - [Maintainers](#maintainers)
    - [Code Standards](#code-standards)
    - [Third-party libraries](#third-party-libraries)
    - [Build](#build)
      - [Linux](#linux)
      - [Windows](#windows)
  - [License](#license)

## Install

### Manual install

#### Download

Download from Jenkins:
    <https://ksp.sarbian.com/jenkins/job/MechJeb2-Release/>

#### Unpack

Unzip the zip in KSP GameData directory. You should have something that looks like that :

    Kerbal Space Program
    -- GameData
       -- MechJeb2
          -- Bundles
          -- Icons
          -- Localization
          -- Parts
          -- Plugins

### Via CKAN

CKAN has all the release of MechJeb, just install it as usual.

#### Development version of Mechjeb

If you want the unstable dev version of MechJeb then :

1. Open CKAN settings (Settings => CKAN Settings)
2. Press the New button
3. Select the MechJeb-dev line, click OK and exit the options.
4. Refresh
5. Select "Mechjeb2 - DEV RELEASE" in the list
6. Then "Go to Change" to install

## Common Issues

1. Why is the Mechjeb menu not showing?

    Make sure you have the part on your ship (AR202 case in the Control section).

2. (Windows) I cannot find Mechjeb anywhere, there aren't even parts in the R&D facility!

    Some Windows protection and anti-virus software can sometimes block KSP from loading MechJeb.
    You should install KSP outside the `C:\Program Files (x86)\` directory. [Steam has an option to change the install directory](https://support.steampowered.com/kb_article.php?ref=7710-tdlc-0426) of a game or you can just copy the directory somewhere else.

3. Why is some Mechjeb function not available?

    Science and career mode requires you to unlock some specific node in the Research and Development tree.
    You also may need to upgrade the tracking station to level 2 (game code restriction we can't do much about).

4. How do I report a bug?

    Check if your problem has already been reported: <https://github.com/MuMech/MechJeb2/issues>  
    If you found a problem which is similar to yours, feel free to add more information to the existing issue.

    **If you cannot find the problem**, get a [log](https://forum.kerbalspaceprogram.com/index.php?/topic/83212-how-to-get-support-read-first/#Logs) and create a new issue with a descriptive title of the problem.

## Development

### Maintainers

- [@sarbian](https://github.com/sarbian)
- [@lamont-granquist](https://github.com/lamont-granquist)

### Code Standards

1. [No var](https://docs.microsoft.com/en-us/visualstudio/ide/reference/convert-var-to-explicit-type): use explicit types.
2. Prefer single lines, when possible: especially if-else blocks!
3. [No null-conditional operators](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/member-access-operators#null-conditional-operators--and-): Unity 4.x has a [custom == for checking object nulls](https://blog.unity.com/technology/custom-operator-should-we-keep-it).
4. Assembly version needs to remain at 2.5.1.0; file version can be incremented.

### Third-party libraries

- [ALGLIB](https://www.alglib.net/)
- [NSubstitute](https://nsubstitute.github.io/)
- [xunit](https://xunit.net/)

### Build

#### Linux

The project uses Mono and Make to build the addon, make sure you have both installed.

1. (optional) Set your KSP directory

```sh
export KSPDIR="${XDG_DATA_HOME}/Steam/SteamApps/common/Kerbal Space Program"
```

2. Build the mod

```sh
make build
```

3. (optional) Install the mod into your KSP directory

```sh
make install
```

#### Windows

1. Install the version of Unity that KSP uses ( Currently 2019.2.2f1 )

2. Configure your system environement variables and add:

- KSPDIR set to where your KSP install is ( usually **C:\Program Files (x86)\Steam\SteamApps\Common\Kerbal Space Program** )
- MONO set to the path of Unity current mono.exe ( usually C:\Program Files\Unity\Hub\Editor\2019.2.2f1\Editor\Data\MonoBleedingEdge\bin\mono.exe )
- PDB2MDB set to the path of pdb2mdb.exe ( usually **C:\Program Files\Unity\Hub\Editor\2019.2.2f1\Editor\Data\MonoBleedingEdge\lib\mono\4.5\pdb2mdb.exe** )

3. Load MechJeb2.sln and open the properties of the MechJeb2 project (Right-Click=>properties). In the "Reference Path" section add the KSP libs folder to the list ( usually **C:\Program Files (x86)\Steam\SteamApps\Common\Kerbal Space Program\KSP_x64_Data\Managed** )

## License

Licensed under the [GNU General Public License, Version 3](LICENSE.md).

Portions Dual-Licensed under the [GNU General Public License, Version2](GPLv2-LICENSE.md) and [MIT License](MIT-LICENSE.md), where it is documented in the affected source code headers.

