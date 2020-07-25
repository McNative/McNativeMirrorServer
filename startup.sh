#!/bin/bash


echo "Starting"

dotnet dev-certs https --trust

echo "Generated certificate"

dotnet McNativeMirrorServer.dll