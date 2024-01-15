#!/usr/bin/env bash

TAG=$1
NUGET_API_KEY=$2
VERSION=`echo ${TAG} | cut -b 2-`

build_output=/tmp/aelf-sdk

if [[ -d ${build_output} ]]; then
    rm -rf ${build_output}
fi

dotnet restore AElf.Client.sln
dotnet build AElf.Client/AElf.Client.csproj /clp:ErrorsOnly --configuration Release -P:Version=${VERSION} -P:Authors=AElf -o ${build_output}

echo `ls ${build_output}/AElf.Client.${VERSION}.nupkg`
dotnet nuget push ${build_output}/AElf.Client.${VERSION}.nupkg -k ${NUGET_API_KEY} -s https://api.nuget.org/v3/index.json

echo 'done'

