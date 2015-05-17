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
			KSPDIR  := ${HOME}/Library/Application Support/Steam/SteamApps/common/Kerbal Space Program
		endif
		MANAGED := ${KSPDIR}/KSP.app/Contents/Data/Managed/
	endif
endif

MECHJEBFILES := $(wildcard MechJeb2/*.cs) \
	$(wildcard MechJeb2/Maneuver/*.cs) \
	$(wildcard MechJeb2/Properties/*.cs) \
	$(wildcard MechJeb2/alglib/*.cs) \
	$(wildcard MechJeb2/LandingAutopilot/*.cs) \
	$(wildcard MechJeb2/KerbalEngineer/*.cs) \
	$(wildcard MechJeb2/KerbalEngineer/Extensions/*.cs) \
	$(wildcard MechJeb2/KerbalEngineer/Helpers/*.cs) \
	$(wildcard MechJeb2/KerbalEngineer/VesselSimulator/*.cs) \
	$(wildcard MechJeb2/FlyingSim/*.cs)

RESGEN2 := resgen2
GMCS    := gmcs
GIT     := git
TAR     := tar
ZIP     := zip

VERSION := $(shell ${GIT} describe --tags --always)

all: build

info:
	@echo "== MechJeb2 Build Information =="
	@echo "  resgen2: ${RESGEN2}"
	@echo "  gmcs:    ${GMCS}"
	@echo "  git:     ${GIT}"
	@echo "  tar:     ${TAR}"
	@echo "  zip:     ${ZIP}"
	@echo "  KSP Data: ${KSPDIR}"
	@echo "================================"

build: build/MechJeb2.dll

build/%.dll: ${MECHJEBFILES}
	mkdir -p build
	${RESGEN2} -usesourcepath MechJeb2/Properties/Resources.resx build/Resources.resources
	${GMCS} -t:library -lib:"${MANAGED}" \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine \
		-out:$@ \
		${MECHJEBFILES} \
		-resource:build/Resources.resources,MuMech.Properties.Resources.resources

package: build ${MECHJEBFILES}
	mkdir -p package/MechJeb2/Plugins
	cp -r Parts package/MechJeb2/
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
	cp build/MechJeb2.dll "${KSPDIR}"/GameData/MechJeb2/Plugins/

uninstall: info
	rm -rf "${KSPDIR}"/GameData/MechJeb2/Plugins
	rm -rf "${KSPDIR}"/GameData/MechJeb2/Parts


.PHONY : all info build package tar.gz zip clean install uninstall
