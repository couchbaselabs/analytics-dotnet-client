counter=1

echo "$counter - Cloning transactions-fit-performer (gRPC-only)"
((counter++))

git clone -n --depth=1 --filter=blob:none --sparse git@github.com:couchbaselabs/transactions-fit-performer.git
cd transactions-fit-performer || exit
git sparse-checkout set --no-cone /gRPC
git checkout

echo "$counter - Deleting jvm folder"
((counter++))

rm -rf gRPC/jvm

echo "$counter - Replacing existing gRPC folder with new"
((counter++))
mv -f gRPC ../

echo "$counter - Deleting transactions-fit-performer"
((counter++))
cd ..
rm -rf transactions-fit-performer