# Makefile for building MechJeb

ifeq ($(OS),Windows_NT)
	# do 'Doze stuff
else
	UNAME_S := $(shell uname -s)
	ifeq ($(UNAME_S),Linux)
		ifndef XDG_DATA_HOME
			XDG_DATA_HOME := ${HOME}/.local/share
		endif
		ifndef KSPDIR
			KSPDIR := ${XDG_DATA_HOME}/Steam/SteamApps/common/Kerbal Space Program
		endif
		MANAGED := ${KSPDIR}/KSP_Data/Managed/
	endif
	ifeq ($(UNAME_S),Darwin)
		ifndef KSPDIR
			KSPDIR  := ${HOME}/Library/Application Support/Steam/steamapps/common/Kerbal Space Program
		endif
		ifndef MANAGED
		MANAGED := ${KSPDIR}/KSP.app/Contents/Resources/Data/Managed/
		endif
	endif
endif

MECHJEBFILES := $(shell find MechJeb2 -name "*.cs")

RESGEN2 := resgen2
CSC     := csc
GIT     := git
TAR     := tar
ZIP     := zip

VERSION := $(shell ${GIT} describe --tags --always)

all: build

info:
	@echo "== MechJeb2 Build Information =="
	@echo "  resgen2: ${RESGEN2}"
	@echo "  csc:     ${CSC}"
	@echo "  git:     ${GIT}"
	@echo "  tar:     ${TAR}"
	@echo "  zip:     ${ZIP}"
	@echo "  KSP Data: ${KSPDIR}"
	@echo "================================"

build: build/MechJeb2.dll


build/%.dll: ${MECHJEBFILES}
	mkdir -p build
	${RESGEN2} -usesourcepath MechJeb2/Properties/Resources.resx build/Resources.resources
	${CSC} /noconfig /target:library /checked- /nowarn:1701,1702,2008 /langversion:8.0 /nostdlib+ /platform:AnyCPU /warn:4 /errorendlocation /highentropyva- /optimize+ /debug- /filealign:512 \
		/reference:${MANAGED}/Assembly-CSharp.dll \
		/reference:${MANAGED}/Assembly-CSharp-firstpass.dll \
		/reference:${MANAGED}/mscorlib.dll \
		/reference:${MANAGED}/System.Core.dll \
		/reference:${MANAGED}/System.dll \
		/reference:${MANAGED}/UnityEngine.dll \
		/reference:${MANAGED}/UnityEngine.AnimationModule.dll \
		/reference:${MANAGED}/UnityEngine.AssetBundleModule.dll \
		/reference:${MANAGED}/UnityEngine.CoreModule.dll \
		/reference:${MANAGED}/UnityEngine.ImageConversionModule.dll \
		/reference:${MANAGED}/UnityEngine.IMGUIModule.dll \
		/reference:${MANAGED}/UnityEngine.InputLegacyModule.dll \
		/reference:${MANAGED}/UnityEngine.PhysicsModule.dll \
		/reference:${MANAGED}/UnityEngine.TextRenderingModule.dll \
		/reference:${MANAGED}/UnityEngine.UI.dll \
		/reference:${MANAGED}/UnityEngine.VehiclesModule.dll \
		/recurse:"MechJeb2/*.cs" \
		-out:$@ \
		-resource:build/Resources.resources,MuMech.Properties.Resources.resources

package: build ${MECHJEBFILES}
	mkdir -p package/MechJeb2/Plugins
	cp -r Parts package/MechJeb2/
	cp -r Icons package/MechJeb2/
	cp -r Bundles package/MechJeb2/
	cp -r Localization package/MechJeb2/
	cp build/MechJeb2.dll package/MechJeb2/Plugins/
	cp LICENSE.md README.md package/MechJeb2/

%.tar.gz:
	${TAR} zcf $@ package/MechJeb2

tar.gz: package MechJeb-${VERSION}.tar.gz

%.zip:
	${ZIP} -9 -r $@ package/MechJeb2

zip: package MechJeb-${VERSION}.zip


clean:
	@echo "Cleaning up build and package directories..."
	rm -rf build/ package/

install: build
	mkdir -p "${KSPDIR}"/GameData/MechJeb2/Plugins
	cp -r Parts "${KSPDIR}"/GameData/MechJeb2/
	cp -r Icons "${KSPDIR}"/GameData/MechJeb2/
	cp -r Bundles "${KSPDIR}"/GameData/MechJeb2/
	cp -r Localization "${KSPDIR}"/GameData/MechJeb2/
	cp build/MechJeb2.dll "${KSPDIR}"/GameData/MechJeb2/Plugins/

uninstall: info
	rm -rf "${KSPDIR}"/GameData/MechJeb2/Plugins
	rm -rf "${KSPDIR}"/GameData/MechJeb2/Parts
	rm -rf "${KSPDIR}"/GameData/MechJeb2/Icons


.PHONY : all info build package tar.gz zip clean install uninstall
