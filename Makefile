# Makefile for building MechJeb

KSPDIR  := ${HOME}/.local/share/Steam/SteamApps/common/Kerbal\ Space\ Program
MANAGED := KSP_Data/Managed/

MECHJEBFILES := $(wildcard MechJeb2/*.cs) \
	$(wildcard MechJeb2/Properties/*.cs) \
	$(wildcard MechJeb2/alglib/*.cs)

RESGEN2 := /usr/bin/resgen2
GMCS    := /usr/bin/gmcs
GIT     := /usr/bin/git
TAR     := /usr/bin/tar
ZIP     := /usr/bin/zip

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

build: info
	mkdir -p build
	${RESGEN2} -usesourcepath MechJeb2/Properties/Resources.resx build/Resources.resources
	${GMCS} -t:library -lib:${KSPDIR}/${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine \
		-out:build/MechJeb2.dll \
		${MECHJEBFILES} \
		-resource:build/Resources.resources,MuMech.Properties.Resources.resources

package: build
	mkdir -p package/MechJeb2/Plugins
	cp -r Parts package/MechJeb2/
	cp build/MechJeb2.dll package/MechJeb2/Plugins/

tar.gz: package
	${TAR} zcf MechJeb2-$(shell ${GIT} describe --tags --long --always).tar.gz package/MechJeb2

zip: package
	${ZIP} -9 -r MechJeb2-$(shell ${GIT} describe --tags --long --always).zip package/MechJeb2

clean:
	@echo "Cleaning up build and package directories..."
	rm -rf build/ package/

install: build
	mkdir -p ${KSPDIR}/GameData/MechJeb2/Plugins
	cp -r Parts ${KSPDIR}/GameData/MechJeb2/
	cp build/MechJeb2.dll ${KSPDIR}/GameData/MechJeb2/Plugins/

uninstall: info
	rm -rf ${KSPDIR}/GameData/MechJeb2/Plugins
	rm -rf ${KSPDIR}/GameData/MechJeb2/Parts


.PHONY : all info build package tar.gz zip clean install uninstall
