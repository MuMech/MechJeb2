#!/usr/bin/env bash
TARGET_PATH=$1
TARGET_DIR=$2
TARGET_NAME=$3
PROJECT_DIR=$4
REFERENCE_PATH=$5

if [ -z "${TARGET_PATH}" ] ; then
  echo 'Expected $TARGET_PATH to be defined but it is not' >&2
  exit 1
elif ! [ -f "${TARGET_PATH}" ] ; then
  echo 'Expected $TARGET_PATH to be a file but it is not' >&2
  exit 1
fi

if [ -z "${TARGET_DIR}" ] ; then
  echo 'Expected $TARGET_DIR to be defined but it is not' >&2
  exit 1
elif ! [ -d "${TARGET_DIR}" ] ; then
  echo 'Expected $TARGET_DIR to be a directory but it is not' >&2
  exit 1
fi

if [ -z "${TARGET_NAME}" ] ; then
  echo 'Expected $TARGET_NAME to be defined but it is not' >&2
  exit 1
fi

if [ -z "${PROJECT_DIR}" ] ; then
  echo 'Expected PROJECT_DIR to be defined but it is not' >&2
  exit 1
fi

if [ -z "${REFERENCE_PATH}" ] ; then
  echo 'Expected REFERENCE_PATH to be defined but it is not' >&2
  exit 1
fi

if [[ $(uname -s) = Darwin ]]; then
  KSPDIR="$(dirname "$(dirname "$(dirname "$(dirname "$(dirname "$REFERENCE_PATH")")")")")"
fi

if [ -z "${KSPDIR}" ] ; then
  if [[ $(uname -s) = Linux ]]; then
    KSPDIR="${HOME}/.local/share/Steam/SteamApps/common/Kerbal Space Program"
  fi
  if [[ $(uname -s) = Darwin ]]; then
    KSPDIR="${HOME}/Library/Application Support/Steam/steamapps/common/Kerbal Space Program"
  fi
fi

if [ -z "${KSPDIR}" ] ; then
  echo '$KSPDIR not found'
else
  if ! [ -d "${KSPDIR}" ] ; then
    echo 'Expected $KSPDIR to point to a directory but it is not' >&2
    exit 1
  fi
  if ! [ -d "${KSPDIR}/GameData/MechJeb2/Plugins" ] ; then
    mkdir -p "${KSPDIR}/GameData/MechJeb2/Plugins/"
  fi
  echo "Copying to '${KSPDIR}'"
  for FILENAME in \
    JetBrains.Annotations \
    MechJeb2 \
    MechJebLib \
    MechJebLibBindings \
    alglib
  do
    cp "${TARGET_DIR}/${FILENAME}.dll" "${KSPDIR}/GameData/MechJeb2/Plugins/"
    test -f "${TARGET_DIR}/${FILENAME}.pdb" && cp "${TARGET_DIR}/${FILENAME}.pdb" "${KSPDIR}/GameData/MechJeb2/Plugins/"
    test -f "${TARGET_DIR}/${FILENAME}.xml" && cp "${TARGET_DIR}/${FILENAME}.xml" "${KSPDIR}/GameData/MechJeb2/Plugins/"
  done

  cp -r ${PROJECT_DIR}/../Bundles "${KSPDIR}/GameData/MechJeb2/"
  cp -r ${PROJECT_DIR}/../Icons "${KSPDIR}/GameData/MechJeb2/"
  cp -r ${PROJECT_DIR}/../Localization "${KSPDIR}/GameData/MechJeb2/"
  cp -r ${PROJECT_DIR}/../Parts "${KSPDIR}/GameData/MechJeb2/"
fi


exit 0
