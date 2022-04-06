#!/bin/bash

old_name=''
PROJECT_FILE='project.godot'

if [ -f "${PROJECT_FILE}" ]; then
  old_name=$(grep -oP '^config/name="\K[^"]+(?="$)' "${PROJECT_FILE}")
else
  echo "The project file does not exist"
  exit 1
fi

read -r -p "Rename project '${old_name}' to: " new_name
old_name_without_spaces=$(echo -n "${old_name}" | sed -e 's/[ :-]//g')
new_name_without_spaces=$(echo -n "${new_name}" | sed -e 's/[ :-]//g')

sed "s|<RootNamespace>${old_name_without_spaces}</RootNamespace>|<RootNamespace>${new_name_without_spaces}</RootNamespace>|" "${old_name}.csproj" > "${new_name}.csproj" || exit 1
sed "s|\"${old_name}|\"${new_name}|g" "${old_name}.sln" > "${new_name}.sln" || exit 1
rm "${old_name}.sln"
rm "${old_name}.csproj"

sed --in-place "s|config/name=\"${old_name}\"|config/name=\"${new_name}\"|" "${PROJECT_FILE}"
sed --in-place "s/# ${old_name}/# ${new_name}/" README.md
