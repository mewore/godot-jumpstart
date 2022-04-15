#!/bin/bash

PROJECT_FILE='project.godot'
project_name=''
lowercase_project_name=''

if [ -f "${PROJECT_FILE}" ]; then
  project_name=$(grep -oP '^config/name="\K[^"]+(?="$)' "${PROJECT_FILE}")
  lowercase_project_name=$(echo "${project_name}" | tr '[:upper:]' '[:lower:]' | sed 's/ /-/g')
else
  echo "The project file does not exist"
  exit 1
fi

echo "Project name: ${project_name}"

WEB_DIR='web'
PWD=$(pwd)

WINDOWS_BASE_DIR='windows64'
WINDOWS_SUB_DIR_NAME="${project_name}"
WINDOWS_EXECUTABLE="${project_name}.exe"
WINDOWS_DIR="${WINDOWS_BASE_DIR}/${WINDOWS_SUB_DIR_NAME}"

LINUX_BASE_DIR='linux64'
LINUX_SUB_DIR_NAME="${lowercase_project_name}"
LINUX_EXECUTABLE="${lowercase_project_name}.x86_64"
LINUX_DIR="${LINUX_BASE_DIR}/${LINUX_SUB_DIR_NAME}"

EXPORT_PRESETS_FILE='export_presets.cfg'
DEFAULT_EXPORT_PRESETS_FILE=".scripts/default_${EXPORT_PRESETS_FILE}"
TMP_EXPORT_PRESETS_FILE="${EXPORT_PRESETS_FILE}.tmp"

EXPORT_DIR='export'

current_version=''
if [ -f "${EXPORT_PRESETS_FILE}" ]; then
  current_version=$(grep -oP "^export_path=\"${EXPORT_DIR}/\K[^/\"]+" "${EXPORT_PRESETS_FILE}" | head -1)
else
  current_version='0.1.0'
  cp "${DEFAULT_EXPORT_PRESETS_FILE}" "${EXPORT_PRESETS_FILE}" || exit 1
fi

if [ -n "$1" ]; then
  version="$1"
elif [ -n "${current_version}" ]; then
  read -rp "Version [${current_version}]: " version
  if [ -z "${version}" ]; then version="${current_version}"; fi
else
  read -rp "Version: " version
fi

is_test_web=0
if [ "${version}" == 'web' ] || [ "${version}" == 'test-web' ]; then
  is_test_web=1
  version='test-web'
fi

if [ "${version}" != 'test-web' ] && ! echo "${version}" | grep -Pq '^[0-9]+\.[0-9]+\.[0-9]+$'; then
  echo "Version '${version}' does not match *.*.* and we are not testing web"
  exit 1
fi

if [ "${version}" != "${current_version}" ]; then
  echo "Setting the export version from [${current_version}] to [${version}]"
  sed --in-place "s|export_path=\"${EXPORT_DIR}/[^/]+/|export_path=\"${EXPORT_DIR}/${version}/|" "${EXPORT_PRESETS_FILE}"
fi

relative_version_dir="${EXPORT_DIR}/${version}"
version_dir="${PWD}/${relative_version_dir}"

echo "Configuring ${EXPORT_PRESETS_FILE}..."
rm -f "${TMP_EXPORT_PRESETS_FILE}" || exit 1

