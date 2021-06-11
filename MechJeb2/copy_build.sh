#!/usr/bin/env bash
TARGET_PATH=$1
TARGET_DIR=$2
TARGET_NAME=$3
PROJECT_DIR=$4

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

if [ -z "${PDB2MDB}" ] ; then
  PDB2MDB=`which pdb2mdb`
fi

# Pretty sure Unity handles Portable PDB files now?
#if [ -z "${PDB2MDB}" ] ; then
#  echo '$PDB2MDB not found'
#else
#  echo "Running '${PDB2MDB}'"
#  "${PDB2MDB}" "${TARGET_PATH}"
#fi

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
  cp "${TARGET_PATH}" "${KSPDIR}/GameData/MechJeb2/Plugins/"
  test -f "${TARGET_DIR}/${TARGET_NAME}.pdb" && cp "${TARGET_DIR}/${TARGET_NAME}.pdb" "${KSPDIR}/GameData/MechJeb2/Plugins/"
  test -f "${TARGET_DIR}/${TARGET_NAME}.dll.mdb" && cp "${TARGET_DIR}/${TARGET_NAME}.dll.mdb" "${KSPDIR}/GameData/MechJeb2/Plugins/"
  cp -r ${PROJECT_DIR}/../Bundles "${KSPDIR}/GameData/MechJeb2/"
  cp -r ${PROJECT_DIR}/../Icons "${KSPDIR}/GameData/MechJeb2/"
  cp -r ${PROJECT_DIR}/../Localization "${KSPDIR}/GameData/MechJeb2/"
  cp -r ${PROJECT_DIR}/../Parts "${KSPDIR}/GameData/MechJeb2/"
fi

exit 0
