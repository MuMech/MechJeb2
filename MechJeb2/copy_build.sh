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

if [ -z "${PDB2MDB}" ] ; then
  echo '$PDB2MDB not found'
else
  echo "Running '${PDB2MDB}'"
  "${PDB2MDB}" "${TARGET_PATH}"
fi

if [ -z "${KSPDIR}" ] ; then
  echo '$KSPDIR not found'
else
  if ! [ -d "${KSPDIR}" ] ; then
    echo 'Expected $KSPDIR to point to a directory but it is not' >&2
    exit 1
  fi
  if ! [ -d "${KSPDIR}/GameData" ] ; then
    echo 'Expected $KSPDIR to contain a GameData subdirectory but it does not' >&2
    exit 1
  fi
  echo "Copying to '${KSPDIR}'"
  cp "${TARGET_PATH}" "${KSPDIR}/GameData/"
  test -f "${TARGET_DIR}/${TARGET_NAME}.pdb" && cp "${TARGET_DIR}/${TARGET_NAME}.pdb" "${KSPDIR}/GameData/"
  test -f "${TARGET_DIR}/${TARGET_NAME}.dll.mdb" && cp "${TARGET_DIR}/${TARGET_NAME}.dll.mdb" "${KSPDIR}/GameData/"
fi