current_preset_platform='<NONE>'
# shellcheck disable=SC2002
IFS='' cat "${EXPORT_PRESETS_FILE}" | while read -r line; do
  new_preset_platform=$(echo -n "${line}" | grep -oP '^platform="\K[^"]+(?="$)')
  if [ -n "${new_preset_platform}" ]; then
    echo "    - Export preset platform: ${new_preset_platform}"
    current_preset_platform="${new_preset_platform}"
  elif echo -n "${line}" | grep -qP '^export_path="[^"]*"$'; then
    case "${current_preset_platform}" in
      'HTML5')
        line="export_path=\"${relative_version_dir}/${WEB_DIR}/index.html\""
        echo "        - Path: ${version_dir}/${WEB_DIR}/index.html"
        ;;
      'Windows Desktop')
        line="export_path=\"${relative_version_dir}/${WINDOWS_DIR}/${WINDOWS_EXECUTABLE}\""
        echo "        - Path: ${version_dir}/${WINDOWS_DIR}/${WINDOWS_EXECUTABLE}"
        ;;
      'Linux/X11')
        line="export_path=\"${relative_version_dir}/${LINUX_DIR}/${LINUX_EXECUTABLE}\""
        echo "        - Path: ${version_dir}/${LINUX_DIR}/${LINUX_EXECUTABLE}"
        ;;
      *)
        echo "Will not change the export path of this unrecognized preset platform: ${current_preset_platform}"
        ;;
    esac
  fi
  echo "${line}" >> "${TMP_EXPORT_PRESETS_FILE}"
done
if diff -q "${EXPORT_PRESETS_FILE}" "${TMP_EXPORT_PRESETS_FILE}"; then
  rm "${TMP_EXPORT_PRESETS_FILE}"
else
  mv "${TMP_EXPORT_PRESETS_FILE}" "${EXPORT_PRESETS_FILE}"
fi

if [ -e "${version_dir}" ]; then rm -rf "${version_dir}"; fi

echo "Exporting into: ${version_dir}"
mkdir -p "${version_dir}/${WEB_DIR}"
web_log="${version_dir}/export-web.log"
#godot --no-window --build-solutions

function make_zip {
  IN_DIR=$1
  TARGET_ZIP=$2
  cd "${IN_DIR}" || return 1
  zip -r "${TARGET_ZIP}" ./* > /dev/null && echo "Successfully created ${TARGET_ZIP}" || echo "Failed to create ${TARGET_ZIP}"
  cd - || return 1
}

function make_tarball {
  IN_DIR=$1
  TARGET_TARBALL=$2
  SOURCE_DIR_NAME=$3
  cd "${IN_DIR}" || return 1
  tar -zcvf "${TARGET_TARBALL}" "${SOURCE_DIR_NAME}" > /dev/null && echo "Successfully created ${TARGET_TARBALL}" \
    || echo "Failed to create ${TARGET_TARBALL}"
  cd - || return 1
}

if [ "${is_test_web}" == 0 ]; then
  mkdir -p "${version_dir}/${WEB_DIR}"
  web_log="${version_dir}/export-web.log"
  godot --no-window --export "HTML5" > "${web_log}" &
  web_pid=$!

  mkdir -p "${version_dir}/${WINDOWS_DIR}"
  windows_log="${version_dir}/export-windows.log"
  godot --no-window --export "Windows Desktop" > "${windows_log}" &
  windows_pid=$!

  mkdir -p "${version_dir}/${LINUX_DIR}"
  linux_log="${version_dir}/export-linux.log"
  godot --no-window --export "Linux/X11" > "${linux_log}" &
  linux_pid=$!

  if wait "${web_pid}"; then
    make_zip "${version_dir}/${WEB_DIR}" "${version_dir}/${lowercase_project_name}-web.zip" &
  else
    echo "Web export failed: file://${web_log}"
  fi

  if wait "${windows_pid}"; then
    make_zip "${version_dir}/${WINDOWS_BASE_DIR}" "${version_dir}/${project_name} - x64.zip" &
  else
    echo "Windows export failed: file://${windows_log}"
  fi

  if wait "${linux_pid}"; then
    make_tarball "${version_dir}/${LINUX_BASE_DIR}" "${version_dir}/${lowercase_project_name}-x64.tar.gz" \
      "${LINUX_SUB_DIR_NAME}" &
  else
    echo "Linux export failed: file://${linux_log}"
  fi

  wait
  echo "Done"
else
  godot --no-window --export-debug "HTML5" > "${web_log}"
  make_zip "${version_dir}/${WEB_DIR}" "${version_dir}/${lowercase_project_name}-web.zip" &
fi
